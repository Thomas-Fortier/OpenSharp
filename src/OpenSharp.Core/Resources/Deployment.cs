namespace OpenSharp.Core.Resources;

/// <summary>
/// A Kubernetes Deployment (<c>apps/v1</c>) or an OpenShift DeploymentConfig
/// (<c>apps.openshift.io/v1</c>). Both are represented by this type and differ
/// only in which <see cref="OpenSharp.Core.Abstractions.IOpenShiftClient"/> property they are accessed through.
/// </summary>
public sealed class Deployment
{
    /// <summary>Standard Kubernetes metadata for this workload.</summary>
    public required ResourceMetadata Metadata { get; init; }

    /// <summary>Desired number of pod replicas.</summary>
    public int Replicas { get; init; }

    /// <summary>Number of replicas considered available (passing readiness).</summary>
    public int AvailableReplicas { get; init; }

    /// <summary>Number of replicas that are ready.</summary>
    public int ReadyReplicas { get; init; }

    /// <summary>Label selector used to identify pod replicas managed by this workload.</summary>
    public IReadOnlyDictionary<string, string> Selector { get; init; } = new Dictionary<string, string>();
}
