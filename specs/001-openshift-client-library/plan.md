# Implementation Plan: OpenShift Client Library

**Branch**: `001-openshift-client-library` | **Date**: 2026-06-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-openshift-client-library/spec.md`

## Summary

Deliver `OpenSharp.Core`, a cross-platform C# class library that lets .NET applications
interact with OpenShift programmatically — much like `oc` — without requiring the `oc` or
`kubectl` binary at runtime. The library builds on the official, maintained Kubernetes .NET
client (`KubernetesClient`) for the Kubernetes-compatible surface and adds a strongly-typed,
DI-friendly OpenShift layer: OpenShift-specific resources (Routes, Projects,
DeploymentConfigs) plus `oc`-style operations (CRUD, logs, exec, scale, rollout, watch) over
a core resource set, with a generic escape hatch for unwrapped resources.

## Technical Context

**Language/Version**: C# 12 on .NET 8 (LTS). The library targets `net8.0` as its primary
framework; multi-targeting (`net8.0;net9.0;net10.0`) is available later at low cost since the
foundation dependency supports them.

**Primary Dependencies**:
- `KubernetesClient` (official kubernetes-client/csharp, v20.x) — auth (kubeconfig/token),
  TLS, transport, core resource models, exec/logs/watch primitives.
- `Microsoft.Extensions.DependencyInjection.Abstractions` — DI registration extensions.
- `Microsoft.Extensions.Logging.Abstractions` — optional structured logging hooks.
- Test-only: `xUnit`, `Moq`, `coverlet.collector` (coverage), `Reqnroll` + `Reqnroll.xUnit`
  (BDD system tests), `WireMock.Net` (simulated OpenShift API server).

**Storage**: N/A. The library is a stateless client; all state lives in the target cluster.

**Testing**: xUnit + Moq unit tests on `OpenSharp.Core` (≥80% coverage gate, core only).
Reqnroll system/acceptance tests driven against a WireMock.Net-simulated OpenShift API so
they are deterministic and run with **no OpenShift installed**. A separate, opt-in "Live"
scenario category targets a real cluster and is skipped unless one is configured.

**Target Platform**: Cross-platform — Windows, Linux, macOS on the .NET 8 runtime.

**Project Type**: Single class library plus test projects.

**Performance Goals**: Fully asynchronous, cancellation-aware API (FR-011). List operations
use server-side continuation/paging so memory stays bounded when enumerating large result
sets (SC-006: 10,000 resources without unbounded growth). No per-call process spawning.

**Constraints**: No `oc`/`kubectl` binary required at runtime (SC-002); cross-platform with no
OS-specific behavior (FR-014/SC-003); all public capability exposed via mockable interfaces
(FR-013/SC-005); typed, distinguishable error categories (FR-010/SC-007); OpenShift-specific
operations fail clearly on a plain Kubernetes target (FR-015).

**Scale/Scope**: MVP first-class resources — Projects/Namespaces, Pods (+containers),
Deployments, DeploymentConfigs, Services, Routes, ConfigMaps, Secrets — with CRUD + logs +
exec + scale + rollout + watch, plus a generic API-group/kind escape hatch (FR-009).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Derived from the OpenSharp Constitution v1.0.0.

- **I. Code Quality**: ✅ Clean, comment-sparse code; lint/format gates apply. No deviation.
- **II. Documentation**: ✅ All public members (client, resource operations, models, errors)
  carry XML doc comments; reference docs generated from them.
- **III. Performance**: ✅ Async + cancellation throughout; paged listing for bounded memory;
  no process spawning. Goals captured in Technical Context.
- **IV. Testing (NON-NEGOTIABLE)**: ✅ Unit tests (xUnit + Moq) gate `OpenSharp.Core` at
  ≥80%; Reqnroll system tests via WireMock.Net run without a live cluster. ⚠ Scope of the
  coverage gate is narrowed to the core library only — see Complexity Tracking.
- **V. Separation of Concerns & DI**: ✅ Core logic isolated in `src/OpenSharp.Core`;
  dependencies injected via interfaces; `AddOpenSharp(...)` DI extension; no inline `new` on
  collaborators that should be injected.
- **VI. Cross-Platform**: ✅ Pure managed code on .NET 8; no OS-specific paths/assumptions.
- **VII. Clarification Over Assumption**: ✅ Approach, breadth, and operation scope were
  confirmed with the user before this plan; the system-test viability question is resolved in
  research.md.

**Result**: PASS (one justified deviation recorded in Complexity Tracking).

## Project Structure

### Documentation (this feature)

```text
specs/001-openshift-client-library/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (public interface contracts)
│   └── public-api.md
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
OpenSharp.sln

src/
└── OpenSharp.Core/                 # The class library (core logic; ≥80% coverage gate)
    ├── OpenSharp.Core.csproj
    ├── Authentication/             # Connection/credential configuration
    ├── Abstractions/               # Public interfaces (IOpenShiftClient, resource ops)
    ├── Resources/                  # Strongly-typed OpenShift resource models
    ├── Operations/                 # CRUD/logs/exec/scale/rollout/watch implementations
    ├── Generic/                    # Generic API-group/kind escape hatch
    ├── Errors/                     # Typed error hierarchy (FR-010)
    └── DependencyInjection/        # AddOpenSharp(...) service registration

tests/
├── OpenSharp.Core.UnitTests/       # xUnit + Moq; the ≥80% coverage target
└── OpenSharp.SystemTests/          # Reqnroll + WireMock.Net (+ opt-in Live category)
    ├── Features/                   # .feature Gherkin specs
    ├── Steps/                      # Step definitions
    └── Support/                    # WireMock OpenShift API simulator + hooks
```

**Structure Decision**: Single class library under `src/` (per user direction that all
source projects live in `src/`) with all test projects under `tests/` at the repository root.
`OpenSharp.Core` is the only project subject to the ≥80% coverage gate. This satisfies
Constitution Principle V (core logic isolated in its own library) and keeps future host
applications (CLI, services) as separate `src/` projects that depend on the core.

## Complexity Tracking

> Records the one justified deviation from the constitution.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Coverage gate scoped to `OpenSharp.Core` only, not the whole solution (Constitution IV says "across the solution") | User explicitly directed that only the core library must exceed 80%. Test projects and future thin host/sample projects contain little testable logic and would distort a solution-wide metric. | A solution-wide gate would either force low-value tests for glue/sample code or be gamed by exclusions; scoping to the library that holds all logic keeps the metric meaningful. |
