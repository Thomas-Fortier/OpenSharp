namespace OpenSharp.Core.Abstractions;

/// <summary>The type of change reported by a watch event.</summary>
public enum WatchEventType
{
    /// <summary>A new resource was created.</summary>
    Added,

    /// <summary>An existing resource was modified.</summary>
    Modified,

    /// <summary>A resource was deleted.</summary>
    Deleted,

    /// <summary>A bookmark event carrying a resourceVersion with no resource change.</summary>
    Bookmark,

    /// <summary>An error occurred on the watch stream.</summary>
    Error,
}

/// <summary>A change notification emitted by a watch operation.</summary>
/// <typeparam name="T">The resource type being watched.</typeparam>
public sealed class WatchEvent<T>
{
    /// <summary>The type of change that occurred.</summary>
    public required WatchEventType Type { get; init; }

    /// <summary>The resource at the time of the event.</summary>
    public required T Resource { get; init; }
}
