using Canyon.GM.Server.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Canyon.GM.Server.Threads
{
    [DisallowConcurrentExecution]
    public sealed class MySQLDumpThread : IJob
    {
        private readonly BackupService backupService;
        private readonly ILogger<MySQLDumpThread> logger;

        public MySQLDumpThread(BackupService backupService, ILogger<MySQLDumpThread> logger)
        {
            this.backupService = backupService;
            this.logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogInformation("Executing automatic database backup!");
            await backupService.DoBackupAsync();
            logger.LogInformation("Executed with success");
        }
    }
}
