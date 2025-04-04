namespace Kata.Db.Console.Database
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    public class PostgreSqlDbContext : MigrationDbContext<PostgreSqlDbContext>
    {
        public PostgreSqlDbContext(IConfiguration configuration)
            : base(new DbContextOptionsBuilder<PostgreSqlDbContext>().UseNpgsql(configuration.GetConnectionString("PostgreSQL")).Options)
        {
        }
    }
}
