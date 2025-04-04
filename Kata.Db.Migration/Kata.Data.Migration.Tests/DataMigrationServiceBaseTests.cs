namespace Kata.Data.Migration.Tests
{
    using Kata.Data.Migration;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    [TestFixture]
    public class DataMigrationServiceBaseTests
    {
        private Mock<ILoggerFactory> _loggerFactoryMock;
        private Mock<IConfiguration> _configurationMock;

        [SetUp]
        public void SetUp()
        {
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            var loggerMock = Mock.Of<ILogger>();
            _loggerFactoryMock
                .Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(loggerMock);
            _configurationMock = new Mock<IConfiguration>();
        }

        private TestDataMigrationService CreateMigrationService()
        {
            // Arrange
            return new TestDataMigrationService(
                _loggerFactoryMock.Object,
                _configurationMock.Object,
                _ => new TestSourceDbContext(),
                _ => new TestDestDbContext());
        }

        [Test]
        public async Task MigrateDataAsync_MigrationUnitsConfigured_CallsMigrateAsync()
        {
            // Arrange
            var service = CreateMigrationService();

            service.AddDataMigration(c => c.TestEntities, c => c.TestEntities);

            // Act
            await service.MigrateDataAsync();

            // Assert
            Assert.That(service.MigrationUnits.Count, Is.EqualTo(1));
        }

        [Test]
        public void AddDataMigration_AddsMigrationUnit()
        {
            // Arrange
            var service = CreateMigrationService();

            // Act
            service.AddDataMigration(c => c.TestEntities, c => c.TestEntities);

            // Assert
            Assert.That(service.MigrationUnits.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GivenNoItemsInDestination_WhenDataMigration_ThenItemsShouldBeInDestination()
        {
            // Arrange
            var sourceContext = new TestSourceDbContext();
            sourceContext.TestEntities.Add(new TestEntity { Id = 1, Name = "Test1" });
            sourceContext.TestEntities.Add(new TestEntity { Id = 2, Name = "Test2" });
            var service = CreateMigrationService();
            service.AddDataMigration(c => c.TestEntities, c => c.TestEntities);

            // Act
            await service.MigrateDataAsync();

            // Assert
            var destContext = new TestDestDbContext();
            var items = await destContext.TestEntities.ToListAsync();
            Assert.That(items.Count, Is.EqualTo(2));
            Assert.That(items[0].Id, Is.EqualTo(1));
            Assert.That(items[0].Name, Is.EqualTo("Test1"));
            Assert.That(items[1].Id, Is.EqualTo(2));
            Assert.That(items[1].Name, Is.EqualTo("Test2"));
        }

        [Test]
        public async Task GivenItemsInDestination_WhenDataMigration_ThenItemsShouldBeUpdatedInDestination()
        {
            // Arrange
            var sourceContext = new TestSourceDbContext();
            sourceContext.TestEntities.Add(new TestEntity { Id = 1, Name = "Test1" });
            sourceContext.TestEntities.Add(new TestEntity { Id = 2, Name = "Test2" });
            sourceContext.SaveChanges();
            var destContext = new TestDestDbContext();
            destContext.TestEntities.Add(new TestEntity { Id = 1, Name = "Test12312" });
            await destContext.SaveChangesAsync();
            var service = CreateMigrationService();
            service.AddDataMigration(c => c.TestEntities, c => c.TestEntities);
            var items = await new TestDestDbContext().TestEntities.ToListAsync();
            Assert.That(items.Count, Is.EqualTo(1));

            // Act
            await service.MigrateDataAsync();

            // Assert
            items = await new TestDestDbContext().TestEntities.ToListAsync();
            Assert.That(items.Count, Is.EqualTo(2));
            Assert.That(items[0].Id, Is.EqualTo(1));
            Assert.That(items[0].Name, Is.EqualTo("Test1"));
            Assert.That(items[1].Id, Is.EqualTo(2));
            Assert.That(items[1].Name, Is.EqualTo("Test2"));
        }

        private class TestDataMigrationService : DataMigrationServiceBase<TestSourceDbContext, TestDestDbContext>
        {
            public TestDataMigrationService(
                ILoggerFactory loggerFactory,
                IConfiguration configuration,
                Func<IConfiguration, TestSourceDbContext> sourceFactory,
                Func<IConfiguration, TestDestDbContext> destFactory)
                : base(loggerFactory, configuration, sourceFactory, destFactory)
            {
            }

            public List<IMigrationUnit> MigrationUnits => (List<IMigrationUnit>)typeof(DataMigrationServiceBase<TestSourceDbContext, TestDestDbContext>)
    .GetField("_migrationUnits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
    .GetValue(this);

            protected override void ConfigureDataMigration()
            {
                // No-op for testing
            }

            internal void AddDataMigration(Expression<Func<TestSourceDbContext, DbSet<TestEntity>>> sourceDbSetSelector, Expression<Func<TestDestDbContext, DbSet<TestEntity>>> destDbSetSelector)
            {
                base.AddDataMigration(sourceDbSetSelector, destDbSetSelector);
            }
        }

        private class TestSourceDbContext : DbContext
        {
            public DbSet<TestEntity> TestEntities { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseInMemoryDatabase("TestSourceDb");
            }
        }

        private class TestDestDbContext : DbContext
        {
            public DbSet<TestEntity> TestEntities { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseInMemoryDatabase("TestDestDb");
            }
        }

        private class TestEntity : Entity
        {
            public string Name { get; set; }
        }
    }
}






