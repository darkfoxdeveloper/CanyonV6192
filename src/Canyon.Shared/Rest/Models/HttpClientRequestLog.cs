namespace Canyon.Shared.Rest.Models
{
    public sealed class HttpClientRequestLog
    {
        public string Method { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Body { get; set; }
    }
}
