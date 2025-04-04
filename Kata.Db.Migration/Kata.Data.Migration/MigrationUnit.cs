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

    internal class MigrationUnit<TSource, TDest, TEntity> : IMigrationUnit
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

        public MigrationUnit(
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
            _logger = loggerFactory.CreateLogger(nameof(MigrationUnit<TSource, TDest, TEntity>));
        }

        public async Task MigrateAsync()
        {

            var sourceDbContext = _sourceFactory.Invoke(_configuration);
            sourceDbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            var destDbContext = _destFactory.Invoke(_configuration);
            destDbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            await InternalMigrateAsync(
                destDbContext,
                _destDbSetSelector.Compile()(destDbContext),
                _sourceDbSetSelector.Compile()(sourceDbContext));
        }

        private async Task InternalMigrateAsync(TDest target, DbSet<TEntity> targetDbSet, DbSet<TEntity> sourceDbSet)
        {
            _logger.LogInformation($"Migrating {typeof(TEntity).Name}...");
            var watcher = Stopwatch.StartNew();
            //var allEntities = await IncludeAll(sourceDbSet).ToListAsync();
            var allEntities = await sourceDbSet.ToListAsync();
            await AddOrUpdate(targetDbSet, allEntities);
            try
            {
                var nbItemSaved = await target.SaveChangesAsync();
                _logger.LogInformation($"Saved {nbItemSaved} items in {watcher.ElapsedMilliseconds} ms");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, $"Concurrency error while saving {typeof(TEntity).Name}");
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Database update error while saving {typeof(TEntity).Name}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error while saving {typeof(TEntity).Name}");
                throw;
            }
        }

        private async Task AddOrUpdate(DbSet<TEntity> targetDbSet, List<TEntity> allEntities)
        {
            foreach (var entity in allEntities)
            {
                var existingEntity = await targetDbSet.FindAsync(entity.Id);
                if (existingEntity == null)
                {
                    //_logger.LogInformation($"Adding new entity of type {typeof(T).Name} with ID {((dynamic)entity).Id}");
                    await targetDbSet.AddAsync(entity);
                }
                else
                {
                    targetDbSet.Entry(existingEntity).State = EntityState.Detached;
                    targetDbSet.Update(entity);
                }
            }
        }

        private bool HasChanges(TEntity existingEntity, TEntity newEntity, bool ignoreDates = true)
        {
            var properties = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);

            foreach (var property in properties)
            {
                var existingValue = property.GetValue(existingEntity);
                var newValue = property.GetValue(newEntity);

                if (existingValue is not DateTime existingDateTime || newValue is not DateTime newDateTime)
                {
                    if (!Equals(existingValue, newValue))
                    {
                        return true;
                    }
                }
                else
                {
                    return ignoreDates switch
                    {
                        true => false,
                        false => existingDateTime.ToUniversalTime() != newDateTime.ToUniversalTime()
                    };
                }
            }

            return false;
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
