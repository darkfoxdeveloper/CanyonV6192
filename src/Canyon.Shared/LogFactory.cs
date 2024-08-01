using Canyon.Shared.Loggers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Canyon.Shared
{
    public class LogFactory
    {
        private const string LogDefaultPath = "Logs";
        private const string LogDefaultName = "Log";

        private static bool DIImplementation = false;

        private static ILoggerFactory loggerFactory;
        private static LogProcessor processor;

        private static string DefaultFileName = LogDefaultName;

        private static readonly ConcurrentDictionary<string, ILogger> loggers = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, ILogger> gmLoggers = new(StringComparer.OrdinalIgnoreCase);

        public static ILoggerFactory GetLoggerFactory => loggerFactory;

        public static void Initialize(ILoggerFactory loggerFactory)
        {
            LogFactory.loggerFactory = loggerFactory;
            DIImplementation = true;
        }

        public static void Initialize(LogProcessor processor, string defaultFileName)
        {
            if (loggerFactory != null)
            {
                return;
            }

            if (processor == null)
            {
                throw new NullReferenceException("The log factory processor has not been assigned.");
            }

            LogFactory.processor = processor;
            if (!string.IsNullOrEmpty(defaultFileName))
            {
                DefaultFileName = defaultFileName;
            }

            loggerFactory = LoggerFactory.Create(builder => builder
                .AddSimpleConsole(c =>
                {
                    c.TimestampFormat = "[yyyyMMdd HHmmss.fff] ";
                    c.SingleLine = true;
                })
#if DEBUG
                .AddDebug()
                .SetMinimumLevel(LogLevel.Debug)
#else
                .SetMinimumLevel(LogLevel.Information)
#endif
                );

            var logger = loggerFactory.CreateLogger<LogFactory>();
            logger.LogInformation("Canyon logger factory has been initialized!");
        }

        public static ILogger CreateLogger<T>()
        {
            if (DIImplementation)
            {
                return loggerFactory.CreateLogger<T>();
            }

            if (loggerFactory == null)
            {
                throw new NullReferenceException("Logger factory has not been initialized! Make sure to call LogFactory.Initialize() before start creating loggers.");
            }

            return loggers.GetOrAdd($"{LogDefaultPath}/{DefaultFileName}/{typeof(T).FullName}", new CanyonFileLogger<T>(loggerFactory.CreateLogger<T>(), processor)
            {
                Folder = LogDefaultPath,
                FileName = DefaultFileName
            });
        }

        public static ILogger CreateLogger<T>(string fileName)
        {
            if (DIImplementation)
            {
                return loggerFactory.CreateLogger<T>();
            }

            if (loggerFactory == null)
            {
                throw new NullReferenceException("Logger factory has not been initialized! Make sure to call LogFactory.Initialize() before start creating loggers.");
            }

            return loggers.GetOrAdd($"{LogDefaultPath}/{fileName}/{typeof(T).FullName}", new CanyonFileLogger<T>(loggerFactory.CreateLogger<T>(), processor)
            {
                Folder = LogDefaultPath,
                FileName = fileName
            });
        }

        public static ILogger CreateLogger<T>(string folder, string fileName)
        {
            if (DIImplementation)
            {
                return loggerFactory.CreateLogger<T>();
            }

            if (loggerFactory == null)
            {
                throw new NullReferenceException("Logger factory has not been initialized! Make sure to call LogFactory.Initialize() before start creating loggers.");
            }

            return loggers.GetOrAdd($"{folder}/{fileName}/{typeof(T).FullName}", new CanyonFileLogger<T>(loggerFactory.CreateLogger<T>(), processor)
            {
                Folder = folder,
                FileName = fileName
            });
        }

        public static ILogger CreateGmLogger(string fileName)
        {
            if (DIImplementation)
            {
                return loggerFactory.CreateLogger(fileName);
            }

            if (loggerFactory == null)
            {
                throw new NullReferenceException("Logger factory has not been initialized! Make sure to call LogFactory.Initialize() before start creating loggers.");
            }

            return gmLoggers.GetOrAdd(fileName, new CanyonGmFileLogger(processor)
            {
                FileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()))
            });
        }
    }
}
