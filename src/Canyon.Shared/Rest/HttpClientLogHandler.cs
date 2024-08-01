using Canyon.Shared.Rest.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Canyon.Shared.Rest
{
    public sealed class HttpClientLogHandler : DelegatingHandler
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<HttpClientLogHandler>();

        public HttpClientLogHandler()
        {
            InnerHandler = new HttpClientHandler();
        }

        private void Log(LogLevel level, string message, params string[] args)
        {
            logger.Log(level, message, args);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpClientLog log = new();
            log.Url = request.RequestUri?.ToString();
            log.Version = request.Version.ToString();
            log.Request = new HttpClientRequestLog();
            log.Request.Method = request.Method.Method;
            if (request.Content != null)
            {
                log.Request.Body = await request.Content.ReadAsStringAsync();
            }
            log.Request.Headers = new Dictionary<string, string>();
            foreach (var header in request.Headers)
            {
                int headerCount = -1;
                if (header.Value.Count() > 1)
                {
                    headerCount++;
                }
                foreach (var headerValue in header.Value)
                {
                    if (headerCount != -1)
                    {
                        log.Request.Headers.Add($"{header.Key}.{headerCount++}", headerValue.ToString());
                    }
                    else
                    {
                        log.Request.Headers.Add($"{header.Key}", headerValue.ToString());
                    }
                }
            }

            HttpResponseMessage httpResponseMessage = await base.SendAsync(request, cancellationToken);

            log.Response = new HttpClientResponseLog();
            log.Response.HttpStatus = (int)httpResponseMessage.StatusCode;
            log.Response.Body = await httpResponseMessage.Content.ReadAsStringAsync();
            log.Response.Headers = new Dictionary<string, string>();
            foreach (var header in httpResponseMessage.Headers)
            {
                int headerCount = -1;
                if (header.Value.Count() > 1)
                {
                    headerCount++;
                }
                foreach (var headerValue in header.Value)
                {
                    if (headerCount != -1)
                    {
                        log.Request.Headers.Add($"{header.Key}.{headerCount++}", headerValue.ToString());
                    }
                    else
                    {
                        log.Request.Headers.Add($"{header.Key}", headerValue.ToString());
                    }
                }
            }

            Log(httpResponseMessage.IsSuccessStatusCode ? LogLevel.Information : LogLevel.Error, JsonConvert.SerializeObject(log, Formatting.Indented));
            return httpResponseMessage;

        }
    }
}
