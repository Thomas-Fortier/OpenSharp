namespace OpenSharp.Core.Resources;

/// <summary>A Kubernetes Service that exposes a set of pods over a stable network address.</summary>
public sealed class Service
{
    /// <summary>Standard Kubernetes metadata for this service.</summary>
    public required ResourceMetadata Metadata { get; init; }

    /// <summary>
    /// Service type, e.g. <c>ClusterIP</c>, <c>NodePort</c>, or <c>LoadBalancer</c>.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Cluster-internal IP address assigned to the service.
    /// <see langword="null"/> for headless services.
    /// </summary>
    public string? ClusterIp { get; init; }

    /// <summary>Ports exposed by this service.</summary>
    public IReadOnlyList<ServicePort> Ports { get; init; } = [];
}
