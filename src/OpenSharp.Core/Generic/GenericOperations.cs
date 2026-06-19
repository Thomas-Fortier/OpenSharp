using System.Text.Json;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Operations;

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
        GenericResourceRef reference, int? limit = null, string? continueToken = null, CancellationToken ct = default)
    {
        object result;
        if (reference.Namespace is not null)
        {
            result = await ExecuteAsync(
                () => K8s.CustomObjects.ListNamespacedCustomObjectAsync(
                    reference.Group, reference.Version, reference.Namespace, reference.Plural,
                    limit: limit, continueParameter: continueToken, cancellationToken: ct)).ConfigureAwait(false);
        }
        else
        {
            result = await ExecuteAsync(
                () => K8s.CustomObjects.ListClusterCustomObjectAsync(
                    reference.Group, reference.Version, reference.Plural,
                    limit: limit, continueParameter: continueToken, cancellationToken: ct)).ConfigureAwait(false);
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
    public async Task DeleteAsync(GenericResourceRef reference, CancellationToken ct = default)
    {
        if (reference.Namespace is not null)
        {
            await ExecuteAsync(
                () => K8s.CustomObjects.DeleteNamespacedCustomObjectAsync(
                    reference.Group, reference.Version, reference.Namespace, reference.Plural,
                    reference.Name!, cancellationToken: ct),
                reference.Name).ConfigureAwait(false);
        }
        else
        {
            await ExecuteAsync(
                () => K8s.CustomObjects.DeleteClusterCustomObjectAsync(
                    reference.Group, reference.Version, reference.Plural,
                    reference.Name!, cancellationToken: ct),
                reference.Name).ConfigureAwait(false);
        }
    }

    private static JsonElement ToJsonElement(object raw)
    {
        var json = JsonSerializer.Serialize(raw);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
