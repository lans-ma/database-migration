namespace Kata.Db.Console.Database
{
    using Kata.Db.Console.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using System.Threading.Tasks;

    public interface IMigrationDbContext
    {

        DbSet<User> Users { get; }

        DbSet<Book> Books { get; }

        DbSet<Rental> Rentals { get; }
        
        ChangeTracker ChangeTracker { get; }

        Task<int> SaveChangesAsync();
    }
}