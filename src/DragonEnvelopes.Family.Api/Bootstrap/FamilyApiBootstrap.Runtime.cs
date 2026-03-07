using Asp.Versioning;
using DragonEnvelopes.Family.Api.CrossCutting.Logging;
using DragonEnvelopes.Family.Api.CrossCutting.Validation;
using DragonEnvelopes.Family.Api.Endpoints;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DragonEnvelopes.Family.Api.Bootstrap;

internal static partial class FamilyApiBootstrap
{
    public static void ApplyDatabaseMigrations(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("StartupMigrations");

        logger.LogInformation("Applying EF Core migrations at startup for Family API.");

        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
        if (dbContext.Database.IsRelational())
        {
            dbContext.Database.Migrate();
            logger.LogInformation("EF Core migrations applied successfully for Family API.");
        }
        else
        {
            dbContext.Database.EnsureCreated();
            logger.LogInformation("Ensured database created for non-relational provider in Family API.");
        }
    }

    public static void ConfigureMiddleware(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
                diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value);
                diagnosticContext.Set("StatusCode", httpContext.Response.StatusCode);
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
            };
        });

        app.UseExceptionHandler();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
    }

    public static void MapHealthEndpoints(WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        })
        .AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        })
        .AllowAnonymous();
    }

    public static void MapVersionedEndpoints(WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var v1 = app.MapGroup("/api/v{version:apiVersion}")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .AddFluentValidation();

        v1.MapFamilyEndpoints();
    }
}
