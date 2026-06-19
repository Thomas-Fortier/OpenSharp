namespace OpenSharp.Core.Abstractions;

/// <summary>
/// Options controlling the behaviour of a watch operation.
/// </summary>
public sealed class WatchOptions
{
    /// <summary>
    /// When <see langword="true"/> (the default), the library automatically re-establishes
    /// the watch from the last observed <c>resourceVersion</c> or bookmark after a transient
    /// termination. A terminal error is only surfaced when resumption is impossible, for
    /// example when the server returns HTTP 410 (Gone) because the resourceVersion is too old.
    /// Set to <see langword="false"/> to receive the stream-end and manage reconnection
    /// yourself.
    /// </summary>
    public bool AutoResume { get; set; } = true;

    /// <summary>Optional Kubernetes label selector to filter watched resources.</summary>
    public string? LabelSelector { get; set; }
}

/// <summary>
/// Provides the ability to watch a resource type for added, modified, and deleted events.
/// </summary>
/// <typeparam name="T">The resource type to watch.</typeparam>
public interface IWatchable<T>
{
    /// <summary>
    /// Starts a watch and yields <see cref="WatchEvent{T}"/> instances until the caller
    /// cancels via <paramref name="ct"/>.
    /// When <see cref="WatchOptions.AutoResume"/> is <see langword="true"/> (the default),
    /// transient stream terminations are recovered transparently.
    /// </summary>
    /// <param name="namespace">Project or namespace to watch; <see langword="null"/> watches across all namespaces.</param>
    /// <param name="options">Optional watch behaviour settings.</param>
    /// <param name="ct">Cancellation token.</param>
    IAsyncEnumerable<WatchEvent<T>> WatchAsync(
        string? @namespace = null,
        WatchOptions? options = null,
        CancellationToken ct = default);
}
