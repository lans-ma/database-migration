namespace Kata.Db.Console.Database
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    public class SqlServerDbContext : MigrationDbContext<SqlServerDbContext>
    {
        public SqlServerDbContext(IConfiguration configuration)
            : base(new DbContextOptionsBuilder<SqlServerDbContext>()
                  .UseSqlServer(configuration.GetConnectionString("SqlServer"))
                  .Options)
        {
        }
    }
}
