namespace Kata.Data.Migration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public abstract class DataMigrationServiceBase<TSource, TDest>
        where TSource : DbContext
        where TDest : DbContext
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly Func<IConfiguration, TSource> _sourceFactory;
        private readonly Func<IConfiguration, TDest> _destFactory;
        private readonly IConfiguration _configuration;
        private readonly List<IMigrationUnit> _migrationUnits = new();

        protected DataMigrationServiceBase(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            Func<IConfiguration, TSource> sourceFactory,
            Func<IConfiguration, TDest> destFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger(nameof(DataMigrationServiceBase<TSource, TDest>));
            _sourceFactory = sourceFactory;
            _destFactory = destFactory;
            _configuration = configuration;
        }

        public async Task MigrateDataAsync()
        {
            ConfigureDataMigration();

            if (_migrationUnits.Count == 0)
            {
                _logger.LogWarning("No migration units configured.");
                return;
            }
            foreach (var unit in _migrationUnits)
            {
                await unit.MigrateAsync();
            }
        }

        protected void AddDataMigration<T>(Expression<Func<TSource, DbSet<T>>> sourceDbSetSelector, Expression<Func<TDest, DbSet<T>>> destDbSetSelector)
            where T : Entity
        {
            _migrationUnits.Add(
                new MigrationUnit<TSource, TDest, T>(
                _configuration,
                _sourceFactory,
                _destFactory,
                sourceDbSetSelector,
                destDbSetSelector,
                _loggerFactory));
        }

        protected abstract void ConfigureDataMigration();
    }
}