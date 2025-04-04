namespace Kata.Db.Console.Database
{
    using Kata.Db.Console.Model;
    using Microsoft.EntityFrameworkCore;
    using System.Threading.Tasks;

    public interface IMigrationDbContext
    {

        DbSet<User> Users { get; }

        DbSet<Book> Books { get; }

        DbSet<Rental> Rentals { get; }

        Task<int> SaveChangesAsync();
    }
}