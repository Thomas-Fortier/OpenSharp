using System.Runtime.CompilerServices;
using System.Text.Json;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Operations;

/// <summary>Implements <see cref="IConfigMapOperations"/> against the core Kubernetes ConfigMap API.</summary>
internal sealed class ConfigMapOperations : WriteOperationsBase<ConfigMap>, IConfigMapOperations
{
    public ConfigMapOperations(IKubernetes k8s, OpenShiftClientOptions options, ILogger logger)
        : base(k8s, options, logger) { }

    public override async Task<ConfigMap> GetAsync(string name, string? @namespace = null, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var result = await ExecuteAsync(() => K8s.CoreV1.ReadNamespacedConfigMapAsync(name, ns, cancellationToken: ct), name)
            .ConfigureAwait(false);
        return Map(result);
    }

    public override async Task<PagedList<ConfigMap>> ListAsync(
        string? @namespace = null, int? limit = null, string? continueToken = null,
        string? labelSelector = null, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var list = await ExecuteAsync(
            () => K8s.CoreV1.ListNamespacedConfigMapAsync(ns, limit: limit, continueParameter: continueToken,
                labelSelector: labelSelector, cancellationToken: ct)).ConfigureAwait(false);
        return new PagedList<ConfigMap>
        {
            Items = list.Items.Select(Map).ToList(),
            ContinueToken = string.IsNullOrEmpty(list.Metadata?.ContinueProperty) ? null : list.Metadata.ContinueProperty,
        };
    }

    public override async Task<ConfigMap> CreateAsync(ConfigMap resource, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(resource.Metadata.Namespace);
        var result = await ExecuteAsync(
            () => K8s.CoreV1.CreateNamespacedConfigMapAsync(ToV1(resource), ns, cancellationToken: ct),
            resource.Metadata.Name).ConfigureAwait(false);
        return Map(result);
    }

    public override async Task<ConfigMap> ReplaceAsync(ConfigMap resource, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(resource.Metadata.Namespace);
        var result = await ExecuteAsync(
            () => K8s.CoreV1.ReplaceNamespacedConfigMapAsync(ToV1(resource), resource.Metadata.Name, ns, cancellationToken: ct),
            resource.Metadata.Name).ConfigureAwait(false);
        return Map(result);
    }

    public override async Task<ConfigMap> PatchAsync(string name, string? @namespace, JsonDocument patch, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var result = await ExecuteAsync(
            () => K8s.CoreV1.PatchNamespacedConfigMapAsync(
                new V1Patch(patch.RootElement.GetRawText(), V1Patch.PatchType.MergePatch), name, ns, cancellationToken: ct),
            name).ConfigureAwait(false);
        return Map(result);
    }

    public override Task DeleteAsync(string name, string? @namespace, DeleteOptions options, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        return ExecuteAsync(
            () => K8s.CoreV1.DeleteNamespacedConfigMapAsync(name, ns,
                gracePeriodSeconds: EffectiveGracePeriod(options),
                propagationPolicy: ToK8sPropagation(options.Propagation), cancellationToken: ct),
            name);
    }

    protected override IAsyncEnumerable<WatchEvent<ConfigMap>> WatchCoreAsync(
        string? @namespace, string? labelSelector, string? resourceVersion, CancellationToken ct)
    {
        var ns = ResolveNamespace(@namespace);
        return WatchCoreInner(ns, labelSelector, resourceVersion, ct);
    }

    private async IAsyncEnumerable<WatchEvent<ConfigMap>> WatchCoreInner(
        string @namespace, string? labelSelector, string? resourceVersion,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var (type, cm) in K8s.CoreV1.ListNamespacedConfigMapWithHttpMessagesAsync(
            @namespace, labelSelector: labelSelector, resourceVersion: resourceVersion,
            watch: true, cancellationToken: ct).WatchAsync<V1ConfigMap, V1ConfigMapList>(cancellationToken: ct))
        {
            yield return new WatchEvent<ConfigMap>
            {
                Type = type switch
                {
                    k8s.WatchEventType.Added => Abstractions.WatchEventType.Added,
                    k8s.WatchEventType.Modified => Abstractions.WatchEventType.Modified,
                    k8s.WatchEventType.Deleted => Abstractions.WatchEventType.Deleted,
                    k8s.WatchEventType.Bookmark => Abstractions.WatchEventType.Bookmark,
                    _ => Abstractions.WatchEventType.Error,
                },
                Resource = Map(cm),
            };
        }
    }

    protected override string? GetResourceVersion(ConfigMap resource) => resource.Metadata.ResourceVersion;

    private static ConfigMap Map(V1ConfigMap cm) => new()
    {
        Metadata = new Resources.ResourceMetadata
        {
            Name = cm.Metadata?.Name ?? string.Empty,
            Namespace = cm.Metadata?.NamespaceProperty,
            Uid = cm.Metadata?.Uid,
            ResourceVersion = cm.Metadata?.ResourceVersion,
            Labels = (IReadOnlyDictionary<string, string>?)cm.Metadata?.Labels ?? new Dictionary<string, string>(),
            Annotations = (IReadOnlyDictionary<string, string>?)cm.Metadata?.Annotations ?? new Dictionary<string, string>(),
            CreationTimestamp = cm.Metadata?.CreationTimestamp,
        },
        Data = (IReadOnlyDictionary<string, string>?)cm.Data ?? new Dictionary<string, string>(),
        BinaryData = cm.BinaryData?.ToDictionary(k => k.Key, v => v.Value) ?? new Dictionary<string, byte[]>(),
    };

    private static V1ConfigMap ToV1(ConfigMap cm) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = cm.Metadata.Name,
            NamespaceProperty = cm.Metadata.Namespace,
            ResourceVersion = cm.Metadata.ResourceVersion,
        },
        Data = cm.Data.Count > 0 ? cm.Data.ToDictionary() : null,
    };
}
