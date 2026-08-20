using System.Reflection;
using ArtemisBank.Core.Application.Behaviors;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Application.Services;
using ArtemisBank.Core.Application.Services.Pending;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBank.Core.Application
{
    public static class ServicesRegistration
    {
        /// <summary>Registro de la capa de aplicacion (extension method — patron Decorator).</summary>
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

            // El orden importa: primero se traza, luego se valida, y solo entonces corre el handler.
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            #endregion

            // Nota: GenericService<TEntity, TDto> NO se registra como generico abierto porque su
            // aridad no coincide con IGenericService<TDto>. Se consume por herencia, igual que en la
            // solucion de referencia: cada servicio de modulo hereda de el y se registra cerrado.

            #region Servicios de negocio — Michael
            services.AddScoped<IAccountNumberGenerator, AccountNumberGenerator>();
            services.AddScoped<ISavingsAccountService, SavingsAccountService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IBeneficiaryService, BeneficiaryService>();
            services.AddScoped<IAdminHomeService, AdminHomeService>();
            services.AddScoped<ICashierHomeService, CashierHomeService>();
            #endregion

            #region Puentes temporales de contratos compartidos
            // IMPORTANTE: en el contenedor de .NET gana el ULTIMO registro de un mismo servicio.
            // Por eso Identity y los modulos de prestamos/tarjetas deben registrarse DESPUES de
            // AddApplicationLayerIoc() en Program.cs: sus implementaciones reales sustituyen a estas
            // sin tocar ni una linea del codigo de Michael.
            services.AddScoped<IUserReadService, PendingUserReadService>();
            services.AddScoped<ILoanReadService, PendingLoanReadService>();
            services.AddScoped<ICreditCardReadService, PendingCreditCardReadService>();
            #endregion

            #region Servicios de negocio — Manuel (productos de credito y pagos)
            // El calculo de amortizacion es puro y sin estado: puede ser singleton.
            services.AddSingleton<IAmortizationService, AmortizationService>();

            services.AddScoped<ILoanService, LoanService>();
            services.AddScoped<ICreditCardService, CreditCardService>();
            services.AddScoped<IHermesPayService, HermesPayService>();

            // Implementaciones REALES de los contratos de lectura. Van DESPUES de los puentes
            // temporales de arriba: en el contenedor de .NET gana el ultimo registro, de modo que
            // estas sustituyen a PendingLoanReadService / PendingCreditCardReadService sin tocar
            // el codigo de Michael. (El contrato de usuarios lo sustituye la capa Identity.)
            services.AddScoped<ILoanReadService, LoanReadService>();
            services.AddScoped<ICreditCardReadService, CreditCardReadService>();
            #endregion
        }
    }
}
