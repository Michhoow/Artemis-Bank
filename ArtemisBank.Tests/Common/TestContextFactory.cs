using ArtemisBank.Infrastructure.Persistence.Contexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Tests.Common
{
    /// <summary>
    /// Fabrica de contextos de prueba. No depende de una base de datos real.
    ///  - InMemory: rapido, para pruebas unitarias de servicios.
    ///  - SQLite en memoria: relacional de verdad, para validar transacciones e indices unicos.
    /// </summary>
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

        /// <summary>
        /// SQLite en memoria. La conexion debe mantenerse abierta mientras dure la prueba,
        /// por eso se devuelve junto al contexto.
        /// </summary>
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
