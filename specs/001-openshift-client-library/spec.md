# Feature Specification: OpenShift Client Library

**Feature Branch**: `001-openshift-client-library`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "Create a C# classlib that is responsible for interacting with OpenShift. I basically would like a means to programmatically interact with OpenShift with a C# library so we can create other applications that interact with the pods, namespaces, containers and everything OpenShift has to offer. Please research existing implementations to determine if this project is unnecessary. Do research and determine how we should do this as well. Should it be a wrapper for the 'oc' project? Should it be its own mirror of 'oc' but within C#? Regardless, the end goal is to have a library that is capable of interacting with OpenShift much like the 'oc' commands themselves."

## Research & Decisions

This section records the research the request asked for and the decisions confirmed with
the stakeholder. It informs scope; implementation detail belongs in the plan.

**Is this project necessary?** Yes. No actively maintained, comprehensive native .NET
OpenShift client exists. The only dedicated one is effectively abandoned (minimal commits,
demonstrates pods only). The official Kubernetes .NET client is mature and already works
against OpenShift's Kubernetes-compatible surface (pods, namespaces, deployments, services,
config), but it offers no first-class support for OpenShift-specific resources (Routes,
Projects, DeploymentConfigs, BuildConfigs, ImageStreams) and no `oc`-style high-level
operations. The gap this library fills is that OpenShift-aware, ergonomic layer.

**Chosen approach (confirmed):** A native client that builds on the official, mature
Kubernetes .NET client for core resources and adds strongly-typed OpenShift resources and
`oc`-style operations on top. **No `oc` binary is required at runtime.** This was chosen
over (a) wrapping the `oc` CLI and (b) a fully generated OpenAPI client, because it best
satisfies the project constitution: cross-platform, performant (no process spawning),
strongly typed, and mockable for testing.

**MVP breadth (confirmed):** A core set of the most-used resources plus an extensibility
path for anything not yet wrapped (see Functional Requirements).

**Operations (confirmed):** Full lifecycle (create/read/update/delete) plus operational
actions (logs, exec, scale, rollout) on supported resources.

## Clarifications

### Session 2026-06-18

- Q: When a watch stream terminates (drop / expired resourceVersion), what should the library do? → A: Auto-resume from the last observed resourceVersion/bookmark by default, configurable to opt out; surface a terminal error only when resume is impossible.
- Q: Should the library authenticate from inside a cluster using the mounted service account? → A: Yes — auto-detect the in-pod service-account token/CA when running in a cluster, otherwise fall back to kubeconfig/explicit token.
- Q: Default deletion behavior for resources with dependents? → A: Background cascade by default, with a configurable propagation policy (Background/Foreground/Orphan).
- Q: Auto-refresh credentials for long-running sessions? → A: Yes — support exec/auth-provider token refresh via the underlying client; static bearer tokens still error on expiry.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Connect and read cluster state (Priority: P1)

A .NET developer building a tool needs to connect to an OpenShift cluster using their
existing credentials and read the state of resources — list the projects they can access,
list and inspect pods in a project, and read details of deployments and services — without
shelling out to an external command-line tool.

**Why this priority**: Authentication plus read access is the foundation every other
capability depends on and is independently the most common need (dashboards, monitoring,
inventory). It is the smallest slice that delivers real value on its own.

**Independent Test**: Point the library at a reachable cluster using standard credentials,
list accessible projects, list pods in a chosen project, and retrieve a single pod's
details — all returning strongly-typed results with no `oc`/`kubectl` binary installed.

**Acceptance Scenarios**:

1. **Given** valid cluster credentials are available, **When** the developer creates a
   client, **Then** the client connects and authenticates without requiring any external
   binary.
2. **Given** an authenticated client, **When** the developer requests the list of accessible
   projects/namespaces, **Then** the library returns them as strongly-typed objects.
3. **Given** an authenticated client and a project name, **When** the developer lists pods in
   that project, **Then** each pod's identity, status, and containers are accessible as typed
   properties.
4. **Given** invalid or expired credentials, **When** the developer creates a client or makes
   a call, **Then** the library surfaces a clear, typed authentication/authorization error
   distinguishable from other failures.

---

### User Story 2 - Manage resource lifecycle (Priority: P2)

A developer needs to create, update, and delete resources — including OpenShift-specific
ones such as Routes and DeploymentConfigs as well as core ones such as Deployments,
Services, ConfigMaps, and Secrets — so applications can provision and reconcile what they
need on a cluster.

**Why this priority**: Mutating resources is the next most valuable capability after reading
them and turns the library from an observer into an automation tool. It depends on US1's
connection/auth foundation.

**Independent Test**: Create a Route (or other supported resource) in a project, read it
back, update one of its fields, confirm the change, then delete it and confirm removal.

**Acceptance Scenarios**:

1. **Given** an authenticated client, **When** the developer creates a supported resource
   from a typed definition, **Then** the resource is created and the created object is
   returned.
2. **Given** an existing supported resource, **When** the developer updates a field, **Then**
   the change is persisted and reflected on subsequent reads.
3. **Given** an existing supported resource, **When** the developer deletes it, **Then** it is
   removed and a subsequent read reports it as absent.
4. **Given** a create/update that violates cluster rules (e.g., name conflict, validation
   failure), **When** the operation runs, **Then** the library surfaces a clear, typed error
   describing the cause.

---

### User Story 3 - Operational actions on workloads (Priority: P3)

A developer building operational tooling needs to perform the everyday actions `oc` users
rely on: read and stream a pod/container's logs, execute a command inside a container, scale
a workload, and trigger a rollout/restart of a deployment.

**Why this priority**: These actions make the library viable for real operational tools
(debugging, dashboards, automation) but build on the connection (US1) and benefit from
lifecycle management (US2).

**Independent Test**: Stream logs from a running pod's container, execute a simple command in
a container and capture its output, scale a deployment to a new replica count, and trigger a
rollout — each verifiable independently.

**Acceptance Scenarios**:

1. **Given** a running pod, **When** the developer requests its container logs, **Then** the
   library returns the logs and supports following (streaming) new output.
2. **Given** a running container, **When** the developer executes a command in it, **Then**
   the library returns the command's output and exit status.
3. **Given** a scalable workload, **When** the developer sets a new replica count, **Then**
   the cluster scales the workload accordingly.
4. **Given** a deployment, **When** the developer triggers a rollout/restart, **Then** a new
   rollout begins and its progress is observable.

---

### User Story 4 - Watch for changes and reach unwrapped resources (Priority: P4)

A developer needs to react to changes in resources over time (added/modified/deleted) and,
when a resource type is not yet first-class in the library, still access it through a generic
mechanism so the library never fully blocks them.

**Why this priority**: Watching enables reactive tooling and the generic escape hatch
guarantees the "extensibility" promise of the MVP, but neither is required for the core
value of US1–US3.

**Independent Test**: Start a watch on pods in a project, cause a change, and confirm the
corresponding change notification is delivered; separately, perform a get/list on a resource
type not explicitly wrapped using the generic mechanism.

**Acceptance Scenarios**:

1. **Given** an authenticated client, **When** the developer watches a supported resource
   type, **Then** added/modified/deleted events are delivered until the watch is stopped.
2. **Given** a resource type not yet first-class in the library, **When** the developer uses
   the generic access mechanism, **Then** they can perform basic operations on it by
   specifying its API group/kind.

---

### Edge Cases

- What happens when the cluster is unreachable or the connection times out? The library MUST
  surface a clear, typed connectivity error and MUST NOT hang indefinitely.
- How does the system handle expired or insufficient credentials mid-session? Operations
  MUST fail with a typed authentication/authorization error.
- What happens when a requested project/resource does not exist or the caller lacks
  permission? The library MUST distinguish "not found" from "forbidden".
- How are large result sets handled? Listing MUST support paging/continuation so memory use
  stays bounded for large clusters.
- What happens when a streamed log or watch connection drops? For watches, the library MUST
  by default automatically resume from the last observed resourceVersion/bookmark, raising a
  terminal error only when resumption is impossible (e.g., the resourceVersion is too old);
  callers MUST be able to opt out and handle reconnection themselves. For log streams, the
  consumer MUST be able to detect termination and re-establish the stream.
- How does the library behave when targeting a plain Kubernetes cluster (no OpenShift
  extensions)? OpenShift-specific operations MUST fail with a clear, typed error indicating
  the resource type is unavailable on the target.
- How are long-running operations cancelled? All potentially long-running calls MUST honor a
  caller-provided cancellation signal.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The library MUST authenticate to an OpenShift cluster using a developer's
  existing standard credentials (kube/oc configuration file and/or bearer token) without
  requiring any external command-line binary at runtime. When running inside a cluster (as a
  pod), the library MUST auto-detect and use the mounted service-account token and CA,
  falling back to kubeconfig/explicit token otherwise.
- **FR-002**: The library MUST allow configuring the target cluster endpoint and TLS trust
  behavior, including the standard configuration resolution that `oc`/`kubectl` users expect.
- **FR-002a**: For long-running sessions, the library MUST support automatic credential
  refresh where the credential source provides it (kubeconfig `exec`/auth-provider plugins);
  static bearer tokens that expire MUST surface a typed authentication error.
- **FR-003**: The library MUST support reading core resources: Projects/Namespaces, Pods (and
  their containers), Deployments, DeploymentConfigs, Services, Routes, ConfigMaps, and
  Secrets — exposing their data as strongly-typed objects.
- **FR-004**: The library MUST support creating, updating, and deleting the resources listed
  in FR-003. Deletion MUST default to background cascade of dependents and MUST allow the
  caller to choose the propagation policy (Background, Foreground, or Orphan).
- **FR-005**: The library MUST support reading and streaming (following) container logs.
- **FR-006**: The library MUST support executing a command inside a running container and
  returning its output and exit status.
- **FR-007**: The library MUST support scaling a workload to a specified replica count and
  triggering a rollout/restart of a deployment.
- **FR-008**: The library MUST support watching supported resource types and delivering
  added/modified/deleted change notifications until the caller stops the watch. Watches MUST
  by default auto-resume from the last observed resourceVersion/bookmark after a transient
  termination, with a caller opt-out to manage reconnection manually.
- **FR-009**: The library MUST provide a generic mechanism to operate on resource types that
  are not yet first-class, identified by API group/version/kind, so coverage is extensible
  without a new release.
- **FR-010**: The library MUST report errors as typed, distinguishable categories at minimum:
  connectivity/timeout, authentication, authorization/forbidden, not-found, validation/
  conflict, and unexpected server error.
- **FR-011**: All potentially long-running or network operations MUST be asynchronous and MUST
  honor a caller-supplied cancellation signal.
- **FR-012**: List operations MUST support continuation/paging so memory consumption remains
  bounded on large clusters.
- **FR-013**: The library MUST be consumable via dependency injection, exposing its
  capabilities through interfaces that consumers can substitute/mock in their own tests.
- **FR-014**: The library MUST function identically across Windows, Linux, and macOS, with no
  OS-specific behavior unless explicitly specified.
- **FR-015**: When targeting a cluster that lacks OpenShift extensions, OpenShift-specific
  operations MUST fail with a clear, typed error rather than undefined behavior.
- **FR-016**: The library's core logic MUST reside in a standalone class library independent
  of any specific application or hosting model.

### Key Entities *(include if feature involves data)*

- **Cluster Connection**: An authenticated session against a cluster endpoint; holds
  credentials, endpoint, and TLS trust settings; the entry point for all operations.
- **Project / Namespace**: An isolated grouping of resources the caller may access; the scope
  for most operations.
- **Pod**: A running unit composed of one or more containers; has identity, status, and
  per-container details; source of logs and exec targets.
- **Container**: A process environment within a pod; target for logs and command execution.
- **Workload (Deployment / DeploymentConfig)**: A managed, scalable set of pods; subject of
  scale and rollout actions.
- **Service**: A stable network endpoint fronting a set of pods.
- **Route**: An OpenShift construct exposing a service externally (an OpenShift-specific
  resource a plain Kubernetes client lacks).
- **Configuration / Secret (ConfigMap, Secret)**: Named configuration and sensitive data
  consumed by workloads.
- **Change Event**: An added/modified/deleted notification emitted by a watch.
- **Operation Error**: A typed description of a failed operation categorized per FR-010.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can connect to a cluster and list pods in a project in 15 lines of
  consuming code or fewer.
- **SC-002**: All capabilities work with no `oc` or `kubectl` binary installed on the host.
- **SC-003**: The same consuming code runs unmodified and passes its tests on Windows, Linux,
  and macOS.
- **SC-004**: A developer can perform a full create → read → update → delete cycle on a
  supported resource (including a Route) using only the library.
- **SC-005**: Every public capability is exposed through an interface that can be substituted
  with a test double, enabling consumers to unit-test their code without a live cluster.
- **SC-006**: Listing 10,000 resources completes without unbounded memory growth by using
  continuation/paging.
- **SC-007**: Every failure mode in FR-010 is observably distinguishable by consuming code
  (i.e., a caller can branch on the error category).
- **SC-008**: 90% of the resource operations a typical `oc get`/`create`/`delete`/`logs`/
  `exec`/`scale` user performs on the supported resource set are achievable through the
  library without dropping to the generic escape hatch.

## Assumptions

- The consuming application runs on a currently supported .NET runtime; the library targets a
  broadly compatible .NET standard/version (exact target decided in planning).
- Callers already possess valid credentials for the target cluster (the library consumes
  existing kube/oc configuration, explicit tokens, or — when running in a pod — the mounted
  service-account credentials; it does not implement an interactive login flow in v1).
- "Everything OpenShift has to offer" is delivered incrementally: v1 covers the core resource
  set in FR-003 with full operations, and the generic mechanism (FR-009) covers the rest;
  additional first-class resources (e.g., BuildConfigs, ImageStreams, Templates, quotas, RBAC)
  are follow-on work.
- The official, maintained Kubernetes .NET client is an acceptable foundation dependency for
  the Kubernetes-compatible surface; the library adds the OpenShift-specific layer on top.
- Network access from the host to the cluster API endpoint is available.
- Cluster-side authorization (RBAC) governs what a given caller can do; the library does not
  add or replace its own permission model.
