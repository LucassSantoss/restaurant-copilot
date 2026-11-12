using System.Globalization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RestaurantCopilot.Api.HealthChecks;
using RestaurantCopilot.Infrastructure;
using Scalar.AspNetCore;
using Serilog;

// Bootstrap logger: captures errors that happen before the host (and its configuration) is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, logger) => logger
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddProblemDetails();
    builder.Services.AddOpenApi();
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = HealthReportResponseWriter.WriteAsync,
    });

    await app.RunAsync();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    // HostAbortedException is thrown on purpose by tooling (e.g. EF Core migrations) and is not a crash.
    Log.Fatal(exception, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
