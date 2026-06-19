using k8s;
using OpenSharp.Core.Authentication;

namespace OpenSharp.SystemTests.Support;

/// <summary>
/// An <see cref="IOpenShiftConnectionFactory"/> that returns a <see cref="Kubernetes"/>
/// client configured to speak to the in-process WireMock simulator.
/// </summary>
internal sealed class WireMockConnectionFactory : IOpenShiftConnectionFactory
{
    private readonly string _baseUrl;

    public WireMockConnectionFactory(string baseUrl) => _baseUrl = baseUrl;

    /// <inheritdoc/>
    public IKubernetes CreateClient(OpenShiftClientOptions options)
    {
        var config = new KubernetesClientConfiguration
        {
            Host = _baseUrl,
            SkipTlsVerify = true,
        };
        return new Kubernetes(config);
    }
}
