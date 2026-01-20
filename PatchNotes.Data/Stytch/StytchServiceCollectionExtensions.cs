using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace PatchNotes.Data.Stytch;

/// <summary>
/// Extension methods for registering Stytch client services.
/// </summary>
public static class StytchServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Stytch client to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStytchClient(
        this IServiceCollection services,
        Action<StytchClientOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.AddOptions<StytchClientOptions>();
        }

        services.AddHttpClient<IStytchClient, StytchClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<StytchClientOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Stytch uses Basic auth with project_id:secret
            if (!string.IsNullOrWhiteSpace(options.ProjectId) && !string.IsNullOrWhiteSpace(options.Secret))
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.ProjectId}:{options.Secret}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }
        });

        return services;
    }
}
