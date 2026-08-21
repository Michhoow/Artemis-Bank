using ArtemisBank.Infrastructure.Persistence.Contexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Tests.Common
{
    public static class TestContextFactory
    {
        public static ArtemisDbContext CreateInMemory(string? name = null)
        {
            var options = new DbContextOptionsBuilder<ArtemisDbContext>()
                .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            var context = new ArtemisDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        public static (ArtemisDbContext Context, SqliteConnection Connection) CreateSqlite()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<ArtemisDbContext>()
                .UseSqlite(connection)
                .EnableSensitiveDataLogging()
                .Options;

            var context = new ArtemisDbContext(options);
            context.Database.EnsureCreated();
            return (context, connection);
        }
    }
}
