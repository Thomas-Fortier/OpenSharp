using System.Runtime.CompilerServices;
using System.Text.Json;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Operations;

/// <summary>Implements <see cref="IServiceOperations"/> against the core Kubernetes service API.</summary>
internal sealed class ServiceOperations : WriteOperationsBase<Service>, IServiceOperations
{
    public ServiceOperations(IKubernetes k8s, OpenShiftClientOptions options, ILogger logger)
        : base(k8s, options, logger) { }

    public override async Task<Service> GetAsync(string name, string? @namespace = null, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var svc = await ExecuteAsync(() => K8s.CoreV1.ReadNamespacedServiceAsync(name, ns, cancellationToken: ct), name)
            .ConfigureAwait(false);
        return Map(svc);
    }

    public override async Task<PagedList<Service>> ListAsync(
        string? @namespace = null, int? limit = null, string? continueToken = null,
        string? labelSelector = null, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var list = await ExecuteAsync(
            () => K8s.CoreV1.ListNamespacedServiceAsync(ns, limit: limit, continueParameter: continueToken,
                labelSelector: labelSelector, cancellationToken: ct)).ConfigureAwait(false);
        return new PagedList<Service>
        {
            Items = list.Items.Select(Map).ToList(),
            ContinueToken = string.IsNullOrEmpty(list.Metadata?.ContinueProperty) ? null : list.Metadata.ContinueProperty,
        };
    }

    public override async Task<Service> CreateAsync(Service resource, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(resource.Metadata.Namespace);
        var result = await ExecuteAsync(
            () => K8s.CoreV1.CreateNamespacedServiceAsync(ToV1(resource), ns, cancellationToken: ct),
            resource.Metadata.Name).ConfigureAwait(false);
        return Map(result);
    }

    public override async Task<Service> ReplaceAsync(Service resource, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(resource.Metadata.Namespace);
        var result = await ExecuteAsync(
            () => K8s.CoreV1.ReplaceNamespacedServiceAsync(ToV1(resource), resource.Metadata.Name, ns, cancellationToken: ct),
            resource.Metadata.Name).ConfigureAwait(false);
        return Map(result);
    }

    public override async Task<Service> PatchAsync(string name, string? @namespace, JsonDocument patch, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var result = await ExecuteAsync(
            () => K8s.CoreV1.PatchNamespacedServiceAsync(
                new V1Patch(patch.RootElement.GetRawText(), V1Patch.PatchType.MergePatch), name, ns, cancellationToken: ct),
            name).ConfigureAwait(false);
        return Map(result);
    }

    public override Task DeleteAsync(string name, string? @namespace = null,
        DeletePropagationPolicy propagation = DeletePropagationPolicy.Background, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        return ExecuteAsync(
            () => K8s.CoreV1.DeleteNamespacedServiceAsync(name, ns,
                propagationPolicy: ToK8sPropagation(propagation), cancellationToken: ct),
            name);
    }

    protected override IAsyncEnumerable<WatchEvent<Service>> WatchCoreAsync(
        string? @namespace, string? labelSelector, string? resourceVersion, CancellationToken ct)
    {
        var ns = ResolveNamespace(@namespace);
        return WatchServicesCoreAsync(ns, labelSelector, resourceVersion, ct);
    }

    private async IAsyncEnumerable<WatchEvent<Service>> WatchServicesCoreAsync(
        string @namespace, string? labelSelector, string? resourceVersion,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var (type, svc) in K8s.CoreV1.ListNamespacedServiceWithHttpMessagesAsync(
            @namespace, labelSelector: labelSelector, resourceVersion: resourceVersion,
            watch: true, cancellationToken: ct).WatchAsync<V1Service, V1ServiceList>(cancellationToken: ct))
        {
            yield return new WatchEvent<Service>
            {
                Type = type switch
                {
                    k8s.WatchEventType.Added => Abstractions.WatchEventType.Added,
                    k8s.WatchEventType.Modified => Abstractions.WatchEventType.Modified,
                    k8s.WatchEventType.Deleted => Abstractions.WatchEventType.Deleted,
                    k8s.WatchEventType.Bookmark => Abstractions.WatchEventType.Bookmark,
                    _ => Abstractions.WatchEventType.Error,
                },
                Resource = Map(svc),
            };
        }
    }

    protected override string? GetResourceVersion(Service resource) => resource.Metadata.ResourceVersion;

    private static Service Map(V1Service svc) => new()
    {
        Metadata = new Resources.ResourceMetadata
        {
            Name = svc.Metadata?.Name ?? string.Empty,
            Namespace = svc.Metadata?.NamespaceProperty,
            Uid = svc.Metadata?.Uid,
            ResourceVersion = svc.Metadata?.ResourceVersion,
            Labels = (IReadOnlyDictionary<string, string>?)svc.Metadata?.Labels ?? new Dictionary<string, string>(),
            Annotations = (IReadOnlyDictionary<string, string>?)svc.Metadata?.Annotations ?? new Dictionary<string, string>(),
            CreationTimestamp = svc.Metadata?.CreationTimestamp,
        },
        Type = svc.Spec?.Type ?? "ClusterIP",
        ClusterIp = svc.Spec?.ClusterIP,
        Ports = svc.Spec?.Ports?.Select(p => new ServicePort
        {
            Name = p.Name,
            Port = p.Port,
            TargetPort = p.TargetPort?.Value,
            Protocol = p.Protocol ?? "TCP",
        }).ToList() ?? [],
    };

    private static V1Service ToV1(Service svc) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = svc.Metadata.Name,
            NamespaceProperty = svc.Metadata.Namespace,
            ResourceVersion = svc.Metadata.ResourceVersion,
        },
        Spec = new V1ServiceSpec { Type = svc.Type },
    };
}
