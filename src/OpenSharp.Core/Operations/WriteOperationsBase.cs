using System.Text.Json;
using k8s;
using Microsoft.Extensions.Logging;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;

namespace OpenSharp.Core.Operations;

/// <summary>
/// Combines read and write operations for resource types that support full CRUD,
/// delegating write calls to subclass implementations.
/// </summary>
/// <typeparam name="T">The library resource model type.</typeparam>
internal abstract class WriteOperationsBase<T> : ReadOperationsBase<T>, IWriteOperations<T>
{
    protected WriteOperationsBase(IKubernetes k8s, OpenShiftClientOptions options, ILogger logger)
        : base(k8s, options, logger) { }

    /// <inheritdoc/>
    public abstract Task<T> CreateAsync(T resource, CancellationToken ct = default);

    /// <inheritdoc/>
    public abstract Task<T> ReplaceAsync(T resource, CancellationToken ct = default);

    /// <inheritdoc/>
    public abstract Task<T> PatchAsync(string name, string? @namespace, JsonDocument patch, CancellationToken ct = default);

    /// <inheritdoc/>
    public abstract Task DeleteAsync(
        string name,
        string? @namespace = null,
        DeletePropagationPolicy propagation = DeletePropagationPolicy.Background,
        CancellationToken ct = default);

    /// <summary>Maps a <see cref="DeletePropagationPolicy"/> to its Kubernetes string value.</summary>
    protected static string ToK8sPropagation(DeletePropagationPolicy policy) => policy switch
    {
        DeletePropagationPolicy.Foreground => "Foreground",
        DeletePropagationPolicy.Orphan => "Orphan",
        _ => "Background",
    };
}
