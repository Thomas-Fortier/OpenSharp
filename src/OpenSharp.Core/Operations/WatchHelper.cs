using System.Runtime.CompilerServices;
using System.Text.Json;
using k8s;
using OpenSharp.Core.Abstractions;

namespace OpenSharp.Core.Operations;

/// <summary>Utility methods for building watch streams over custom objects.</summary>
internal static class WatchHelper
{
    /// <summary>
    /// Watches a cluster-scoped custom resource and yields typed <see cref="WatchEvent{T}"/>
    /// instances until the stream ends or the cancellation token fires.
    /// </summary>
    public static async IAsyncEnumerable<WatchEvent<T>> WatchClusterAsync<T>(
        IKubernetes k8s,
        string group, string version, string plural,
        string? labelSelector,
        string? resourceVersion,
        Func<object, T> mapper,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var (type, item) in k8s.CustomObjects.ListClusterCustomObjectWithHttpMessagesAsync(
            group, version, plural,
            labelSelector: labelSelector,
            resourceVersion: resourceVersion,
            watch: true,
            cancellationToken: ct).WatchAsync<JsonElement, object>(cancellationToken: ct))
        {
            var eventType = MapEventType(type);
            yield return new WatchEvent<T>
            {
                Type = eventType,
                Resource = mapper(item),
            };
        }
    }

    /// <summary>
    /// Watches a namespace-scoped custom resource and yields typed <see cref="WatchEvent{T}"/>.
    /// </summary>
    public static async IAsyncEnumerable<WatchEvent<T>> WatchNamespacedAsync<T>(
        IKubernetes k8s,
        string group, string version, string plural,
        string @namespace,
        string? labelSelector,
        string? resourceVersion,
        Func<object, T> mapper,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var (type, item) in k8s.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
            group, version, @namespace, plural,
            labelSelector: labelSelector,
            resourceVersion: resourceVersion,
            watch: true,
            cancellationToken: ct).WatchAsync<JsonElement, object>(cancellationToken: ct))
        {
            yield return new WatchEvent<T>
            {
                Type = MapEventType(type),
                Resource = mapper(item),
            };
        }
    }

    private static Abstractions.WatchEventType MapEventType(k8s.WatchEventType type) => type switch
    {
        k8s.WatchEventType.Added => Abstractions.WatchEventType.Added,
        k8s.WatchEventType.Modified => Abstractions.WatchEventType.Modified,
        k8s.WatchEventType.Deleted => Abstractions.WatchEventType.Deleted,
        k8s.WatchEventType.Bookmark => Abstractions.WatchEventType.Bookmark,
        k8s.WatchEventType.Error => Abstractions.WatchEventType.Error,
        _ => Abstractions.WatchEventType.Error,
    };
}
