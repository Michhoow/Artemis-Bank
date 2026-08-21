using System.Text.Json.Serialization;
using ArtemisBank.Core.Application;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Infrastructure.Identity;
using ArtemisBank.Infrastructure.Shared;
using ArtemisBank.Infrastructure.Persistence;
using ArtemisBank.WebApi.Common;
using ArtemisBank.WebApi.Extensions;
using ArtemisBank.WebApi.Middleware;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

#region Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Aplicacion", "ArtemisBank.WebApi")
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("Logs/artemis-webapi-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));
#endregion

#region Servicios
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services.AddPersistenceLayerIoc(builder.Configuration);
builder.Services.AddApplicationLayerIoc();
builder.Services.AddIdentityLayerIocForWebApi(builder.Configuration);
builder.Services.AddSharedLayerIoc(builder.Configuration);

builder.Services.AddScoped<IAuthenticatedUser, HttpContextAuthenticatedUser>();

builder.Services.AddApiVersioningExtension();
builder.Services.AddSwaggerExtension();
builder.Services.AddHealthChecks();
#endregion

var app = builder.Build();

#region Base de datos
await app.Services.RunPersistenceMigrationsAsync(app.Configuration);
await app.Services.RunIdentityMigrationsAsync(app.Configuration);
#endregion

#region Pipeline

app.UseGlobalExceptionHandler();

app.UseSwaggerExtension();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseRequestAudit();

app.MapHealthChecks("/health");
app.MapControllers();
#endregion

await app.RunAsync();
