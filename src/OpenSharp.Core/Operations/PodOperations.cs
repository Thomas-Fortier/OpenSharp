using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Operations;

/// <summary>Implements <see cref="IPodOperations"/> against the core Kubernetes pod API.</summary>
internal sealed class PodOperations : WriteOperationsBase<Pod>, IPodOperations
{
    public PodOperations(IKubernetes k8s, OpenShiftClientOptions options, ILogger logger)
        : base(k8s, options, logger) { }

    public override async Task<Pod> GetAsync(string name, string? @namespace = null, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var pod = await ExecuteAsync(() => K8s.CoreV1.ReadNamespacedPodAsync(name, ns, cancellationToken: ct), name)
            .ConfigureAwait(false);
        return MapPod(pod);
    }

    public override async Task<PagedList<Pod>> ListAsync(
        string? @namespace = null, int? limit = null, string? continueToken = null,
        string? labelSelector = null, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var list = await ExecuteAsync(
            () => K8s.CoreV1.ListNamespacedPodAsync(ns, limit: limit, continueParameter: continueToken,
                labelSelector: labelSelector, cancellationToken: ct)).ConfigureAwait(false);
        return new PagedList<Pod>
        {
            Items = list.Items.Select(MapPod).ToList(),
            ContinueToken = string.IsNullOrEmpty(list.Metadata?.ContinueProperty) ? null : list.Metadata.ContinueProperty,
        };
    }

    public override async Task<Pod> CreateAsync(Pod resource, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(resource.Metadata.Namespace);
        var pod = await ExecuteAsync(
            () => K8s.CoreV1.CreateNamespacedPodAsync(ToV1Pod(resource), ns, cancellationToken: ct),
            resource.Metadata.Name).ConfigureAwait(false);
        return MapPod(pod);
    }

    public override async Task<Pod> ReplaceAsync(Pod resource, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(resource.Metadata.Namespace);
        var pod = await ExecuteAsync(
            () => K8s.CoreV1.ReplaceNamespacedPodAsync(ToV1Pod(resource), resource.Metadata.Name, ns, cancellationToken: ct),
            resource.Metadata.Name).ConfigureAwait(false);
        return MapPod(pod);
    }

    public override async Task<Pod> PatchAsync(string name, string? @namespace, JsonDocument patch, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var pod = await ExecuteAsync(
            () => K8s.CoreV1.PatchNamespacedPodAsync(
                new V1Patch(patch.RootElement.GetRawText(), V1Patch.PatchType.MergePatch), name, ns, cancellationToken: ct),
            name).ConfigureAwait(false);
        return MapPod(pod);
    }

    public override Task DeleteAsync(string name, string? @namespace = null,
        DeletePropagationPolicy propagation = DeletePropagationPolicy.Background, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        return ExecuteAsync(
            () => K8s.CoreV1.DeleteNamespacedPodAsync(name, ns,
                propagationPolicy: ToK8sPropagation(propagation), cancellationToken: ct),
            name);
    }

    public async Task<string> ReadLogsAsync(string name, string @namespace, LogOptions options, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var stream = await ExecuteAsync(
            () => K8s.CoreV1.ReadNamespacedPodLogAsync(name, ns,
                container: options.Container,
                follow: options.Follow,
                tailLines: options.TailLines,
                previous: options.Previous,
                sinceSeconds: options.SinceSeconds,
                cancellationToken: ct),
            name).ConfigureAwait(false);

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<string> FollowLogsAsync(string name, string @namespace, LogOptions options,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var stream = await ExecuteAsync(
            () => K8s.CoreV1.ReadNamespacedPodLogAsync(name, ns,
                container: options.Container,
                follow: true,
                tailLines: options.TailLines,
                previous: options.Previous,
                sinceSeconds: options.SinceSeconds,
                cancellationToken: ct),
            name).ConfigureAwait(false);

        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is null) yield break;
            yield return line;
        }
    }

    public async Task<ExecResult> ExecAsync(string name, string @namespace, ExecRequest request, CancellationToken ct = default)
    {
        var ns = ResolveNamespace(@namespace);
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        var webSocket = await ExecuteAsync(
            () => K8s.WebSocketNamespacedPodExecAsync(name, ns, request.Command,
                container: request.Container,
                stdout: true, stderr: true, stdin: request.Stdin is not null,
                cancellationToken: ct),
            name).ConfigureAwait(false);

        using var demux = new StreamDemuxer(webSocket);
        demux.Start();

        using var stdOutStream = demux.GetStream(ChannelIndex.StdOut, ChannelIndex.StdIn);
        using var stdErrStream = demux.GetStream(ChannelIndex.StdErr, null);

        var outTask = new StreamReader(stdOutStream).ReadToEndAsync(ct);
        var errTask = new StreamReader(stdErrStream).ReadToEndAsync(ct);

        await Task.WhenAll(outTask, errTask).ConfigureAwait(false);

        return new ExecResult
        {
            StdOut = await outTask,
            StdErr = await errTask,
            ExitCode = 0,
        };
    }

    protected override IAsyncEnumerable<WatchEvent<Pod>> WatchCoreAsync(
        string? @namespace, string? labelSelector, string? resourceVersion, CancellationToken ct)
    {
        var ns = ResolveNamespace(@namespace);
        return WatchPodsCoreAsync(ns, labelSelector, resourceVersion, ct);
    }

    private async IAsyncEnumerable<WatchEvent<Pod>> WatchPodsCoreAsync(
        string @namespace, string? labelSelector, string? resourceVersion,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var (type, pod) in K8s.CoreV1.ListNamespacedPodWithHttpMessagesAsync(
            @namespace, labelSelector: labelSelector, resourceVersion: resourceVersion,
            watch: true, cancellationToken: ct).WatchAsync<V1Pod, V1PodList>(cancellationToken: ct))
        {
            yield return new WatchEvent<Pod>
            {
                Type = MapEventType(type),
                Resource = MapPod(pod),
            };
        }
    }

    protected override string? GetResourceVersion(Pod resource) => resource.Metadata.ResourceVersion;

    private static Abstractions.WatchEventType MapEventType(k8s.WatchEventType type) => type switch
    {
        k8s.WatchEventType.Added => Abstractions.WatchEventType.Added,
        k8s.WatchEventType.Modified => Abstractions.WatchEventType.Modified,
        k8s.WatchEventType.Deleted => Abstractions.WatchEventType.Deleted,
        k8s.WatchEventType.Bookmark => Abstractions.WatchEventType.Bookmark,
        _ => Abstractions.WatchEventType.Error,
    };

    private static Pod MapPod(V1Pod pod) => new()
    {
        Metadata = new Resources.ResourceMetadata
        {
            Name = pod.Metadata?.Name ?? string.Empty,
            Namespace = pod.Metadata?.NamespaceProperty,
            Uid = pod.Metadata?.Uid,
            ResourceVersion = pod.Metadata?.ResourceVersion,
            Labels = (IReadOnlyDictionary<string, string>?)pod.Metadata?.Labels ?? new Dictionary<string, string>(),
            Annotations = (IReadOnlyDictionary<string, string>?)pod.Metadata?.Annotations ?? new Dictionary<string, string>(),
            CreationTimestamp = pod.Metadata?.CreationTimestamp,
        },
        Phase = pod.Status?.Phase ?? "Unknown",
        Containers = pod.Status?.ContainerStatuses?.Select(c => new ContainerInfo
        {
            Name = c.Name,
            Image = c.Image,
            Ready = c.Ready,
            RestartCount = (int)c.RestartCount,
            State = c.State?.Running is not null ? "Running"
                : c.State?.Waiting is not null ? "Waiting"
                : c.State?.Terminated is not null ? "Terminated"
                : "Unknown",
        }).ToList() ?? [],
    };

    private static V1Pod ToV1Pod(Pod pod) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = pod.Metadata.Name,
            NamespaceProperty = pod.Metadata.Namespace,
            ResourceVersion = pod.Metadata.ResourceVersion,
            Labels = pod.Metadata.Labels.Count > 0 ? pod.Metadata.Labels.ToDictionary() : null,
        },
        Spec = new V1PodSpec { Containers = [] },
    };
}
