using System.Text.Json.Serialization;

using CashFlow.Api.Endpoints;
using CashFlow.Api.Errors;
using CashFlow.Infrastructure;

using FluentValidation;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("frontend");

app.MapHealthChecks("/health");

var api = app.MapGroup("/api/v1");
api.MapGet("/ping", () => TypedResults.Ok(new { message = "pong" }));
api.MapScenarioEndpoints();
api.MapEntryEndpoints();
api.MapProjectionEndpoints();
api.MapSampleEndpoints();

app.Run();

// Makes Program visible to integration tests.
public partial class Program { }