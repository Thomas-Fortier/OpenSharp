using System.Text.Json;
using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Abstractions;

/// <summary>The propagation policy to use when deleting a resource with dependents.</summary>
public enum DeletePropagationPolicy
{
    /// <summary>
    /// The cluster garbage-collects dependents in the background after the owner is deleted.
    /// This is the default.
    /// </summary>
    Background,

    /// <summary>
    /// The operation blocks until all dependents are deleted before the owner is removed.
    /// </summary>
    Foreground,

    /// <summary>
    /// Dependents are left running and detached from the deleted owner.
    /// </summary>
    Orphan,
}

/// <summary>
/// Write operations common to all resource types: create, replace, patch, and delete.
/// </summary>
/// <typeparam name="T">The resource type.</typeparam>
public interface IWriteOperations<T>
{
    /// <summary>Creates a new resource on the cluster.</summary>
    /// <param name="resource">The resource to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created resource as returned by the server.</returns>
    Task<T> CreateAsync(T resource, CancellationToken ct = default);

    /// <summary>
    /// Replaces an existing resource. The <c>ResourceVersion</c> in the resource metadata
    /// is used for optimistic concurrency.
    /// </summary>
    /// <param name="resource">The resource with updated fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated resource as returned by the server.</returns>
    Task<T> ReplaceAsync(T resource, CancellationToken ct = default);

    /// <summary>
    /// Applies a JSON Patch document to an existing resource.
    /// </summary>
    /// <param name="name">Resource name.</param>
    /// <param name="namespace">Project or namespace.</param>
    /// <param name="patch">The patch to apply, serialised as a <see cref="JsonDocument"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The patched resource as returned by the server.</returns>
    Task<T> PatchAsync(string name, string? @namespace, JsonDocument patch, CancellationToken ct = default);

    /// <summary>Deletes a resource from the cluster.</summary>
    /// <param name="name">Resource name.</param>
    /// <param name="namespace">Project or namespace.</param>
    /// <param name="propagation">
    /// How to handle dependent resources. Defaults to
    /// <see cref="DeletePropagationPolicy.Background"/>.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(
        string name,
        string? @namespace = null,
        DeletePropagationPolicy propagation = DeletePropagationPolicy.Background,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a resource using full <see cref="DeleteOptions"/> — propagation policy plus an
    /// optional grace period and force flag (e.g. immediate deletion via
    /// <see cref="DeleteOptions.Force"/>).
    /// </summary>
    /// <param name="name">Resource name.</param>
    /// <param name="namespace">Project or namespace.</param>
    /// <param name="options">Delete behaviour: propagation, grace period, and force.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string name, string? @namespace, DeleteOptions options, CancellationToken ct = default);
}
