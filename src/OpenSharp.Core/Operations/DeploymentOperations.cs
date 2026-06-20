using System.Runtime.CompilerServices;
using System.Text.Json;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Errors;
using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Operations;

/// <summary>
/// Implements <see cref="IWorkloadOperations"/> for both <c>apps/v1 Deployment</c> and
/// <c>apps.openshift.io/v1 DeploymentConfig</c>. The <c>isDeploymentConfig</c>
/// constructor flag selects which API to target.
/// </summary>
internal sealed class DeploymentOperations : WriteOperationsBase<Deployment>, IWorkloadOperations
{
    private readonly bool _isDc;
    private const string DcGroup = "apps.openshift.io";
    private const string DcVersion = "v1";
    private const string DcPlural = "deploymentconfigs";

    public DeploymentOperations(IKubernetes k8s, OpenShiftClientOptions options, ILogger logger, bool isDeploymentConfig)
        : base(k8s, options, logger) => _isDc = isDeploymentConfig;

    public override async Task<Deployment> GetAsync(string name, string? @namespace = null, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        if (_isDc)
        {
            var obj = await ExecuteAsync(
                () => K8s.CustomObjects.GetNamespacedCustomObjectAsync(DcGroup, DcVersion, ns, DcPlural, name, ct),
                name).ConfigureAwait(false);
            return MapDcFromJson(obj);
        }

        var dep = await ExecuteAsync(
            () => K8s.AppsV1.ReadNamespacedDeploymentAsync(name, ns, cancellationToken: ct), name)
            .ConfigureAwait(false);
        return MapDeployment(dep);
    }

    public override async Task<PagedList<Deployment>> ListAsync(
        string? @namespace = null, int? limit = null, string? continueToken = null,
        string? labelSelector = null, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        if (_isDc)
        {
            var obj = await ExecuteAsync(
                () => K8s.CustomObjects.ListNamespacedCustomObjectAsync(DcGroup, DcVersion, ns, DcPlural,
                    limit: limit, continueParameter: continueToken, labelSelector: labelSelector,
                    cancellationToken: ct)).ConfigureAwait(false);
            return MapDcList(obj);
        }

        var list = await ExecuteAsync(
            () => K8s.AppsV1.ListNamespacedDeploymentAsync(ns, limit: limit, continueParameter: continueToken,
                labelSelector: labelSelector, cancellationToken: ct)).ConfigureAwait(false);
        return new PagedList<Deployment>
        {
            Items = list.Items.Select(MapDeployment).ToList(),
            ContinueToken = string.IsNullOrEmpty(list.Metadata?.ContinueProperty) ? null : list.Metadata.ContinueProperty,
        };
    }

    public override async Task<Deployment> CreateAsync(Deployment resource, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(resource.Metadata.Namespace);
        if (_isDc)
        {
            var result = await ExecuteAsync(
                () => K8s.CustomObjects.CreateNamespacedCustomObjectAsync(ToDcJson(resource), DcGroup, DcVersion, ns, DcPlural,
                    cancellationToken: ct),
                resource.Metadata.Name).ConfigureAwait(false);
            return MapDcFromJson(result);
        }
        var dep = await ExecuteAsync(
            () => K8s.AppsV1.CreateNamespacedDeploymentAsync(ToV1Deployment(resource), ns, cancellationToken: ct),
            resource.Metadata.Name).ConfigureAwait(false);
        return MapDeployment(dep);
    }

    public override async Task<Deployment> ReplaceAsync(Deployment resource, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(resource.Metadata.Namespace);
        if (_isDc)
        {
            var result = await ExecuteAsync(
                () => K8s.CustomObjects.ReplaceNamespacedCustomObjectAsync(ToDcJson(resource), DcGroup, DcVersion, ns, DcPlural,
                    resource.Metadata.Name, cancellationToken: ct),
                resource.Metadata.Name).ConfigureAwait(false);
            return MapDcFromJson(result);
        }
        var dep = await ExecuteAsync(
            () => K8s.AppsV1.ReplaceNamespacedDeploymentAsync(ToV1Deployment(resource), resource.Metadata.Name, ns, cancellationToken: ct),
            resource.Metadata.Name).ConfigureAwait(false);
        return MapDeployment(dep);
    }

    public override async Task<Deployment> PatchAsync(string name, string? @namespace, JsonDocument patch, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var k8sPatch = new V1Patch(patch.RootElement.GetRawText(), V1Patch.PatchType.MergePatch);
        if (_isDc)
        {
            var result = await ExecuteAsync(
                () => K8s.CustomObjects.PatchNamespacedCustomObjectAsync(k8sPatch, DcGroup, DcVersion, ns, DcPlural, name, cancellationToken: ct),
                name).ConfigureAwait(false);
            return MapDcFromJson(result);
        }
        var dep = await ExecuteAsync(
            () => K8s.AppsV1.PatchNamespacedDeploymentAsync(k8sPatch, name, ns, cancellationToken: ct),
            name).ConfigureAwait(false);
        return MapDeployment(dep);
    }

    public override async Task DeleteAsync(string name, string? @namespace, DeleteOptions options, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var prop = ToK8sPropagation(options.Propagation);
        var grace = EffectiveGracePeriod(options);
        if (_isDc)
        {
            await ExecuteAsync(
                () => K8s.CustomObjects.DeleteNamespacedCustomObjectAsync(DcGroup, DcVersion, ns, DcPlural, name,
                    gracePeriodSeconds: grace, propagationPolicy: prop, cancellationToken: ct),
                name).ConfigureAwait(false);
            return;
        }
        await ExecuteAsync(
            () => K8s.AppsV1.DeleteNamespacedDeploymentAsync(name, ns,
                gracePeriodSeconds: grace, propagationPolicy: prop, cancellationToken: ct),
            name).ConfigureAwait(false);
    }

    public async Task ScaleAsync(string name, string @namespace, int replicas, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        if (replicas < 0)
            throw new OpenShiftValidationException("Replica count must be >= 0.");

        if (_isDc)
        {
            var patch = JsonDocument.Parse($"{{\"spec\":{{\"replicas\":{replicas}}}}}");
            await ExecuteAsync(
                () => K8s.CustomObjects.PatchNamespacedCustomObjectAsync(
                    new V1Patch(patch.RootElement.GetRawText(), V1Patch.PatchType.MergePatch), DcGroup, DcVersion, ns, DcPlural, name,
                    cancellationToken: ct),
                name).ConfigureAwait(false);
            return;
        }
        await ExecuteAsync(
            () => K8s.AppsV1.PatchNamespacedDeploymentAsync(
                new V1Patch($"{{\"spec\":{{\"replicas\":{replicas}}}}}", V1Patch.PatchType.MergePatch),
                name, ns, cancellationToken: ct),
            name).ConfigureAwait(false);
    }

    public async Task RolloutRestartAsync(string name, string @namespace, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var restartedAt = DateTimeOffset.UtcNow.ToString("O");
        var patchJson = $"{{\"spec\":{{\"template\":{{\"metadata\":{{\"annotations\":{{\"kubectl.kubernetes.io/restartedAt\":\"{restartedAt}\"}}}}}}}}}}";
        var k8sPatch = new V1Patch(patchJson, V1Patch.PatchType.MergePatch);
        if (_isDc)
        {
            await ExecuteAsync(
                () => K8s.CustomObjects.PatchNamespacedCustomObjectAsync(k8sPatch, DcGroup, DcVersion, ns, DcPlural, name, cancellationToken: ct),
                name).ConfigureAwait(false);
            return;
        }
        await ExecuteAsync(
            () => K8s.AppsV1.PatchNamespacedDeploymentAsync(k8sPatch, name, ns, cancellationToken: ct),
            name).ConfigureAwait(false);
    }

    protected override IAsyncEnumerable<WatchEvent<Deployment>> WatchCoreAsync(
        string? @namespace, string? labelSelector, string? resourceVersion, CancellationToken ct)
    {
        var ns = ResolveNamespace(@namespace);
        if (_isDc)
            return WatchHelper.WatchNamespacedAsync(K8s, DcGroup, DcVersion, DcPlural, ns, labelSelector, resourceVersion, MapDcFromJson, ct);
        return WatchDeploymentsCoreAsync(ns, labelSelector, resourceVersion, ct);
    }

    private async IAsyncEnumerable<WatchEvent<Deployment>> WatchDeploymentsCoreAsync(
        string @namespace, string? labelSelector, string? resourceVersion,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var (type, dep) in K8s.AppsV1.ListNamespacedDeploymentWithHttpMessagesAsync(
            @namespace, labelSelector: labelSelector, resourceVersion: resourceVersion,
            watch: true, cancellationToken: ct).WatchAsync<V1Deployment, V1DeploymentList>(cancellationToken: ct))
        {
            yield return new WatchEvent<Deployment>
            {
                Type = type switch
                {
                    k8s.WatchEventType.Added => Abstractions.WatchEventType.Added,
                    k8s.WatchEventType.Modified => Abstractions.WatchEventType.Modified,
                    k8s.WatchEventType.Deleted => Abstractions.WatchEventType.Deleted,
                    k8s.WatchEventType.Bookmark => Abstractions.WatchEventType.Bookmark,
                    _ => Abstractions.WatchEventType.Error,
                },
                Resource = MapDeployment(dep),
            };
        }
    }

    protected override string? GetResourceVersion(Deployment resource) => resource.Metadata.ResourceVersion;

    private static Deployment MapDeployment(V1Deployment dep) => new()
    {
        Metadata = new Resources.ResourceMetadata
        {
            Name = dep.Metadata?.Name ?? string.Empty,
            Namespace = dep.Metadata?.NamespaceProperty,
            Uid = dep.Metadata?.Uid,
            ResourceVersion = dep.Metadata?.ResourceVersion,
            Labels = (IReadOnlyDictionary<string, string>?)dep.Metadata?.Labels ?? new Dictionary<string, string>(),
            Annotations = (IReadOnlyDictionary<string, string>?)dep.Metadata?.Annotations ?? new Dictionary<string, string>(),
            CreationTimestamp = dep.Metadata?.CreationTimestamp,
        },
        Replicas = dep.Spec?.Replicas ?? 0,
        AvailableReplicas = dep.Status?.AvailableReplicas ?? 0,
        ReadyReplicas = dep.Status?.ReadyReplicas ?? 0,
        Selector = (IReadOnlyDictionary<string, string>?)dep.Spec?.Selector?.MatchLabels ?? new Dictionary<string, string>(),
    };

    private static Deployment MapDcFromJson(object raw)
    {
        var json = JsonSerializer.Serialize(raw);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new Deployment
        {
            Metadata = MetadataMapper.Map(root),
            Replicas = root.TryGetProperty("spec", out var spec) && spec.TryGetProperty("replicas", out var r) ? r.GetInt32() : 0,
            AvailableReplicas = root.TryGetProperty("status", out var st) && st.TryGetProperty("availableReplicas", out var a) ? a.GetInt32() : 0,
            ReadyReplicas = root.TryGetProperty("status", out var st2) && st2.TryGetProperty("readyReplicas", out var rr) ? rr.GetInt32() : 0,
            Selector = new Dictionary<string, string>(),
        };
    }

    private static object ToDcJson(Deployment d) => new
    {
        apiVersion = $"{DcGroup}/{DcVersion}",
        kind = "DeploymentConfig",
        metadata = new { name = d.Metadata.Name, resourceVersion = d.Metadata.ResourceVersion },
        spec = new { replicas = d.Replicas },
    };

    private static V1Deployment ToV1Deployment(Deployment d) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = d.Metadata.Name,
            NamespaceProperty = d.Metadata.Namespace,
            ResourceVersion = d.Metadata.ResourceVersion,
        },
        Spec = new V1DeploymentSpec
        {
            Replicas = d.Replicas,
            Selector = new V1LabelSelector { MatchLabels = d.Selector.ToDictionary() },
            Template = new V1PodTemplateSpec
            {
                Metadata = new V1ObjectMeta(),
                Spec = new V1PodSpec { Containers = [] },
            },
        },
    };

    private static PagedList<Deployment> MapDcList(object raw)
    {
        var json = JsonSerializer.Serialize(raw);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var items = root.TryGetProperty("items", out var el)
            ? el.EnumerateArray().Select(i => MapDcFromJson((object)i)).ToList()
            : new List<Deployment>();
        var cont = root.TryGetProperty("metadata", out var m) && m.TryGetProperty("continue", out var c) ? c.GetString() : null;
        return new PagedList<Deployment> { Items = items, ContinueToken = string.IsNullOrEmpty(cont) ? null : cont };
    }
}
