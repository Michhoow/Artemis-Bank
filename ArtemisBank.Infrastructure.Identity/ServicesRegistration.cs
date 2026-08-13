using System.Text;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Domain.Settings;
using ArtemisBank.Infrastructure.Identity.Contexts;
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
    /// <summary>
    /// Extensiones de registro de la capa Identity.
    /// Las firmas de los métodos públicos son CONTRATOS CONGELADOS — no modificar
    /// para no tocar los Program.cs de Michael.
    /// </summary>
    public static class ServicesRegistration
    {
        // ── WebApp (MVC + Cookies) ───────────────────────────────────────────

        /// <summary>
        /// Registra Identity, cookie de autenticación y servicios de usuario para la WebApp MVC.
        /// </summary>
        public static void AddIdentityLayerIocForWebApp(this IServiceCollection services,
            IConfiguration configuration)
        {
            // DbContext de Identity en su propio esquema, independiente de ArtemisDbContext
            RegisterIdentityDbContext(services, configuration);

            // ASP.NET Identity con AppUser y IdentityRole
            services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                // Política de contraseñas (nivel académico razonable)
                options.Password.RequiredLength         = 8;
                options.Password.RequireDigit           = true;
                options.Password.RequireLowercase       = true;
                options.Password.RequireUppercase       = true;
                options.Password.RequireNonAlphanumeric = true;

                // Lockout
                options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;

                // El email puede estar sin confirmar para el flujo de activación
                options.SignIn.RequireConfirmedEmail = false;

                // Unicidad de email y username
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<IdentityContext>()
            .AddDefaultTokenProviders();

            // Cookie de sesión MVC
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

            // Implementación real de IUserReadService (sustituye al puente temporal)
            services.AddScoped<IUserReadService, UserReadService>();

            // Servicios de cuenta e identidad — WebApp
            services.AddScoped<IAccountServiceForWebApp, AccountServiceForWebApp>();
        }

        // ── WebApi (JWT Bearer) ──────────────────────────────────────────────

        /// <summary>
        /// Registra Identity, JWT Bearer y servicios de usuario para la Web API.
        /// </summary>
        public static void AddIdentityLayerIocForWebApi(this IServiceCollection services,
            IConfiguration configuration)
        {
            // DbContext de Identity
            RegisterIdentityDbContext(services, configuration);

            // ASP.NET Identity (sin cookie, sin SignIn UI)
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
            .AddEntityFrameworkStores<IdentityContext>()
            .AddDefaultTokenProviders();

            // JWT Bearer
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

                // Respuestas 401 / 403 limpias (sin redirect a login)
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

            // Implementación real de IUserReadService
            services.AddScoped<IUserReadService, UserReadService>();

            // Servicios de cuenta e identidad — WebApi
            services.AddScoped<JwtService>();
            services.AddScoped<IAccountServiceForWebApi, AccountServiceForWebApi>();
        }

        // ── Migraciones y seeding de Identity ───────────────────────────────

        /// <summary>
        /// Aplica migraciones del <see cref="IdentityContext"/> y ejecuta el seeding
        /// de roles y usuarios por defecto. Llamar desde Program.cs después de
        /// <c>RunPersistenceMigrationsAsync</c>.
        /// </summary>
        public static async Task RunIdentityMigrationsAsync(this IServiceProvider services,
            IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var sp     = scope.ServiceProvider;
            var logger = sp.GetRequiredService<ILogger<AppUser>>();

            // Migraciones de IdentityContext
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

            // Seed solo si está habilitado en la config (igual que el de Michael)
            if (!configuration.GetValue<bool>("SeedDemoData")) return;

            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = sp.GetRequiredService<UserManager<AppUser>>();

            await DefaultRoles.SeedAsync(roleManager, logger);
            await DefaultUsers.SeedAsync(userManager, logger, sp);

            logger.LogInformation("Identity: seeding de roles y usuarios completado.");
        }

        // ── Helper privado ───────────────────────────────────────────────────

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
