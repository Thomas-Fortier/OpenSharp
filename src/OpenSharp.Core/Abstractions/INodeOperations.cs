using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Abstractions;

/// <summary>
/// Operations on cluster <c>Node</c> resources. Nodes are cluster-scoped, so namespace
/// arguments on the read operations are ignored.
/// </summary>
public interface INodeOperations : IReadOperations<Node>, IWatchable<Node>
{
    /// <summary>
    /// Marks a node unschedulable (cordon) so the scheduler places no new pods on it.
    /// </summary>
    /// <param name="name">Node name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CordonAsync(string name, CancellationToken ct = default);

    /// <summary>Marks a node schedulable again (uncordon).</summary>
    /// <param name="name">Node name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UncordonAsync(string name, CancellationToken ct = default);
}
