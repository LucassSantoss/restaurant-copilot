using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RestaurantCopilot.Api.Endpoints;
using RestaurantCopilot.Api.HealthChecks;
using RestaurantCopilot.Application;
using RestaurantCopilot.Application.Agent;
using RestaurantCopilot.Application.Chat;
using RestaurantCopilot.Infrastructure;
using RestaurantCopilot.Infrastructure.Persistence;
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
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddApplication();
    builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection(AgentOptions.SectionName));
    builder.Services.Configure<ChatCacheOptions>(builder.Configuration.GetSection(ChatCacheOptions.SectionName));
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    await app.Services.InitializeDatabaseAsync();

    app.UseExceptionHandler(new ExceptionHandlerOptions
    {
        // Malformed requests (e.g. an unknown enum in the query string) are client errors, not 500s.
        StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
            ? badRequest.StatusCode
            : StatusCodes.Status500InternalServerError,
    });
    app.UseStatusCodePages();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.MapChatEndpoints();
    app.MapMenuEndpoints();
    app.MapReviewEndpoints();
    app.MapIngestionEndpoints();
    app.MapSearchEndpoints();

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
