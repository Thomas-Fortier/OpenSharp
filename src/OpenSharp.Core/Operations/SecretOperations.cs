using System.Runtime.CompilerServices;
using System.Text.Json;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Operations;

/// <summary>
/// Implements <see cref="ISecretOperations"/> against the core Kubernetes Secret API.
/// Secret values are never logged.
/// </summary>
internal sealed class SecretOperations : WriteOperationsBase<Secret>, ISecretOperations
{
    public SecretOperations(IKubernetes k8s, OpenShiftClientOptions options, ILogger logger)
        : base(k8s, options, logger) { }

    public override async Task<Secret> GetAsync(string name, string? @namespace = null, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var result = await ExecuteAsync(() => K8s.CoreV1.ReadNamespacedSecretAsync(name, ns, cancellationToken: ct), name)
            .ConfigureAwait(false);
        return Map(result);
    }

    public override async Task<PagedList<Secret>> ListAsync(
        string? @namespace = null, int? limit = null, string? continueToken = null,
        string? labelSelector = null, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var list = await ExecuteAsync(
            () => K8s.CoreV1.ListNamespacedSecretAsync(ns, limit: limit, continueParameter: continueToken,
                labelSelector: labelSelector, cancellationToken: ct)).ConfigureAwait(false);
        return new PagedList<Secret>
        {
            Items = list.Items.Select(Map).ToList(),
            ContinueToken = string.IsNullOrEmpty(list.Metadata?.ContinueProperty) ? null : list.Metadata.ContinueProperty,
        };
    }

    public override async Task<Secret> CreateAsync(Secret resource, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(resource.Metadata.Namespace);
        var result = await ExecuteAsync(
            () => K8s.CoreV1.CreateNamespacedSecretAsync(ToV1(resource), ns, cancellationToken: ct),
            resource.Metadata.Name).ConfigureAwait(false);
        return Map(result);
    }

    public override async Task<Secret> ReplaceAsync(Secret resource, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(resource.Metadata.Namespace);
        var result = await ExecuteAsync(
            () => K8s.CoreV1.ReplaceNamespacedSecretAsync(ToV1(resource), resource.Metadata.Name, ns, cancellationToken: ct),
            resource.Metadata.Name).ConfigureAwait(false);
        return Map(result);
    }

    public override async Task<Secret> PatchAsync(string name, string? @namespace, JsonDocument patch, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var result = await ExecuteAsync(
            () => K8s.CoreV1.PatchNamespacedSecretAsync(
                new V1Patch(patch.RootElement.GetRawText(), V1Patch.PatchType.MergePatch), name, ns, cancellationToken: ct),
            name).ConfigureAwait(false);
        return Map(result);
    }

    public override Task DeleteAsync(string name, string? @namespace, DeleteOptions options, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        return ExecuteAsync(
            () => K8s.CoreV1.DeleteNamespacedSecretAsync(name, ns,
                gracePeriodSeconds: EffectiveGracePeriod(options),
                propagationPolicy: ToK8sPropagation(options.Propagation), cancellationToken: ct),
            name);
    }

    protected override IAsyncEnumerable<WatchEvent<Secret>> WatchCoreAsync(
        string? @namespace, string? labelSelector, string? resourceVersion, CancellationToken ct)
    {
        var ns = ResolveNamespace(@namespace);
        return WatchCoreInner(ns, labelSelector, resourceVersion, ct);
    }

    private async IAsyncEnumerable<WatchEvent<Secret>> WatchCoreInner(
        string @namespace, string? labelSelector, string? resourceVersion,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var (type, s) in K8s.CoreV1.ListNamespacedSecretWithHttpMessagesAsync(
            @namespace, labelSelector: labelSelector, resourceVersion: resourceVersion,
            watch: true, cancellationToken: ct).WatchAsync<V1Secret, V1SecretList>(cancellationToken: ct))
        {
            yield return new WatchEvent<Secret>
            {
                Type = type switch
                {
                    k8s.WatchEventType.Added => Abstractions.WatchEventType.Added,
                    k8s.WatchEventType.Modified => Abstractions.WatchEventType.Modified,
                    k8s.WatchEventType.Deleted => Abstractions.WatchEventType.Deleted,
                    k8s.WatchEventType.Bookmark => Abstractions.WatchEventType.Bookmark,
                    _ => Abstractions.WatchEventType.Error,
                },
                Resource = Map(s),
            };
        }
    }

    protected override string? GetResourceVersion(Secret resource) => resource.Metadata.ResourceVersion;

    private static Secret Map(V1Secret s) => new()
    {
        Metadata = new Resources.ResourceMetadata
        {
            Name = s.Metadata?.Name ?? string.Empty,
            Namespace = s.Metadata?.NamespaceProperty,
            Uid = s.Metadata?.Uid,
            ResourceVersion = s.Metadata?.ResourceVersion,
            Labels = (IReadOnlyDictionary<string, string>?)s.Metadata?.Labels ?? new Dictionary<string, string>(),
            Annotations = (IReadOnlyDictionary<string, string>?)s.Metadata?.Annotations ?? new Dictionary<string, string>(),
            CreationTimestamp = s.Metadata?.CreationTimestamp,
        },
        Type = s.Type ?? "Opaque",
        Data = s.Data?.ToDictionary(k => k.Key, v => v.Value) ?? new Dictionary<string, byte[]>(),
    };

    private static V1Secret ToV1(Secret s) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = s.Metadata.Name,
            NamespaceProperty = s.Metadata.Namespace,
            ResourceVersion = s.Metadata.ResourceVersion,
        },
        Type = s.Type,
        Data = s.Data.Count > 0 ? s.Data.ToDictionary() : null,
    };
}
