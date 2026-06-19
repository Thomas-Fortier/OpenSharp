using k8s;

namespace OpenSharp.Core.Authentication;

/// <summary>
/// Creates configured <see cref="IKubernetes"/> clients from connection options.
/// Injected into operations to allow substitution in tests.
/// </summary>
public interface IOpenShiftConnectionFactory
{
    /// <summary>
    /// Creates a <see cref="IKubernetes"/> client configured according to
    /// <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The connection and authentication options.</param>
    /// <returns>A ready-to-use Kubernetes client.</returns>
    IKubernetes CreateClient(OpenShiftClientOptions options);
}
