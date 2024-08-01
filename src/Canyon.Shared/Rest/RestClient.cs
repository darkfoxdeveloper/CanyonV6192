using IdentityModel.Client;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace Canyon.Shared.Rest
{
    public sealed class RestClient
    {
        private readonly HttpClient httpClient;
        private AuthenticationData authenticationData;

        private AuthenticationResponse authorization;

        private DateTime? authorizationExpiration;

        public RestClient()
        {
#if HTTP_CLIENT_DEBUG
            httpClient = new HttpClient(new HttpClientLogHandler());
#else
            httpClient = new HttpClient();
#endif
        }

        public async Task AuthorizeAsync(string url, string clientId, string clientSecret, string scope)
        {
            if (authenticationData == null)
            {
                authenticationData = new AuthenticationData
                {
                    Url = url,
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    Scope = scope
                };
            }

            DiscoveryDocumentResponse discovery = await httpClient.GetDiscoveryDocumentAsync(url);
            if (discovery.IsError)
            {
                throw new Exception(discovery.Error);
            }

            TokenResponse tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = discovery.TokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scope = scope
            });
            if (tokenResponse.IsError)
            {
                throw new Exception(tokenResponse.Error);
            }

            authorization = new AuthenticationResponse
            {
                AccessToken = tokenResponse.AccessToken,
                ExpiresIn = tokenResponse.ExpiresIn
            };
            authorizationExpiration = DateTime.Now.AddSeconds(authorization.ExpiresIn);
            httpClient.SetBearerToken(authorization.AccessToken);
        }

        private async Task CheckAuthorization()
        {
            if (authorizationExpiration.HasValue && authorizationExpiration.Value.AddSeconds(-1 * (authorization.ExpiresIn / 2)) < DateTime.Now)
            {
                await AuthorizeAsync(authenticationData.Url, authenticationData.ClientId, authenticationData.ClientSecret, authenticationData.Scope);
            }
        }

        public async Task<TResult> GetAsync<TResult>(string url, object body = null)
        {
            await CheckAuthorization();

            return await httpClient.GetFromJsonAsync<TResult>(url);
        }

        public async Task PostAsync(string url, object body = null)
        {
            await CheckAuthorization();

            using HttpResponseMessage httpResponseMessage = await httpClient.PostAsJsonAsync(url, body);
            string resultString = await httpResponseMessage.Content.ReadAsStringAsync();
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                throw new HttpRequestException(resultString);
            }
        }

        public async Task<TResult> PostAsync<TResult>(string url, object body = null)
        {
            await CheckAuthorization();

            using HttpResponseMessage httpResponseMessage = await httpClient.PostAsJsonAsync(url, body);
            string resultString = await httpResponseMessage.Content.ReadAsStringAsync();
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                throw new HttpRequestException(resultString);
            }
            return JsonConvert.DeserializeObject<TResult>(resultString);
        }

        public async Task<TResult> PutAsync<TResult>(string url, object body = null)
        {
            await CheckAuthorization();

            using HttpResponseMessage httpResponseMessage = await httpClient.PutAsJsonAsync(url, body);
            string resultString = await httpResponseMessage.Content.ReadAsStringAsync();
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                throw new HttpRequestException(resultString);
            }
            return JsonConvert.DeserializeObject<TResult>(resultString);
        }

        public async Task<TResult> PatchAsync<TResult>(string url, object body = null)
        {
            await CheckAuthorization();

            using HttpResponseMessage httpResponseMessage = await httpClient.PatchAsync(url, JsonContent.Create(body));
            string resultString = await httpResponseMessage.Content.ReadAsStringAsync();
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                throw new HttpRequestException(resultString);
            }
            return JsonConvert.DeserializeObject<TResult>(resultString);
        }

        private class AuthenticationResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("error")]
            public string Error { get; set; }
        }

        private class AuthenticationData
        {
            public string Url { get; set; }
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
            public string Scope { get; set; }
        }
    }
}
