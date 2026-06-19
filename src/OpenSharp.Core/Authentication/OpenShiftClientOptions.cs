namespace OpenSharp.Core.Authentication;

/// <summary>Configuration options for establishing a connection to an OpenShift cluster.</summary>
public sealed class OpenShiftClientOptions
{
    /// <summary>
    /// Optional explicit path to the kube-config file. When <see langword="null"/>, standard
    /// resolution is used (<c>KUBECONFIG</c> env var, then <c>~/.kube/config</c>).
    /// </summary>
    public string? KubeConfigPath { get; set; }

    /// <summary>
    /// Optional named context to activate within the kube-config file. When <see langword="null"/>,
    /// the current context from the config is used.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Optional explicit API server endpoint. When set, this overrides the server URL from
    /// the kube-config file.
    /// </summary>
    public Uri? ServerUrl { get; set; }

    /// <summary>
    /// Optional bearer token for authentication. Credential refresh is not performed for
    /// static tokens; an expired token surfaces an <see cref="Errors.OpenShiftAuthenticationException"/>.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// When <see langword="true"/>, TLS certificate validation for the cluster API server is
    /// disabled. This is an explicit opt-out and should only be used in controlled environments.
    /// </summary>
    public bool SkipTlsVerify { get; set; }

    /// <summary>
    /// Default project or namespace used when a scoped call does not supply one explicitly.
    /// </summary>
    public string? DefaultNamespace { get; set; }

    /// <summary>
    /// Optional per-request timeout. When <see langword="null"/>, the underlying HTTP client
    /// default applies.
    /// </summary>
    public TimeSpan? RequestTimeout { get; set; }

    /// <summary>
    /// Credential resolution strategy. Defaults to <see cref="AuthMode.Auto"/>, which detects
    /// in-cluster credentials when running inside a pod and falls back to kubeconfig otherwise.
    /// </summary>
    public AuthMode AuthMode { get; set; } = AuthMode.Auto;
}
