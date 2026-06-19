namespace OpenSharp.Core.Resources;

/// <summary>
/// An OpenShift project, which is a namespace enriched with OpenShift-specific metadata
/// such as a display name and description.
/// </summary>
public sealed class Project
{
    /// <summary>Standard Kubernetes/OpenShift metadata for this project.</summary>
    public required ResourceMetadata Metadata { get; init; }

    /// <summary>Human-readable display name for the project.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Optional longer description of the project's purpose.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Current lifecycle phase of the project, e.g. <c>Active</c> or <c>Terminating</c>.
    /// </summary>
    public string? Status { get; init; }
}
