using ArtemisBank.Core.Application;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Infrastructure.Identity;
using ArtemisBank.Infrastructure.Shared;
using ArtemisBank.Infrastructure.Persistence;
using ArtemisBank.WebApp.Common;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

#region Serilog
// Logging de auditoria y diagnostico. Nunca se registran contrasenias, tokens,
// CVC, numeros completos de tarjeta ni cadenas de conexion.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Aplicacion", "ArtemisBank.WebApp")
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("Logs/artemis-webapp-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));
#endregion

#region Servicios
builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// El orden es deliberado: Application registra los puentes temporales de contratos
// compartidos y las capas de Identity y Shared, al registrarse despues, los sustituyen.
builder.Services.AddPersistenceLayerIoc(builder.Configuration);
builder.Services.AddApplicationLayerIoc();
builder.Services.AddIdentityLayerIocForWebApp(builder.Configuration);
builder.Services.AddSharedLayerIoc(builder.Configuration);

builder.Services.AddScoped<IAuthenticatedUser, HttpContextAuthenticatedUser>();
#endregion

var app = builder.Build();

#region Base de datos
await app.Services.RunPersistenceMigrationsAsync(app.Configuration);
#endregion

#region Pipeline
app.UseGlobalExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Va despues de la autenticacion para que el log ya conozca usuario y rol.
app.UseRequestAudit();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
#endregion

await app.RunAsync();
