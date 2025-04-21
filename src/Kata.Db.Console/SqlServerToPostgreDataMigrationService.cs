namespace Kata.Db.Console
{
    using Kata.Data.Migration;
    using Kata.Db.Console.Database;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;

    internal class SqlServerToPostgreDataMigrationService : DataMigrationServiceBase<SqlServerDbContext, PostgreSqlDbContext>
    {
        public SqlServerToPostgreDataMigrationService(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            SqlServerDbContext sourceDbContext,
            PostgreSqlDbContext destinationDbContext)
            : base(loggerFactory, configuration, sourceDbContext, destinationDbContext)
        {
        }

        protected override void ConfigureDataMigration()
        {
            this.AddDataMigration(s => s.Rentals, d => d.Rentals);
            this.AddDataMigration(s => s.Books, d => d.Books);
            this.AddDataMigration(s => s.Users, d => d.Users);
        }
    }
}