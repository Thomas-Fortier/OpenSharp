namespace OpenSharp.Core.Authentication;

/// <summary>Selects how the client resolves credentials when connecting to a cluster.</summary>
public enum AuthMode
{
    /// <summary>
    /// Automatically detect the appropriate mode: use the mounted service-account token and
    /// CA when running inside an OpenShift pod; otherwise fall back to kubeconfig or an
    /// explicit token.
    /// </summary>
    Auto,

    /// <summary>Use the mounted service-account token and CA bundle unconditionally.</summary>
    InCluster,

    /// <summary>Use kubeconfig file resolution or an explicitly supplied token.</summary>
    KubeConfig,
}
