using Microsoft.Extensions.DependencyInjection;

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

        // The Stytch SDK handles HTTP internally, so we just register as singleton
        services.AddSingleton<IStytchClient, StytchClient>();

        return services;
    }
}
