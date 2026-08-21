using ArtemisBank.Core.Application.Services;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using ArtemisBank.Infrastructure.Persistence.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ArtemisDbContext>(options =>
            options.UseSqlServer(connectionString,
                sql => sql.MigrationsAssembly(typeof(ArtemisDbContext).Assembly.FullName)));

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ILoanInstallmentRepository, LoanInstallmentRepository>();

        services.AddScoped<IOverdueInstallmentProcessor, OverdueInstallmentProcessor>();
    })
    .Build();

await host.RunAsync();
