# Phase 0 Research: OpenShift Client Library

All Technical Context unknowns are resolved below. Each entry follows Decision / Rationale /
Alternatives considered.

## 1. Foundation: how to talk to the OpenShift API

**Decision**: Build on the official Kubernetes .NET client (`KubernetesClient`, v20.x) and add
an OpenShift-specific layer on top. Do not shell out to `oc`/`kubectl`; do not generate a raw
client from the OpenShift OpenAPI spec.

**Rationale**: OpenShift's API is Kubernetes-compatible plus extension API groups
(`route.openshift.io`, `project.openshift.io`, `apps.openshift.io`, …). `KubernetesClient` is
mature, actively maintained, cross-platform, and already provides kubeconfig/token auth, TLS,
exec, log streaming, watch, and server-side paging. Reusing it removes the need for an
external binary at runtime (SC-002), keeps everything async and in-process (Principle III),
and lets us focus effort on the OpenShift value-add. OpenShift-specific resources are reached
through the client's custom-object / generic mechanisms and wrapped in strongly-typed models.

**Alternatives considered**:
- *Wrap the `oc` CLI*: full parity for free, but requires `oc` installed, spawns a process
  per call, parses CLI output (fragile/weakly typed) — conflicts with Principles III/IV.
- *Generate a client from the OpenShift OpenAPI spec*: broad coverage but verbose, poor
  ergonomics, and the spec has known C#-generation issues; high maintenance burden.

## 2. Target framework

**Decision**: Target `net8.0` (LTS) for `OpenSharp.Core`; keep multi-targeting
(`net8.0;net9.0;net10.0`) as a low-cost later option.

**Rationale**: `KubernetesClient` v20 supports net8/9/10 with full features (netstandard2.0 is
limited via `KubernetesClient.Classic`). net8.0 is the current LTS, maximizes feature
availability, and keeps the build simple while remaining fully cross-platform (Principle VI).

**Alternatives considered**: `netstandard2.0` for maximum reach — rejected because it forces
the feature-limited Classic client and complicates the design for little real-world gain.

## 3. Dependency injection surface

**Decision**: Expose an `AddOpenSharp(...)` extension on `IServiceCollection`
(`Microsoft.Extensions.DependencyInjection.Abstractions`) that registers `IOpenShiftClient`
and its operation interfaces; allow connection options via configuration delegate.

**Rationale**: Constitution Principle V mandates DI and mockable interfaces. Abstractions-only
dependency keeps the library host-agnostic (works in console, ASP.NET Core, worker, tests).

**Alternatives considered**: Static factory/singleton — rejected; not mockable, fights DI.

## 4. Error model

**Decision**: A typed exception hierarchy rooted at `OpenShiftException` with distinct
subtypes mapping to FR-010 categories: connectivity/timeout, authentication, authorization
(forbidden), not-found, validation/conflict, and unexpected server error. Map
`KubernetesClient` HTTP status/operation exceptions onto these.

**Rationale**: SC-007 requires callers to branch on error category. Typed exceptions are the
idiomatic .NET way and keep the public contract explicit.

**Alternatives considered**: Result/union return types — viable but heavier for consumers and
inconsistent with the underlying client's exception-based model.

## 5. Listing large result sets

**Decision**: Provide async listing that uses Kubernetes server-side `continue` tokens
(limit/continue paging), exposed to callers via paged results and/or `IAsyncEnumerable<T>`
streaming.

**Rationale**: SC-006 / Principle III — bounded memory at 10k+ resources without loading
everything at once.

**Alternatives considered**: Single unbounded list call — rejected; unbounded memory growth.

## 6. Are system tests worth it without OpenShift installed? (explicit user question)

**Decision**: Yes — write Reqnroll system/acceptance tests, but drive them against a
**WireMock.Net-simulated OpenShift API server** rather than a real cluster. Add a separate,
opt-in `@live` scenario category that targets a real cluster and is **skipped by default**
when no cluster is configured (via env var / runsettings).

**Rationale**: The host has no OpenShift, so live system tests cannot run here or in standard
CI. WireMock.Net lets us stand up a fake API endpoint and assert that the library issues the
correct requests and correctly maps responses, errors, paging, and watch streams — genuine
end-to-end behavior coverage of *our* code that is deterministic, fast, and cross-platform.
This directly satisfies Constitution Principle IV's "system tests SHOULD be written wherever
feasible" by making them feasible. The `@live` category preserves a path to real-cluster
validation when one becomes available, without blocking the default build.

**Alternatives considered**:
- *No system tests* — rejected; loses end-to-end coverage and contradicts Principle IV when a
  feasible option exists.
- *Require a real/ephemeral cluster (e.g., CRC/kind) in CI* — rejected for now; heavy,
  slow, and not available on this host. Kept as the optional `@live` path.

## 7. Testing stack

**Decision**: xUnit as the test runner; Moq for mocking collaborators in unit tests;
`coverlet.collector` for coverage (gate ≥80% on `OpenSharp.Core` only, per user direction);
`Reqnroll` + `Reqnroll.xUnit` for BDD system tests; `WireMock.Net` for the simulated API.

**Rationale**: Matches Constitution Principle IV (Moq, Reqnroll) and the user's framework
choices; xUnit integrates cleanly with Reqnroll and coverlet; coverage scoping is recorded as
a justified deviation in plan.md Complexity Tracking.

**Alternatives considered**: NUnit/MSTest — equivalent, but xUnit is the common default and is
well supported by Reqnroll; no reason to diverge.
