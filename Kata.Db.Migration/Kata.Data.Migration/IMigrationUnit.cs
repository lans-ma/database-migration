namespace Kata.Data.Migration
{
    using System.Threading.Tasks;

    internal interface IMigrationUnit
    {
        Task MigrateAsync();
    }
}
