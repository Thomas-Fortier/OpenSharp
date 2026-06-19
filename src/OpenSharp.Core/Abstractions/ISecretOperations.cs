using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Abstractions;

/// <summary>Operations for Kubernetes <c>Secret</c> resources.</summary>
public interface ISecretOperations : IReadOperations<Secret>, IWriteOperations<Secret>, IWatchable<Secret>
{
}
