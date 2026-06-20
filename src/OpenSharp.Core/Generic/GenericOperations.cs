using System.Text.Json;
using System.Text.Json.Serialization;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Operations;
using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Generic;

/// <summary>
/// Implements <see cref="IGenericOperations"/> using the Kubernetes custom-objects API,
/// allowing any resource type to be accessed by API group, version, and plural name.
/// </summary>
internal sealed class GenericOperations : OperationBase, IGenericOperations
{
    public GenericOperations(IKubernetes k8s, OpenShiftClientOptions options, ILogger logger)
        : base(k8s, options, logger) { }

    /// <inheritdoc/>
    public async Task<JsonElement> GetAsync(GenericResourceRef reference, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(reference.Group))
            return await GetCoreAsync(reference, ct).ConfigureAwait(false);

        object result;
        if (reference.Namespace is not null)
        {
            result = await ExecuteAsync(
                () => K8s.CustomObjects.GetNamespacedCustomObjectAsync(
                    reference.Group, reference.Version, reference.Namespace, reference.Plural,
                    reference.Name!, ct),
                reference.Name).ConfigureAwait(false);
        }
        else
        {
            result = await ExecuteAsync(
                () => K8s.CustomObjects.GetClusterCustomObjectAsync(
                    reference.Group, reference.Version, reference.Plural, reference.Name!, ct),
                reference.Name).ConfigureAwait(false);
        }

        return ToJsonElement(result);
    }

    /// <inheritdoc/>
    public async Task<PagedList<JsonElement>> ListAsync(
        GenericResourceRef reference, int? limit = null, string? continueToken = null,
        string? labelSelector = null, string? fieldSelector = null, CancellationToken ct = default)
    {
        object result;
        if (reference.Namespace is not null)
        {
            result = await ExecuteAsync(
                () => K8s.CustomObjects.ListNamespacedCustomObjectAsync(
                    reference.Group, reference.Version, reference.Namespace, reference.Plural,
                    limit: limit, continueParameter: continueToken,
                    labelSelector: labelSelector, fieldSelector: fieldSelector, cancellationToken: ct)).ConfigureAwait(false);
        }
        else
        {
            result = await ExecuteAsync(
                () => K8s.CustomObjects.ListClusterCustomObjectAsync(
                    reference.Group, reference.Version, reference.Plural,
                    limit: limit, continueParameter: continueToken,
                    labelSelector: labelSelector, fieldSelector: fieldSelector, cancellationToken: ct)).ConfigureAwait(false);
        }

        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var items = root.TryGetProperty("items", out var el)
            ? el.EnumerateArray().Select(i => i.Clone()).ToList()
            : new List<JsonElement>();
        var cont = root.TryGetProperty("metadata", out var m) && m.TryGetProperty("continue", out var c) ? c.GetString() : null;
        return new PagedList<JsonElement> { Items = items, ContinueToken = string.IsNullOrEmpty(cont) ? null : cont };
    }

    /// <inheritdoc/>
    public async Task<JsonElement> CreateAsync(GenericResourceRef reference, JsonElement body, CancellationToken ct = default)
    {
        object result;
        if (reference.Namespace is not null)
        {
            result = await ExecuteAsync(
                () => K8s.CustomObjects.CreateNamespacedCustomObjectAsync(
                    (object)body, reference.Group, reference.Version, reference.Namespace, reference.Plural,
                    cancellationToken: ct)).ConfigureAwait(false);
        }
        else
        {
            result = await ExecuteAsync(
                () => K8s.CustomObjects.CreateClusterCustomObjectAsync(
                    (object)body, reference.Group, reference.Version, reference.Plural,
                    cancellationToken: ct)).ConfigureAwait(false);
        }

        return ToJsonElement(result);
    }

    /// <inheritdoc/>
    public async Task<JsonElement> PatchAsync(
        GenericResourceRef reference, JsonDocument patch, PatchType type = PatchType.Merge, CancellationToken ct = default)
    {
        var v1patch = new V1Patch(patch.RootElement.GetRawText(), ToK8sPatchType(type));
        object result;
        if (reference.Namespace is not null)
        {
            result = await ExecuteAsync(
                () => K8s.CustomObjects.PatchNamespacedCustomObjectAsync(
                    v1patch, reference.Group, reference.Version, reference.Namespace, reference.Plural,
                    reference.Name!, cancellationToken: ct),
                reference.Name).ConfigureAwait(false);
        }
        else
        {
            result = await ExecuteAsync(
                () => K8s.CustomObjects.PatchClusterCustomObjectAsync(
                    v1patch, reference.Group, reference.Version, reference.Plural, reference.Name!, cancellationToken: ct),
                reference.Name).ConfigureAwait(false);
        }

        return ToJsonElement(result);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(GenericResourceRef reference, CancellationToken ct = default) =>
        DeleteAsync(reference, new DeleteOptions(), ct);

    /// <inheritdoc/>
    public async Task DeleteAsync(GenericResourceRef reference, DeleteOptions options, CancellationToken ct = default)
    {
        var grace = options.Force ? 0 : options.GracePeriodSeconds;
        var prop = MapPropagation(options.Propagation);
        if (reference.Namespace is not null)
        {
            await ExecuteAsync(
                () => K8s.CustomObjects.DeleteNamespacedCustomObjectAsync(
                    reference.Group, reference.Version, reference.Namespace, reference.Plural, reference.Name!,
                    gracePeriodSeconds: grace, propagationPolicy: prop, cancellationToken: ct),
                reference.Name).ConfigureAwait(false);
        }
        else
        {
            await ExecuteAsync(
                () => K8s.CustomObjects.DeleteClusterCustomObjectAsync(
                    reference.Group, reference.Version, reference.Plural, reference.Name!,
                    gracePeriodSeconds: grace, propagationPolicy: prop, cancellationToken: ct),
                reference.Name).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reaches a resource in the core (legacy) API group, whose address is <c>/api/{version}/…</c>
    /// rather than <c>/apis/{group}/{version}/…</c>, using the Kubernetes <see cref="GenericClient"/>.
    /// </summary>
    private async Task<JsonElement> GetCoreAsync(GenericResourceRef reference, CancellationToken ct)
    {
        using var gc = new GenericClient(K8s, reference.Group, reference.Version, reference.Plural);
        var raw = reference.Namespace is not null
            ? await ExecuteAsync(() => gc.ReadNamespacedAsync<RawKubernetesObject>(reference.Namespace, reference.Name!, ct), reference.Name).ConfigureAwait(false)
            : await ExecuteAsync(() => gc.ReadAsync<RawKubernetesObject>(reference.Name!, ct), reference.Name).ConfigureAwait(false);
        return ToJsonElement(raw);
    }

    /// <summary>
    /// Minimal <see cref="IKubernetesObject"/> used to deserialize an arbitrary resource through
    /// <see cref="GenericClient"/> (which requires a Kubernetes object type). All fields beyond
    /// apiVersion/kind are captured via JSON extension data and reconstructed on re-serialization.
    /// </summary>
    private sealed class RawKubernetesObject : IKubernetesObject
    {
        [JsonPropertyName("apiVersion")] public string ApiVersion { get; set; } = string.Empty;
        [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;
        [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; } = new();
    }

    /// <summary>Maps the library <see cref="PatchType"/> to the Kubernetes client patch type.</summary>
    internal static V1Patch.PatchType ToK8sPatchType(PatchType type) => type switch
    {
        PatchType.Json => V1Patch.PatchType.JsonPatch,
        PatchType.StrategicMerge => V1Patch.PatchType.StrategicMergePatch,
        _ => V1Patch.PatchType.MergePatch,
    };

    private static string MapPropagation(DeletePropagationPolicy policy) => policy switch
    {
        DeletePropagationPolicy.Foreground => "Foreground",
        DeletePropagationPolicy.Orphan => "Orphan",
        _ => "Background",
    };

    private static JsonElement ToJsonElement(object raw)
    {
        var json = JsonSerializer.Serialize(raw);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
