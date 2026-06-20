namespace OpenSharp.Core.Resources;

/// <summary>
/// A cluster Node (a control-plane or worker machine that hosts pods). Cluster-scoped:
/// <see cref="ResourceMetadata.Namespace"/> is always <see langword="null"/>.
/// </summary>
public sealed class Node
{
    /// <summary>Standard Kubernetes metadata for this node.</summary>
    public required ResourceMetadata Metadata { get; init; }

    /// <summary>
    /// Whether the node is marked unschedulable (cordoned). <see langword="true"/> means new
    /// pods will not be scheduled onto it.
    /// </summary>
    public bool Unschedulable { get; init; }

    /// <summary>Status conditions reported for this node (e.g. <c>Ready</c>, <c>DiskPressure</c>).</summary>
    public IReadOnlyList<NodeCondition> Conditions { get; init; } = [];

    /// <summary>Version of the kubelet running on this node, when reported.</summary>
    public string? KubeletVersion { get; init; }
}

/// <summary>A single status condition reported for a <see cref="Node"/>.</summary>
public sealed class NodeCondition
{
    /// <summary>The condition type, e.g. <c>Ready</c> or <c>MemoryPressure</c>.</summary>
    public required string Type { get; init; }

    /// <summary>The condition status: <c>True</c>, <c>False</c>, or <c>Unknown</c>.</summary>
    public required string Status { get; init; }

    /// <summary>A machine-readable reason for the condition's current status, when present.</summary>
    public string? Reason { get; init; }
}
