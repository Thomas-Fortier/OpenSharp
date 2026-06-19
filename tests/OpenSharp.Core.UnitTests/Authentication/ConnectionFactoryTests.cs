using Microsoft.Extensions.Logging.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Errors;

namespace OpenSharp.Core.UnitTests.Authentication;

public sealed class ConnectionFactoryTests
{
    private readonly OpenShiftConnectionFactory _factory =
        new(NullLogger<OpenShiftConnectionFactory>.Instance);

    // ─── IsRunningInCluster ──────────────────────────────────────────────────

    [Fact]
    public void IsRunningInCluster_OutsidePod_ReturnsFalse()
    {
        var result = OpenShiftConnectionFactory.IsRunningInCluster();
        Assert.False(result, "Should be false in a non-pod environment.");
    }

    // ─── Auto mode resolves to KubeConfig outside a cluster ──────────────────

    [Fact]
    public void CreateClient_AutoMode_UsesKubeConfigWhenNotInCluster()
    {
        var options = new OpenShiftClientOptions
        {
            AuthMode = AuthMode.Auto,
            AccessToken = "test-token",
            ServerUrl = new Uri("http://localhost:8080"),
        };

        var client = _factory.CreateClient(options);
        Assert.NotNull(client);
        client.Dispose();
    }

    // ─── Explicit token + server URL ─────────────────────────────────────────

    [Fact]
    public void CreateClient_ExplicitTokenAndServer_CreatesClientSuccessfully()
    {
        var options = new OpenShiftClientOptions
        {
            AuthMode = AuthMode.KubeConfig,
            AccessToken = "mytoken",
            ServerUrl = new Uri("https://api.example.com:6443"),
            SkipTlsVerify = true,
        };

        var client = _factory.CreateClient(options);
        Assert.NotNull(client);
        client.Dispose();
    }

    // ─── InCluster outside a pod ──────────────────────────────────────────────

    [Fact]
    public void CreateClient_InClusterOutsidePod_ThrowsAuthenticationException()
    {
        var options = new OpenShiftClientOptions { AuthMode = AuthMode.InCluster };

        Assert.Throws<OpenShiftAuthenticationException>(() => _factory.CreateClient(options));
    }

    // ─── KubeConfig mode with invalid path ───────────────────────────────────

    [Fact]
    public void CreateClient_MissingKubeConfig_ThrowsAuthenticationException()
    {
        var options = new OpenShiftClientOptions
        {
            AuthMode = AuthMode.KubeConfig,
            KubeConfigPath = "/nonexistent/path/to/kubeconfig",
        };

        Assert.Throws<OpenShiftAuthenticationException>(() => _factory.CreateClient(options));
    }

    // ─── SkipTlsVerify propagation ────────────────────────────────────────────

    [Fact]
    public void CreateClient_SkipTlsVerify_DoesNotThrow()
    {
        var options = new OpenShiftClientOptions
        {
            AuthMode = AuthMode.KubeConfig,
            AccessToken = "tok",
            ServerUrl = new Uri("https://api.example.com"),
            SkipTlsVerify = true,
        };

        var client = _factory.CreateClient(options);
        Assert.NotNull(client);
        client.Dispose();
    }
}
