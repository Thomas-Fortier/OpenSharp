# Public API Contract: oc Command Parity (additions to OpenSharp.Core)

Additive surface on top of feature 001's contract. Signatures are indicative; final XML docs
live on the implementation. All async methods accept a `CancellationToken` (001 FR-011). Existing
001 members are unchanged.

## Entry point additions

```csharp
public interface IOpenShiftClient
{
    // … existing 001 facades (Projects, Pods, Deployments, …, Generic) …
    INodeOperations Nodes { get; }       // FR-004, FR-006
    IClusterOperations Cluster { get; }  // FR-007, FR-008
}
```

## Generic escape hatch — extended (US1, FR-001/002/003/005)

```csharp
public interface IGenericOperations
{
    Task<JsonElement> GetAsync(GenericResourceRef reference, CancellationToken ct = default);

    Task<PagedList<JsonElement>> ListAsync(
        GenericResourceRef reference,
        int? limit = null,
        string? continueToken = null,
        string? labelSelector = null,     // NEW (server-side for named groups)
        string? fieldSelector = null,     // NEW (optional; SHOULD per FR-011)
        CancellationToken ct = default);

    Task<JsonElement> CreateAsync(GenericResourceRef reference, JsonElement body, CancellationToken ct = default);

    Task<JsonElement> PatchAsync(                        // NEW (FR-002)
        GenericResourceRef reference, JsonDocument patch,
        PatchType type = PatchType.Merge, CancellationToken ct = default);

    Task DeleteAsync(GenericResourceRef reference, CancellationToken ct = default); // existing

    Task DeleteAsync(                                    // NEW overload (FR-003)
        GenericResourceRef reference, DeleteOptions options, CancellationToken ct = default);
}
```

`Group == ""` on the reference routes to the core (legacy) API path (FR-005). Selector filtering
is server-side for named groups; core-group list is unfiltered (research D2).

## Delete options (US1, FR-003) — applies to first-class and generic

```csharp
public sealed class DeleteOptions
{
    public DeletePropagationPolicy Propagation { get; init; } = DeletePropagationPolicy.Background;
    public int? GracePeriodSeconds { get; init; }   // 0 = immediate
    public bool Force { get; init; }                 // true ⇒ effective grace period 0
}

public interface IWriteOperations<T>
{
    // … existing Create/Replace/Patch and propagation-only Delete …
    Task DeleteAsync(string name, string? @namespace, DeleteOptions options,
        CancellationToken ct = default);             // NEW overload
}

public enum PatchType { Merge, JsonMerge, StrategicMerge, Json }   // maps to V1Patch.PatchType
```

## Nodes & node administration (US2, FR-004/006)

```csharp
public interface INodeOperations : IReadOperations<Node>, IWatchable<Node>
{
    Task CordonAsync(string name, CancellationToken ct = default);    // spec.unschedulable = true
    Task UncordonAsync(string name, CancellationToken ct = default);  // spec.unschedulable = false
}

public sealed class Node
{
    public required ResourceMetadata Metadata { get; init; }   // cluster-scoped: Namespace null
    public bool Unschedulable { get; init; }
    public IReadOnlyList<NodeCondition> Conditions { get; init; }
    public string? KubeletVersion { get; init; }
}

public sealed class NodeCondition
{
    public required string Type { get; init; }     // Ready, DiskPressure, …
    public required string Status { get; init; }   // True / False / Unknown
    public string? Reason { get; init; }
}
```

`IReadOperations<Node>` namespace parameters are ignored (nodes are cluster-scoped).

## Cluster information & discovery (US3, FR-007/008)

```csharp
public interface IClusterOperations
{
    Task<ClusterInfo> GetInfoAsync(CancellationToken ct = default);  // FR-007

    Task<bool> IsResourceTypeAvailableAsync(                          // FR-008
        string group, string version, string plural, CancellationToken ct = default);
}

public sealed class ClusterInfo
{
    public required string ApiServerEndpoint { get; init; }
    public required string ServerVersion { get; init; }
    public bool Reachable { get; init; }
}
```

## Error contract (reuses 001)

| Condition | Exception |
|-----------|-----------|
| Cluster unreachable / timeout (incl. `GetInfoAsync`) | `OpenShiftConnectionException` |
| Missing node / generic resource on delete/patch/get | `OpenShiftNotFoundException` |
| Malformed patch / invalid request | `OpenShiftValidationException` |
| Authenticated but not permitted | `OpenShiftAuthorizationException` |
| Unexpected server error | `OpenShiftServerException` |

`IsResourceTypeAvailableAsync` is the deliberate non-throwing exception: an unavailable type
returns `false` (FR-008), distinguishing "type unavailable" from "instance not found".

## Contract test obligations (Reqnroll + WireMock, `@live` excluded)

- Generic list filtered by label selector — namespaced and all-namespaces; empty-match ⇒ empty list.
- Generic patch persists a change; invalid patch ⇒ validation error.
- Force / zero-grace delete removes a resource; delete of absent resource ⇒ not-found.
- Nodes: list, get, cordon (→ unschedulable), uncordon (→ schedulable); absent node ⇒ not-found.
- Core-group generic get succeeds (group `""`).
- Cluster info returns endpoint + version + reachable; unreachable ⇒ connection error.
- Resource-type availability: served ⇒ true; not served (e.g. OpenShift type on plain k8s) ⇒ false.
