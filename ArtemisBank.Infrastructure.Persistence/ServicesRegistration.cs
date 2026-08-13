using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using ArtemisBank.Infrastructure.Persistence.Repositories;
using ArtemisBank.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBank.Infrastructure.Persistence
{
    public static class ServicesRegistration
    {
        /// <summary>Registro de la capa de persistencia (extension method — patron Decorator).</summary>
        public static void AddPersistenceLayerIoc(this IServiceCollection services, IConfiguration configuration)
        {
            #region Contexto
            if (configuration.GetValue<bool>("UseInMemoryDatabase"))
            {
                services.AddDbContext<ArtemisDbContext>(options => options.UseInMemoryDatabase("ArtemisBankDb"));
            }
            else
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                services.AddDbContext<ArtemisDbContext>(options =>
                {
                    options.UseSqlServer(connectionString,
                        sql => sql.MigrationsAssembly(typeof(ArtemisDbContext).Assembly.FullName));
                },
                contextLifetime: ServiceLifetime.Scoped,
                optionsLifetime: ServiceLifetime.Scoped);
            }
            #endregion

            #region Repositorios
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<ISavingsAccountRepository, SavingsAccountRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IBeneficiaryRepository, BeneficiaryRepository>();
            services.AddScoped<ICommerceRepository, CommerceRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            #endregion
        }

        /// <summary>
        /// Aplica migraciones pendientes y, si procede, los datos minimos de prueba.
        /// Se invoca desde Program.cs de la WebApp y de la WebApi.
        /// </summary>
        public static async Task RunPersistenceMigrationsAsync(this IServiceProvider services,
            IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ArtemisDbContext>();

            if (context.Database.IsRelational())
            {
                // Si todavia no se ha generado la migracion inicial, MigrateAsync crearia una base
                // vacia con solo __EFMigrationsHistory. Se cae a EnsureCreated para que el equipo
                // pueda trabajar, y se avisa por log.
                var hasMigrations = context.Database.GetMigrations().Any();

                if (hasMigrations) await context.Database.MigrateAsync();
                else await context.Database.EnsureCreatedAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            if (configuration.GetValue<bool>("SeedDemoData"))
                await SavingsAccountSeed.RunAsync(context);
        }
    }
}
