namespace Kata.Db.Console
{
    using Kata.Db.Console.Database;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;

    public abstract class DataMigrationServiceBase<TSource, TDest>
        where TSource : MigrationDbContext<TSource>
        where TDest : MigrationDbContext<TDest>
    {
        private readonly ILogger _logger;
        private readonly Func<IConfiguration, TSource> _sourceFactory;
        private readonly Func<IConfiguration, TDest> _destFactory;
        private readonly IConfiguration _configuration;

        public DataMigrationServiceBase(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            Func<IConfiguration, TSource> sourceFactory,
            Func<IConfiguration, TDest> destFactory)
        {
            _logger = loggerFactory.CreateLogger(nameof(DataMigrationServiceBase<TSource,TDest>));
            _sourceFactory = sourceFactory;
            _destFactory = destFactory;
            _configuration = configuration;
        }

        public abstract Task MigrateDataAsync();

        protected async Task MigrateAsync<T>(Expression<Func<IMigrationDbContext, DbSet<T>>> dbSetSelector)
            where T: class
        {
            var sourceDbContext = _sourceFactory?.Invoke(_configuration);
            sourceDbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            var destDbContext = _destFactory?.Invoke(_configuration);
            destDbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            await InternalMigrateAsync(destDbContext,
                                  dbSetSelector.Compile()(destDbContext),
                                  dbSetSelector.Compile()(sourceDbContext));
        }

        private async Task InternalMigrateAsync<T>(TDest target, DbSet<T> targetDbSet, DbSet<T> sourceDbSet) where T : class
        {
            _logger.LogInformation($"Migrating {typeof(T).Name}...");
            var watcher = Stopwatch.StartNew();
            var allEntities = await IncludeAll(sourceDbSet).ToListAsync();
            await AddOrUpdate(targetDbSet, allEntities);
            var nbItemSaved = await target.SaveChangesAsync();
            _logger.LogInformation($"Saved {nbItemSaved} items in {watcher.ElapsedMilliseconds} ms");
        }

        private void UntrackAll<T>(DbSet<T> targetDbSet) where T : class
        {
            foreach (var entry in targetDbSet.ToList())
            {
                targetDbSet.Entry(entry).State = EntityState.Detached;
            }
        }

        private void UntrackAll(IMigrationDbContext target)
        {
            foreach (var entry in target.ChangeTracker.Entries())
            {
                entry.State = EntityState.Detached;
            }
        }

        private async Task AddOrUpdate<T>(DbSet<T> targetDbSet, List<T> allEntities) where T : class
        {
            foreach (var entity in allEntities)
            {
                var existingEntity = await targetDbSet.FindAsync(((dynamic)entity).Id);
                if (existingEntity == null)
                {
                    //_logger.LogInformation($"Adding new entity of type {typeof(T).Name} with ID {((dynamic)entity).Id}");
                    await targetDbSet.AddAsync(entity);
                }
                else
                {
                    // untrack existing entity
                    targetDbSet.Entry(existingEntity).State = EntityState.Detached;
                    targetDbSet.Update(entity);
                }
            }
        }

        private IQueryable<T> IncludeAll<T>(DbSet<T> dbSet) where T : class
        {
            var query = dbSet.AsQueryable();
            var navigations = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => typeof(IEnumerable<object>).IsAssignableFrom(p.PropertyType) || !p.PropertyType.IsValueType && p.PropertyType != typeof(string));

            foreach (var navigation in navigations)
            {
                query = query.Include(navigation.Name);
            }

            return query;
        }
    }
}