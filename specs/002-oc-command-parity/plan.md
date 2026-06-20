# Implementation Plan: oc Command Parity — Cluster, Node & Generic Operation Coverage

**Branch**: `002-oc-command-parity` | **Date**: 2026-06-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-oc-command-parity/spec.md`

## Summary

Extend the existing `OpenSharp.Core` library (feature 001) so the remaining real-world `oc`
workflows translate directly. Three additive capability groups: (US1) complete the **generic
escape hatch** — label-selector filtering on list, partial-update (patch), and delete with
grace-period/force control; (US2) add **cluster-scoped core-resource access** — first-class
`Nodes` (list/get) with cordon/uncordon, plus generic reach into the core (legacy) API group;
(US3) add **cluster information & capability discovery** — API endpoint, server version,
reachability, and "is this resource type served?" checks. Everything inherits feature 001's
async/cancellation, typed-error, DI/mockability, paging, and cross-platform guarantees, and
must not break its existing public surface (additions are source-compatible).

## Technical Context

**Language/Version**: C# 12 on .NET 8 (LTS), matching feature 001. Same `net8.0` target.

**Primary Dependencies** (unchanged from feature 001):
- `KubernetesClient` (official kubernetes-client/csharp, **v19.0.2** as referenced in
  `src/OpenSharp.Core/OpenSharp.Core.csproj`) — provides `CoreV1` (nodes), `CustomObjects`
  (generic named-group resources), `GenericClient`/version/discovery surfaces used here.
- `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Options`,
  `Microsoft.Extensions.Logging.Abstractions`.
- Test-only: `xUnit`, `Moq`, `coverlet.collector` (pinned to **10.0.1** in both test projects),
  `Reqnroll` + `Reqnroll.xUnit`, `WireMock.Net`.

**Storage**: N/A — stateless client; all state lives in the target cluster.

**Testing**: xUnit + Moq unit tests on `OpenSharp.Core` (≥80% coverage gate, core only);
Reqnroll + WireMock.Net system tests with no cluster required; `@live` opt-in category skipped
unless `OPENSHARP_LIVE` is set (skips cleanly via xUnit dynamic skip).

**Target Platform**: Cross-platform — Windows, Linux, macOS on .NET 8.

**Project Type**: Extension of the single existing class library plus its two test projects.

**Performance Goals**: Fully asynchronous and cancellation-aware (FR-009 → 001 FR-011). Node
and generic list operations use server-side continuation/paging so memory stays bounded.
Cluster-info and availability checks are single, cheap discovery calls. No process spawning.

**Constraints**: No `oc`/`kubectl` binary at runtime; additive-only public API (existing
`IGenericOperations`/`IWriteOperations`/`IOpenShiftClient` members keep their signatures —
new behavior is added via overloads and new members); typed/distinguishable error categories
preserved; core-group generic reach must coexist with the existing named-group path; the
"unavailable" outcome of capability discovery must not surface on the generic error path.

**Scale/Scope**: New first-class entity (`Node`) + node admin (cordon/uncordon); generic
list label/field selectors; generic patch; delete options (grace period/force) on first-class
and generic deletes; core-group generic reach; cluster info (endpoint/version/reachability)
and resource-type availability discovery.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Derived from the OpenSharp Constitution v1.0.0.

- **I. Code Quality**: ✅ Additions follow existing patterns (`OperationBase`/`ReadOperationsBase`/
  `WriteOperationsBase`); comment-sparse; lint/format gates apply. No deviation.
- **II. Documentation**: ✅ All new public members (`Node`, `INodeOperations`,
  `IClusterOperations`, `ClusterInfo`, `DeleteOptions`, generic overloads) carry XML doc
  comments; build still fails on `CS1591`.
- **III. Performance**: ✅ Async + cancellation throughout; paged list for nodes/generic;
  discovery is a single call. Goals captured above.
- **IV. Testing (NON-NEGOTIABLE)**: ✅ Moq unit tests for cordon patch shaping, generic
  selector/patch/delete-option request shaping, cluster-info/availability mapping; Reqnroll +
  WireMock system tests for every new scenario. ⚠ Coverage gate remains scoped to
  `OpenSharp.Core` only — inherited deviation from feature 001 (see Complexity Tracking).
- **V. Separation of Concerns & DI**: ✅ New operations live in `src/OpenSharp.Core`; exposed
  through interfaces; registered via the existing `AddOpenSharp(...)`/`OpenShiftClient`
  composition; no inline `new` on injected collaborators.
- **VI. Cross-Platform**: ✅ Pure managed code; no OS-specific paths/assumptions.
- **VII. Clarification Over Assumption**: ✅ Scope and the documented assumptions (drain out of
  scope, merge-patch default, cluster-info subset) were settled in the spec; remaining choices
  are library-internal and resolved in research.md.

**Result**: PASS (one inherited, justified deviation recorded in Complexity Tracking).

## Project Structure

### Documentation (this feature)

```text
specs/002-oc-command-parity/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── public-api.md
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

Extends the existing layout from feature 001; new and modified files only:

```text
src/OpenSharp.Core/
├── Abstractions/
│   ├── IOpenShiftClient.cs          # MODIFY: add Nodes, Cluster facades
│   ├── INodeOperations.cs           # NEW: read + Cordon/Uncordon
│   ├── IClusterOperations.cs        # NEW: GetInfoAsync, IsResourceTypeAvailableAsync
│   ├── IGenericOperations.cs        # MODIFY: list selectors, PatchAsync, Delete overload
│   └── IWriteOperations.cs          # MODIFY: add DeleteAsync(name, ns, DeleteOptions, ct) overload
├── Resources/
│   ├── Node.cs                      # NEW: Node model (+ NodeCondition)
│   ├── ClusterInfo.cs               # NEW: endpoint/version/reachability
│   └── DeleteOptions.cs             # NEW: propagation + grace period + force
├── Operations/
│   ├── NodeOperations.cs            # NEW: CoreV1 nodes + cordon/uncordon patch
│   ├── ClusterOperations.cs         # NEW: version/discovery
│   ├── WriteOperationsBase.cs       # MODIFY: DeleteOptions overload plumbing
│   └── OpenShiftClient.cs           # MODIFY: construct + expose Nodes, Cluster
├── Generic/
│   └── GenericOperations.cs         # MODIFY: selectors, patch, delete options, core-group reach
└── DependencyInjection/
    └── ServiceCollectionExtensions.cs  # (no change expected; client already aggregates facades)

tests/OpenSharp.Core.UnitTests/
├── Operations/NodeOperationsTests.cs            # NEW
├── Operations/ClusterOperationsTests.cs         # NEW
├── Operations/GenericOperationsTests.cs         # NEW (selector/patch/delete-option shaping)
└── Resources/DeleteOptionsTests.cs              # NEW

tests/OpenSharp.SystemTests/
├── Features/GenericLabelSelector.feature        # NEW (US1)
├── Features/GenericPatch.feature                # NEW (US1)
├── Features/ForceDelete.feature                 # NEW (US1)
├── Features/Nodes.feature                       # NEW (US2: list/get/cordon/uncordon)
├── Features/CoreGroupGeneric.feature            # NEW (US2: core-group reach)
├── Features/ClusterInfo.feature                 # NEW (US3: info + availability)
├── Steps/NodeSteps.cs                           # NEW
├── Steps/ClusterSteps.cs                        # NEW
├── Steps/GenericExtendedSteps.cs                # NEW
└── Support/OpenShiftApiSimulator.cs             # MODIFY: node, discovery, version, selector stubs
```

**Structure Decision**: Extend the existing single class library (`src/OpenSharp.Core`) and its
two test projects — no new projects. This preserves Constitution V (all logic in the one core
library) and keeps the ≥80% coverage gate scoped to that library. New capabilities are added as
new interfaces/operations and additive overloads so feature 001's public contract is unchanged.

## Complexity Tracking

> Records the one inherited, justified deviation from the constitution.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Coverage gate scoped to `OpenSharp.Core` only, not the whole solution (Constitution IV says "across the solution") | Inherited from feature 001 per explicit user direction; test/host/sample projects hold little testable logic and would distort a solution-wide metric. | A solution-wide gate would force low-value tests for glue/sample code or be gamed by exclusions; scoping to the library that holds all logic keeps the metric meaningful. |
