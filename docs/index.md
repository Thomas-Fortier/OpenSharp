# OpenSharp.Core API Reference

`OpenSharp.Core` is a cross-platform C# library for interacting with OpenShift programmatically —
much like `oc`, but without requiring the `oc`/`kubectl` binary at runtime. It builds on the
official Kubernetes .NET client and adds a strongly-typed, dependency-injection-friendly
OpenShift layer.

- **Getting started, install, and quickstart**: see the [project README](https://github.com/Thomas-Fortier/OpenSharp#readme).
- **API reference**: browse the [API documentation](api/OpenSharp.Core.Abstractions.yml) — every
  public type and member is documented from its XML doc comments.

## Entry point

Everything is reached through [`IOpenShiftClient`](api/OpenSharp.Core.Abstractions.IOpenShiftClient.yml),
registered via `AddOpenSharp(...)`:

```csharp
var client = provider.GetRequiredService<IOpenShiftClient>();

await foreach (var pod in client.Pods.EnumerateAsync("my-project"))
    Console.WriteLine($"{pod.Metadata.Name}: {pod.Phase}");
```

## Capability map

| Area | Surface |
|------|---------|
| Read / lifecycle | `Projects`, `Pods`, `Deployments`, `DeploymentConfigs`, `Services`, `Routes`, `ConfigMaps`, `Secrets` |
| Logs, exec, scale, rollout | `Pods` and workload operations |
| Watch | `WatchAsync` on supported resources (auto-resume) |
| Nodes | `Nodes` — list/get plus cordon/uncordon |
| Cluster | `Cluster` — info and capability discovery |
| Generic escape hatch | `Generic` — any group/version/plural, with selectors, patch, and force-delete |
