using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Abstractions;

/// <summary>Operations for Kubernetes <c>Service</c> resources.</summary>
public interface IServiceOperations : IReadOperations<Service>, IWriteOperations<Service>, IWatchable<Service>
{
}
