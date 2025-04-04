namespace Kata.Db.Console
{
    using Kata.Db.Console.Database;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Migrations;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
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

            using var sqlServerDbContext = new SqlServerDbContext(configuration);
            using var postgreSqlDbContext = new PostgreSqlDbContext(configuration);

            Logger = loggerFactory.CreateLogger(nameof(Program));
            Logger.LogInformation("Starting migration...");

            await Migrate(sqlServerDbContext);
            await Migrate(postgreSqlDbContext);
            await MigrateDataFromSqlServerToPostgreSql(loggerFactory, sqlServerDbContext, postgreSqlDbContext);

            Console.ReadKey();
        }

        private static async Task Migrate<T>(MigrationDbContext<T> dbContext)
            where T : MigrationDbContext<T>
        {
            Logger.LogInformation($"Starting migration for {typeof(T).Name}...");
            var migrator = dbContext.GetService<IMigrator>();
            if (migrator.HasPendingModelChanges())
            {
                Logger.LogInformation("Pending model changes detected. Applying migrations...");
                await dbContext.Database.MigrateAsync();
                await dbContext.Database.EnsureCreatedAsync();
            }
            else
            {
                Logger.LogInformation("No pending model changes detected.");
            }
            Logger.LogInformation($"Migration for {typeof(T).Name} completed.");
        }

        private static async Task MigrateDataFromSqlServerToPostgreSql<TSource, TDest>(ILoggerFactory loggerFactory, MigrationDbContext<TSource> source, MigrationDbContext<TDest> dest)
            where TSource : MigrationDbContext<TSource>
            where TDest : MigrationDbContext<TDest>
        {
            Logger.LogInformation("Starting data migration from SQL Server to PostgreSQL...");

            var dataMigrationService = new DataMigrationService(loggerFactory);
            await dataMigrationService.MigrateDataAsync(source, dest);

            Logger.LogInformation("Data migration from SQL Server to PostgreSQL completed.");
        }
    }
}
