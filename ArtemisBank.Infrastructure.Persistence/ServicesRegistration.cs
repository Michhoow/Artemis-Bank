using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using ArtemisBank.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBank.Infrastructure.Persistence
{
    public static class ServicesRegistration
    {
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
            services.AddScoped<ILoanRepository, LoanRepository>();
            services.AddScoped<ILoanInstallmentRepository, LoanInstallmentRepository>();
            services.AddScoped<ICreditCardRepository, CreditCardRepository>();
            services.AddScoped<ICardConsumptionRepository, CardConsumptionRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            #endregion
        }

        public static async Task RunPersistenceMigrationsAsync(this IServiceProvider services,
            IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ArtemisDbContext>();

            if (context.Database.IsRelational())
            {
                var hasMigrations = context.Database.GetMigrations().Any();

                if (hasMigrations) await context.Database.MigrateAsync();
                else await context.Database.EnsureCreatedAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

        }
    }
}
