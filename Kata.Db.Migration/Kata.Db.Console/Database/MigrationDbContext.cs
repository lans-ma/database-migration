namespace Kata.Db.Console.Database
{
    using Kata.Db.Console.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using System.Threading.Tasks;

    public abstract class MigrationDbContext<T> : DbContext, IMigrationDbContext
        where T : MigrationDbContext<T>
    {
        protected MigrationDbContext(IConfiguration configuration) : base()
        {
            Configuration = configuration;
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Book> Books { get; set; }

        public DbSet<Rental> Rentals { get; set; }

        public async Task<int> SaveChangesAsync()
        {
            return await base.SaveChangesAsync();
        }
        protected IConfiguration Configuration { get; }
    }
}
