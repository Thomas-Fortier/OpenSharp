using System.Text.Json;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Errors;
using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Operations;

/// <summary>
/// Implements <see cref="IRouteOperations"/> against the OpenShift Route API
/// (<c>route.openshift.io/v1</c>). Throws <see cref="OpenShiftValidationException"/> with
/// a clear message when the cluster does not support Routes (FR-015).
/// </summary>
internal sealed class RouteOperations : WriteOperationsBase<Route>, IRouteOperations
{
    private const string Group = "route.openshift.io";
    private const string Version = "v1";
    private const string Plural = "routes";

    public RouteOperations(IKubernetes k8s, OpenShiftClientOptions options, ILogger logger)
        : base(k8s, options, logger) { }

    public override async Task<Route> GetAsync(string name, string? @namespace = null, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var result = await ExecuteRouteAsync(
            () => K8s.CustomObjects.GetNamespacedCustomObjectAsync(Group, Version, ns, Plural, name, ct),
            name).ConfigureAwait(false);
        return MapFromJson(result);
    }

    public override async Task<PagedList<Route>> ListAsync(
        string? @namespace = null, int? limit = null, string? continueToken = null,
        string? labelSelector = null, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var result = await ExecuteRouteAsync(
            () => K8s.CustomObjects.ListNamespacedCustomObjectAsync(Group, Version, ns, Plural,
                limit: limit, continueParameter: continueToken, labelSelector: labelSelector,
                cancellationToken: ct)).ConfigureAwait(false);
        return MapList(result);
    }

    public override async Task<Route> CreateAsync(Route resource, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(resource.Metadata.Namespace);
        var result = await ExecuteRouteAsync(
            () => K8s.CustomObjects.CreateNamespacedCustomObjectAsync(ToJson(resource), Group, Version, ns, Plural,
                cancellationToken: ct),
            resource.Metadata.Name).ConfigureAwait(false);
        return MapFromJson(result);
    }

    public override async Task<Route> ReplaceAsync(Route resource, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(resource.Metadata.Namespace);
        var result = await ExecuteRouteAsync(
            () => K8s.CustomObjects.ReplaceNamespacedCustomObjectAsync(ToJson(resource), Group, Version, ns, Plural,
                resource.Metadata.Name, cancellationToken: ct),
            resource.Metadata.Name).ConfigureAwait(false);
        return MapFromJson(result);
    }

    public override async Task<Route> PatchAsync(string name, string? @namespace, JsonDocument patch, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var result = await ExecuteRouteAsync(
            () => K8s.CustomObjects.PatchNamespacedCustomObjectAsync(
                new V1Patch(patch.RootElement.GetRawText(), V1Patch.PatchType.MergePatch), Group, Version, ns, Plural, name,
                cancellationToken: ct),
            name).ConfigureAwait(false);
        return MapFromJson(result);
    }

    public override Task DeleteAsync(string name, string? @namespace, DeleteOptions options, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        return ExecuteRouteAsync(
            () => K8s.CustomObjects.DeleteNamespacedCustomObjectAsync(Group, Version, ns, Plural, name,
                gracePeriodSeconds: EffectiveGracePeriod(options),
                propagationPolicy: ToK8sPropagation(options.Propagation), cancellationToken: ct),
            name);
    }

    /// <summary>
    /// Wraps <see cref="OperationBase.ExecuteAsync{T}"/> and translates a 404 that indicates the
    /// <c>route.openshift.io</c> API group is not served (a plain Kubernetes target) into a clear
    /// <see cref="OpenShiftValidationException"/> (FR-015), rather than a generic not-found error.
    /// </summary>
    private static async Task<T> ExecuteRouteAsync<T>(Func<Task<T>> call, string? resourceRef = null)
    {
        try
        {
            return await ExecuteAsync(call, resourceRef).ConfigureAwait(false);
        }
        catch (OpenShiftNotFoundException ex) when (IsRouteApiUnavailable(ex))
        {
            throw ErrorMapper.UnsupportedResourceType("Route");
        }
    }

    private static async Task ExecuteRouteAsync(Func<Task> call, string? resourceRef = null)
    {
        try
        {
            await ExecuteAsync(call, resourceRef).ConfigureAwait(false);
        }
        catch (OpenShiftNotFoundException ex) when (IsRouteApiUnavailable(ex))
        {
            throw ErrorMapper.UnsupportedResourceType("Route");
        }
    }

    /// <summary>
    /// Detects the Kubernetes/OpenShift "the server could not find the requested resource"
    /// response body that is returned when the Route API group is not registered, as opposed
    /// to a specific Route simply not existing.
    /// </summary>
    private static bool IsRouteApiUnavailable(OpenShiftNotFoundException ex) =>
        ex.InnerException is HttpOperationException http &&
        (http.Response?.Content?.Contains("could not find the requested resource", StringComparison.OrdinalIgnoreCase) ?? false);

    protected override IAsyncEnumerable<WatchEvent<Route>> WatchCoreAsync(
        string? @namespace, string? labelSelector, string? resourceVersion, CancellationToken ct)
    {
        var ns = ResolveNamespace(@namespace);
        return WatchHelper.WatchNamespacedAsync(K8s, Group, Version, Plural, ns, labelSelector, resourceVersion, MapFromJson, ct);
    }

    protected override string? GetResourceVersion(Route resource) => resource.Metadata.ResourceVersion;

    private static Route MapFromJson(object raw)
    {
        var json = JsonSerializer.Serialize(raw);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string? toKind = null, toName = null;
        int? toWeight = null;
        if (root.TryGetProperty("spec", out var spec) && spec.TryGetProperty("to", out var to))
        {
            toKind = to.TryGetProperty("kind", out var k) ? k.GetString() : null;
            toName = to.TryGetProperty("name", out var n) ? n.GetString() : null;
            toWeight = to.TryGetProperty("weight", out var w) ? w.GetInt32() : null;
        }

        return new Route
        {
            Metadata = MetadataMapper.Map(root),
            Host = root.TryGetProperty("spec", out var sp) && sp.TryGetProperty("host", out var h) ? h.GetString() ?? string.Empty : string.Empty,
            Path = root.TryGetProperty("spec", out var sp2) && sp2.TryGetProperty("path", out var p) ? p.GetString() : null,
            To = new RouteTarget
            {
                Kind = toKind ?? "Service",
                Name = toName ?? string.Empty,
                Weight = toWeight,
            },
            Port = root.TryGetProperty("spec", out var sp3) && sp3.TryGetProperty("port", out var port) &&
                   port.TryGetProperty("targetPort", out var tp) ? tp.GetString() : null,
            TlsTermination = root.TryGetProperty("spec", out var sp4) && sp4.TryGetProperty("tls", out var tls) &&
                             tls.TryGetProperty("termination", out var term) ? term.GetString() : null,
        };
    }

    private static PagedList<Route> MapList(object raw)
    {
        var json = JsonSerializer.Serialize(raw);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var items = root.TryGetProperty("items", out var el)
            ? el.EnumerateArray().Select(i => MapFromJson((object)i)).ToList()
            : new List<Route>();
        var cont = root.TryGetProperty("metadata", out var m) && m.TryGetProperty("continue", out var c) ? c.GetString() : null;
        return new PagedList<Route> { Items = items, ContinueToken = string.IsNullOrEmpty(cont) ? null : cont };
    }

    private static object ToJson(Route route) => new
    {
        apiVersion = $"{Group}/{Version}",
        kind = "Route",
        metadata = new { name = route.Metadata.Name, @namespace = route.Metadata.Namespace, resourceVersion = route.Metadata.ResourceVersion },
        spec = new
        {
            host = route.Host,
            path = route.Path,
            to = new { kind = route.To.Kind, name = route.To.Name, weight = route.To.Weight },
            port = route.Port is not null ? new { targetPort = route.Port } : null,
            tls = route.TlsTermination is not null ? new { termination = route.TlsTermination } : null,
        },
    };
}
