namespace OpenSharp.Core.Resources;

/// <summary>Specifies a command to execute inside a running container.</summary>
public sealed class ExecRequest
{
    /// <summary>The command and its arguments to run.</summary>
    public required IReadOnlyList<string> Command { get; init; }

    /// <summary>
    /// Name of the container to execute the command in. Required when the pod has multiple
    /// containers.
    /// </summary>
    public string? Container { get; init; }

    /// <summary>Optional stream to write to the container's standard input.</summary>
    public Stream? Stdin { get; init; }
}

/// <summary>The result of executing a command inside a container.</summary>
public sealed class ExecResult
{
    /// <summary>The command's standard output.</summary>
    public required string StdOut { get; init; }

    /// <summary>The command's standard error output.</summary>
    public required string StdErr { get; init; }

    /// <summary>The exit code returned by the command.</summary>
    public int ExitCode { get; init; }
}
