using Microsoft.Extensions.DependencyInjection;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Operations;

namespace OpenSharp.Core.DependencyInjection;

/// <summary>Extension methods for registering OpenSharp services into a DI container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the OpenSharp client and all required services into the
    /// <paramref name="services"/> collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">
    /// A delegate to configure <see cref="OpenShiftClientOptions"/>.
    /// </param>
    /// <returns>The original <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddOpenSharp(
        this IServiceCollection services,
        Action<OpenShiftClientOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IOpenShiftConnectionFactory, OpenShiftConnectionFactory>();
        services.AddSingleton<IOpenShiftClient, OpenShiftClient>();
        return services;
    }
}
