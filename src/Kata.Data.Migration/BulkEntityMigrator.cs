namespace Kata.Data.Migration
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Npgsql;
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;

    internal class BulkEntityMigrator<TSource, TDest, TEntity> : IEntityMigrator
        where TSource : DbContext
        where TDest : DbContext
        where TEntity : class
    {
        private readonly TSource _sourceDbContext;
        private readonly TDest _destDbContext;
        private readonly Expression<Func<TSource, DbSet<TEntity>>> _sourceDbSetSelector;
        private readonly Expression<Func<TDest, DbSet<TEntity>>> _destDbSetSelector;
        private readonly ILogger _logger;

        public BulkEntityMigrator(
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
            _logger.LogInformation("Starting bulk migration for {EntityType}", typeof(TEntity).Name);

            // Get EF Core metadata for table and columns
            var entityType = _destDbContext.Model.FindEntityType(typeof(TEntity));
            var tableName = entityType.GetTableName();
            var properties = entityType.GetProperties().ToList();

            // With these lines:
            var sourceConn = _sourceDbContext.Database.GetDbConnection();
            var destConn = _destDbContext.Database.GetDbConnection();

            if (sourceConn.State != System.Data.ConnectionState.Open)
                await sourceConn.OpenAsync();
            if (destConn.State != System.Data.ConnectionState.Open)
                await destConn.OpenAsync();

            // Build SELECT for all columns in the correct order
            var columnNames = properties.Select(p => p.GetColumnName()).ToArray();
            var selectSql = $"SELECT {string.Join(", ", columnNames.Select(n => $"[{n}]"))} FROM {tableName}";

            using var cmd = sourceConn.CreateCommand();
            cmd.CommandText = selectSql;

            using var reader = await cmd.ExecuteReaderAsync();

            // Use Npgsql's binary COPY for fast bulk insert
            var npgsqlConn = (NpgsqlConnection)destConn;
            var copyCommand = $"COPY \"{tableName}\" ({string.Join(", ", columnNames.Select(n => $"\"{n}\""))}) FROM STDIN (FORMAT BINARY)";
            using var importer = npgsqlConn.BeginBinaryImport(copyCommand);

            int rowCount = 0;
            while (await reader.ReadAsync())
            {
                await importer.StartRowAsync();
                for (int i = 0; i < properties.Count; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    await importer.WriteAsync(value, properties[i].GetColumnType());
                }
                rowCount++;
            }
            await importer.CompleteAsync();

            _logger.LogInformation("Bulk migration completed for {EntityType}: {RowCount} rows copied.", typeof(TEntity).Name, rowCount);
        }
    }
}
