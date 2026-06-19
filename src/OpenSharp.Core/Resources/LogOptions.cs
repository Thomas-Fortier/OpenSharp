namespace OpenSharp.Core.Resources;

/// <summary>Options for retrieving or following container logs.</summary>
public sealed class LogOptions
{
    /// <summary>
    /// Name of the container to fetch logs from. Required when a pod contains more than
    /// one container.
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// When <see langword="true"/>, stream new log lines as they are written.
    /// Only applicable to <c>ReadLogsAsync</c>; <c>FollowLogsAsync</c> always follows.
    /// </summary>
    public bool Follow { get; set; }

    /// <summary>Limits output to the most recent <see cref="TailLines"/> lines.</summary>
    public int? TailLines { get; set; }

    /// <summary>When <see langword="true"/>, returns logs from the previous container run.</summary>
    public bool Previous { get; set; }

    /// <summary>Returns logs written within the last N seconds.</summary>
    public int? SinceSeconds { get; set; }
}
