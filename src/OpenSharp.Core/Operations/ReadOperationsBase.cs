using System.Runtime.CompilerServices;
using k8s;
using Microsoft.Extensions.Logging;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;

namespace OpenSharp.Core.Operations;

/// <summary>
/// Provides the default read operations (get, paged list, auto-paging enumeration, and
/// watch) for any resource type. Concrete operations subclass this and supply the
/// fetch delegates.
/// </summary>
/// <typeparam name="T">The library resource model type.</typeparam>
internal abstract class ReadOperationsBase<T> : OperationBase, IReadOperations<T>, IWatchable<T>
{
    protected ReadOperationsBase(IKubernetes k8s, OpenShiftClientOptions options, ILogger logger)
        : base(k8s, options, logger) { }

    /// <inheritdoc/>
    public abstract Task<T> GetAsync(string name, string? @namespace = null, CancellationToken ct = default);

    /// <inheritdoc/>
    public abstract Task<PagedList<T>> ListAsync(
        string? @namespace = null,
        int? limit = null,
        string? continueToken = null,
        string? labelSelector = null,
        CancellationToken ct = default);

    /// <summary>
    /// Streams all resources by automatically following continuation tokens from
    /// <see cref="ListAsync"/> until the last page is reached.
    /// </summary>
    public async IAsyncEnumerable<T> EnumerateAsync(
        string? @namespace = null,
        string? labelSelector = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        string? continueToken = null;
        do
        {
            var page = await ListAsync(@namespace, limit: 100, continueToken, labelSelector, ct)
                .ConfigureAwait(false);

            foreach (var item in page.Items)
                yield return item;

            continueToken = page.ContinueToken;
        }
        while (continueToken is not null);
    }

    /// <summary>
    /// Starts a watch. When <see cref="WatchOptions.AutoResume"/> is <see langword="true"/>
    /// (the default), transient terminations are recovered by re-establishing the watch from
    /// the last observed resourceVersion.
    /// </summary>
    public async IAsyncEnumerable<WatchEvent<T>> WatchAsync(
        string? @namespace = null,
        WatchOptions? options = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        options ??= new WatchOptions();
        string? lastResourceVersion = null;

        while (!ct.IsCancellationRequested)
        {
            bool resumed = false;
            await foreach (var evt in WatchCoreAsync(@namespace, options.LabelSelector, lastResourceVersion, ct)
                               .ConfigureAwait(false))
            {
                resumed = true;
                if (evt.Type == Abstractions.WatchEventType.Bookmark)
                {
                    lastResourceVersion = GetResourceVersion(evt.Resource);
                    yield return evt;
                    continue;
                }

                if (evt.Type != Abstractions.WatchEventType.Error)
                    lastResourceVersion = GetResourceVersion(evt.Resource) ?? lastResourceVersion;

                yield return evt;
            }

            if (!options.AutoResume || ct.IsCancellationRequested)
                yield break;

            if (!resumed)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Implemented by subclasses to perform the actual API watch call and translate events.
    /// </summary>
    protected abstract IAsyncEnumerable<WatchEvent<T>> WatchCoreAsync(
        string? @namespace,
        string? labelSelector,
        string? resourceVersion,
        CancellationToken ct);

    /// <summary>
    /// Extracts the resourceVersion string from a resource for watch bookkeeping.
    /// Override in subclasses that can provide it; base returns <see langword="null"/>.
    /// </summary>
    protected virtual string? GetResourceVersion(T resource) => null;
}
