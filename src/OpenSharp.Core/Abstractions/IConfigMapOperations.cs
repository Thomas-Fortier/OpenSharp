using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Abstractions;

/// <summary>Operations for Kubernetes <c>ConfigMap</c> resources.</summary>
public interface IConfigMapOperations : IReadOperations<ConfigMap>, IWriteOperations<ConfigMap>, IWatchable<ConfigMap>
{
}
