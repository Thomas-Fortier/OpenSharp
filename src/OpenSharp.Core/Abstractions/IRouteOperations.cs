using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Abstractions;

/// <summary>Operations for OpenShift <c>Route</c> resources.</summary>
public interface IRouteOperations : IReadOperations<Route>, IWriteOperations<Route>, IWatchable<Route>
{
}
