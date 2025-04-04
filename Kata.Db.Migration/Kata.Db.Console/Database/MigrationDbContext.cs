namespace Kata.Db.Console.Database
{
    using Kata.Db.Console.Model;
    using Microsoft.EntityFrameworkCore;
    using System.Threading.Tasks;

    public abstract class MigrationDbContext<T> : DbContext, IMigrationDbContext
        where T : MigrationDbContext<T>
    {
        public MigrationDbContext(DbContextOptions<T> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Book> Books { get; set; }

        public DbSet<Rental> Rentals { get; set; }

        public async Task<int> SaveChangesAsync()
        {
            return await base.SaveChangesAsync();
        }
    }
}
