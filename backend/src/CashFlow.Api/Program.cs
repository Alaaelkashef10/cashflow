using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

using CashFlow.Api.Endpoints;
using CashFlow.Api.Errors;
using CashFlow.Infrastructure;

using FluentValidation;

using Microsoft.AspNetCore.RateLimiting;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Requests here are tiny JSON bodies. Reject anything larger than 64 KB.
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("categorize", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// First in the pipeline, so every response (including errors) carries these headers.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    if (!app.Environment.IsDevelopment())
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

    await next();
});

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseRateLimiter();

app.MapHealthChecks("/health");

var api = app.MapGroup("/api/v1");
api.MapGet("/ping", () => TypedResults.Ok(new { message = "pong" }));
api.MapScenarioEndpoints();
api.MapEntryEndpoints();
api.MapProjectionEndpoints();
api.MapSampleEndpoints();
api.MapCategorizeEndpoints();

app.Run();

// Makes Program visible to integration tests.
public partial class Program { }