namespace OpenSharp.Core.Generic;

/// <summary>
/// Identifies an API resource by its group/version/plural and optional scope, for use
/// with the generic escape-hatch API when the resource type does not have a first-class
/// implementation.
/// </summary>
public sealed class GenericResourceRef
{
    /// <summary>The API group, e.g. <c>apps.openshift.io</c>. Empty string for core resources.</summary>
    public required string Group { get; init; }

    /// <summary>The API version, e.g. <c>v1</c>.</summary>
    public required string Version { get; init; }

    /// <summary>The plural resource name, e.g. <c>deploymentconfigs</c>.</summary>
    public required string Plural { get; init; }

    /// <summary>The project or namespace scope, or <see langword="null"/> for cluster-scoped resources.</summary>
    public string? Namespace { get; init; }

    /// <summary>The resource name for single-resource operations, or <see langword="null"/> for list operations.</summary>
    public string? Name { get; init; }
}
