using System.Text;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Domain.Settings;
using ArtemisBank.Infrastructure.Identity.Contexts;
using ArtemisBank.Infrastructure.Identity.Common;
using ArtemisBank.Infrastructure.Identity.Entities;
using ArtemisBank.Infrastructure.Identity.Interfaces;
using ArtemisBank.Infrastructure.Identity.Seeds;
using ArtemisBank.Infrastructure.Identity.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ArtemisBank.Infrastructure.Identity
{
    public static class ServicesRegistration
    {
        public static void AddIdentityLayerIocForWebApp(this IServiceCollection services,
            IConfiguration configuration)
        {
            RegisterIdentityDbContext(services, configuration);

            services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength         = 8;
                options.Password.RequireDigit           = true;
                options.Password.RequireLowercase       = true;
                options.Password.RequireUppercase       = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;

                options.SignIn.RequireConfirmedEmail = false;

                options.User.RequireUniqueEmail = true;
            })
            .AddErrorDescriber<SpanishIdentityErrorDescriber>()
            .AddEntityFrameworkStores<IdentityContext>()
            .AddDefaultTokenProviders();

            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromMinutes(30);
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath        = "/Login/Index";
                options.AccessDeniedPath = "/Login/AccessDenied";
                options.ExpireTimeSpan   = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly  = true;
                options.Cookie.IsEssential = true;
            });

            services.AddAuthorization();

            services.AddScoped<IUserReadService, UserReadService>();

            services.AddScoped<IUserManagementService, UserManagementService>();

            services.AddScoped<IAccountServiceForWebApp, AccountServiceForWebApp>();
            services.AddScoped<IAccountAuthService, WebAppAccountAuthAdapter>();
        }

        public static void AddIdentityLayerIocForWebApi(this IServiceCollection services,
            IConfiguration configuration)
        {
            RegisterIdentityDbContext(services, configuration);

            services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength         = 8;
                options.Password.RequireDigit           = true;
                options.Password.RequireLowercase       = true;
                options.Password.RequireUppercase       = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;

                options.SignIn.RequireConfirmedEmail = false;
                options.User.RequireUniqueEmail      = true;
            })
            .AddErrorDescriber<SpanishIdentityErrorDescriber>()
            .AddEntityFrameworkStores<IdentityContext>()
            .AddDefaultTokenProviders();

            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromMinutes(30);
            });

            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            var jwt = configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken            = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.Zero,
                    ValidIssuer              = jwt.Issuer,
                    ValidAudience            = jwt.Audience,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            string.IsNullOrWhiteSpace(jwt.Key)
                                ? "clave-de-desarrollo-artemis-banking-pro-2026-cambiar"
                                : jwt.Key))
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode  = 401;
                        ctx.Response.ContentType = "application/json";
                        return ctx.Response.WriteAsync(
                            "{\"error\":\"No autenticado. Proporcione un token JWT v\\u00e1lido.\"}");
                    },
                    OnForbidden = ctx =>
                    {
                        ctx.Response.StatusCode  = 403;
                        ctx.Response.ContentType = "application/json";
                        return ctx.Response.WriteAsync(
                            "{\"error\":\"Acceso denegado. No tiene permiso para este recurso.\"}");
                    }
                };
            });

            services.AddAuthorization();

            services.AddScoped<IUserReadService, UserReadService>();

            services.AddScoped<IUserManagementService, UserManagementService>();

            services.AddScoped<JwtService>();
            services.AddScoped<IAccountServiceForWebApi, AccountServiceForWebApi>();
            services.AddScoped<IAccountAuthService>(sp => sp.GetRequiredService<IAccountServiceForWebApi>());
        }

        public static async Task RunIdentityMigrationsAsync(this IServiceProvider services,
            IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var sp     = scope.ServiceProvider;
            var logger = sp.GetRequiredService<ILogger<AppUser>>();

            var context = sp.GetRequiredService<IdentityContext>();
            if (context.Database.IsRelational())
            {
                var hasMigrations = context.Database.GetMigrations().Any();
                if (hasMigrations)
                    await context.Database.MigrateAsync();
                else
                    await context.Database.EnsureCreatedAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();

            await DefaultRoles.SeedAsync(roleManager, logger);
            logger.LogInformation("Identity: roles verificados.");

            if (!configuration.GetValue<bool>("SeedDefaultUsers")) return;

            var userManager = sp.GetRequiredService<UserManager<AppUser>>();
            await DefaultUsers.SeedAsync(userManager, logger, sp);

            logger.LogInformation("Identity: usuarios de arranque verificados.");
        }

        private static void RegisterIdentityDbContext(IServiceCollection services,
            IConfiguration configuration)
        {
            if (configuration.GetValue<bool>("UseInMemoryDatabase"))
            {
                services.AddDbContext<IdentityContext>(options =>
                    options.UseInMemoryDatabase("ArtemisBankIdentityDb"));
            }
            else
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                services.AddDbContext<IdentityContext>(options =>
                    options.UseSqlServer(connectionString,
                        sql => sql.MigrationsAssembly(
                            typeof(IdentityContext).Assembly.FullName)),
                    contextLifetime: ServiceLifetime.Scoped,
                    optionsLifetime: ServiceLifetime.Scoped);
            }
        }
    }
}
