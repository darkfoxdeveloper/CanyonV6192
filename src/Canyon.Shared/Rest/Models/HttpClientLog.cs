namespace Canyon.Shared.Rest.Models
{
    public sealed class HttpClientLog
    {
        public string Version { get; set; }
        public string Url { get; set; }
        public HttpClientRequestLog Request { get; set; }
        public HttpClientResponseLog Response { get; set; }
    }
}
