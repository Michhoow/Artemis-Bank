using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Infrastructure.Shared.Services;
using ArtemisBank.Core.Domain.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBank.Infrastructure.Shared
{
    public static class ServicesRegistration
    {
        public static void AddSharedLayerIoc(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MailSettings>(configuration.GetSection("MailSettings"));
            services.AddTransient<IEmailService, EmailService>();

            services.AddSingleton<ICryptoService, CryptoService>();
        }
    }
}
