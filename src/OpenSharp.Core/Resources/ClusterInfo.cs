namespace OpenSharp.Core.Resources;

/// <summary>Summary information about the connected cluster.</summary>
public sealed class ClusterInfo
{
    /// <summary>The API server endpoint the client is connected to.</summary>
    public required string ApiServerEndpoint { get; init; }

    /// <summary>The cluster's reported server version (e.g. <c>v1.28.3</c>).</summary>
    public required string ServerVersion { get; init; }

    /// <summary>Whether the API server responded successfully when the information was retrieved.</summary>
    public bool Reachable { get; init; }
}
