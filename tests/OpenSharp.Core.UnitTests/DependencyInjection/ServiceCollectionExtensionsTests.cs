using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.DependencyInjection;

namespace OpenSharp.Core.UnitTests.DependencyInjection;

/// <summary>Tests for the <c>AddOpenSharp</c> DI registration extension (FR-013).</summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddOpenSharp_RegistersClientAndFactory()
    {
        var services = new ServiceCollection();

        var returned = services.AddOpenSharp(o => o.DefaultNamespace = "demo");

        Assert.Same(services, returned);
        Assert.Contains(services, d => d.ServiceType == typeof(IOpenShiftClient));
        Assert.Contains(services, d => d.ServiceType == typeof(IOpenShiftConnectionFactory));
    }

    [Fact]
    public void AddOpenSharp_AppliesOptionsConfiguration()
    {
        var services = new ServiceCollection();
        services.AddOpenSharp(o =>
        {
            o.DefaultNamespace = "demo";
            o.AuthMode = AuthMode.KubeConfig;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OpenShiftClientOptions>>().Value;

        Assert.Equal("demo", options.DefaultNamespace);
        Assert.Equal(AuthMode.KubeConfig, options.AuthMode);
    }
}
