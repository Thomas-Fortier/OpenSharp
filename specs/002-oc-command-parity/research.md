# Phase 0 Research: oc Command Parity

All decisions below were verified against the `KubernetesClient` **19.0.2** surface already
referenced by `src/OpenSharp.Core/OpenSharp.Core.csproj` (confirmed present: `GetCodeAsync`,
`GenericClient`, `GetAPIResources`/`GetAPIVersions`/`GetAPIGroup`, `Patch/DeleteNamespacedCustomObject`,
`ReadNode`/`ListNode`/`PatchNode`, `VersionInfo`). No NEEDS CLARIFICATION remained from the spec;
the spec's Assumptions resolved scope (drain out, merge-patch default, cluster-info subset).

## D1 — Label/field selectors on generic list (FR-001, FR-011)

**Decision**: Add `labelSelector` (string) and optional `fieldSelector` (string) parameters to
`IGenericOperations.ListAsync`. For named API groups, pass them straight through to the existing
`ListNamespacedCustomObjectAsync` / `ListClusterCustomObjectAsync` calls, which already accept
both (we already use `labelSelector` in `RouteOperations`/`DeploymentOperations`).

**Rationale**: The capability already exists one layer down; the only gap is that the generic
facade never exposed it. The driving workflow (KubeVirt `virtualmachines` filtered by
`aircraftType`) is a *named*-group CRD, so this fully unblocks it server-side with paging intact.

**Alternatives considered**: Client-side filtering after an unfiltered list — rejected, breaks
bounded-memory (SC/FR-012) on large collections and is wasteful.

## D2 — Core (legacy) API-group reach for the generic mechanism (FR-005)

**Decision**: Route generic calls by `GenericResourceRef.Group`: keep the current
`CustomObjects` path for **named** groups (`Group != ""`), and use `k8s.GenericClient` for the
**core** group (`Group == ""`), which addresses `/api/{version}/...` correctly. `GenericClient`
provides `ListAsync`/`ListNamespacedAsync`/`ReadAsync`/`ReadNamespacedAsync`/`CreateAsync`/
`DeleteAsync`/`PatchAsync`.

**Rationale**: `CustomObjects` always targets `/apis/{group}/...` and cannot serve core
resources; `GenericClient` constructs the correct core path. Splitting on empty group is a
small, well-contained branch.

**Limitation & follow-up**: `GenericClient.ListAsync` in 19.0.2 does not surface
`labelSelector`/`fieldSelector` parameters. Since every selector-filtered command in the
reference set is named-group, **selector filtering is specified and tested for named groups**;
core-group generic *list* returns unfiltered results, and selector support there is recorded as
a follow-on (documented in quickstart and data-model). Core-group get/create/delete/patch are
fully supported. This keeps FR-001 satisfied where it actually applies without a brittle
client-side filter.

## D3 — Generic patch (FR-002)

**Decision**: Add `IGenericOperations.PatchAsync(GenericResourceRef reference, JsonDocument patch,
PatchType type = PatchType.Merge, CancellationToken ct)`. Implement with
`Patch{Namespaced|Cluster}CustomObjectAsync` (named groups) / `GenericClient.PatchAsync`
(core group), wrapping the document in `V1Patch` with the mapped `V1Patch.PatchType`. Introduce a
small `PatchType` enum (`Merge`, `JsonMerge`, `StrategicMerge`, `Json`) so callers can pick the
strategy; default `Merge` (JSON merge patch) as the spec assumes.

**Rationale**: Mirrors how first-class `PatchAsync` already wraps `V1Patch` in
`PodOperations`/`RouteOperations`; merge patch is the safe, widely-supported default and matches
typical `oc patch`/cordon usage.

**Alternatives considered**: A single hard-coded merge patch — rejected; cordon and CRD updates
sometimes need strategic/JSON patch, and exposing the strategy is cheap.

## D4 — Delete options: grace period & force (FR-003)

**Decision**: Introduce a `DeleteOptions` type `{ DeletePropagationPolicy Propagation = Background;
int? GracePeriodSeconds = null; bool Force = false }`. Add additive overloads:
`IWriteOperations<T>.DeleteAsync(string name, string? @namespace, DeleteOptions options,
CancellationToken ct)` and `IGenericOperations.DeleteAsync(GenericResourceRef reference,
DeleteOptions options, CancellationToken ct)`. Map to the underlying delete calls'
`gracePeriodSeconds` + `propagationPolicy` parameters; `Force == true` implies
`gracePeriodSeconds = 0` (matching `oc delete --force --grace-period=0`). The existing
propagation-only `DeleteAsync` overloads are kept and delegate to the new path with default
options, so feature 001's public contract is unchanged.

**Rationale**: Both `CoreV1.Delete*Async` and `Delete{Namespaced|Cluster}CustomObjectAsync`
accept `gracePeriodSeconds` and `propagationPolicy`. Additive overloads avoid a breaking change
to the 001 contract while exposing the full delete semantics.

**Alternatives considered**: Adding more optional parameters to the existing `DeleteAsync` —
rejected; a single `DeleteOptions` object is clearer and future-proof, and avoids ambiguous
optional-argument call sites.

## D5 — First-class Nodes + cordon/uncordon (FR-004, FR-006)

**Decision**: Add a `Node` model (cluster-scoped) and `INodeOperations : IReadOperations<Node>,
IWatchable<Node>` with `CordonAsync(string name, CancellationToken ct)` and
`UncordonAsync(string name, CancellationToken ct)`. Implement `NodeOperations` over
`CoreV1.ListNodeAsync` / `ReadNodeAsync`; cordon/uncordon issue a merge patch of
`spec.unschedulable` (`true`/`false`) via `PatchNodeAsync`. `Node` exposes identity,
schedulability (`Unschedulable`), and conditions (`NodeCondition { Type, Status, Reason }`).
Because nodes are cluster-scoped, `IReadOperations<Node>` namespace arguments are ignored.

**Rationale**: Nodes live in the core group and are cleanly served by `CoreV1`; cordon is exactly
a `spec.unschedulable` patch (what `oc adm cordon` does). Reusing `IReadOperations`/`IWatchable`
keeps the surface consistent with every other resource.

**Alternatives considered**: Reaching nodes through the generic core-group path (D2) —
rejected for the *first-class* requirement; nodes are common enough (and cordon is specific
enough) to warrant a typed operation. Generic core-group access still covers other core types.

## D6 — Cluster information (FR-007)

**Decision**: Add `ClusterInfo { string ApiServerEndpoint; string ServerVersion; bool Reachable }`
and `IClusterOperations.GetInfoAsync(CancellationToken ct)`. Endpoint comes from the underlying
client's `BaseUri`; version from `GetCodeAsync()` (`VersionInfo.GitVersion`); `Reachable` is
`true` when the version call succeeds, otherwise the call surfaces a typed
`OpenShiftConnectionException` (per the unreachable edge case).

**Rationale**: `oc cluster-info` essentials are "which server, what version, is it up". `BaseUri`
and `GetCodeAsync` provide exactly that without extra dependencies.

**Alternatives considered**: Parsing the full `oc cluster-info` service list — rejected; the spec
scopes cluster-info to endpoint/version/reachability.

## D7 — Resource-type availability discovery (FR-008)

**Decision**: Add `IClusterOperations.IsResourceTypeAvailableAsync(string group, string version,
string plural, CancellationToken ct)` returning `bool`. Implement via API discovery: for the
core group use `GetAPIResources`/`GetAPIVersions`; for named groups use
`GetAPIResources(group, version)` / `GetAPIGroup` and check whether `plural` appears in the
advertised resource list. A missing group/version yields `false` (available = false) rather than
throwing, so callers can branch "type unavailable" vs "instance not found" (reinforces 001
FR-015).

**Rationale**: Discovery is the authoritative, side-effect-free way to answer "is this served?"
without attempting and catching a failed operation. It also gives `RouteOperations`-style
graceful degradation a cleaner basis than string-matching 404 bodies.

**Alternatives considered**: Issue a list and catch the error — rejected; conflates
"unavailable" with transient/permission failures and is not side-effect-free.

## D8 — Backward compatibility & DI

**Decision**: All changes are additive. New facades `Nodes` (`INodeOperations`) and `Cluster`
(`IClusterOperations`) are added to `IOpenShiftClient` and constructed in `OpenShiftClient`
alongside the existing ones; no change to `AddOpenSharp(...)` is required because registration
flows through the single `IOpenShiftClient`. Existing `IGenericOperations`/`IWriteOperations`
members keep their signatures; new selectors are added as parameters with defaults and new
behavior via overloads/new members.

**Rationale**: Preserves feature 001's published surface (SC-008: no regression to its
SC-005/FR-011 guarantees) while extending it.

## Testing approach (Constitution IV)

- **Unit (xUnit + Moq)**: cordon/uncordon patch-body shaping; `DeleteOptions` → grace/force/
  propagation mapping (incl. `Force ⇒ gracePeriodSeconds 0`); generic selector/patch request
  shaping; `ClusterInfo`/availability mapping and the not-throwing "unavailable" path. These hit
  pure shaping/mapping logic without a live client.
- **System (Reqnroll + WireMock.Net)**: drive full flows against the simulator — generic list
  filtered by label (namespaced + all-namespaces), generic patch, force/zero-grace delete, node
  list/get/cordon/uncordon, core-group generic get, cluster-info, and availability (served vs not
  served). Simulator gains node, `/version`, discovery, and selector-aware stubs.
- **Coverage**: combined unit + system coverage keeps `OpenSharp.Core` ≥80% (gate enforced in CI
  via merged ReportGenerator summary, as established in feature 001).
