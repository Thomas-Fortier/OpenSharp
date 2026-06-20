# Quickstart & Validation Guide: oc Command Parity

Proves the feature 002 additions work end-to-end on top of feature 001. See
[contracts/public-api.md](./contracts/public-api.md) and [data-model.md](./data-model.md) for
type details. No OpenShift cluster is required for the default build/tests.

## Prerequisites

- .NET 8 SDK (Windows/Linux/macOS).
- Feature 001 already in place (this feature extends `OpenSharp.Core`).
- No `oc`/`kubectl` binary and no cluster required for the default tests; `@live` scenarios need
  `OPENSHARP_LIVE` and a real cluster.

## Build & test (default — no cluster)

```bash
dotnet build OpenSharp.slnx -c Release

# Unit tests (Moq) + system tests (Reqnroll + WireMock simulator), live excluded
dotnet test tests/OpenSharp.Core.UnitTests -c Release --no-build
dotnet test tests/OpenSharp.SystemTests  -c Release --no-build --filter "Category!=live"

# Coverage gate: OpenSharp.Core must stay >= 80% (merged unit + system, per feature 001 CI)
dotnet test OpenSharp.slnx -c Release --settings coverlet.runsettings --filter "Category!=live"
```

**Expected**: build succeeds on all three OSes; unit + system tests pass with no cluster;
combined `OpenSharp.Core` coverage stays ≥ 80%.

## Consumer smoke test (the previously-blocked workflows)

```csharp
var client = provider.GetRequiredService<IOpenShiftClient>();

// US1 — filter custom resources by label across all namespaces (was: get vm --all-namespaces -l …)
var vmRef = new GenericResourceRef { Group = "kubevirt.io", Version = "v1", Plural = "virtualmachines" };
var matches = await client.Generic.ListAsync(vmRef, labelSelector: "aircraftType=F18");

// US1 — force-delete a stuck instance (was: delete vmi … --force --grace-period=0)
var vmiRef = new GenericResourceRef { Group = "kubevirt.io", Version = "v1",
    Plural = "virtualmachineinstances", Namespace = "flightline", Name = "vmi-123" };
await client.Generic.DeleteAsync(vmiRef, new DeleteOptions { Force = true });

// US2 — inspect and cordon a node (was: get nodes / adm cordon)
var nodes = await client.Nodes.ListAsync();
await client.Nodes.CordonAsync("worker-3");

// US3 — cluster info + capability check (was: cluster-info)
var info = await client.Cluster.GetInfoAsync();           // endpoint, version, reachable
bool hasRoutes = await client.Cluster.IsResourceTypeAvailableAsync("route.openshift.io", "v1", "routes");
```

## Validation scenarios (map to user stories)

Backed by Reqnroll feature files under `tests/OpenSharp.SystemTests/Features` against the
WireMock OpenShift API simulator.

| Scenario | Proves | Spec ref |
|----------|--------|----------|
| Generic list filtered by label (namespaced + all-namespaces) | Selector filtering on generic | US1, FR-001 |
| Generic patch updates a resource | Partial update via generic | US1, FR-002 |
| Force / zero-grace delete removes a resource | Delete grace/force control | US1, FR-003 |
| Node list/get with status & schedulability | Cluster-scoped core read | US2, FR-004 |
| Cordon then uncordon a node | Node administration | US2, FR-006 |
| Generic get on a core-group resource | Core-group reach | US2, FR-005 |
| Cluster info returns endpoint + version + reachable | Cluster information | US3, FR-007 |
| Resource-type availability: served vs not served | Capability discovery | US3, FR-008 |
| Empty-match selector ⇒ empty list; absent resource ⇒ not-found | Edge cases / typed errors | US1/US2 |

## Reference workflow coverage (SC-007)

After this feature, every `oc` command in the spec's Reference Workflows table is achievable via
the library (excluding `-o json`/`-o jsonpath`/`--no-headers` formatting). Node **drain**
(`oc adm drain`) remains out of scope (documented follow-on).

## Optional: live validation (`@live`)

```bash
export OPENSHARP_LIVE=1
dotnet test tests/OpenSharp.SystemTests -c Release --filter "Category=live"
```

Unset `OPENSHARP_LIVE` ⇒ `@live` scenarios are skipped (not failed), so the absence of a cluster
never breaks the build.

## Done / acceptance

Validated when: default build + unit + system tests pass with no cluster; `OpenSharp.Core`
coverage ≥ 80%; the four smoke-test snippets run; every scenario-table row passes; and feature
001's existing tests still pass (no regression to its public contract).
