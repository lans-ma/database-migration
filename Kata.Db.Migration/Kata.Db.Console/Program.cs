namespace Kata.Db.Console
{
    using Kata.Db.Console.Database;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    internal class Program
    {
        private static ILogger Logger;

        static async Task Main(string[] args)
        {

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var loggerFactory = new ConsoleLoggerFactory();


            Logger = loggerFactory.CreateLogger(nameof(Program));
            Logger.LogInformation("Starting migration...");
            await MigrateDataFromSqlServerToPostgreSql(loggerFactory, configuration);

            Console.ReadKey();
        }

        private static async Task MigrateDataFromSqlServerToPostgreSql(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            Logger.LogInformation("Starting data migration from SQL Server to PostgreSQL...");
            var stopWatcher = Stopwatch.StartNew();
            var dataMigrationService = new SqlServerToPostgreDataMigrationService(
                loggerFactory,
                configuration,
                c => new SqlServerDbContext(c),
                c => new PostgreSqlDbContext(c)
                );
            await dataMigrationService.MigrateDataAsync();

            Logger.LogInformation($"Data migration from SQL Server to PostgreSQL completed. TotalElapsed:{stopWatcher.Elapsed}");
        }
    }
}
