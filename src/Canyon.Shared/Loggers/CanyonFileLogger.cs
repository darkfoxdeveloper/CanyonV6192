using Microsoft.Extensions.Logging;

namespace Canyon.Shared.Loggers
{
    public sealed class CanyonFileLogger<T> : ILogger<T>
    {
        private readonly ILogger<T> logger;
        private readonly LogProcessor logProcessor;

        public CanyonFileLogger(ILogger<T> logger, LogProcessor logProcessor)
        {
            this.logger = logger;
            this.logProcessor = logProcessor;
        }

        public string Folder { get; init; }
        public string FileName { get; init; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return default;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
#if DEBUG
            return true;
#else
            return logLevel > LogLevel.Debug;
#endif
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log(logLevel, eventId, state, exception, formatter);
            DateTime now = DateTime.Now;
            logProcessor.Queue(new LogQueueMessage
            {
                Path = Path.Combine(Environment.CurrentDirectory, Folder),
                FileName = $"{now:yyyyMMdd}-{FileName}.log",
                Message = $"[{now:HHmmss.fff}][{Environment.CurrentManagedThreadId:0000}][{logLevel,11}] - {formatter(state, exception)}"
            });
        }
    }
}
