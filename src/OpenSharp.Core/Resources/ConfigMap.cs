namespace OpenSharp.Core.Resources;

/// <summary>A Kubernetes ConfigMap holding non-sensitive configuration data.</summary>
public sealed class ConfigMap
{
    /// <summary>Standard Kubernetes metadata for this config map.</summary>
    public required ResourceMetadata Metadata { get; init; }

    /// <summary>UTF-8 string key-value pairs stored in this config map.</summary>
    public IReadOnlyDictionary<string, string> Data { get; init; } = new Dictionary<string, string>();

    /// <summary>Binary key-value pairs stored in this config map.</summary>
    public IReadOnlyDictionary<string, byte[]> BinaryData { get; init; } = new Dictionary<string, byte[]>();
}
