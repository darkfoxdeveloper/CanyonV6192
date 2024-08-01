using FluentFTP.Logging;
using FluentFTP;
using Microsoft.Extensions.Logging;
using SharpCompress.Archives.GZip;
using System.Diagnostics;

namespace Canyon.GM.Server.Services
{
    public sealed class BackupService
    {
        private const string LOCK_CMD = "--host={0} --port={1} --default-character-set=utf8mb4 --user={2} -p\"{3}\" --protocol=tcp --order-by-primary=TRUE --single-transaction=TRUE \"{4}\" -r \"{5}\"";
        private const string NO_LOCK_CMD = "--host={0} --port={1} --default-character-set=utf8mb4 --user={2} -p\"{3}\" --protocol=tcp --order-by-primary=TRUE --single-transaction=TRUE --skip-lock-tables --lock-tables=FALSE --skip-triggers \"{4}\" -r \"{5}\"";
        private readonly ILogger<BackupService> logger;

        public BackupService(ILogger<BackupService> logger)
        {
            this.logger = logger;
        }

        public Task DoBackupAsync(bool noLock = true)
        {
            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "Output")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Output"));
            }

            string mysqlDump = Program.ServerConfiguration.MySQLDumpPath;
            logger.LogInformation($"Checking if file exists: {mysqlDump}");
            if (!File.Exists(mysqlDump))
            {
                throw new FileNotFoundException("Could not find file.", mysqlDump);
            }

            // --host=localhost --port=3306 --default-character-set=utf8 --user=root --protocol=tcp --order-by-primary=TRUE --single-transaction=TRUE --column-statistics=0 --skip-triggers "cq"
            string outputFile = Path.Combine(Environment.CurrentDirectory, "Output", $"dump-{Program.ServerConfiguration.RealmName}-{DateTime.Now:yyyyMMddHHmmss}.sql");
            var databaseSettings = Program.ServerConfiguration.Database;
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = mysqlDump,
                    Arguments = string.Format(noLock ? NO_LOCK_CMD : LOCK_CMD, databaseSettings.Hostname, databaseSettings.Port, databaseSettings.Username, databaseSettings.Password, databaseSettings.Schema, outputFile),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logger.LogError("{}", e.Data);
                }
            };
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logger.LogInformation("{}", e.Data);
                }
            };

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();
            process.Close();

            logger.LogInformation("Finished dump process!");
            logger.LogWarning("Starting GZIPING process!! Do not close application!!!!");

            // I know MySQL offers this option, but I wanted to test this lib
            using var archive = GZipArchive.Create();
            using MemoryStream memoryStream = new MemoryStream(File.ReadAllBytes(outputFile));
            archive.AddEntry(Path.GetFileName(outputFile), memoryStream);
            string gzippedFile = Path.Combine(Environment.CurrentDirectory, "Output", $"{Path.GetFileNameWithoutExtension(outputFile)}.gz");
            archive.SaveTo(gzippedFile);

            File.Delete(outputFile);

            logger.LogInformation("GZIPING complete!");
            logger.LogInformation($"Backup saved to \"{gzippedFile}\"");

            var ftpSettings = Program.ServerConfiguration.Ftp ?? new ServerConfiguration.FtpConfiguration();
            if (!string.IsNullOrEmpty(ftpSettings.Hostname)
                && !string.IsNullOrEmpty(ftpSettings.Username)
                && !string.IsNullOrEmpty(ftpSettings.Password))
            {
                logger.LogInformation("Starting FTP upload");

                using var ftp = new FtpClient(ftpSettings.Hostname, ftpSettings.Username, ftpSettings.Password, 0, null, new FtpLogAdapter(logger));
                ftp.Connect();
                ftp.UploadFile(gzippedFile, Path.GetFileName(gzippedFile), FtpRemoteExists.Overwrite, true, FtpVerify.Retry);

                const int backupDays = 14;
                const int backupsPerDay = 4;
                const int filesToKeep = backupDays * backupsPerDay;

                logger.LogInformation("Accessing FTP to delete files older than {} days", backupDays);

                var fileList = ftp.GetListing()
                    .Where(x => x.Name.Contains(Program.ServerConfiguration.RealmName, StringComparison.InvariantCultureIgnoreCase))
                    .OrderByDescending(x => x.RawModified)
                    .Skip(filesToKeep);
                foreach (var deleteFile in fileList)
                {
                    logger.LogWarning($"Deleting backup \"{deleteFile.FullName}\" from {deleteFile.RawModified}");
                    ftp.DeleteFile(deleteFile.FullName);
                }

                logger.LogInformation("Cleaning complete!");

                ftp.Disconnect();
            }
            else
            {
                logger.LogInformation("No FTP settings set! Skipping FTP upload");
            }

            logger.LogInformation("Process finished!");
            return Task.CompletedTask;
        }
    }
}
