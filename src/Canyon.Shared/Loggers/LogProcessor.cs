using Microsoft.Extensions.Hosting;
using System.Text;
using System.Threading.Channels;

namespace Canyon.Shared.Loggers
{
    public sealed class LogProcessor : BackgroundService
    {
        private Task backgroundTask;
        private readonly CancellationToken cancellationToken;
        private readonly Channel<LogQueueMessage> queue;

        public LogProcessor(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            queue = Channel.CreateUnbounded<LogQueueMessage>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            backgroundTask = DequeueAsync();
            return Task.WhenAll(backgroundTask);
        }

        public void Queue(LogQueueMessage log)
        {
            cancellationToken.ThrowIfCancellationRequested();
            queue.Writer.TryWrite(log);
        }

        private async Task DequeueAsync()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    LogQueueMessage message = await queue.Reader.ReadAsync(cancellationToken);
                    if (message.Equals(default))
                    {
                        continue;
                    }

                    if (!Directory.Exists(message.Path))
                    {
                        Directory.CreateDirectory(message.Path);
                    }

                    string fullPath = Path.Combine(message.Path, message.FileName).Replace("*", "");
                    using StreamWriter writer = new(fullPath, true, Encoding.UTF8);
                    await writer.WriteLineAsync(message.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to log file: {ex.Message}");
                }
            }
        }
    }
}
