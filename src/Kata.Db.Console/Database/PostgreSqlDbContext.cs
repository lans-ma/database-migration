namespace Kata.Db.Console.Database
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    public class PostgreSqlDbContext : MigrationDbContext
    {
        public PostgreSqlDbContext(IConfiguration configuration)
            : base(configuration)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(Configuration.GetConnectionString("PostgreSQL"));
            base.OnConfiguring(optionsBuilder);
        }
    }
}
