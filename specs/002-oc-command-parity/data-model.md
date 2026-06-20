# Phase 1 Data Model: oc Command Parity

New and extended types added to `OpenSharp.Core`. Existing feature-001 types
(`ResourceMetadata`, `PagedList<T>`, `GenericResourceRef`, `DeletePropagationPolicy`,
`WatchEvent<T>`, the error hierarchy) are reused as-is. All new public types carry XML doc
comments (Constitution II).

## New resource models

### Node (`Resources/Node.cs`) — NEW, cluster-scoped, core group

| Field | Type | Notes |
|-------|------|-------|
| `Metadata` | `ResourceMetadata` | Name, labels, etc. `Namespace` is null (cluster-scoped). |
| `Unschedulable` | `bool` | Mirrors `spec.unschedulable`; `true` ⇒ cordoned. |
| `Conditions` | `IReadOnlyList<NodeCondition>` | Node status conditions (Ready, MemoryPressure, …). |
| `KubeletVersion` | `string?` | From `status.nodeInfo.kubeletVersion` (informational). |

Mapped from `k8s.Models.V1Node` (`CoreV1.ReadNodeAsync`/`ListNodeAsync`).

### NodeCondition (`Resources/Node.cs`) — NEW

| Field | Type | Notes |
|-------|------|-------|
| `Type` | `string` | e.g. `Ready`, `DiskPressure`. |
| `Status` | `string` | `True` / `False` / `Unknown`. |
| `Reason` | `string?` | Machine-readable reason, when present. |

### ClusterInfo (`Resources/ClusterInfo.cs`) — NEW

| Field | Type | Notes |
|-------|------|-------|
| `ApiServerEndpoint` | `string` | From the underlying client `BaseUri`. |
| `ServerVersion` | `string` | From `GetCodeAsync()` → `VersionInfo.GitVersion`. |
| `Reachable` | `bool` | `true` when the version call succeeds. |

## New option / value types

### DeleteOptions (`Resources/DeleteOptions.cs`) — NEW

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `Propagation` | `DeletePropagationPolicy` | `Background` | Reuses the 001 enum. |
| `GracePeriodSeconds` | `int?` | `null` | `0` = immediate. |
| `Force` | `bool` | `false` | `true` ⇒ effective `GracePeriodSeconds = 0` (matches `--force --grace-period=0`). |

**Rule**: when `Force` is true, the effective grace period sent to the cluster is `0`
regardless of `GracePeriodSeconds`.

### PatchType (`Abstractions/PatchType.cs` or alongside `IGenericOperations`) — NEW enum

`Merge` (JSON merge patch, default) · `JsonMerge` · `StrategicMerge` · `Json` (JSON Patch).
Maps 1:1 to `k8s.Models.V1Patch.PatchType`.

## Extended types (additive)

### GenericResourceRef (`Generic/GenericResourceRef.cs`) — UNCHANGED

No structural change. Behavior change: `Group == ""` now routes to the core-API path
(see research D2). `Name`/`Namespace` semantics unchanged.

### IGenericOperations (`Abstractions/IGenericOperations.cs`) — EXTENDED

- `ListAsync` gains `string? labelSelector = null` and `string? fieldSelector = null`
  (named-group selectors are server-side; core-group list is unfiltered — research D2).
- New `PatchAsync(GenericResourceRef, JsonDocument patch, PatchType type = Merge, ct)`.
- New `DeleteAsync(GenericResourceRef, DeleteOptions options, ct)` overload.

### IWriteOperations<T> (`Abstractions/IWriteOperations.cs`) — EXTENDED

- New `DeleteAsync(string name, string? @namespace, DeleteOptions options, ct)` overload; the
  existing propagation-only overload remains and delegates with default options.

### IOpenShiftClient (`Abstractions/IOpenShiftClient.cs`) — EXTENDED

- New `INodeOperations Nodes { get; }`
- New `IClusterOperations Cluster { get; }`

## New operation interfaces

### INodeOperations (`Abstractions/INodeOperations.cs`) — NEW

`: IReadOperations<Node>, IWatchable<Node>` plus:
- `Task CordonAsync(string name, CancellationToken ct = default)` — set `spec.unschedulable=true`.
- `Task UncordonAsync(string name, CancellationToken ct = default)` — set `spec.unschedulable=false`.

`IReadOperations<Node>` namespace arguments are ignored (cluster-scoped).

### IClusterOperations (`Abstractions/IClusterOperations.cs`) — NEW

- `Task<ClusterInfo> GetInfoAsync(CancellationToken ct = default)`
- `Task<bool> IsResourceTypeAvailableAsync(string group, string version, string plural, CancellationToken ct = default)`

## Entity → spec mapping

| Spec entity | Type(s) | FR |
|-------------|---------|----|
| Node | `Node`, `NodeCondition` | FR-004, FR-006 |
| Cluster Information | `ClusterInfo` | FR-007 |
| Resource Type Availability | `IClusterOperations.IsResourceTypeAvailableAsync` → `bool` | FR-008 |
| Delete Options (extended) | `DeleteOptions` | FR-003 |
| Patch | `PatchType` + `JsonDocument` on `PatchAsync` | FR-002 |
| Generic Resource Selector | `labelSelector`/`fieldSelector` on `ListAsync` | FR-001, FR-011 |

## Error mapping (reuses 001)

All new operations route through the existing `OperationBase.ExecuteAsync` → `ErrorMapper`, so
they yield the same typed categories: not-found (missing node/resource), connection (unreachable
cluster on cluster-info), validation (malformed patch), authorization, server. Capability
discovery (FR-008) is the deliberate exception: an unavailable type returns `false`, it does not
throw.
