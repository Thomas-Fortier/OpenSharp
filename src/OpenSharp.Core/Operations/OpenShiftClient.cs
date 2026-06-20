using k8s;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Generic;
using INodeOperations = OpenSharp.Core.Abstractions.INodeOperations;

namespace OpenSharp.Core.Operations;

/// <summary>
/// The concrete implementation of <see cref="IOpenShiftClient"/>. Holds the underlying
/// Kubernetes client and exposes per-resource operation facades.
/// </summary>
public sealed class OpenShiftClient : IOpenShiftClient, IDisposable
{
    private readonly IKubernetes _k8s;
    private readonly OpenShiftClientOptions _options;
    private readonly ILogger<OpenShiftClient> _logger;

    /// <summary>
    /// Initialises the client by building a Kubernetes connection from the supplied options.
    /// </summary>
    public OpenShiftClient(
        IOpenShiftConnectionFactory factory,
        IOptions<OpenShiftClientOptions> options,
        ILogger<OpenShiftClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _k8s = factory.CreateClient(_options);

        Projects = new ProjectOperations(_k8s, _options, logger);
        Pods = new PodOperations(_k8s, _options, logger);
        Deployments = new DeploymentOperations(_k8s, _options, logger, isDeploymentConfig: false);
        DeploymentConfigs = new DeploymentOperations(_k8s, _options, logger, isDeploymentConfig: true);
        Services = new ServiceOperations(_k8s, _options, logger);
        Routes = new RouteOperations(_k8s, _options, logger);
        ConfigMaps = new ConfigMapOperations(_k8s, _options, logger);
        Secrets = new SecretOperations(_k8s, _options, logger);
        Nodes = new NodeOperations(_k8s, _options, logger);
        Cluster = new ClusterOperations(_k8s, _options, logger);
        Generic = new GenericOperations(_k8s, _options, logger);
    }

    /// <inheritdoc/>
    public IProjectOperations Projects { get; }

    /// <inheritdoc/>
    public IPodOperations Pods { get; }

    /// <inheritdoc/>
    public IWorkloadOperations Deployments { get; }

    /// <inheritdoc/>
    public IWorkloadOperations DeploymentConfigs { get; }

    /// <inheritdoc/>
    public IServiceOperations Services { get; }

    /// <inheritdoc/>
    public IRouteOperations Routes { get; }

    /// <inheritdoc/>
    public IConfigMapOperations ConfigMaps { get; }

    /// <inheritdoc/>
    public ISecretOperations Secrets { get; }

    /// <inheritdoc/>
    public INodeOperations Nodes { get; }

    /// <inheritdoc/>
    public IClusterOperations Cluster { get; }

    /// <inheritdoc/>
    public IGenericOperations Generic { get; }

    /// <inheritdoc/>
    public void Dispose() => (_k8s as IDisposable)?.Dispose();
}
