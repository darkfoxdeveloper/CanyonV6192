namespace Canyon.Shared.Loggers
{
    public struct LogQueueMessage
    {
        public string Path { get; set; }
        public string FileName { get; set; }
        public string Message { get; set; }
    }
}