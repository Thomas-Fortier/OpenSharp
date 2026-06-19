using k8s;
using k8s.Authentication;
using Microsoft.Extensions.Logging;
using OpenSharp.Core.Errors;

namespace OpenSharp.Core.Authentication;

/// <summary>
/// Creates and configures <see cref="IKubernetes"/> client instances from
/// <see cref="OpenShiftClientOptions"/>.
/// </summary>
public sealed class OpenShiftConnectionFactory : IOpenShiftConnectionFactory
{
    private readonly ILogger<OpenShiftConnectionFactory> _logger;

    /// <summary>Initialises the factory with the required logger.</summary>
    public OpenShiftConnectionFactory(ILogger<OpenShiftConnectionFactory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a configured <see cref="IKubernetes"/> client from the supplied options.
    /// </summary>
    /// <param name="options">Connection options including auth mode, endpoint, and TLS settings.</param>
    /// <returns>A configured Kubernetes client ready for use.</returns>
    /// <exception cref="OpenShiftAuthenticationException">
    /// Thrown when no valid credential source can be resolved.
    /// </exception>
    public IKubernetes CreateClient(OpenShiftClientOptions options)
    {
        var config = ResolveConfig(options);
        ApplyOverrides(config, options);
        return new Kubernetes(config);
    }

    private KubernetesClientConfiguration ResolveConfig(OpenShiftClientOptions options)
    {
        var mode = ResolveMode(options);

        if (mode == AuthMode.InCluster)
        {
            _logger.LogDebug("Using in-cluster service account credentials.");
            try
            {
                return KubernetesClientConfiguration.InClusterConfig();
            }
            catch (Exception ex)
            {
                throw new OpenShiftAuthenticationException(
                    "In-cluster credential resolution failed. Ensure the pod has a mounted service account.", ex);
            }
        }

        try
        {
            if (options.AccessToken is not null && options.ServerUrl is not null)
            {
                _logger.LogDebug("Using explicit bearer token and server URL.");
                return new KubernetesClientConfiguration
                {
                    Host = options.ServerUrl.ToString(),
                    AccessToken = options.AccessToken,
                    SkipTlsVerify = options.SkipTlsVerify,
                };
            }

            _logger.LogDebug("Loading kube-config from {Path}.", options.KubeConfigPath ?? "default location");
            return KubernetesClientConfiguration.BuildConfigFromConfigFile(
                options.KubeConfigPath,
                options.Context);
        }
        catch (OpenShiftException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OpenShiftAuthenticationException(
                "Failed to load kube-config. Ensure a valid configuration is available.", ex);
        }
    }

    private static AuthMode ResolveMode(OpenShiftClientOptions options)
    {
        if (options.AuthMode != AuthMode.Auto)
            return options.AuthMode;

        return IsRunningInCluster() ? AuthMode.InCluster : AuthMode.KubeConfig;
    }

    /// <summary>
    /// Detects whether the process is running inside a Kubernetes/OpenShift pod by
    /// checking for the service-account token file that the kubelet mounts.
    /// </summary>
    internal static bool IsRunningInCluster() =>
        File.Exists("/var/run/secrets/kubernetes.io/serviceaccount/token");

    private static void ApplyOverrides(KubernetesClientConfiguration config, OpenShiftClientOptions options)
    {
        if (options.ServerUrl is not null)
            config.Host = options.ServerUrl.ToString();

        if (options.SkipTlsVerify)
        {
            config.SkipTlsVerify = true;
            config.SslCaCerts = null;
        }

        if (options.RequestTimeout.HasValue)
            config.HttpClientTimeout = options.RequestTimeout.Value;
    }
}
