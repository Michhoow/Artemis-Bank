using System.Reflection;
using ArtemisBank.Core.Application.Behaviors;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Application.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBank.Core.Application
{
    public static class ServicesRegistration
    {
        public static void AddApplicationLayerIoc(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            #region Configuraciones
            services.AddAutoMapper(assembly);
            services.AddValidatorsFromAssembly(assembly);

            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(assembly);
            });

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            #endregion

            #region Servicios de negocio — Michael
            services.AddScoped<IAccountNumberGenerator, AccountNumberGenerator>();
            services.AddScoped<ISavingsAccountService, SavingsAccountService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IBeneficiaryService, BeneficiaryService>();
            services.AddScoped<IAdminHomeService, AdminHomeService>();
            services.AddScoped<ICashierHomeService, CashierHomeService>();
            #endregion

            #region Servicios de negocio — Manuel (prestamos, tarjetas, Hermes Pay)

            services.AddScoped<ILoanReadService, LoanReadService>();
            services.AddScoped<ICreditCardReadService, CreditCardReadService>();

            services.AddScoped<IRiskAssessmentService, RiskAssessmentService>();
            services.AddScoped<ILoanService, LoanService>();
            services.AddScoped<ICreditCardService, CreditCardService>();
            services.AddScoped<IHermesPayService, HermesPayService>();

            services.AddScoped<IOverdueInstallmentProcessor, OverdueInstallmentProcessor>();
            #endregion

            #region Servicios de negocio — Monserrat (comercios)
            services.AddScoped<ICommerceService, CommerceService>();
            #endregion
        }
    }
}
