namespace Kata.Data.Migration
{
    using Microsoft.EntityFrameworkCore;
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
        where TEntity : class
    {
        private readonly TSource _sourceDbContext;
        private readonly TDest _destDbContext;
        private readonly Expression<Func<TSource, DbSet<TEntity>>> _sourceDbSetSelector;
        private readonly Expression<Func<TDest, DbSet<TEntity>>> _destDbSetSelector;
        private readonly ILogger _logger;

        public EntityMigrator(
            TSource sourceFactory,
            TDest destFactory,
            Expression<Func<TSource, DbSet<TEntity>>> sourceDbSetSelector,
            Expression<Func<TDest, DbSet<TEntity>>> destDbSetSelector,
            ILoggerFactory loggerFactory)
        {
            _sourceDbContext = sourceFactory;
            _destDbContext = destFactory;
            _sourceDbSetSelector = sourceDbSetSelector;
            _destDbSetSelector = destDbSetSelector;
            _logger = loggerFactory.CreateLogger(nameof(EntityMigrator<TSource, TDest, TEntity>));
        }

        public async Task MigrateAsync()
        {
            _sourceDbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            _destDbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            await InternalMigrateAsync(
                _sourceDbContext,
                _destDbContext,
                _destDbSetSelector.Compile()(_destDbContext),
                _sourceDbSetSelector.Compile()(_sourceDbContext));
        }

        private async Task InternalMigrateAsync(TSource srcDbContext, TDest destDbContext, DbSet<TEntity> targetDbSet, DbSet<TEntity> sourceDbSet)
        {
            _logger.LogInformation($"Migrating {typeof(TEntity).Name}...");
            var watcher = Stopwatch.StartNew();
            var allEntities = await IncludeAll(destDbContext, sourceDbSet).ToListAsync();
            await AddOrUpdateAsync(destDbContext, targetDbSet, allEntities);
        }

        private async Task AddOrUpdateAsync(TDest destDbContext, DbSet<TEntity> targetDbSet, List<TEntity> allEntities)
        {
            var primaryKeyProperties = destDbContext.Model.FindEntityType(typeof(TEntity)).FindPrimaryKey().Properties;
            foreach (var entity in allEntities)
            {
                object[] keyValues = primaryKeyProperties.Select(p => entity.GetType().GetProperty(p.Name).GetValue(entity)).ToArray();
                var existingEntity = await targetDbSet.FindAsync(keyValues);

                if (existingEntity != null)
                {
                    _destDbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
                }
                else
                {
                    await targetDbSet.AddAsync(entity);
                }
            }
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

            return query;
        }
    }
}
