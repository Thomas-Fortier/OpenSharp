namespace OpenSharp.Core.Resources;

/// <summary>Describes the backend service an OpenShift Route forwards traffic to.</summary>
public sealed class RouteTarget
{
    /// <summary>Kind of the target object, typically <c>Service</c>.</summary>
    public required string Kind { get; init; }

    /// <summary>Name of the target service.</summary>
    public required string Name { get; init; }

    /// <summary>Relative weight when multiple targets are specified. <see langword="null"/> when only one target exists.</summary>
    public int? Weight { get; init; }
}
