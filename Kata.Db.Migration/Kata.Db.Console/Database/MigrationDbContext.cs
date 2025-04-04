namespace Kata.Db.Console.Database
{
    using Kata.Db.Console.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    public abstract class MigrationDbContext : DbContext
    {
        protected MigrationDbContext(IConfiguration configuration) : base()
        {
            Configuration = configuration;
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Book> Books { get; set; }

        public DbSet<Rental> Rentals { get; set; }
        protected IConfiguration Configuration { get; }
    }
}
