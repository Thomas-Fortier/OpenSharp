---
description: "Task list for OpenShift Client Library implementation"
---

# Tasks: OpenShift Client Library

**Input**: Design documents from `/specs/001-openshift-client-library/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-api.md, quickstart.md

**Tests**: MANDATORY per the OpenSharp Constitution (Principle IV). Unit tests use xUnit +
Moq on `OpenSharp.Core` (≥80% coverage gate). System/acceptance tests use Reqnroll driven by
a WireMock.Net-simulated OpenShift API (no OpenShift required); a `@live` category is skipped
unless a cluster is configured.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US4)
- Paths follow plan.md: core lib in `src/OpenSharp.Core/`, tests in `tests/`

## Path Conventions

- Core library: `src/OpenSharp.Core/`
- Unit tests: `tests/OpenSharp.Core.UnitTests/`
- System tests: `tests/OpenSharp.SystemTests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Solution and project skeletons, dependencies, quality tooling

- [x] T001 Create solution `OpenSharp.sln` at repository root
- [x] T002 [P] Create class library `src/OpenSharp.Core/OpenSharp.Core.csproj` (net8.0) with folders Authentication/, Abstractions/, Resources/, Operations/, Generic/, Errors/, DependencyInjection/
- [x] T003 [P] Create xUnit project `tests/OpenSharp.Core.UnitTests/` referencing `OpenSharp.Core`
- [x] T004 [P] Create xUnit project `tests/OpenSharp.SystemTests/` referencing `OpenSharp.Core`, with folders Features/, Steps/, Support/
- [x] T005 Add NuGet packages to `src/OpenSharp.Core/OpenSharp.Core.csproj`: `KubernetesClient` (v20.x), `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`
- [x] T006 [P] Add NuGet packages to `tests/OpenSharp.Core.UnitTests/`: `xunit`, `xunit.runner.visualstudio`, `Moq`, `coverlet.collector`
- [x] T007 [P] Add NuGet packages to `tests/OpenSharp.SystemTests/`: `Reqnroll`, `Reqnroll.xUnit`, `WireMock.Net`, `xunit.runner.visualstudio`
- [x] T008 [P] Add root `.editorconfig` and `Directory.Build.props` enabling nullable, `TreatWarningsAsErrors`, analyzers, and `GenerateDocumentationFile` for `OpenSharp.Core` (Constitution II)
- [x] T009 [P] Add `coverlet.runsettings` scoping coverage collection to `OpenSharp.Core` only with an 80% threshold (Constitution IV, user-scoped to core)

**Checkpoint**: Solution builds empty; `dotnet test` runs with zero tests.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Connection, error model, public abstractions, DI, and test harness that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T010 [P] Implement `OpenShiftClientOptions` (incl. `AuthMode` enum: Auto/InCluster/KubeConfig) in `src/OpenSharp.Core/Authentication/OpenShiftClientOptions.cs`
- [x] T011 [P] Implement shared value types (`ResourceMetadata`, `ContainerInfo`, `ServicePort`, `RouteTarget`, `PagedList<T>`, `WatchEvent<T>`, `WatchEventType`) in `src/OpenSharp.Core/Resources/` and `src/OpenSharp.Core/Abstractions/`
- [x] T012 [P] Implement error hierarchy (`OpenShiftException` + `Connection`/`Authentication`/`Authorization`/`NotFound`/`Validation`/`Server` subtypes) in `src/OpenSharp.Core/Errors/`
- [x] T013 Implement connection factory wrapping `KubernetesClient` (kubeconfig/context/token/TLS resolution) in `src/OpenSharp.Core/Authentication/OpenShiftConnectionFactory.cs`, including `AuthMode` resolution — auto-detect in-cluster service account when running in a pod, else kubeconfig/token (FR-001) — and reliance on the underlying client's exec/auth-provider token refresh (FR-002a) (depends on T010)
- [x] T014 Implement Kubernetes-to-typed error mapping (`HttpOperationException`/status → error subtypes) in `src/OpenSharp.Core/Errors/ErrorMapper.cs` (depends on T012)
- [x] T015 Define public abstractions `IOpenShiftClient`, `IReadOperations<T>`, `IWriteOperations<T>`, `IWatchable<T>`, `IPodOperations`, `IWorkloadOperations`, resource operation interfaces, and `IGenericOperations` in `src/OpenSharp.Core/Abstractions/` per contracts/public-api.md (depends on T011)
- [x] T016 Implement `AddOpenSharp(IServiceCollection, Action<OpenShiftClientOptions>)` DI extension wiring `IOpenShiftClient` in `src/OpenSharp.Core/DependencyInjection/ServiceCollectionExtensions.cs` (depends on T013, T015)
- [x] T017 [P] Build WireMock OpenShift API simulator harness (endpoints, paging, watch stream, error responses) in `tests/OpenSharp.SystemTests/Support/OpenShiftApiSimulator.cs`
- [x] T018 [P] Configure Reqnroll (`reqnroll.json`), shared `ScenarioContext`, and a `@live` skip hook reading `OPENSHARP_LIVE` in `tests/OpenSharp.SystemTests/Support/`
- [x] T019 [P] Add unit tests for `ErrorMapper` covering every error category in `tests/OpenSharp.Core.UnitTests/Errors/ErrorMapperTests.cs` (Moq) (SC-007)
- [x] T020 [P] Add unit tests for `OpenShiftConnectionFactory` config resolution, incl. `AuthMode.Auto` in-cluster vs kubeconfig selection and in-cluster detection (FR-001), in `tests/OpenSharp.Core.UnitTests/Authentication/ConnectionFactoryTests.cs` (Moq)

**Checkpoint**: Connection, errors, abstractions, DI, and test harness ready — user stories can begin.

---

## Phase 3: User Story 1 - Connect and read cluster state (Priority: P1) 🎯 MVP

**Goal**: Authenticate and read core resources (Projects, Pods, Deployments, Services) as strongly-typed objects, with paging and typed errors, no external binary.

**Independent Test**: Against the simulated API, list projects, list/enumerate pods with paging, get a single pod; invalid credentials yield a typed auth error.

### Tests for User Story 1 (write first, ensure they FAIL) ⚠️

- [X] T021 [P] [US1] Reqnroll feature `tests/OpenSharp.SystemTests/Features/ConnectAndListProjects.feature` (connect + list projects)
- [X] T022 [P] [US1] Reqnroll feature `tests/OpenSharp.SystemTests/Features/ListPodsPaging.feature` (list/enumerate pods across continuation pages, SC-006)
- [X] T023 [P] [US1] Reqnroll feature `tests/OpenSharp.SystemTests/Features/InvalidCredentials.feature` (auth error, SC-007)
- [X] T024 [P] [US1] Unit tests for read base + auto-paging `EnumerateAsync` in `tests/OpenSharp.Core.UnitTests/Operations/ReadOperationsTests.cs` (Moq)
- [X] T025 [P] [US1] Unit tests for Project/Pod mapping in `tests/OpenSharp.Core.UnitTests/Resources/ResourceMappingTests.cs` (Moq)

### Implementation for User Story 1

- [X] T026 [P] [US1] Implement `Project` model + mapping in `src/OpenSharp.Core/Resources/Project.cs`
- [X] T027 [P] [US1] Implement `Pod` + `ContainerInfo` mapping in `src/OpenSharp.Core/Resources/Pod.cs`
- [X] T028 [P] [US1] Implement `Deployment` and `Service` read models in `src/OpenSharp.Core/Resources/Deployment.cs` and `Service.cs`
- [X] T029 [US1] Implement read base (`GetAsync`/`ListAsync` with limit+continue/`EnumerateAsync` auto-paging) in `src/OpenSharp.Core/Operations/ReadOperationsBase.cs` (depends on T014)
- [X] T030 [US1] Implement `ProjectOperations` (read) in `src/OpenSharp.Core/Operations/ProjectOperations.cs`
- [X] T031 [US1] Implement `PodOperations` (read) in `src/OpenSharp.Core/Operations/PodOperations.cs`
- [X] T032 [US1] Implement `DeploymentOperations` and `ServiceOperations` (read) in `src/OpenSharp.Core/Operations/`
- [X] T033 [US1] Wire read operations into `IOpenShiftClient` implementation + DI registration in `src/OpenSharp.Core/Operations/OpenShiftClient.cs`
- [X] T034 [US1] Implement step definitions for US1 features in `tests/OpenSharp.SystemTests/Steps/ReadSteps.cs`

**Checkpoint**: US1 fully functional and independently testable (MVP).

---

## Phase 4: User Story 2 - Manage resource lifecycle (Priority: P2)

**Goal**: Create/update/delete core and OpenShift-specific resources (Routes, DeploymentConfigs, ConfigMaps, Secrets, plus write on US1 resources).

**Independent Test**: Create a Route, read it back, update a field, delete it; a name conflict yields a typed validation error.

### Tests for User Story 2 (write first, ensure they FAIL) ⚠️

- [X] T035 [P] [US2] Reqnroll feature `tests/OpenSharp.SystemTests/Features/RouteLifecycle.feature` (create→read→update→delete, SC-004)
- [X] T036 [P] [US2] Reqnroll feature `tests/OpenSharp.SystemTests/Features/CreateConflict.feature` (validation/conflict error, SC-007)
- [X] T037 [P] [US2] Reqnroll feature `tests/OpenSharp.SystemTests/Features/NonOpenShiftTarget.feature` (Route on plain k8s target errors clearly, FR-015)
- [X] T038 [P] [US2] Unit tests for write base (create/replace/patch/delete incl. ResourceVersion conflict and `DeletePropagationPolicy` Background/Foreground/Orphan) in `tests/OpenSharp.Core.UnitTests/Operations/WriteOperationsTests.cs` (Moq)

### Implementation for User Story 2

- [X] T039 [P] [US2] Implement `Route` + `RouteTarget` model in `src/OpenSharp.Core/Resources/Route.cs`
- [X] T040 [P] [US2] Implement `DeploymentConfig` model — folded into the shared `Deployment` model (`src/OpenSharp.Core/Resources/Deployment.cs`); DeploymentConfig is served via `DeploymentOperations(isDeploymentConfig: true)` per the public-api contract
- [X] T041 [P] [US2] Implement `ConfigMap` and `Secret` models in `src/OpenSharp.Core/Resources/ConfigMap.cs` and `Secret.cs` (Secret values never logged)
- [X] T042 [US2] Implement write base (`CreateAsync`/`ReplaceAsync`/`PatchAsync`/`DeleteAsync`) including `DeletePropagationPolicy` (Background default; Foreground/Orphan) on delete in `src/OpenSharp.Core/Operations/WriteOperationsBase.cs` (FR-004) (depends on T029)
- [X] T043 [US2] Implement `RouteOperations` (read+write, with non-OpenShift-target detection FR-015) in `src/OpenSharp.Core/Operations/RouteOperations.cs`
- [X] T044 [US2] Implement `ConfigMapOperations` and `SecretOperations` (read+write) in `src/OpenSharp.Core/Operations/`
- [X] T045 [US2] Add write capability to Project/Pod/Deployment/Service operations and DeploymentConfig read+write (extend Phase 3 operation classes)
- [X] T046 [US2] Expose Routes/DeploymentConfigs/ConfigMaps/Secrets on `IOpenShiftClient` + DI registration in `src/OpenSharp.Core/Operations/OpenShiftClient.cs`
- [X] T047 [US2] Implement step definitions for US2 features in `tests/OpenSharp.SystemTests/Steps/LifecycleSteps.cs`

**Checkpoint**: US1 and US2 both independently functional.

---

## Phase 5: User Story 3 - Operational actions on workloads (Priority: P3)

**Goal**: Read/follow logs, exec into containers, scale workloads, and trigger rollout/restart.

**Independent Test**: Read and follow pod logs; exec returns output + exit code; scale changes replicas; rollout restart triggers a new rollout.

### Tests for User Story 3 (write first, ensure they FAIL) ⚠️

- [X] T048 [P] [US3] Reqnroll feature `tests/OpenSharp.SystemTests/Features/PodLogs.feature` (read + follow streaming, FR-005)
- [X] T049 [P] [US3] Reqnroll feature `tests/OpenSharp.SystemTests/Features/PodExec.feature` (exec output + exit code, FR-006) — tagged `@live` (the exec WebSocket streaming protocol cannot be reproduced deterministically by the WireMock simulator); request shaping is unit-tested in ActionsTests
- [X] T050 [P] [US3] Reqnroll feature `tests/OpenSharp.SystemTests/Features/ScaleAndRollout.feature` (scale + rollout restart, FR-007)
- [X] T051 [P] [US3] Unit tests for log/exec option building and scale/rollout request shaping in `tests/OpenSharp.Core.UnitTests/Operations/ActionsTests.cs` (Moq)

### Implementation for User Story 3

- [X] T052 [P] [US3] Implement `LogOptions`, `ExecRequest`, `ExecResult` types in `src/OpenSharp.Core/Resources/`
- [X] T053 [US3] Implement `ReadLogsAsync` + `FollowLogsAsync` (IAsyncEnumerable streaming) on `PodOperations` in `src/OpenSharp.Core/Operations/PodOperations.cs`
- [X] T054 [US3] Implement `ExecAsync` on `PodOperations` in `src/OpenSharp.Core/Operations/PodOperations.cs`
- [X] T055 [US3] Implement `ScaleAsync` + `RolloutRestartAsync` on `WorkloadOperations` (Deployment + DeploymentConfig) in `src/OpenSharp.Core/Operations/DeploymentOperations.cs`
- [X] T056 [US3] Implement step definitions for US3 features in `tests/OpenSharp.SystemTests/Steps/ActionsSteps.cs`

**Checkpoint**: US1–US3 independently functional.

---

## Phase 6: User Story 4 - Watch for changes and reach unwrapped resources (Priority: P4)

**Goal**: Watch supported resources for Added/Modified/Deleted events; operate on unwrapped resource types via the generic escape hatch.

**Independent Test**: Watch pods and receive change events; perform generic get/list on a resource type not first-class.

### Tests for User Story 4 (write first, ensure they FAIL) ⚠️

- [X] T057 [P] [US4] Reqnroll feature `tests/OpenSharp.SystemTests/Features/WatchPods.feature` (Added/Modified/Deleted, plus auto-resume after a simulated stream drop and `AutoResume=false` termination, FR-008). Auto-resume/resourceVersion-tracking is unit-tested in `WatchAndGenericTests` (`WatchAsync_AutoResume_ResumesFromLastResourceVersion`/`BookmarkUpdatesResumeToken`)
- [X] T058 [P] [US4] Reqnroll feature `tests/OpenSharp.SystemTests/Features/GenericResource.feature` (get/list by group/version/plural, FR-009)
- [X] T059 [P] [US4] Unit tests for watch event parsing, auto-resume/resourceVersion-tracking logic, and generic ref construction in `tests/OpenSharp.Core.UnitTests/Operations/WatchAndGenericTests.cs` (Moq)

### Implementation for User Story 4

- [X] T060 [P] [US4] Implement `GenericResourceRef` type in `src/OpenSharp.Core/Generic/GenericResourceRef.cs`
- [X] T061 [US4] Implement `WatchAsync` (IAsyncEnumerable<WatchEvent<T>>) with `WatchOptions.AutoResume` (default true: resume from last resourceVersion/bookmark, terminal error only when resume impossible; false: surface termination) in `src/OpenSharp.Core/Operations/ReadOperationsBase.cs`, and apply `IWatchable<T>` to watchable operations (FR-008)
- [X] T062 [US4] Implement `GenericOperations` (get/list/create/delete) in `src/OpenSharp.Core/Generic/GenericOperations.cs` and expose on `IOpenShiftClient` + DI
- [X] T063 [US4] Implement step definitions for US4 features in `tests/OpenSharp.SystemTests/Steps/WatchAndGenericSteps.cs`

**Checkpoint**: All user stories independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, coverage enforcement, cross-platform CI, and validation

- [X] T064 [P] Verify XML doc comments on all public members (build passes `/warnaserror:CS1591`) and configure API reference doc generation via DocFX (`docfx.json` → `docs/`) (Constitution II)
- [X] T065 [P] Add `README.md` with the ≤15-line connect-and-list-pods quickstart sample (SC-001)
- [X] T066 Enforce ≥80% coverage gate on `OpenSharp.Core` in build/CI using `coverlet.runsettings` + ReportGenerator (CI `coverage-gate` job fails under threshold); measured 91.1%
- [X] T067 [P] Add cross-platform CI matrix (Windows/Linux/macOS) running build, unit tests, and system tests with `--filter "Category!=live"` in `.github/workflows/ci.yml` (SC-002, SC-003)
- [X] T068 [P] Add a performance/bounded-memory test enumerating 10,000 simulated resources via paging in `tests/OpenSharp.SystemTests/Features/LargeListPaging.feature` (SC-006)
- [X] T069 Run `quickstart.md` validation end-to-end and confirm all scenario-table rows pass

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **User Stories (Phases 3–6)**: All depend on Foundational; then proceed in priority order
  (P1 → P2 → P3 → P4) or in parallel where staffed
- **Polish (Phase 7)**: Depends on the desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: Depends only on Foundational. Establishes read base reused by later stories.
- **US2 (P2)**: Builds on US1's read/operation classes (write base extends read base, T042→T029).
- **US3 (P3)**: Extends US1's `PodOperations`/workload operations with actions.
- **US4 (P4)**: Extends the read base (`WatchAsync`) and adds the generic escape hatch.

### Within Each User Story

- Tests are written first and MUST FAIL before implementation (Constitution IV)
- Models before operations; operations before client wiring; step definitions last

### Parallel Opportunities

- Setup: T002–T004, T006–T009 in parallel; T005 after T002
- Foundational: T010–T012, T017–T020 in parallel; T013/T014/T015 then T016
- US1: tests T021–T025 in parallel; models T026–T028 in parallel; then T029→ops→wiring
- US2: tests T035–T038 in parallel; models T039–T041 in parallel
- US3: tests T048–T051 in parallel; T052 in parallel
- US4: tests T057–T059 in parallel; T060 in parallel
- Polish: T064, T065, T067, T068 in parallel

---

## Parallel Example: User Story 1

```bash
# Tests for US1 together:
Task: "Reqnroll feature ConnectAndListProjects.feature"
Task: "Reqnroll feature ListPodsPaging.feature"
Task: "Reqnroll feature InvalidCredentials.feature"
Task: "Unit tests ReadOperationsTests.cs"
Task: "Unit tests ResourceMappingTests.cs"

# Models for US1 together:
Task: "Project model in src/OpenSharp.Core/Resources/Project.cs"
Task: "Pod + ContainerInfo in src/OpenSharp.Core/Resources/Pod.cs"
Task: "Deployment + Service read models in src/OpenSharp.Core/Resources/"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: list projects/pods against the simulated API; confirm typed auth error
5. This is a usable read-only OpenShift client MVP.

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. US1 → read MVP → validate
3. US2 → lifecycle (incl. Routes/DeploymentConfigs) → validate
4. US3 → operational actions (logs/exec/scale/rollout) → validate
5. US4 → watch + generic escape hatch → validate
6. Polish → docs, coverage gate, cross-platform CI, performance

---

## Notes

- [P] tasks = different files, no dependencies
- Tests are MANDATORY (Constitution IV); verify they fail before implementing
- Coverage gate (≥80%) applies to `OpenSharp.Core` only (plan.md Complexity Tracking)
- System tests run against the WireMock simulator — no OpenShift required; `@live` is opt-in
- Commit after each task or logical group
