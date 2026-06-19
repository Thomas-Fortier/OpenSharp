namespace OpenSharp.Core.Resources;

/// <summary>A port exposed by a Kubernetes Service.</summary>
public sealed class ServicePort
{
    /// <summary>Optional name for this port, required when the service exposes multiple ports.</summary>
    public string? Name { get; init; }

    /// <summary>Port number the service is accessible on.</summary>
    public int Port { get; init; }

    /// <summary>Port or named port on the pod to forward traffic to.</summary>
    public string? TargetPort { get; init; }

    /// <summary>IP protocol, e.g. <c>TCP</c> or <c>UDP</c>.</summary>
    public required string Protocol { get; init; }
}
