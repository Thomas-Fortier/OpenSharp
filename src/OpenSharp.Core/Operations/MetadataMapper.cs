using System.Text.Json;
using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Operations;

/// <summary>Extracts a <see cref="ResourceMetadata"/> from a raw JSON element.</summary>
internal static class MetadataMapper
{
    /// <summary>Maps the <c>metadata</c> section of a Kubernetes/OpenShift JSON object.</summary>
    public static ResourceMetadata Map(JsonElement root)
    {
        if (!root.TryGetProperty("metadata", out var meta))
            return new ResourceMetadata { Name = string.Empty };

        return new ResourceMetadata
        {
            Name = meta.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty,
            Namespace = meta.TryGetProperty("namespace", out var ns) ? ns.GetString() : null,
            Uid = meta.TryGetProperty("uid", out var uid) ? uid.GetString() : null,
            ResourceVersion = meta.TryGetProperty("resourceVersion", out var rv) ? rv.GetString() : null,
            Labels = ReadStringDict(meta, "labels"),
            Annotations = ReadStringDict(meta, "annotations"),
            CreationTimestamp = meta.TryGetProperty("creationTimestamp", out var ts) &&
                                DateTimeOffset.TryParse(ts.GetString(), out var dto) ? dto : null,
        };
    }

    private static IReadOnlyDictionary<string, string> ReadStringDict(JsonElement parent, string key)
    {
        if (!parent.TryGetProperty(key, out var el) || el.ValueKind != JsonValueKind.Object)
            return new Dictionary<string, string>();

        return el.EnumerateObject()
                 .Where(p => p.Value.ValueKind == JsonValueKind.String)
                 .ToDictionary(p => p.Name, p => p.Value.GetString()!);
    }
}
