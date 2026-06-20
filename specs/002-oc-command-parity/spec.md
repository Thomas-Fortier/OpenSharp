# Feature Specification: oc Command Parity — Cluster, Node & Generic Operation Coverage

**Feature Branch**: `002-oc-command-parity`

**Created**: 2026-06-19

**Status**: Draft

**Input**: User description: "Update the spec to add everything you identified" — referring to the gaps found when assessing whether a set of real-world `oc` (PowerShell) commands could be translated into the OpenSharp client library built in feature 001.

## Context & Reference Workflows

This feature extends [feature 001 — OpenShift Client Library](../001-openshift-client-library/spec.md). It does **not** redefine 001's connection/auth, error model, dependency injection, paging, or cross-platform requirements — those are inherited and continue to apply to everything added here.

The scope was derived by checking whether the following operational `oc` commands (drawn from a KubeVirt/Ceph fleet-management script) translate directly into the existing library. The ones that did **not** translate cleanly define this feature:

| `oc` command | Status against feature 001 | Addressed by |
|---|---|---|
| `get vm --all-namespaces -l aircraftType=… -o json` | ⚠ list works, but no label filtering on generic resources | US1 |
| `get vm -n $ns ${label} -o json` | ⚠ label filtering missing | US1 |
| `get vm --all-namespaces ${label} -o json` | ⚠ label filtering missing | US1 |
| `delete vmi $name -n $ns --force --grace-period=0` | ⚠ deletes, but no force / grace-period control | US1 |
| `get nodes -o json` | ❌ no access to cluster-scoped core resources | US2 |
| `adm cordon $nodeName` | ❌ no node administration | US2 |
| `cluster-info` | ❌ no cluster information / discovery | US3 |

Commands that already translate in feature 001 (e.g. `get pods/deployments -n $ns`, `scale deployment … --replicas=0`, `get vm $name -n $ns -o json`, `get namespace`, `get pod … -o jsonpath={.status…}`) are out of scope here; output-formatting flags (`-o json`, `-o jsonpath`, `--no-headers`) are consumer presentation concerns and are not library responsibilities.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Filter, patch, and force-delete any resource type (Priority: P1)

A developer automating a fleet of custom resources (e.g. KubeVirt VirtualMachines and VirtualMachineInstances, reached through the generic mechanism because they are CRDs) needs to: select resources by label across a namespace or the whole cluster; apply a partial update to a resource; and delete a resource immediately, bypassing graceful termination.

**Why this priority**: These three gaps block the actual driving workflow (selecting VMs by `aircraftType` and force-deleting stuck VMIs). They are also the lowest-effort, highest-value additions because the underlying capability already exists for first-class resources — it simply is not exposed on the generic mechanism. Delivering this slice alone makes the library usable for the real automation it was evaluated against.

**Independent Test**: Using the generic mechanism, list a custom resource type filtered by a label selector in a single namespace and again across all namespaces; apply a partial update to one instance and confirm the change on read-back; delete an instance with a zero grace period and confirm immediate removal.

**Acceptance Scenarios**:

1. **Given** a custom resource type and a label selector, **When** the developer lists that type in a namespace via the generic mechanism, **Then** only matching resources are returned.
2. **Given** a custom resource type and a label selector, **When** the developer lists that type across all namespaces, **Then** only matching resources from every namespace are returned.
3. **Given** an existing resource reached via the generic mechanism, **When** the developer applies a partial update (patch), **Then** the change is persisted and reflected on a subsequent read.
4. **Given** an existing resource, **When** the developer deletes it with a zero (immediate) grace period and force semantics, **Then** the resource is removed without waiting for graceful termination.
5. **Given** a label selector that matches nothing, **When** the developer lists, **Then** an empty result is returned rather than an error.

---

### User Story 2 - Access and administer cluster-scoped core resources (Priority: P2)

An operator needs to inspect cluster Nodes and take a node out of (and back into) scheduling rotation — for example, to cordon a node before maintenance — and, more generally, to reach resources that live in the core (legacy) API group rather than a named API group.

**Why this priority**: Node inspection and cordon/uncordon are required by the maintenance portion of the reference workflow (`get nodes`, `adm cordon`). They depend on reaching core/cluster-scoped resources, which feature 001 deliberately scoped out, so this is a genuine capability addition rather than a small exposure.

**Independent Test**: List Nodes and retrieve a single Node with its status and schedulability; mark a Node unschedulable (cordon) and confirm; mark it schedulable again (uncordon) and confirm; separately, read a core-group resource through the generic mechanism.

**Acceptance Scenarios**:

1. **Given** an authenticated client, **When** the operator lists Nodes, **Then** each Node's identity, status/conditions, and schedulability are returned as accessible data.
2. **Given** a Node name, **When** the operator retrieves that Node, **Then** its details (including whether it is currently schedulable) are returned.
3. **Given** a schedulable Node, **When** the operator cordons it, **Then** the Node is marked unschedulable and a subsequent read reflects that.
4. **Given** a cordoned Node, **When** the operator uncordons it, **Then** the Node is marked schedulable again.
5. **Given** a resource type in the core (legacy) API group, **When** the operator accesses it through the generic mechanism, **Then** the operation succeeds (core-group resources are reachable, not only named-group resources).
6. **Given** a Node name that does not exist, **When** the operator retrieves or cordons it, **Then** a typed "not found" error is surfaced.

---

### User Story 3 - Retrieve cluster information and verify resource-type availability (Priority: P3)

A developer needs to confirm which cluster they are talking to and what it supports — the API server endpoint and version — and to determine programmatically whether a given resource type is served by the target cluster (so they can branch on "this cluster does not offer that type" versus "that specific resource was not found").

**Why this priority**: Cluster information (`cluster-info`) is a convenience for diagnostics and multi-cluster tooling, and capability discovery strengthens the graceful-degradation behavior introduced in feature 001 (FR-015). Both are valuable but not blockers for the core automation, so they come last.

**Independent Test**: Retrieve the cluster's API endpoint and server version; query whether a specific API group/version/resource is served and confirm the answer distinguishes an unavailable type from a missing instance.

**Acceptance Scenarios**:

1. **Given** an authenticated client, **When** the developer requests cluster information, **Then** the API server endpoint, server version, and reachability are returned.
2. **Given** a target cluster, **When** the developer asks whether a given API group/version/resource type is served, **Then** the library reports availability without throwing for the "unavailable" case.
3. **Given** an OpenShift-specific type requested against a plain Kubernetes cluster, **When** the developer checks availability, **Then** the result is "unavailable" and is distinguishable from a "not found" instance error (reinforces feature 001 FR-015).

---

### Edge Cases

- A label selector that matches no resources MUST yield an empty list, not an error.
- Force/immediate delete of a non-existent resource MUST surface a typed "not found" error, consistent with feature 001's error categories.
- Cordoning or retrieving a non-existent Node MUST surface a typed "not found" error.
- Requesting cluster information when the cluster is unreachable MUST surface a typed connectivity error and MUST NOT hang indefinitely.
- A malformed or invalid patch document MUST surface a typed validation error.
- Generic access to a core (legacy) API-group resource MUST resolve correctly even though its address differs from named-group resources.
- Capability discovery for a type that is not served MUST return "unavailable" rather than raising the generic-error path.
- All new long-running or network operations MUST honor a caller-supplied cancellation signal (inherited from feature 001 FR-011).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The generic resource mechanism MUST allow listing by label selector, for both a single namespace and across all namespaces, returning only matching resources.
- **FR-002**: The generic resource mechanism MUST support applying a partial update (patch) to a resource identified by API group/version/kind, persisting the change.
- **FR-003**: Delete operations (both first-class and generic) MUST allow the caller to specify a grace period — including an immediate/zero grace period and force semantics — in addition to the propagation policy already provided by feature 001.
- **FR-004**: The library MUST support reading cluster-scoped core resources, specifically Nodes — list and get — exposing each Node's identity, status/conditions, and schedulability as accessible data.
- **FR-005**: The generic resource mechanism MUST be able to reach resources in the core (legacy) API group, not only resources in named API groups.
- **FR-006**: The library MUST support node administration actions: cordon (mark a Node unschedulable) and uncordon (mark it schedulable).
- **FR-007**: The library MUST provide cluster information: the API server endpoint, the server version, and a reachability indication.
- **FR-008**: The library MUST allow a caller to determine whether a given API group/version/resource type is served by the target cluster, returning an "available/unavailable" result without raising for the unavailable case, so callers can distinguish "type unavailable" from "instance not found".
- **FR-009**: Every capability added by this feature MUST conform to the cross-cutting requirements inherited from feature 001: fully asynchronous and cancellation-aware (FR-011), typed/distinguishable error categories (FR-010), exposed through mockable interfaces consumable via dependency injection (FR-013), continuation/paging for list operations (FR-012), and identical behavior across Windows, Linux, and macOS (FR-014).
- **FR-010**: First-class additions (Node access and node administration) MUST NOT be prerequisites for the generic mechanism — the generic escape hatch (feature 001 FR-009) MUST continue to function independently and MUST gain the label-selector, patch, and delete-option capabilities above.
- **FR-011**: Field-selector filtering on the generic list mechanism SHOULD be supported where the cluster offers it, but is not required by the reference workflows.

### Key Entities *(include if feature involves data)*

- **Node**: A cluster machine (control-plane or worker) that hosts pods; has identity, status/conditions (e.g. Ready), and schedulability (schedulable vs cordoned). New cluster-scoped, core-group entity.
- **Cluster Information**: A summary of the connected cluster — API server endpoint, server version, and reachability status.
- **Resource Type Availability**: Whether a given API group/version/resource type is served by the target cluster.
- **Delete Options (extended)**: The caller-controlled parameters of a delete — grace period (including immediate/zero), force semantics, and propagation policy.
- **Patch**: A partial-update document applied to an existing resource (first-class or generic).
- **Generic Resource Selector**: The label selector (and optionally field selector) constraining a generic list operation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can list resources of any type (first-class or generic) filtered by a label selector, in both a single namespace and across all namespaces, with no `oc`/`kubectl` binary installed.
- **SC-002**: A developer can delete a resource with a zero grace period and force semantics through the library, matching `--force --grace-period=0`.
- **SC-003**: A developer can apply a partial update to a generically-addressed resource and observe the change on read-back.
- **SC-004**: A developer can list and get Nodes and toggle a Node's schedulability (cordon/uncordon) through the library.
- **SC-005**: A developer can retrieve the connected cluster's API endpoint and server version through the library.
- **SC-006**: A caller can determine whether a resource type is served by the cluster and branch on "type unavailable" versus "instance not found" as distinct outcomes.
- **SC-007**: Every `oc` command in the Reference Workflows table is achievable through the library without shelling out to `oc`, excluding output-formatting flags (`-o json`, `-o jsonpath`, `--no-headers`), which remain consumer-side presentation concerns.
- **SC-008**: Every capability added by this feature is exposed through an interface that can be substituted with a test double and honors cancellation, consistent with feature 001 (no regression to its SC-005/FR-011 guarantees).

## Assumptions

- This feature **extends** feature 001 and reuses its connection/authentication, typed error model, dependency-injection surface, paging, and cross-platform guarantees; it does not re-specify them.
- Node **draining** (`oc adm drain` — evicting/rescheduling pods off a node) is **out of scope** for this feature; only cordon/uncordon (schedulability toggling) is included. Drain is a candidate follow-on because it composes pod eviction with cordon.
- Output-formatting flags (`-o json`, `-o jsonpath=…`, `--no-headers`) are consumer presentation concerns; the library returns typed objects or raw resource documents and leaves formatting to the caller.
- `cluster-info` scope is the API server endpoint, server version, and reachability — not the full list of auxiliary service URLs that the `oc cluster-info` CLI may print.
- KubeVirt resources (VirtualMachine, VirtualMachineInstance) and similar CRDs are reached through the **generic** mechanism rather than being added as first-class types; this feature makes that mechanism complete enough to manage them.
- Label selectors use standard Kubernetes selector syntax; generic patch defaults to a merge-style partial update, with richer patch strategies supported where the target allows.
- Capability discovery relies on the cluster's standard API discovery surface; "unavailable" is determined from the cluster's advertised resources rather than by attempting and catching a failed operation.
- Nodes and other core-group resources are reachable with the same authenticated session and authorization model as feature 001; cluster-side RBAC still governs what the caller may do.
