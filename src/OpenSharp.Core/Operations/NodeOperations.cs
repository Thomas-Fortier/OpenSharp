using System.Runtime.CompilerServices;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Resources;
using INodeOperations = OpenSharp.Core.Abstractions.INodeOperations;

namespace OpenSharp.Core.Operations;

/// <summary>
/// Implements <see cref="INodeOperations"/> against the core Kubernetes Node API. Nodes are
/// cluster-scoped; namespace arguments are ignored.
/// </summary>
internal sealed class NodeOperations : ReadOperationsBase<Node>, INodeOperations
{
    public NodeOperations(IKubernetes k8s, OpenShiftClientOptions options, ILogger logger)
        : base(k8s, options, logger) { }

    public override async Task<Node> GetAsync(string name, string? @namespace = null, CancellationToken ct = default)
    {
        var node = await ExecuteAsync(() => K8s.CoreV1.ReadNodeAsync(name, cancellationToken: ct), name)
            .ConfigureAwait(false);
        return MapNode(node);
    }

    public override async Task<PagedList<Node>> ListAsync(
        string? @namespace = null, int? limit = null, string? continueToken = null,
        string? labelSelector = null, CancellationToken ct = default)
    {
        var list = await ExecuteAsync(
            () => K8s.CoreV1.ListNodeAsync(limit: limit, continueParameter: continueToken,
                labelSelector: labelSelector, cancellationToken: ct)).ConfigureAwait(false);
        return new PagedList<Node>
        {
            Items = list.Items.Select(MapNode).ToList(),
            ContinueToken = string.IsNullOrEmpty(list.Metadata?.ContinueProperty) ? null : list.Metadata.ContinueProperty,
        };
    }

    public Task CordonAsync(string name, CancellationToken ct = default) => SetUnschedulableAsync(name, true, ct);

    public Task UncordonAsync(string name, CancellationToken ct = default) => SetUnschedulableAsync(name, false, ct);

    private Task SetUnschedulableAsync(string name, bool value, CancellationToken ct)
    {
        var patch = new V1Patch(UnschedulablePatch(value), V1Patch.PatchType.MergePatch);
        return ExecuteAsync(() => K8s.CoreV1.PatchNodeAsync(patch, name, cancellationToken: ct), name);
    }

    /// <summary>Builds the merge-patch body that sets a node's <c>spec.unschedulable</c> flag.</summary>
    internal static string UnschedulablePatch(bool value) =>
        $"{{\"spec\":{{\"unschedulable\":{(value ? "true" : "false")}}}}}";

    protected override IAsyncEnumerable<WatchEvent<Node>> WatchCoreAsync(
        string? @namespace, string? labelSelector, string? resourceVersion, CancellationToken ct) =>
        WatchNodesCoreAsync(labelSelector, resourceVersion, ct);

    private async IAsyncEnumerable<WatchEvent<Node>> WatchNodesCoreAsync(
        string? labelSelector, string? resourceVersion, [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var (type, node) in K8s.CoreV1.ListNodeWithHttpMessagesAsync(
            labelSelector: labelSelector, resourceVersion: resourceVersion,
            watch: true, cancellationToken: ct).WatchAsync<V1Node, V1NodeList>(cancellationToken: ct))
        {
            yield return new WatchEvent<Node>
            {
                Type = type switch
                {
                    k8s.WatchEventType.Added => Abstractions.WatchEventType.Added,
                    k8s.WatchEventType.Modified => Abstractions.WatchEventType.Modified,
                    k8s.WatchEventType.Deleted => Abstractions.WatchEventType.Deleted,
                    k8s.WatchEventType.Bookmark => Abstractions.WatchEventType.Bookmark,
                    _ => Abstractions.WatchEventType.Error,
                },
                Resource = MapNode(node),
            };
        }
    }

    protected override string? GetResourceVersion(Node resource) => resource.Metadata.ResourceVersion;

    internal static Node MapNode(V1Node node) => new()
    {
        Metadata = new Resources.ResourceMetadata
        {
            Name = node.Metadata?.Name ?? string.Empty,
            Uid = node.Metadata?.Uid,
            ResourceVersion = node.Metadata?.ResourceVersion,
            Labels = (IReadOnlyDictionary<string, string>?)node.Metadata?.Labels ?? new Dictionary<string, string>(),
            Annotations = (IReadOnlyDictionary<string, string>?)node.Metadata?.Annotations ?? new Dictionary<string, string>(),
            CreationTimestamp = node.Metadata?.CreationTimestamp,
        },
        Unschedulable = node.Spec?.Unschedulable ?? false,
        Conditions = node.Status?.Conditions?.Select(c => new NodeCondition
        {
            Type = c.Type,
            Status = c.Status,
            Reason = c.Reason,
        }).ToList() ?? [],
        KubeletVersion = node.Status?.NodeInfo?.KubeletVersion,
    };
}
