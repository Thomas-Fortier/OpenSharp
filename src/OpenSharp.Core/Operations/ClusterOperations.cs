using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Operations;

/// <summary>
/// Implements <see cref="IClusterOperations"/> — cluster information from the connection and the
/// version endpoint, and resource-type availability from API discovery.
/// </summary>
internal sealed class ClusterOperations : OperationBase, IClusterOperations
{
    public ClusterOperations(IKubernetes k8s, OpenShiftClientOptions options, ILogger logger)
        : base(k8s, options, logger) { }

    /// <inheritdoc/>
    public async Task<ClusterInfo> GetInfoAsync(CancellationToken ct = default)
    {
        var endpoint = K8s.BaseUri?.ToString().TrimEnd('/') ?? string.Empty;
        var version = await ExecuteAsync(() => K8s.Version.GetCodeAsync(ct)).ConfigureAwait(false);
        return new ClusterInfo
        {
            ApiServerEndpoint = endpoint,
            ServerVersion = version.GitVersion ?? string.Empty,
            Reachable = true,
        };
    }

    /// <inheritdoc/>
    public async Task<bool> IsResourceTypeAvailableAsync(
        string group, string version, string plural, CancellationToken ct = default)
    {
        try
        {
            var list = string.IsNullOrEmpty(group)
                ? await K8s.CoreV1.GetAPIResourcesAsync(ct).ConfigureAwait(false)
                : await K8s.CustomObjects.GetAPIResourcesAsync(group, version, ct).ConfigureAwait(false);
            return Serves(list, plural);
        }
        catch (HttpOperationException ex) when ((int?)ex.Response?.StatusCode == 404)
        {
            // The group/version is not served by this cluster — the type is unavailable.
            return false;
        }
    }

    /// <summary>Returns whether <paramref name="list"/> advertises a resource with the given plural name.</summary>
    internal static bool Serves(V1APIResourceList list, string plural) =>
        list.Resources?.Any(r => string.Equals(r.Name, plural, StringComparison.Ordinal)) ?? false;
}
