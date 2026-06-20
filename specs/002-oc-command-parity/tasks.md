---
description: "Task list for oc Command Parity (cluster, node & generic operation coverage)"
---

# Tasks: oc Command Parity — Cluster, Node & Generic Operation Coverage

**Input**: Design documents from `/specs/002-oc-command-parity/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-api.md, quickstart.md

**Tests**: MANDATORY per the OpenSharp Constitution (Principle IV). Unit tests use xUnit + Moq
on `OpenSharp.Core`; system/acceptance tests use Reqnroll driven by the existing WireMock.Net
OpenShift API simulator (no cluster required); the `@live` category stays opt-in. Combined
unit + system coverage MUST keep `OpenSharp.Core` ≥ 80%.

**Organization**: Grouped by user story. This feature **extends** feature 001 — all additions
are source-compatible with its existing public surface.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1–US3 (maps to spec.md user stories)
- Paths follow plan.md: core lib in `src/OpenSharp.Core/`, tests in `tests/`

## Path Conventions

- Core library: `src/OpenSharp.Core/`
- Unit tests: `tests/OpenSharp.Core.UnitTests/`
- System tests: `tests/OpenSharp.SystemTests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the feature-001 baseline and add the small public value types this feature introduces

- [X] T001 Verify baseline: `dotnet build OpenSharp.slnx -c Release` is green and feature-001 unit + system tests pass (`--filter "Category!=live"`); no new NuGet packages are required (all needed KubernetesClient 19.0.2 surface is present)
- [X] T002 [P] Implement `DeleteOptions` value type (`Propagation`, `GracePeriodSeconds`, `Force`) in `src/OpenSharp.Core/Resources/DeleteOptions.cs`
- [X] T003 [P] Implement `PatchType` enum (`Merge`/`JsonMerge`/`StrategicMerge`/`Json`) in `src/OpenSharp.Core/Abstractions/PatchType.cs`

**Checkpoint**: Solution builds with the new value types; no behavior change yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared base-class and test-harness changes the user stories build on

**⚠️ CRITICAL**: The delete-options base refactor (T004–T006) changes the shared
`WriteOperationsBase` that every write operation inherits; complete it before US1's generic
delete work and before touching the simulator broadly.

- [X] T004 Add `DeleteAsync(string name, string? @namespace, DeleteOptions options, CancellationToken ct)` overload to `IWriteOperations<T>` in `src/OpenSharp.Core/Abstractions/IWriteOperations.cs` (keep the existing propagation-only overload)
- [X] T005 Refactor `WriteOperationsBase<T>` to expose both delete overloads, mapping `Force == true` to an effective `GracePeriodSeconds = 0` and delegating the propagation-only overload to the options path, in `src/OpenSharp.Core/Operations/WriteOperationsBase.cs` (depends on T002, T004)
- [X] T006 Thread `DeleteOptions` (grace period + propagation) through every concrete write operation's delete call — `PodOperations`, `ProjectOperations`, `RouteOperations`, `ConfigMapOperations`, `SecretOperations`, `DeploymentOperations`, `ServiceOperations` in `src/OpenSharp.Core/Operations/` (depends on T005)
- [X] T007 [P] Extend the WireMock simulator with shared helpers reused by multiple stories — selector-aware custom-object list, custom-object patch echo, and delete-with-`gracePeriodSeconds` capture — in `tests/OpenSharp.SystemTests/Support/OpenShiftApiSimulator.cs`

**Checkpoint**: Base delete path honors `DeleteOptions`; shared simulator helpers ready.

---

## Phase 3: User Story 1 - Filter, patch, and force-delete any resource type (Priority: P1) 🎯 MVP

**Goal**: Complete the generic escape hatch — label-selector filtering on list (namespaced + all-namespaces), partial-update (patch), and delete with grace-period/force control.

**Independent Test**: Via the generic mechanism, list a custom type filtered by label in a namespace and across all namespaces; patch an instance and confirm on read-back; force-delete with zero grace period and confirm removal; an empty-match selector returns an empty list.

### Tests for User Story 1 (write first, ensure they FAIL) ⚠️

- [X] T008 [P] [US1] Reqnroll feature `tests/OpenSharp.SystemTests/Features/GenericLabelSelector.feature` (label-filtered list, namespaced + all-namespaces, empty-match ⇒ empty)
- [X] T009 [P] [US1] Reqnroll feature `tests/OpenSharp.SystemTests/Features/GenericPatch.feature` (patch persists a change; invalid patch ⇒ validation error)
- [X] T010 [P] [US1] Reqnroll feature `tests/OpenSharp.SystemTests/Features/ForceDelete.feature` (force/zero-grace delete removes a resource; absent resource ⇒ not-found)
- [X] T011 [P] [US1] Unit tests for generic selector/patch request shaping in `tests/OpenSharp.Core.UnitTests/Operations/GenericOperationsTests.cs` (Moq)
- [X] T012 [P] [US1] Unit tests for `DeleteOptions` mapping (`Force ⇒ grace 0`, propagation passthrough) in `tests/OpenSharp.Core.UnitTests/Resources/DeleteOptionsTests.cs`

### Implementation for User Story 1

- [X] T013 [US1] Extend `IGenericOperations` with `labelSelector`/`fieldSelector` on `ListAsync`, a `PatchAsync(ref, JsonDocument, PatchType, ct)`, and a `DeleteAsync(ref, DeleteOptions, ct)` overload in `src/OpenSharp.Core/Abstractions/IGenericOperations.cs`
- [X] T014 [US1] Implement label/field selector pass-through on `GenericOperations.ListAsync` (named groups → `List{Namespaced|Cluster}CustomObjectAsync`) in `src/OpenSharp.Core/Generic/GenericOperations.cs` (depends on T013)
- [X] T015 [US1] Implement `GenericOperations.PatchAsync` (wrap `JsonDocument` in `V1Patch`, map `PatchType`; named-group patch) in `src/OpenSharp.Core/Generic/GenericOperations.cs` (depends on T003, T014)
- [X] T016 [US1] Implement `GenericOperations.DeleteAsync(ref, DeleteOptions, ct)` (grace/force/propagation) in `src/OpenSharp.Core/Generic/GenericOperations.cs` (depends on T005, T015)
- [X] T017 [US1] Add simulator stubs for selector-filtered list, patch echo, and delete-with-grace capture using the shared helpers in `tests/OpenSharp.SystemTests/Support/OpenShiftApiSimulator.cs` (depends on T007)
- [X] T018 [US1] Implement step definitions for US1 features in `tests/OpenSharp.SystemTests/Steps/GenericExtendedSteps.cs`

**Checkpoint**: US1 fully functional — generic list filtering, patch, and force-delete (MVP; unblocks the VM/VMI workflows).

---

## Phase 4: User Story 2 - Access and administer cluster-scoped core resources (Priority: P2)

**Goal**: First-class `Nodes` (list/get) with cordon/uncordon, plus generic reach into the core (legacy) API group.

**Independent Test**: List Nodes and get one with status/schedulability; cordon then uncordon a Node and confirm; read a core-group resource through the generic mechanism; an absent Node yields a typed not-found error.

### Tests for User Story 2 (write first, ensure they FAIL) ⚠️

- [X] T019 [P] [US2] Reqnroll feature `tests/OpenSharp.SystemTests/Features/Nodes.feature` (list, get, cordon → unschedulable, uncordon → schedulable; absent node ⇒ not-found)
- [X] T020 [P] [US2] Reqnroll feature `tests/OpenSharp.SystemTests/Features/CoreGroupGeneric.feature` (generic get on a core-group resource, FR-005)
- [X] T021 [P] [US2] Unit tests for node mapping and cordon/uncordon patch-body shaping in `tests/OpenSharp.Core.UnitTests/Operations/NodeOperationsTests.cs` (Moq)

### Implementation for User Story 2

- [X] T022 [P] [US2] Implement `Node` + `NodeCondition` models (mapping from `V1Node`) in `src/OpenSharp.Core/Resources/Node.cs`
- [X] T023 [P] [US2] Define `INodeOperations` (`IReadOperations<Node>` + `IWatchable<Node>` + `CordonAsync`/`UncordonAsync`) in `src/OpenSharp.Core/Abstractions/INodeOperations.cs` (depends on T022)
- [X] T024 [US2] Implement `NodeOperations` over `CoreV1` (list/get/watch) with cordon/uncordon as a `spec.unschedulable` merge patch in `src/OpenSharp.Core/Operations/NodeOperations.cs` (depends on T022, T023)
- [X] T025 [US2] Add core-group reach to `GenericOperations` (route `Group == ""` to `GenericClient`/core path for get/list/create/delete/patch) in `src/OpenSharp.Core/Generic/GenericOperations.cs` (sequential after T016)
- [X] T026 [US2] Expose `Nodes` on `IOpenShiftClient` and construct `NodeOperations` in `src/OpenSharp.Core/Abstractions/IOpenShiftClient.cs` and `src/OpenSharp.Core/Operations/OpenShiftClient.cs` (depends on T024)
- [X] T027 [US2] Add simulator stubs for node list/get/patch and a core-group resource get in `tests/OpenSharp.SystemTests/Support/OpenShiftApiSimulator.cs`
- [X] T028 [US2] Implement step definitions for US2 features in `tests/OpenSharp.SystemTests/Steps/NodeSteps.cs`

**Checkpoint**: US1 and US2 both independently functional.

---

## Phase 5: User Story 3 - Retrieve cluster information and verify resource-type availability (Priority: P3)

**Goal**: Cluster information (API endpoint, server version, reachability) and capability discovery (is a given group/version/resource served?).

**Independent Test**: Retrieve cluster info (endpoint + version + reachable); query availability of a served type (true) and an unavailable type such as an OpenShift type on plain Kubernetes (false, without throwing); an unreachable cluster surfaces a typed connection error.

### Tests for User Story 3 (write first, ensure they FAIL) ⚠️

- [X] T029 [P] [US3] Reqnroll feature `tests/OpenSharp.SystemTests/Features/ClusterInfo.feature` (info returns endpoint+version+reachable; availability served ⇒ true, not served ⇒ false)
- [X] T030 [P] [US3] Unit tests for cluster-info mapping, availability true/false, and unreachable ⇒ connection error in `tests/OpenSharp.Core.UnitTests/Operations/ClusterOperationsTests.cs` (Moq)

### Implementation for User Story 3

- [X] T031 [P] [US3] Implement `ClusterInfo` model (`ApiServerEndpoint`, `ServerVersion`, `Reachable`) in `src/OpenSharp.Core/Resources/ClusterInfo.cs`
- [X] T032 [P] [US3] Define `IClusterOperations` (`GetInfoAsync`, `IsResourceTypeAvailableAsync`) in `src/OpenSharp.Core/Abstractions/IClusterOperations.cs` (depends on T031)
- [X] T033 [US3] Implement `ClusterOperations` — endpoint from `BaseUri`, version from `GetCodeAsync`, availability via `GetAPIResources`/`GetAPIGroup` discovery (unavailable ⇒ `false`, not thrown) in `src/OpenSharp.Core/Operations/ClusterOperations.cs` (depends on T031, T032)
- [X] T034 [US3] Expose `Cluster` on `IOpenShiftClient` and construct `ClusterOperations` in `src/OpenSharp.Core/Abstractions/IOpenShiftClient.cs` and `src/OpenSharp.Core/Operations/OpenShiftClient.cs` (depends on T033; sequential after T026 — same files)
- [X] T035 [US3] Add simulator stubs for the `/version` endpoint and API discovery (groups/resources, served vs not-served) in `tests/OpenSharp.SystemTests/Support/OpenShiftApiSimulator.cs`
- [X] T036 [US3] Implement step definitions for US3 features in `tests/OpenSharp.SystemTests/Steps/ClusterSteps.cs`

**Checkpoint**: All three user stories independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, coverage enforcement, and end-to-end validation

- [X] T037 [P] Verify XML doc comments on all new public members — `dotnet build src/OpenSharp.Core -c Release /warnaserror:CS1591` is clean (Constitution II)
- [X] T038 Re-run the coverage gate (merged unit + system via `coverlet.runsettings` + ReportGenerator); confirm `OpenSharp.Core` ≥ 80% and add focused unit tests for any new file below threshold
- [X] T039 [P] Update `README.md` capability table to include Nodes/cluster-info/generic selector+patch+force-delete additions
- [X] T040 Run `quickstart.md` validation end-to-end: every scenario-table row passes, the four smoke-test snippets work, and feature 001's existing tests still pass (no regression to its public contract)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup (needs `DeleteOptions`/`PatchType`); the delete-options base refactor blocks US1's generic delete
- **User Stories (Phases 3–5)**: Depend on Foundational; then proceed in priority order (P1 → P2 → P3) or in parallel where staffed
- **Polish (Phase 6)**: Depends on the desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: Depends on Foundational (delete-options base + shared simulator). Independent of US2/US3.
- **US2 (P2)**: Depends only on Foundational. Independent of US1/US3 in behavior, but **T025 edits `GenericOperations.cs`** (shared with US1 T014–T016) and **T026 edits `IOpenShiftClient.cs`/`OpenShiftClient.cs`** — sequence those edits.
- **US3 (P3)**: Depends only on Foundational. **T034 edits `IOpenShiftClient.cs`/`OpenShiftClient.cs`** (shared with US2 T026) — sequence after T026.

### Shared-file sequencing (not parallel across stories)

- `src/OpenSharp.Core/Generic/GenericOperations.cs`: US1 T014→T015→T016, then US2 T025
- `src/OpenSharp.Core/Abstractions/IOpenShiftClient.cs` and `Operations/OpenShiftClient.cs`: US2 T026, then US3 T034
- `tests/OpenSharp.SystemTests/Support/OpenShiftApiSimulator.cs`: T007 (foundational) → T017 (US1) → T027 (US2) → T035 (US3)

### Within Each User Story

- Tests are written first and MUST FAIL before implementation (Constitution IV)
- Models/types before interfaces; interfaces before operations; operations before client wiring; step definitions last

### Parallel Opportunities

- Setup: T002, T003 in parallel
- Foundational: T007 in parallel with T004–T006
- US1: tests T008–T012 in parallel; implementation T014–T016 are sequential (same file)
- US2: tests T019–T021 in parallel; models/interfaces T022–T023 in parallel
- US3: tests T029–T030 in parallel; model/interface T031–T032 in parallel
- Polish: T037, T039 in parallel

---

## Parallel Example: User Story 1

```bash
# Tests for US1 together:
Task: "Reqnroll feature GenericLabelSelector.feature"
Task: "Reqnroll feature GenericPatch.feature"
Task: "Reqnroll feature ForceDelete.feature"
Task: "Unit tests GenericOperationsTests.cs"
Task: "Unit tests DeleteOptionsTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (delete-options base + shared simulator)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: list a custom type filtered by label, patch it, force-delete it against the simulator
5. This delivers the actual blocked workflows (label-filtered VM listing, force-delete VMI).

### Incremental Delivery

1. Setup + Foundational → base ready
2. US1 → generic completion → validate (MVP)
3. US2 → nodes + node admin + core-group reach → validate
4. US3 → cluster info + capability discovery → validate
5. Polish → docs, coverage gate, quickstart validation

---

## Notes

- [P] tasks = different files, no dependencies
- Tests are MANDATORY (Constitution IV); verify they fail before implementing
- All additions are source-compatible with feature 001 — do not change existing public signatures
- Coverage gate (≥80%) applies to `OpenSharp.Core` only (inherited from feature 001 plan)
- System tests run against the WireMock simulator — no OpenShift required; `@live` is opt-in
- Node **drain** (`oc adm drain`) is out of scope (documented follow-on); core-group generic **selector** filtering is a documented follow-on (research D2)
- Commit after each task or logical group
