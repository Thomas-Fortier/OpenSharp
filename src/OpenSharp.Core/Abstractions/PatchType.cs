namespace OpenSharp.Core.Abstractions;

/// <summary>
/// The strategy used to apply a patch document to a resource. Maps to the corresponding
/// Kubernetes patch content types.
/// </summary>
public enum PatchType
{
    /// <summary>JSON Merge Patch (RFC 7386) — the default, simplest partial update.</summary>
    Merge,

    /// <summary>JSON Merge Patch sent with the strict merge content type.</summary>
    JsonMerge,

    /// <summary>Kubernetes strategic merge patch, honouring resource-specific merge keys.</summary>
    StrategicMerge,

    /// <summary>JSON Patch (RFC 6902) — an ordered list of operations.</summary>
    Json,
}
