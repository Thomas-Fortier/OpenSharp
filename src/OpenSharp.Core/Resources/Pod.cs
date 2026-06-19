namespace OpenSharp.Core.Resources;

/// <summary>
/// A running Kubernetes pod — the smallest deployable unit, consisting of one or more
/// containers.
/// </summary>
public sealed class Pod
{
    /// <summary>Standard Kubernetes metadata for this pod.</summary>
    public required ResourceMetadata Metadata { get; init; }

    /// <summary>
    /// Current lifecycle phase of the pod, e.g. <c>Pending</c>, <c>Running</c>,
    /// <c>Succeeded</c>, <c>Failed</c>, or <c>Unknown</c>.
    /// </summary>
    public required string Phase { get; init; }

    /// <summary>Runtime state of each container in this pod.</summary>
    public IReadOnlyList<ContainerInfo> Containers { get; init; } = [];
}
