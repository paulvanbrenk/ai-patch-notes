using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace PatchNotes.Data.Groq;

/// <summary>
/// Extension methods for registering Groq client services.
/// </summary>
public static class GroqServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Groq client to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGroqClient(
        this IServiceCollection services,
        Action<GroqClientOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.AddOptions<GroqClientOptions>();
        }

        services.AddHttpClient<IGroqClient, GroqClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<GroqClientOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            }
        });

        return services;
    }
}
