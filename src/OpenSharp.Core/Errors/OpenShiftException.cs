namespace OpenSharp.Core.Errors;

/// <summary>Base exception for all errors surfaced by the OpenSharp client library.</summary>
public class OpenShiftException : Exception
{
    /// <summary>HTTP status code returned by the cluster, when applicable.</summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Reference identifying which resource was involved in the failing operation,
    /// when applicable.
    /// </summary>
    public string? ResourceRef { get; }

    /// <summary>Initialises a new instance with a message.</summary>
    public OpenShiftException(string message)
        : base(message) { }

    /// <summary>Initialises a new instance with a message and inner exception.</summary>
    public OpenShiftException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>Initialises a new instance with a message, status code, and optional resource ref.</summary>
    public OpenShiftException(string message, int? statusCode, string? resourceRef = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResourceRef = resourceRef;
    }

    /// <summary>Initialises a new instance with all fields.</summary>
    public OpenShiftException(string message, int? statusCode, string? resourceRef, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResourceRef = resourceRef;
    }
}

/// <summary>
/// Thrown when the client cannot reach the cluster API server (network failure, DNS,
/// or request timeout).
/// </summary>
public sealed class OpenShiftConnectionException : OpenShiftException
{
    /// <inheritdoc cref="OpenShiftException(string)"/>
    public OpenShiftConnectionException(string message) : base(message) { }

    /// <inheritdoc cref="OpenShiftException(string, Exception)"/>
    public OpenShiftConnectionException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Thrown when the credentials provided are missing, invalid, or have expired (HTTP 401).
/// </summary>
public sealed class OpenShiftAuthenticationException : OpenShiftException
{
    /// <inheritdoc cref="OpenShiftException(string)"/>
    public OpenShiftAuthenticationException(string message) : base(message, 401) { }

    /// <inheritdoc cref="OpenShiftException(string, Exception)"/>
    public OpenShiftAuthenticationException(string message, Exception innerException)
        : base(message, 401, null, innerException) { }
}

/// <summary>
/// Thrown when the authenticated identity does not have permission to perform the
/// requested operation (HTTP 403).
/// </summary>
public sealed class OpenShiftAuthorizationException : OpenShiftException
{
    /// <inheritdoc cref="OpenShiftException(string)"/>
    public OpenShiftAuthorizationException(string message, string? resourceRef = null)
        : base(message, 403, resourceRef) { }

    /// <inheritdoc cref="OpenShiftException(string, Exception)"/>
    public OpenShiftAuthorizationException(string message, Exception innerException, string? resourceRef = null)
        : base(message, 403, resourceRef, innerException) { }
}

/// <summary>
/// Thrown when the requested resource or namespace does not exist (HTTP 404).
/// </summary>
public sealed class OpenShiftNotFoundException : OpenShiftException
{
    /// <inheritdoc cref="OpenShiftException(string)"/>
    public OpenShiftNotFoundException(string message, string? resourceRef = null)
        : base(message, 404, resourceRef) { }

    /// <inheritdoc cref="OpenShiftException(string, Exception)"/>
    public OpenShiftNotFoundException(string message, Exception innerException, string? resourceRef = null)
        : base(message, 404, resourceRef, innerException) { }
}

/// <summary>
/// Thrown when the request fails server-side validation or conflicts with an existing
/// resource (HTTP 409, 422), including optimistic-concurrency conflicts.
/// Also thrown when an OpenShift-specific resource type is used against a cluster that
/// does not support it.
/// </summary>
public sealed class OpenShiftValidationException : OpenShiftException
{
    /// <inheritdoc cref="OpenShiftException(string)"/>
    public OpenShiftValidationException(string message, int? statusCode = null, string? resourceRef = null)
        : base(message, statusCode, resourceRef) { }

    /// <inheritdoc cref="OpenShiftException(string, Exception)"/>
    public OpenShiftValidationException(string message, Exception innerException, int? statusCode = null, string? resourceRef = null)
        : base(message, statusCode, resourceRef, innerException) { }
}

/// <summary>
/// Thrown for unexpected server errors (HTTP 5xx) that do not map to a more specific
/// exception type.
/// </summary>
public sealed class OpenShiftServerException : OpenShiftException
{
    /// <inheritdoc cref="OpenShiftException(string)"/>
    public OpenShiftServerException(string message, int? statusCode = null)
        : base(message, statusCode) { }

    /// <inheritdoc cref="OpenShiftException(string, Exception)"/>
    public OpenShiftServerException(string message, Exception innerException, int? statusCode = null)
        : base(message, statusCode, null, innerException) { }
}
