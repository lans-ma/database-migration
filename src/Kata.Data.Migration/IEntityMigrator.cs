namespace Kata.Data.Migration
{
    using System.Threading.Tasks;

    internal interface IEntityMigrator
    {
        Task MigrateAsync();
    }
}
