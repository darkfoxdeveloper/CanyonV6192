namespace Canyon.Shared.Rest.Models
{
    public sealed class HttpClientResponseLog
    {

        public int HttpStatus { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Body { get; set; }

    }
}
