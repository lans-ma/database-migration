namespace Kata.Db.Console
{
    using Kata.Db.Console.Database;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public class DataMigrationService
    {
        private readonly ILogger _logger;

        public DataMigrationService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(nameof(DataMigrationService));
        }

        public async Task MigrateDataAsync(IMigrationDbContext source, IMigrationDbContext target)
        {
            //source.ChangeTracker.Clear();
            await MigrateAsync(target, target.Books, source.Books);
            await MigrateAsync(target, target.Users, source.Users);
            await MigrateAsync(target, target.Rentals, source.Rentals);

        }

        private async Task MigrateAsync<T>(IMigrationDbContext target, DbSet<T> targetDbSet, DbSet<T> sourceDbSet) where T : class
        {
            _logger.LogInformation($"Migrating {typeof(T).Name}...");

            var watcher = Stopwatch.StartNew();
            var allEntities = await sourceDbSet.AsNoTracking().ToListAsync();
            watcher.Stop();
            _logger.LogInformation($"Retrieved {allEntities.Count} items in {watcher.ElapsedMilliseconds} ms");
            _logger.LogInformation($"Migrating {allEntities.Count} items for the entity {sourceDbSet.GetType().GetGenericArguments()[0].Name}");

            watcher.Restart();
            await AddOrUpdate(targetDbSet, allEntities);
            var nbItemSaved = await target.SaveChangesAsync();
            watcher.Stop();
            _logger.LogInformation($"Saved {nbItemSaved} items in {watcher.ElapsedMilliseconds} ms");
            _logger.LogInformation($"{nbItemSaved} items migrated for the entity {sourceDbSet.GetType().GetGenericArguments()[0].Name}");
        }

        private async Task AddOrUpdate<T>(DbSet<T> targetDbSet, List<T> allEntities) where T : class
        {
            foreach (var entity in allEntities)
            {
                var existingEntity = await targetDbSet.FindAsync(((dynamic)entity).Id);
                if (existingEntity == null)
                {
                    _logger.LogInformation($"Adding new entity of type {typeof(T).Name} with ID {((dynamic)entity).Id}");
                    await targetDbSet.AddAsync(entity);
                }
                else
                {
                    // untrack existing entity
                    targetDbSet.Entry(existingEntity).State = EntityState.Detached;
                    _logger.LogInformation($"Updating existing entity of type {typeof(T).Name} with ID {((dynamic)entity).Id}");
                    targetDbSet.Update(entity);
                }
            }
        }
    }
}