namespace OpenSharp.Core.Resources;

/// <summary>
/// An OpenShift Route (<c>route.openshift.io/v1</c>) that exposes a Service to external
/// traffic at a specified hostname and optional path.
/// </summary>
public sealed class Route
{
    /// <summary>Standard Kubernetes/OpenShift metadata for this route.</summary>
    public required ResourceMetadata Metadata { get; init; }

    /// <summary>
    /// The external hostname the route is reachable at, e.g.
    /// <c>myapp.apps.cluster.example.com</c>.
    /// </summary>
    public required string Host { get; init; }

    /// <summary>Optional URL path prefix; traffic is matched only for this path if set.</summary>
    public string? Path { get; init; }

    /// <summary>The backend service this route forwards traffic to.</summary>
    public required RouteTarget To { get; init; }

    /// <summary>Named port on the target service to forward to, if specified.</summary>
    public string? Port { get; init; }

    /// <summary>
    /// TLS termination strategy: <c>edge</c>, <c>passthrough</c>, <c>reencrypt</c>, or
    /// <see langword="null"/> for plain HTTP.
    /// </summary>
    public string? TlsTermination { get; init; }
}
