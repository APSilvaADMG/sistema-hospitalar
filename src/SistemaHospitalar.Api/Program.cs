using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SistemaHospitalar.Application.Serialization;
using SistemaHospitalar.Infrastructure;
using SistemaHospitalar.Api.HealthChecks;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Realtime;
using SistemaHospitalar.Infrastructure.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new PortugueseEnumJsonConverterFactory());
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Sistema Hospitalar API", Version = "v1" });
});

var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin => IsAllowedLanOrigin(origin, configuredOrigins))
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(configuredOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRequestTimeouts(options =>
{
    options.AddPolicy("LongRunningImport", TimeSpan.FromHours(2));
});
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadinessHealthCheck>("database", tags: ["ready"])
    .AddCheck<WhatsAppReadinessHealthCheck>("whatsapp", tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRequestTimeouts();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SistemaHospitalar.Infrastructure.Middleware.AuditLoggingMiddleware>();
app.MapControllers();
app.MapHub<OperationsHub>("/hubs/operations");
app.MapHub<ConnectHub>("/hubs/connect");
app.MapHub<TvSignageHub>("/hubs/tv-signage");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

try
{
    await InitializeDatabaseWithRetryAsync(app.Services, app.Logger, app.Lifetime.ApplicationStopping);
    app.Services.GetRequiredService<DatabaseInitializationState>().MarkComplete();
}
catch (Exception ex)
{
    app.Services.GetRequiredService<DatabaseInitializationState>().MarkFailed(ex);
    throw;
}

app.Run();

static bool IsAllowedLanOrigin(string? origin, string[] configuredOrigins)
{
    if (string.IsNullOrWhiteSpace(origin))
    {
        return false;
    }

    if (configuredOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
    {
        return true;
    }

    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
    {
        return false;
    }

    if (uri.Scheme is not ("http" or "https"))
    {
        return false;
    }

    if (uri.Host is "localhost" or "127.0.0.1" or "10.0.2.2")
    {
        return true;
    }

    if (uri.Host.StartsWith("192.168.", StringComparison.Ordinal)
        || uri.Host.StartsWith("10.", StringComparison.Ordinal)
        || uri.Host.StartsWith("172.", StringComparison.Ordinal))
    {
        return uri.Port is 80 or 443 or 3000 or 5173 or 8080;
    }

    return false;
}

static async Task InitializeDatabaseWithRetryAsync(
    IServiceProvider services,
    ILogger logger,
    CancellationToken cancellationToken)
{
    const int maxAttempts = 8;
    var delay = TimeSpan.FromSeconds(2);

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await DatabaseInitializer.InitializeAsync(services, cancellationToken);
            logger.LogInformation("Database initialization completed on attempt {Attempt}.", attempt);
            return;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(
                ex,
                "Database initialization failed on attempt {Attempt}/{MaxAttempts}. Retrying in {DelaySeconds}s.",
                attempt,
                maxAttempts,
                delay.TotalSeconds);

            await Task.Delay(delay, cancellationToken);
            delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
        }
    }

    await DatabaseInitializer.InitializeAsync(services, cancellationToken);
}
