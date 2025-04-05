namespace Kata.Data.Migration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using System.Linq;
    using System.Reflection;
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    internal class EntityMigrator<TSource, TDest, TEntity> : IEntityMigrator
        where TSource : DbContext
        where TDest : DbContext
        where TEntity : Entity
    {
        private readonly Func<IConfiguration, TSource> _sourceFactory;
        private readonly Func<IConfiguration, TDest> _destFactory;
        private readonly Expression<Func<TSource, DbSet<TEntity>>> _sourceDbSetSelector;
        private readonly Expression<Func<TDest, DbSet<TEntity>>> _destDbSetSelector;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public EntityMigrator(
            IConfiguration configuration,
            Func<IConfiguration, TSource> sourceFactory,
            Func<IConfiguration, TDest> destFactory,
            Expression<Func<TSource, DbSet<TEntity>>> sourceDbSetSelector,
            Expression<Func<TDest, DbSet<TEntity>>> destDbSetSelector,
            ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _sourceFactory = sourceFactory;
            _destFactory = destFactory;
            _sourceDbSetSelector = sourceDbSetSelector;
            _destDbSetSelector = destDbSetSelector;
            _logger = loggerFactory.CreateLogger(nameof(EntityMigrator<TSource, TDest, TEntity>));
        }

        public async Task MigrateAsync()
        {

            using var sourceDbContext = _sourceFactory.Invoke(_configuration);
            using var destDbContext = _destFactory.Invoke(_configuration);
            sourceDbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            destDbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            await InternalMigrateAsync(
                destDbContext,
                _destDbSetSelector.Compile()(destDbContext),
                _sourceDbSetSelector.Compile()(sourceDbContext));
        }

        private async Task InternalMigrateAsync(TDest target, DbSet<TEntity> targetDbSet, DbSet<TEntity> sourceDbSet)
        {
            if(await targetDbSet.AnyAsync())
            {
                _logger.LogInformation($"Skipping migration for {typeof(TEntity).Name} as it already exists in the target database.");
                return;
            }

            _logger.LogInformation($"Migrating {typeof(TEntity).Name}...");
            var watcher = Stopwatch.StartNew();
            var allEntities = await IncludeAll(target, sourceDbSet).ToListAsync();
            await targetDbSet.AddRangeAsync(allEntities);
            var nbItemSaved = await target.SaveChangesAsync();
            _logger.LogInformation($"Saved {nbItemSaved} items in {watcher.ElapsedMilliseconds} ms");
        }

        private IQueryable<T> IncludeAll<T>(TDest dbContext, DbSet<T> dbSet) where T : class
        {
            var query = dbSet.AsQueryable();
            var navigations = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => typeof(IEnumerable<object>).IsAssignableFrom(p.PropertyType) || !p.PropertyType.IsValueType && p.PropertyType != typeof(string));

            foreach (var navigation in navigations)
            {
                query = query.Include(navigation.Name);
            }

            var entities = query.ToList();
            foreach (var entity in entities)
            {
                foreach (var navigation in navigations)
                {
                    var navigationValue = navigation.GetValue(entity);
                    if (navigationValue != null)
                    {
                        if (navigationValue is IEnumerable<object> navigationCollection)
                        {
                            foreach (var item in navigationCollection)
                            {
                                dbContext.Entry(item).State = EntityState.Unchanged;
                            }
                        }
                        else
                        {
                            dbContext.Entry(navigationValue).State = EntityState.Unchanged;
                        }
                    }
                }
            }

            return query;
        }




    }
}
