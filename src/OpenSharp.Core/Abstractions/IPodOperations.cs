using OpenSharp.Core.Resources;

namespace OpenSharp.Core.Abstractions;

/// <summary>Operations for Kubernetes <c>Pod</c> resources, including log and exec access.</summary>
public interface IPodOperations : IReadOperations<Pod>, IWriteOperations<Pod>, IWatchable<Pod>
{
    /// <summary>
    /// Retrieves a snapshot of a container's logs as a single string.
    /// </summary>
    /// <param name="name">Pod name.</param>
    /// <param name="namespace">Project or namespace.</param>
    /// <param name="options">Log retrieval options such as container name and tail lines.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string> ReadLogsAsync(string name, string @namespace, LogOptions options, CancellationToken ct = default);

    /// <summary>
    /// Streams live container logs, yielding each new line as it arrives.
    /// The stream ends when the container stops or the caller cancels.
    /// </summary>
    /// <param name="name">Pod name.</param>
    /// <param name="namespace">Project or namespace.</param>
    /// <param name="options">Log retrieval options; <see cref="LogOptions.Follow"/> is
    /// ignored — the stream always follows.</param>
    /// <param name="ct">Cancellation token.</param>
    IAsyncEnumerable<string> FollowLogsAsync(string name, string @namespace, LogOptions options, CancellationToken ct = default);

    /// <summary>
    /// Executes a command inside a running container and returns its output and exit code.
    /// </summary>
    /// <param name="name">Pod name.</param>
    /// <param name="namespace">Project or namespace.</param>
    /// <param name="request">Command and optional container name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ExecResult> ExecAsync(string name, string @namespace, ExecRequest request, CancellationToken ct = default);
}
