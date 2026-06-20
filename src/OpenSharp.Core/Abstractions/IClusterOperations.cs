using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Abstractions;

/// <summary>Cluster-level operations: connection information and capability discovery.</summary>
public interface IClusterOperations
{
    /// <summary>
    /// Retrieves cluster information — the API server endpoint, server version, and reachability.
    /// Throws a connection error if the cluster cannot be reached.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<ClusterInfo> GetInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Determines whether the target cluster serves the given API group/version/resource type.
    /// Returns <see langword="false"/> (rather than throwing) when the type is unavailable, so
    /// callers can distinguish "type unavailable" from "instance not found".
    /// </summary>
    /// <param name="group">API group, or empty string for the core group.</param>
    /// <param name="version">API version, e.g. <c>v1</c>.</param>
    /// <param name="plural">Plural resource name, e.g. <c>routes</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> IsResourceTypeAvailableAsync(string group, string version, string plural, CancellationToken ct = default);
}
