using CashFlow.Core.Categorization;
using CashFlow.Core.Projection;
using CashFlow.Infrastructure.Categorization;
using CashFlow.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CashFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddSingleton<IProjectionService, ProjectionService>();

        services.Configure<AiOptions>(configuration.GetSection("Ai"));
        services.AddSingleton<KeywordCategorizer>();

        services.AddHttpClient<LlmCategorizer>((sp, client) =>
        {
            var ai = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            client.BaseAddress = new Uri(ai.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(ai.TimeoutSeconds);
        });

        services.AddHttpClient<AzureOpenAiCategorizer>((sp, client) =>
        {
            var ai = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(ai.AzureEndpoint))
                client.BaseAddress = new Uri(ai.AzureEndpoint.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(ai.TimeoutSeconds);
        });

        services.AddTransient<ICategorizer>(sp =>
        {
            var ai = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            var keyword = sp.GetRequiredService<KeywordCategorizer>();

            ICategorizer? primary = ai.UseAzure
                ? sp.GetRequiredService<AzureOpenAiCategorizer>()
                : ai.UseLlm
                    ? sp.GetRequiredService<LlmCategorizer>()
                    : null;

            return primary is null
                ? keyword
                : new FallbackCategorizer(primary, keyword, sp.GetRequiredService<ILogger<FallbackCategorizer>>());
        });

        return services;
    }
}