namespace OpenSharp.Core.Resources;

/// <summary>Runtime state of a single container within a pod.</summary>
public sealed class ContainerInfo
{
    /// <summary>Name of the container as declared in the pod spec.</summary>
    public required string Name { get; init; }

    /// <summary>Container image reference including tag or digest.</summary>
    public required string Image { get; init; }

    /// <summary>Whether the container has passed its readiness probe.</summary>
    public bool Ready { get; init; }

    /// <summary>Number of times the container has been restarted.</summary>
    public int RestartCount { get; init; }

    /// <summary>Human-readable current state, e.g. <c>Running</c>, <c>Waiting</c>, <c>Terminated</c>.</summary>
    public required string State { get; init; }
}
