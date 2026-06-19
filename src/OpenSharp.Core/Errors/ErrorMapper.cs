using System.Net;
using k8s.Autorest;

namespace OpenSharp.Core.Errors;

/// <summary>
/// Maps exceptions and HTTP status codes from the underlying Kubernetes client into
/// the typed <see cref="OpenShiftException"/> hierarchy.
/// </summary>
public static class ErrorMapper
{
    /// <summary>
    /// Maps a <see cref="HttpOperationException"/> returned by <c>KubernetesClient</c> to
    /// the appropriate <see cref="OpenShiftException"/> subtype.
    /// </summary>
    /// <param name="ex">The exception to map.</param>
    /// <param name="resourceRef">Optional resource identifier for error context.</param>
    /// <returns>A typed <see cref="OpenShiftException"/>.</returns>
    public static OpenShiftException Map(HttpOperationException ex, string? resourceRef = null)
    {
        var status = (int?)ex.Response?.StatusCode;

        return status switch
        {
            (int)HttpStatusCode.Unauthorized => new OpenShiftAuthenticationException(
                $"Authentication failed: {ex.Message}", ex),

            (int)HttpStatusCode.Forbidden => new OpenShiftAuthorizationException(
                $"Access denied: {ex.Message}", ex, resourceRef),

            (int)HttpStatusCode.NotFound => new OpenShiftNotFoundException(
                $"Resource not found: {ex.Message}", ex, resourceRef),

            (int)HttpStatusCode.Conflict or (int)HttpStatusCode.UnprocessableEntity => new OpenShiftValidationException(
                $"Request rejected by server: {ex.Message}", ex, status, resourceRef),

            >= 500 => new OpenShiftServerException(
                $"Server error: {ex.Message}", ex, status),

            _ => new OpenShiftServerException(
                $"Unexpected error (HTTP {status}): {ex.Message}", ex, status),
        };
    }

    /// <summary>
    /// Maps a network-level or timeout exception to an
    /// <see cref="OpenShiftConnectionException"/>.
    /// </summary>
    /// <param name="ex">The connectivity exception.</param>
    /// <returns>An <see cref="OpenShiftConnectionException"/>.</returns>
    public static OpenShiftConnectionException MapConnectivity(Exception ex) =>
        new($"Could not connect to the cluster: {ex.Message}", ex);

    /// <summary>
    /// Returns an <see cref="OpenShiftValidationException"/> indicating that the requested
    /// OpenShift-specific resource type is not available on the target cluster.
    /// </summary>
    /// <param name="resourceKind">The OpenShift resource kind that was requested.</param>
    /// <returns>An <see cref="OpenShiftValidationException"/>.</returns>
    public static OpenShiftValidationException UnsupportedResourceType(string resourceKind) =>
        new($"The resource type '{resourceKind}' is an OpenShift extension and is not available on this cluster.");
}
