using System.Text.Json;
using System.Text.Json.Serialization;

using CashFlow.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

namespace CashFlow.Tests.Integration;

public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:17").Build();
    private WebApplicationFactory<Program>? _factory;

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public IServiceProvider Services => _factory!.Services;

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        // Environment variables override user-secrets, so tests never touch your dev database.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _db.GetConnectionString());

        _factory = new WebApplicationFactory<Program>();

        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public HttpClient CreateClient(Guid? ownerId = null)
    {
        var client = _factory!.CreateClient();
        if (ownerId.HasValue)
            client.DefaultRequestHeaders.Add("X-Owner-Id", ownerId.Value.ToString());
        return client;
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();
        await _db.DisposeAsync();
    }
}

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFixture>;