using System.Text.Json;
using OpenSharp.Core.Generic;

namespace OpenSharp.Core.Abstractions;

/// <summary>
/// Generic escape-hatch API for operating on resource types that do not yet have a
/// first-class implementation in the library.
/// Resources are identified by API group, version, and plural name via
/// <see cref="GenericResourceRef"/>.
/// </summary>
public interface IGenericOperations
{
    /// <summary>Retrieves a single resource identified by <paramref name="reference"/>.</summary>
    /// <param name="reference">Full resource identity including name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<JsonElement> GetAsync(GenericResourceRef reference, CancellationToken ct = default);

    /// <summary>
    /// Returns one page of resources of the type identified by <paramref name="reference"/>.
    /// </summary>
    /// <param name="reference">Resource type identity (name need not be set).</param>
    /// <param name="limit">Maximum items per page.</param>
    /// <param name="continueToken">Continuation token from the previous page, or <see langword="null"/> for the first page.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PagedList<JsonElement>> ListAsync(
        GenericResourceRef reference,
        int? limit = null,
        string? continueToken = null,
        CancellationToken ct = default);

    /// <summary>Creates a resource described by <paramref name="body"/>.</summary>
    /// <param name="reference">Resource type identity.</param>
    /// <param name="body">The resource document.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<JsonElement> CreateAsync(GenericResourceRef reference, JsonElement body, CancellationToken ct = default);

    /// <summary>Deletes the resource identified by <paramref name="reference"/>.</summary>
    /// <param name="reference">Full resource identity including name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(GenericResourceRef reference, CancellationToken ct = default);
}
