using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Abstractions;

/// <summary>
/// Operations for scalable workload resources (Deployments and DeploymentConfigs).
/// </summary>
public interface IWorkloadOperations : IReadOperations<Deployment>, IWriteOperations<Deployment>, IWatchable<Deployment>
{
    /// <summary>
    /// Scales a workload to the specified number of replicas.
    /// </summary>
    /// <param name="name">Workload name.</param>
    /// <param name="namespace">Project or namespace.</param>
    /// <param name="replicas">Desired replica count (must be ≥ 0).</param>
    /// <param name="ct">Cancellation token.</param>
    Task ScaleAsync(string name, string @namespace, int replicas, CancellationToken ct = default);

    /// <summary>
    /// Triggers a rolling restart of the workload by updating a restart annotation.
    /// </summary>
    /// <param name="name">Workload name.</param>
    /// <param name="namespace">Project or namespace.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RolloutRestartAsync(string name, string @namespace, CancellationToken ct = default);
}
