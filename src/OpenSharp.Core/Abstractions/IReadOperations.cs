namespace OpenSharp.Core.Abstractions;

/// <summary>
/// Read operations common to all resource types: get, list with paging, and streaming
/// enumeration.
/// </summary>
/// <typeparam name="T">The resource type.</typeparam>
public interface IReadOperations<T>
{
    /// <summary>
    /// Retrieves a single resource by name.
    /// </summary>
    /// <param name="name">Resource name.</param>
    /// <param name="namespace">Project or namespace; <see langword="null"/> uses the client default.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<T> GetAsync(string name, string? @namespace = null, CancellationToken ct = default);

    /// <summary>
    /// Returns one page of resources. Pass <paramref name="continueToken"/> from the
    /// previous result to retrieve the next page.
    /// </summary>
    /// <param name="namespace">Project or namespace; <see langword="null"/> uses the client default.</param>
    /// <param name="limit">Maximum number of items per page.</param>
    /// <param name="continueToken">Continuation token from a previous call, or <see langword="null"/> for the first page.</param>
    /// <param name="labelSelector">Optional Kubernetes label selector expression.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PagedList<T>> ListAsync(
        string? @namespace = null,
        int? limit = null,
        string? continueToken = null,
        string? labelSelector = null,
        CancellationToken ct = default);

    /// <summary>
    /// Enumerates all resources, transparently paging through continuation tokens so the
    /// caller does not need to manage pages.
    /// </summary>
    /// <param name="namespace">Project or namespace; <see langword="null"/> uses the client default.</param>
    /// <param name="labelSelector">Optional Kubernetes label selector expression.</param>
    /// <param name="ct">Cancellation token.</param>
    IAsyncEnumerable<T> EnumerateAsync(
        string? @namespace = null,
        string? labelSelector = null,
        CancellationToken ct = default);
}
