using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Abstractions;

/// <summary>Operations for OpenShift <c>Project</c> resources.</summary>
public interface IProjectOperations : IReadOperations<Project>, IWriteOperations<Project>, IWatchable<Project>
{
}
