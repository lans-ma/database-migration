namespace Kata.Data.Migration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public abstract class DataMigrationServiceBase<TSource, TDest>
        where TSource : DbContext
        where TDest : DbContext
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly TSource _sourceDbContext;
        private readonly TDest _destDbContext;
        private readonly List<IEntityMigrator> _entityMigrators = new();

        protected DataMigrationServiceBase(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            TSource sourceDbContext,
            TDest destDbContext)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger(nameof(DataMigrationServiceBase<TSource, TDest>));
            _sourceDbContext = sourceDbContext;
            _destDbContext = destDbContext;
        }

        public async Task MigrateDataAsync()
        {
            ConfigureDataMigration();

            if (_entityMigrators.Count == 0)
            {
                _logger.LogWarning("No migration units configured.");
                return;
            }
            foreach (var unit in _entityMigrators)
            {
                await unit.MigrateAsync();
            }
            await _destDbContext.SaveChangesAsync();
        }

        protected void AddDataMigration<T>(Expression<Func<TSource, DbSet<T>>> sourceDbSetSelector, Expression<Func<TDest, DbSet<T>>> destDbSetSelector)
            where T : class
        {
            _entityMigrators.Add(
                new EntityMigrator<TSource, TDest, T>(
                _sourceDbContext,
                _destDbContext,
                sourceDbSetSelector,
                destDbSetSelector,
                _loggerFactory));
        }

        protected abstract void ConfigureDataMigration();
    }
}