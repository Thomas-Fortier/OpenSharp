# Public API Contract: OpenSharp.Core

For a library, the "contract" is its public surface. These interfaces define what consumers
depend on and what unit tests mock (Constitution Principle V / FR-013). Signatures are
indicative; final XML docs live on the implementation. All async methods accept a
`CancellationToken` (FR-011). Namespaces omitted for brevity; types are defined in data-model.

## Entry point & DI

```csharp
public interface IOpenShiftClient
{
    IProjectOperations Projects { get; }
    IPodOperations Pods { get; }
    IWorkloadOperations Deployments { get; }        // core apps/v1
    IWorkloadOperations DeploymentConfigs { get; }  // apps.openshift.io/v1
    IServiceOperations Services { get; }
    IRouteOperations Routes { get; }
    IConfigMapOperations ConfigMaps { get; }
    ISecretOperations Secrets { get; }
    IGenericOperations Generic { get; }             // FR-009 escape hatch
}

// DI registration (FR-013)
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenSharp(
        this IServiceCollection services, Action<OpenShiftClientOptions> configure);
}
```

**Authentication (FR-001 / FR-002a)**: `OpenShiftClientOptions.AuthMode` defaults to `Auto` —
when running inside a pod the mounted service-account token + CA are detected and used,
otherwise kubeconfig/explicit token resolution applies (`InCluster`/`KubeConfig` force a
mode). Credentials backed by kubeconfig `exec`/auth-provider plugins refresh automatically
for long-running sessions; an expired static `AccessToken` surfaces
`OpenShiftAuthenticationException`.

## Read operations (US1)

```csharp
public interface IReadOperations<T>
{
    Task<T> GetAsync(string name, string? @namespace = null, CancellationToken ct = default);
    Task<PagedList<T>> ListAsync(
        string? @namespace = null, int? limit = null, string? continueToken = null,
        string? labelSelector = null, CancellationToken ct = default);
    IAsyncEnumerable<T> EnumerateAsync(
        string? @namespace = null, string? labelSelector = null,
        CancellationToken ct = default);   // auto-pages (SC-006)
}
```

## Lifecycle operations (US2)

```csharp
public interface IWriteOperations<T>
{
    Task<T> CreateAsync(T resource, CancellationToken ct = default);
    Task<T> ReplaceAsync(T resource, CancellationToken ct = default);   // uses ResourceVersion
    Task<T> PatchAsync(string name, string? @namespace, JsonPatch patch,
        CancellationToken ct = default);
    Task DeleteAsync(string name, string? @namespace = null,
        DeletePropagationPolicy propagation = DeletePropagationPolicy.Background,
        CancellationToken ct = default);   // FR-004: Background default; Foreground/Orphan
}
```

Resource-specific interfaces compose read + write:

```csharp
public interface IProjectOperations   : IReadOperations<Project>, IWriteOperations<Project> { }
public interface IServiceOperations   : IReadOperations<Service>, IWriteOperations<Service> { }
public interface IRouteOperations     : IReadOperations<Route>,   IWriteOperations<Route> { }
public interface IConfigMapOperations : IReadOperations<ConfigMap>, IWriteOperations<ConfigMap> { }
public interface ISecretOperations    : IReadOperations<Secret>,  IWriteOperations<Secret> { }
```

## Pod operations + actions (US3)

```csharp
public interface IPodOperations : IReadOperations<Pod>, IWriteOperations<Pod>
{
    Task<string> ReadLogsAsync(string name, string @namespace, LogOptions options,
        CancellationToken ct = default);
    IAsyncEnumerable<string> FollowLogsAsync(string name, string @namespace, LogOptions options,
        CancellationToken ct = default);                 // FR-005 streaming
    Task<ExecResult> ExecAsync(string name, string @namespace, ExecRequest request,
        CancellationToken ct = default);                 // FR-006
}

public interface IWorkloadOperations : IReadOperations<Deployment>, IWriteOperations<Deployment>
{
    Task ScaleAsync(string name, string @namespace, int replicas,
        CancellationToken ct = default);                 // FR-007
    Task RolloutRestartAsync(string name, string @namespace,
        CancellationToken ct = default);                 // FR-007
}
```

## Watch (US4)

```csharp
public interface IWatchable<T>
{
    IAsyncEnumerable<WatchEvent<T>> WatchAsync(
        string? @namespace = null, WatchOptions? options = null,
        CancellationToken ct = default);                 // FR-008
}
// WatchOptions.AutoResume defaults to true: the stream transparently resumes from the last
// resourceVersion/bookmark after a transient drop, surfacing a terminal error only when
// resume is impossible. Set AutoResume=false to manage reconnection manually.
// Resource operation interfaces that support watching also implement IWatchable<T>.
```

## Generic escape hatch (FR-009)

```csharp
public interface IGenericOperations
{
    Task<JsonElement> GetAsync(GenericResourceRef reference, CancellationToken ct = default);
    Task<PagedList<JsonElement>> ListAsync(GenericResourceRef reference, int? limit = null,
        string? continueToken = null, CancellationToken ct = default);
    Task<JsonElement> CreateAsync(GenericResourceRef reference, JsonElement body,
        CancellationToken ct = default);
    Task DeleteAsync(GenericResourceRef reference, CancellationToken ct = default);
}
```

## Error contract (FR-010 / SC-007)

Every operation throws an `OpenShiftException` subtype on failure:

| Condition | Exception |
|-----------|-----------|
| Cannot reach cluster / timeout | `OpenShiftConnectionException` |
| Missing/expired/invalid credentials | `OpenShiftAuthenticationException` |
| Authenticated but not permitted | `OpenShiftAuthorizationException` |
| Resource/namespace absent | `OpenShiftNotFoundException` |
| Validation failure or version conflict | `OpenShiftValidationException` |
| OpenShift resource on non-OpenShift target (FR-015) | `OpenShiftValidationException` (clear message) |
| Unexpected server error | `OpenShiftServerException` |

## Contract test obligations

Each interface above maps to system-test scenarios (Reqnroll, WireMock.Net-backed):
- Happy-path get/list/create/replace/delete per resource.
- Paging continuation and `EnumerateAsync` auto-paging (SC-006).
- Logs read + follow; exec output/exit code; scale; rollout restart.
- Delete honors propagation policy (Background default; Foreground/Orphan when requested).
- Watch emits Added/Modified/Deleted; auto-resumes after a transient drop and surfaces a
  terminal error only when resume is impossible (and respects `AutoResume=false`).
- Auth resolves in-cluster service account when running in a pod, else kubeconfig/token.
- Generic get/list/create/delete by group/version/plural.
- Each error category in the table is provoked and asserted (SC-007), including the
  non-OpenShift-target case (FR-015).
