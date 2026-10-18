using MedicationCatalogue.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedicationCatalogue.Infrastructure;

public static class DependencyInjection
{
    // Called only by the host composition root; API handlers depend on Application contracts.
    public static IServiceCollection AddMedicationCatalogue(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<HealthCanadaDpdOptions>()
            .Bind(configuration.GetSection(HealthCanadaDpdOptions.SectionName))
            .Validate(HealthCanadaDpdOptions.IsValid, "DPD requires the official HTTPS API base URL and a timeout between 1 and 30 seconds.")
            .ValidateOnStart();
        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient<IMedicationProductLookup, HealthCanadaDpdClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<HealthCanadaDpdOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            // One deadline covers the entire multi-resource lookup, including body reads.
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        });
        services.AddLogging(logging => logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning));
        return services;
    }
}
