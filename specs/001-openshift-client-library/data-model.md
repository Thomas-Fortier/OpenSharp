# Phase 1 Data Model: OpenShift Client Library

Models are strongly-typed, mostly immutable DTOs returned/accepted by the library. Where the
underlying `KubernetesClient` already provides faithful core models (e.g., `V1Pod`), the
library reuses or thinly maps them; OpenShift-specific resources are first-class types here.
All public types and members carry XML doc comments (Constitution Principle II).

## Connection & configuration

### OpenShiftClientOptions
Configuration for establishing a connection.

| Field | Type | Notes / Validation |
|-------|------|--------------------|
| `KubeConfigPath` | `string?` | Optional explicit path; null = standard resolution (env/`~/.kube/config`). |
| `Context` | `string?` | Optional named context to select. |
| `ServerUrl` | `Uri?` | Optional explicit API endpoint (overrides config). |
| `AccessToken` | `string?` | Optional bearer token. |
| `SkipTlsVerify` | `bool` | Default `false`; when true, server cert is not validated. |
| `DefaultNamespace` | `string?` | Fallback project/namespace for scoped calls. |
| `RequestTimeout` | `TimeSpan?` | Per-request timeout; bounds connectivity failures. |
| `AuthMode` | `AuthMode` | Default `Auto`: detect in-cluster service account when running in a pod, else kubeconfig/token. Can be forced to `InCluster` or `KubeConfig`. |

**Rules**: Credential resolution order (FR-001): when `AuthMode=Auto` and running inside a
pod, use the mounted service-account token + CA; otherwise resolve from explicit token,
kubeconfig/context, or in-cluster as configured. At least one source must resolve, else
construction fails with an authentication error (FR-001/FR-010). For long-running sessions,
credentials backed by kubeconfig `exec`/auth-provider plugins are refreshed automatically by
the underlying client; a static `AccessToken` that expires surfaces a typed authentication
error (FR-002a). `SkipTlsVerify=true` is surfaced as an explicit, logged opt-out.

### AuthMode
Enum selecting credential resolution strategy: `Auto` (default — in-cluster when in a pod,
else kubeconfig/token), `InCluster` (force mounted service account), `KubeConfig` (force
kubeconfig/explicit token).

## Resource envelope

### ResourceMetadata
Common identity/metadata shared by all resources.

| Field | Type | Notes |
|-------|------|-------|
| `Name` | `string` | Required for named operations. |
| `Namespace` | `string?` | Project/namespace; null for cluster-scoped (e.g., Project). |
| `Uid` | `string?` | Server-assigned. |
| `ResourceVersion` | `string?` | Optimistic-concurrency token for updates. |
| `Labels` | `IReadOnlyDictionary<string,string>` | May be empty. |
| `Annotations` | `IReadOnlyDictionary<string,string>` | May be empty. |
| `CreationTimestamp` | `DateTimeOffset?` | Server-assigned. |

## First-class resources (MVP)

### Project (cluster-scoped; OpenShift-specific)
Represents an OpenShift project (a namespace with OpenShift metadata).
- `Metadata: ResourceMetadata`
- `DisplayName: string?`, `Description: string?`, `Status: string?` (e.g., Active/Terminating)

### Pod
Reuses `KubernetesClient` `V1Pod` as the canonical model; surfaced via typed helpers:
- `Metadata`, `Phase: string` (Pending/Running/Succeeded/Failed/Unknown)
- `Containers: IReadOnlyList<ContainerInfo>`

### ContainerInfo
- `Name: string`, `Image: string`, `Ready: bool`, `RestartCount: int`, `State: string`

### Deployment / DeploymentConfig
- `Metadata`
- `Replicas: int` (desired), `AvailableReplicas: int`, `ReadyReplicas: int`
- `Selector: IReadOnlyDictionary<string,string>`
- `DeploymentConfig` is the OpenShift-specific variant (`apps.openshift.io/v1`); `Deployment`
  is core `apps/v1`. Scale/rollout operations target either.

### Service
- `Metadata`, `Type: string` (ClusterIP/NodePort/LoadBalancer), `ClusterIp: string?`
- `Ports: IReadOnlyList<ServicePort>` (`Name?`, `Port`, `TargetPort`, `Protocol`)

### Route (OpenShift-specific)
- `Metadata`, `Host: string`, `Path: string?`
- `To: RouteTarget` (`Kind`, `Name`, `Weight?`)
- `Port: string?`, `TlsTermination: string?` (edge/passthrough/reencrypt/none)

### ConfigMap
- `Metadata`, `Data: IReadOnlyDictionary<string,string>`,
  `BinaryData: IReadOnlyDictionary<string,byte[]>`

### Secret
- `Metadata`, `Type: string`, `Data: IReadOnlyDictionary<string,byte[]>`
- Values are never logged (sensitive-data handling).

## Operation inputs/outputs

### PagedList&lt;T&gt;
Result of a paged list call.
- `Items: IReadOnlyList<T>`, `ContinueToken: string?` (null = last page)

### LogOptions
- `Container: string?`, `Follow: bool`, `TailLines: int?`, `Previous: bool`,
  `SinceSeconds: int?`

### ExecRequest / ExecResult
- Request: `Command: IReadOnlyList<string>`, `Container: string?`, `Stdin: Stream?`
- Result: `StdOut: string`, `StdErr: string`, `ExitCode: int`

### ScaleRequest
- `Replicas: int` (>= 0)

### DeletePropagationPolicy (FR-004)
- Enum: `Background` (default — dependents garbage-collected asynchronously), `Foreground`
  (block until dependents are deleted), `Orphan` (leave dependents running). Passed to delete
  operations; default is `Background`.

### WatchOptions (FR-008)
- `AutoResume: bool` (default `true` — transparently re-establish the watch from the last
  observed resourceVersion/bookmark after a transient termination; set `false` to manage
  reconnection manually), `LabelSelector: string?`.

### WatchEvent&lt;T&gt;
- `Type: WatchEventType` (`Added`/`Modified`/`Deleted`/`Bookmark`/`Error`), `Resource: T`

### GenericResourceRef (escape hatch, FR-009)
- `Group: string`, `Version: string`, `Plural: string` (resource kind plural), `Namespace?`,
  `Name?`

## Error model (FR-010)

`OpenShiftException` (base) with subtypes:
`OpenShiftConnectionException`, `OpenShiftAuthenticationException`,
`OpenShiftAuthorizationException`, `OpenShiftNotFoundException`,
`OpenShiftValidationException` (includes conflicts), `OpenShiftServerException`.
Each carries `Message`, optional `StatusCode`, and `ResourceRef` context where applicable.

## State transitions (relevant flows)

- **Workload scale**: `Replicas(desired=n)` → cluster reconciles → `AvailableReplicas`/
  `ReadyReplicas` converge to `n` (observable via get/watch).
- **Rollout/restart**: trigger → new rollout begins → progress observable via deployment
  status/watch.
- **Watch lifecycle**: `Added`* (initial) → `Modified`/`Deleted` … until caller cancels.
  With `AutoResume=true` (default), a transient termination is transparently re-established
  from the last resourceVersion/bookmark; a terminal `Error` is surfaced only when resume is
  impossible (e.g., resourceVersion too old / 410 Gone). With `AutoResume=false`, the stream
  ends on termination and the caller re-establishes it.
