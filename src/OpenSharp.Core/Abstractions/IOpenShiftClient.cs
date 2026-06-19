namespace OpenSharp.Core.Abstractions;

/// <summary>
/// The primary entry point for interacting with an OpenShift cluster. Provides access to
/// per-resource operation interfaces for all first-class resource types, and a generic
/// escape hatch for unwrapped types.
/// </summary>
public interface IOpenShiftClient
{
    /// <summary>Operations on OpenShift <c>Project</c> resources (cluster-scoped).</summary>
    IProjectOperations Projects { get; }

    /// <summary>Operations on Kubernetes <c>Pod</c> resources, including logs and exec.</summary>
    IPodOperations Pods { get; }

    /// <summary>Operations on Kubernetes <c>Deployment</c> resources (<c>apps/v1</c>).</summary>
    IWorkloadOperations Deployments { get; }

    /// <summary>Operations on OpenShift <c>DeploymentConfig</c> resources (<c>apps.openshift.io/v1</c>).</summary>
    IWorkloadOperations DeploymentConfigs { get; }

    /// <summary>Operations on Kubernetes <c>Service</c> resources.</summary>
    IServiceOperations Services { get; }

    /// <summary>Operations on OpenShift <c>Route</c> resources (<c>route.openshift.io/v1</c>).</summary>
    IRouteOperations Routes { get; }

    /// <summary>Operations on Kubernetes <c>ConfigMap</c> resources.</summary>
    IConfigMapOperations ConfigMaps { get; }

    /// <summary>Operations on Kubernetes <c>Secret</c> resources.</summary>
    ISecretOperations Secrets { get; }

    /// <summary>
    /// Generic escape hatch for resource types not yet first-class in the library.
    /// Accepts any API group/version/plural.
    /// </summary>
    IGenericOperations Generic { get; }
}
