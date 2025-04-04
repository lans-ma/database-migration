namespace Kata.Db.Console
{
    using Kata.Db.Console.Database;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading.Tasks;

    internal class SqlServerToPostgreDataMigrationService : DataMigrationServiceBase<SqlServerDbContext, PostgreSqlDbContext>
    {
        public SqlServerToPostgreDataMigrationService(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            Func<IConfiguration, SqlServerDbContext> sourceFactory,
            Func<IConfiguration, PostgreSqlDbContext> destFactory)
            : base(loggerFactory, configuration, sourceFactory, destFactory)
        {
            
        }

        public async override Task MigrateDataAsync()
        {
            await MigrateAsync(c => c.Rentals);
            await MigrateAsync(c => c.Books);
            await MigrateAsync(c => c.Users);
        }
    }
}