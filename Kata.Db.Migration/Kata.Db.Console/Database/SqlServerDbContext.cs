namespace Kata.Db.Console.Database
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using System;

    public class SqlServerDbContext : MigrationDbContext
    {
        public SqlServerDbContext(IConfiguration configuration)
            : base(configuration)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Configuration.GetConnectionString("SqlServer"), o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
            });
            base.OnConfiguring(optionsBuilder);
        }
    }
}
