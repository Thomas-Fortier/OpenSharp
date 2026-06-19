# Quickstart & Validation Guide: OpenSharp.Core

This guide proves the feature works end-to-end. It assumes the solution and projects from
[plan.md](./plan.md) exist. See [contracts/public-api.md](./contracts/public-api.md) and
[data-model.md](./data-model.md) for type details.

## Prerequisites

- .NET 8 SDK (cross-platform: Windows/Linux/macOS).
- No `oc`/`kubectl` binary and **no OpenShift cluster required** for the default build/tests.
- Optional, only for `@live` scenarios: access to a real cluster via standard kube config.

## Build & test (default — no cluster)

```bash
# From repository root
dotnet restore OpenSharp.sln
dotnet build OpenSharp.sln -c Release

# Unit tests + coverage (gate applies to OpenSharp.Core only)
dotnet test tests/OpenSharp.Core.UnitTests -c Release \
  --collect:"XPlat Code Coverage"

# System/acceptance tests (Reqnroll, WireMock.Net simulated API; @live excluded)
dotnet test tests/OpenSharp.SystemTests -c Release \
  --filter "Category!=live"
```

**Expected outcomes**:
- Build succeeds on all three OS platforms (SC-003).
- Unit-test coverage for `OpenSharp.Core` ≥ 80% (Constitution IV; user-scoped to core).
- System tests pass against the WireMock-simulated OpenShift API with no OpenShift installed
  (SC-002, research.md §6).

## Consumer smoke test (US1 — connect & read)

A consuming app should be able to connect and list pods in ≤15 lines (SC-001):

```csharp
var services = new ServiceCollection();
services.AddOpenSharp(o => o.DefaultNamespace = "my-project"); // standard kube config resolution
using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IOpenShiftClient>();

var projects = await client.Projects.ListAsync();
await foreach (var pod in client.Pods.EnumerateAsync("my-project"))
    Console.WriteLine($"{pod.Metadata.Name}: {pod.Phase}");
```

Validates: DI registration, connection/auth, project listing, paged pod enumeration.

## Validation scenarios (map to spec user stories)

Run via the Reqnroll feature files in `tests/OpenSharp.SystemTests/Features`. Each is backed
by the WireMock OpenShift API simulator (`Support/`).

| Scenario | Proves | Spec ref |
|----------|--------|----------|
| Connect with valid config and list projects | Auth + read, no external binary | US1, SC-002 |
| List pods with continuation across pages | Bounded-memory paging | US1, SC-006 |
| Invalid credentials surface auth error | Typed error categories | US1, SC-007 |
| Create → read → update → delete a Route | Full lifecycle incl. OpenShift resource | US2, SC-004 |
| Create with name conflict surfaces validation error | Typed validation error | US2, SC-007 |
| Read and follow pod logs | Log streaming | US3 |
| Exec command returns output + exit code | Exec | US3 |
| Scale workload and trigger rollout restart | Scale/rollout | US3 |
| Watch pods emits Added/Modified/Deleted | Change notifications | US4 |
| Generic get/list on an unwrapped resource type | Extensibility escape hatch | US4, FR-009 |
| OpenShift resource against non-OpenShift target errors clearly | Graceful degradation | FR-015 |

## Optional: live validation (`@live`)

Only when a real cluster is available:

```bash
export OPENSHARP_LIVE=1   # step hooks read this to enable @live
dotnet test tests/OpenSharp.SystemTests -c Release --filter "Category=live"
```

If `OPENSHARP_LIVE` is unset, `@live` scenarios are skipped (the default), so the absence of
OpenShift never fails the build.

## Done / acceptance

Feature is validated when: the default build + unit + system tests pass with no cluster, core
coverage ≥80%, the consumer smoke test runs in ≤15 lines, and every scenario in the table
above passes (mapping all functional requirements and success criteria).
