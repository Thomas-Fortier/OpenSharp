namespace OpenSharp.Core.Resources;

/// <summary>
/// A Kubernetes Secret holding sensitive data. Secret values are never logged by the library.
/// </summary>
public sealed class Secret
{
    /// <summary>Standard Kubernetes metadata for this secret.</summary>
    public required ResourceMetadata Metadata { get; init; }

    /// <summary>
    /// Secret type, e.g. <c>Opaque</c>, <c>kubernetes.io/tls</c>,
    /// <c>kubernetes.io/service-account-token</c>.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>Base64-decoded key-value pairs stored in this secret.</summary>
    public IReadOnlyDictionary<string, byte[]> Data { get; init; } = new Dictionary<string, byte[]>();
}
