# OpenSharp

`OpenSharp.Core` is a cross-platform C# class library for interacting with OpenShift
programmatically — much like `oc`, but **without requiring the `oc` or `kubectl` binary at
runtime**. It builds on the official [Kubernetes .NET client](https://github.com/kubernetes-client/csharp)
and adds a strongly-typed, dependency-injection-friendly OpenShift layer: OpenShift-specific
resources (Routes, Projects, DeploymentConfigs) plus `oc`-style operations (CRUD, logs, exec,
scale, rollout, watch), with a generic escape hatch for unwrapped resource types.

## Install

```bash
dotnet add package OpenSharp.Core
```

Targets `net8.0`. Cross-platform: Windows, Linux, macOS.

## Quickstart — connect and list pods

```csharp
using Microsoft.Extensions.DependencyInjection;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.DependencyInjection;

var services = new ServiceCollection();
services.AddOpenSharp(o => o.DefaultNamespace = "my-project"); // standard kube config resolution
using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IOpenShiftClient>();

var projects = await client.Projects.ListAsync();
await foreach (var pod in client.Pods.EnumerateAsync("my-project"))
    Console.WriteLine($"{pod.Metadata.Name}: {pod.Phase}");
```

## What you can do

| Area | API | Notes |
|------|-----|-------|
| Read | `Projects`, `Pods`, `Deployments`, `Services`, … | `GetAsync` / `ListAsync` / `EnumerateAsync` (auto-pages) |
| Lifecycle | `CreateAsync` / `ReplaceAsync` / `PatchAsync` / `DeleteAsync` | Delete honours `Background` / `Foreground` / `Orphan` propagation |
| OpenShift | `Routes`, `DeploymentConfigs`, `Projects` | Clear typed error on a plain-Kubernetes target |
| Logs & exec | `Pods.ReadLogsAsync` / `FollowLogsAsync` / `ExecAsync` | Streaming log follow via `IAsyncEnumerable` |
| Scale & rollout | `Deployments.ScaleAsync` / `RolloutRestartAsync` | Works for Deployments and DeploymentConfigs |
| Watch | `…​.WatchAsync` | Added/Modified/Deleted, auto-resume after transient drops |
| Generic | `Generic.GetAsync` / `ListAsync` / `CreateAsync` / `DeleteAsync` | Reach any resource by group/version/plural |

## Authentication

`OpenShiftClientOptions.AuthMode` defaults to `Auto`: inside a pod the mounted service-account
token and CA are detected and used; otherwise standard kube config / explicit token resolution
applies. `InCluster` and `KubeConfig` force a specific mode.

## Errors

Every operation throws an `OpenShiftException` subtype: `OpenShiftConnectionException`,
`OpenShiftAuthenticationException`, `OpenShiftAuthorizationException`,
`OpenShiftNotFoundException`, `OpenShiftValidationException`, or `OpenShiftServerException`.

## Building & testing

```bash
dotnet build OpenSharp.sln -c Release

# Unit tests
dotnet test tests/OpenSharp.Core.UnitTests -c Release

# System/acceptance tests (Reqnroll + WireMock.Net simulated API — no cluster required)
dotnet test tests/OpenSharp.SystemTests -c Release --filter "Category!=live"
```

System tests run against an in-process WireMock-simulated OpenShift API, so **no OpenShift
cluster is required**. Scenarios tagged `@live` are skipped unless `OPENSHARP_LIVE` is set and a
real cluster is configured.

## License

See [LICENSE](./LICENSE).
