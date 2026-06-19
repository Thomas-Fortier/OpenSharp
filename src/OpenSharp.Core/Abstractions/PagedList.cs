namespace OpenSharp.Core.Abstractions;

/// <summary>
/// A page of resources returned by a list operation, together with a continuation token
/// that can be passed to the next call to retrieve the following page.
/// </summary>
/// <typeparam name="T">The resource type contained in this page.</typeparam>
public sealed class PagedList<T>
{
    /// <summary>The resources on this page.</summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// Opaque token to pass as <c>continueToken</c> in the next list call.
    /// <see langword="null"/> indicates this is the last page.
    /// </summary>
    public string? ContinueToken { get; init; }
}
