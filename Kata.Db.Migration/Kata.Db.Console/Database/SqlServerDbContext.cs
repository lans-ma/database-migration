namespace Kata.Db.Console.Database
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    public class SqlServerDbContext : MigrationDbContext<SqlServerDbContext>
    {
        public SqlServerDbContext(IConfiguration configuration)
            : base(configuration)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Configuration.GetConnectionString("SqlServer"));
            base.OnConfiguring(optionsBuilder);
        }
    }
}
