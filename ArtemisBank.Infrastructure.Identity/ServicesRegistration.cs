using System.Text;
using ArtemisBank.Core.Domain.Settings;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ArtemisBank.Infrastructure.Identity
{
    /// <summary>
    /// PLACEHOLDER — propiedad de Monserrat.
    ///
    /// Hoy solo configura los esquemas de autenticacion minimos para que la WebApp y la WebApi
    /// arranquen y los atributos [Authorize(Roles = ...)] de los modulos de Michael funcionen.
    ///
    /// Monserrat sustituye el cuerpo de estos metodos por ASP.NET Identity completo
    /// (UserManager, SignInManager, roles, tokens de activacion y restablecimiento, JwtService)
    /// SIN cambiar sus firmas, para no tocar los Program.cs.
    /// </summary>
    public static class ServicesRegistration
    {
        public static void AddIdentityLayerIocForWebApp(this IServiceCollection services,
            IConfiguration configuration)
        {
            // TODO (Monserrat): AddDbContext<IdentityContext>, AddIdentity<AppUser, IdentityRole>,
            // servicios de cuenta, seeding de roles y usuarios por defecto activos.

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Login/Index";
                    options.AccessDeniedPath = "/Login/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                    options.SlidingExpiration = true;
                });

            services.AddAuthorization();
        }

        public static void AddIdentityLayerIocForWebApi(this IServiceCollection services,
            IConfiguration configuration)
        {
            // TODO (Monserrat): AddDbContext<IdentityContext>, AddIdentity, JwtService,
            // endpoints publicos de Account y tokens de un solo uso.

            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            var jwt = configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(jwt.Key)
                            ? "clave-de-desarrollo-artemis-banking-pro-2026-cambiar" : jwt.Key))
                };
            });

            services.AddAuthorization();
        }
    }
}
