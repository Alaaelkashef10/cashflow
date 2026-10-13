using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

var api = app.MapGroup("/api/v1");
api.MapGet("/ping", () => TypedResults.Ok(new { message = "pong" }));

app.Run();

// Makes Program visible to integration tests.
public partial class Program { }