namespace OpenSharp.Core.Resources;

/// <summary>Common identity and metadata shared by all OpenShift/Kubernetes resources.</summary>
public sealed class ResourceMetadata
{
    /// <summary>The resource name, unique within its namespace.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// The project or namespace containing the resource. <see langword="null"/> for
    /// cluster-scoped resources such as <c>Project</c>.
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>Server-assigned unique identifier for this resource.</summary>
    public string? Uid { get; init; }

    /// <summary>
    /// Opaque token used for optimistic concurrency control. Supply this when replacing
    /// a resource to guard against concurrent updates.
    /// </summary>
    public string? ResourceVersion { get; init; }

    /// <summary>Labels attached to the resource, keyed by label name.</summary>
    public IReadOnlyDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>();

    /// <summary>Annotations attached to the resource, keyed by annotation name.</summary>
    public IReadOnlyDictionary<string, string> Annotations { get; init; } = new Dictionary<string, string>();

    /// <summary>Timestamp at which the resource was created on the server.</summary>
    public DateTimeOffset? CreationTimestamp { get; init; }
}
