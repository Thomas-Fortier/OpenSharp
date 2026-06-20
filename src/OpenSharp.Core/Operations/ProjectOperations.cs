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
/// Implements <see cref="IProjectOperations"/> using the cluster's project API
/// (<c>project.openshift.io/v1</c>) with a fallback to core namespaces.
/// </summary>
internal sealed class ProjectOperations : WriteOperationsBase<Project>, IProjectOperations
{
    private const string Group = "project.openshift.io";
    private const string Version = "v1";
    private const string Plural = "projects";

    public ProjectOperations(IKubernetes k8s, OpenShiftClientOptions options, ILogger logger)
        : base(k8s, options, logger) { }

    public override async Task<Project> GetAsync(string name, string? @namespace = null, CancellationToken ct = default)
    {
        var result = await ExecuteAsync(
            () => K8s.CustomObjects.GetClusterCustomObjectAsync(Group, Version, Plural, name, ct),
            name).ConfigureAwait(false);
        return MapFromJson(result);
    }

    public override async Task<PagedList<Project>> ListAsync(
        string? @namespace = null, int? limit = null, string? continueToken = null,
        string? labelSelector = null, CancellationToken ct = default)
    {
        var result = await ExecuteAsync(
            () => K8s.CustomObjects.ListClusterCustomObjectAsync(Group, Version, Plural,
                limit: limit, continueParameter: continueToken, labelSelector: labelSelector,
                cancellationToken: ct)).ConfigureAwait(false);
        return MapList(result);
    }

    public override async Task<Project> CreateAsync(Project resource, CancellationToken ct = default)
    {
        var body = ToJson(resource);
        var result = await ExecuteAsync(
            () => K8s.CustomObjects.CreateClusterCustomObjectAsync(body, Group, Version, Plural, cancellationToken: ct),
            resource.Metadata.Name).ConfigureAwait(false);
        return MapFromJson(result);
    }

    public override async Task<Project> ReplaceAsync(Project resource, CancellationToken ct = default)
    {
        var body = ToJson(resource);
        var result = await ExecuteAsync(
            () => K8s.CustomObjects.ReplaceClusterCustomObjectAsync(body, Group, Version, Plural, resource.Metadata.Name, cancellationToken: ct),
            resource.Metadata.Name).ConfigureAwait(false);
        return MapFromJson(result);
    }

    public override async Task<Project> PatchAsync(string name, string? @namespace, JsonDocument patch, CancellationToken ct = default)
    {
        var result = await ExecuteAsync(
            () => K8s.CustomObjects.PatchClusterCustomObjectAsync(
                new V1Patch(patch.RootElement.GetRawText(), V1Patch.PatchType.MergePatch), Group, Version, Plural, name, cancellationToken: ct),
            name).ConfigureAwait(false);
        return MapFromJson(result);
    }

    public override Task DeleteAsync(string name, string? @namespace, DeleteOptions options, CancellationToken ct = default) =>
        ExecuteAsync(
            () => K8s.CustomObjects.DeleteClusterCustomObjectAsync(Group, Version, Plural, name,
                gracePeriodSeconds: EffectiveGracePeriod(options),
                propagationPolicy: ToK8sPropagation(options.Propagation), cancellationToken: ct),
            name);

    protected override IAsyncEnumerable<WatchEvent<Project>> WatchCoreAsync(
        string? @namespace, string? labelSelector, string? resourceVersion, CancellationToken ct) =>
        WatchHelper.WatchClusterAsync<Project>(K8s, Group, Version, Plural, labelSelector, resourceVersion, MapFromJson, ct);

    protected override string? GetResourceVersion(Project resource) => resource.Metadata.ResourceVersion;

    private static Project MapFromJson(object raw)
    {
        var json = JsonSerializer.Serialize(raw);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new Project
        {
            Metadata = MetadataMapper.Map(root),
            DisplayName = root.TryGetProperty("metadata", out var meta) &&
                          meta.TryGetProperty("annotations", out var ann) &&
                          ann.TryGetProperty("openshift.io/display-name", out var dn)
                ? dn.GetString() : null,
            Description = root.TryGetProperty("metadata", out var m2) &&
                          m2.TryGetProperty("annotations", out var a2) &&
                          a2.TryGetProperty("openshift.io/description", out var desc)
                ? desc.GetString() : null,
            Status = root.TryGetProperty("status", out var status) &&
                     status.TryGetProperty("phase", out var phase)
                ? phase.GetString() : null,
        };
    }

    private static object ToJson(Project project)
    {
        return new
        {
            apiVersion = $"{Group}/{Version}",
            kind = "Project",
            metadata = new
            {
                name = project.Metadata.Name,
                resourceVersion = project.Metadata.ResourceVersion,
                labels = project.Metadata.Labels,
                annotations = project.Metadata.Annotations,
            },
        };
    }

    private static PagedList<Project> MapList(object raw)
    {
        var json = JsonSerializer.Serialize(raw);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var items = root.TryGetProperty("items", out var itemsEl)
            ? itemsEl.EnumerateArray().Select(i => MapFromJson((object)i)).ToList()
            : [];
        var continueToken = root.TryGetProperty("metadata", out var meta) &&
                            meta.TryGetProperty("continue", out var c) ? c.GetString() : null;
        return new PagedList<Project> { Items = items, ContinueToken = string.IsNullOrEmpty(continueToken) ? null : continueToken };
    }
}
