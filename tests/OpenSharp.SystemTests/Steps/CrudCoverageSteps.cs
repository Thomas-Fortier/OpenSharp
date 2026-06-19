using System.Text.Json;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Generic;
using OpenSharp.Core.Operations;
using OpenSharp.Core.Resources;
using OpenSharp.SystemTests.Support;
using Reqnroll;

namespace OpenSharp.SystemTests.Steps;

/// <summary>
/// Step definitions that drive the full create/read/list/replace/patch/delete cycle for every
/// first-class resource, plus namespaced and cluster-scoped watches and generic create/delete.
/// These exercise the resource-mapping code paths that the focused US scenarios do not, raising
/// coverage of <c>OpenSharp.Core</c> toward the constitution's ≥80% gate.
/// </summary>
[Binding]
public sealed class CrudCoverageSteps
{
    private const string Ns = "test-ns";

    private readonly OpenShiftApiSimulator _sim;
    private readonly OpenShiftClient _client;

    private string? _readName;
    private Exception? _error;
    private int _watchCount;

    public CrudCoverageSteps(ScenarioContext ctx)
    {
        _sim = ctx.Get<OpenShiftApiSimulator>();
        _client = ctx.Get<OpenShiftClient>();
    }

    private static ResourceMetadata Md(string name, string? ns) =>
        new() { Name = name, Namespace = ns, ResourceVersion = "1" };

    private static JsonDocument Patch() =>
        JsonDocument.Parse("{\"metadata\":{\"labels\":{\"team\":\"qa\"}}}");

    // ─── Given ───────────────────────────────────────────────────────────────

    [Given("the namespace {string} supports full CRUD for {word} {string}")]
    public void GivenNamespacedCrud(string ns, string kind, string name)
    {
        switch (kind)
        {
            case "configmap":
                _sim.StubFullCrud($"/api/v1/namespaces/{ns}/configmaps", name, OpenShiftApiSimulator.MakeConfigMap(name, ns));
                break;
            case "secret":
                _sim.StubFullCrud($"/api/v1/namespaces/{ns}/secrets", name, OpenShiftApiSimulator.MakeSecret(name, ns));
                break;
            case "service":
                _sim.StubFullCrud($"/api/v1/namespaces/{ns}/services", name, OpenShiftApiSimulator.MakeService(name, ns));
                break;
            case "deployment":
                _sim.StubFullCrud($"/apis/apps/v1/namespaces/{ns}/deployments", name, OpenShiftApiSimulator.MakeDeployment(name, ns));
                break;
            case "deploymentconfig":
                _sim.StubFullCrud($"/apis/apps.openshift.io/v1/namespaces/{ns}/deploymentconfigs", name, OpenShiftApiSimulator.MakeDeploymentConfig(name, ns));
                break;
            case "pod":
                _sim.StubFullCrud($"/api/v1/namespaces/{ns}/pods", name, OpenShiftApiSimulator.MakePod(name, ns));
                break;
            case "route":
                _sim.StubFullCrud($"/apis/route.openshift.io/v1/namespaces/{ns}/routes", name, OpenShiftApiSimulator.MakeRoute(name, ns, "rt.example.com"));
                break;
            default:
                throw new NotSupportedException($"Unknown resource kind '{kind}'.");
        }
    }

    [Given("the cluster supports full CRUD for project {string}")]
    public void GivenProjectCrud(string name)
    {
        _sim.StubFullCrud("/apis/project.openshift.io/v1/projects", name, OpenShiftApiSimulator.MakeProject(name));
    }

    [Given("a deploymentconfig watch in namespace {string} emits an added event for {string}")]
    public void GivenDcWatch(string ns, string name)
    {
        _sim.StubWatchCustom("apps.openshift.io", "v1", ns, "deploymentconfigs",
            new[] { ("ADDED", OpenShiftApiSimulator.MakeDeploymentConfig(name, ns)) });
    }

    [Given("a project watch emits an added event for {string}")]
    public void GivenProjectWatch(string name)
    {
        _sim.StubWatchClusterCustom("project.openshift.io", "v1", "projects",
            new[] { ("ADDED", OpenShiftApiSimulator.MakeProject(name)) });
    }

    [Given("the cluster serves generic widgets for create, delete, and cluster listing")]
    public void GivenGenericWidgets()
    {
        _sim.StubGenericCreate("example.com", "v1", Ns, "widgets", OpenShiftApiSimulator.MakeCustom("example.com", "v1", "Widget", "gw1", Ns));
        _sim.StubGenericDelete("example.com", "v1", Ns, "widgets", "gw1");
        _sim.StubGenericClusterList("example.com", "v1", "widgets", new[] { OpenShiftApiSimulator.MakeCustom("example.com", "v1", "Widget", "cw1") });
    }

    [Given("the cluster serves generic widgets for namespaced get, namespaced list, and cluster get")]
    public void GivenGenericGetList()
    {
        _sim.StubGenericGet("example.com", "v1", Ns, "widgets", "nw1", OpenShiftApiSimulator.MakeCustom("example.com", "v1", "Widget", "nw1", Ns));
        _sim.StubGenericList("example.com", "v1", Ns, "widgets", new[] { OpenShiftApiSimulator.MakeCustom("example.com", "v1", "Widget", "nw1", Ns) });
        _sim.StubGenericClusterGet("example.com", "v1", "widgets", "cw1", OpenShiftApiSimulator.MakeCustom("example.com", "v1", "Widget", "cw1"));
    }

    [Given("a {string} watch in namespace {string} emits an added event for {string}")]
    public void GivenResourceWatch(string plural, string ns, string name)
    {
        switch (plural)
        {
            case "configmaps":
                _sim.StubWatchCore($"/api/v1/namespaces/{ns}/configmaps", new[] { ("ADDED", OpenShiftApiSimulator.MakeConfigMap(name, ns)) });
                break;
            case "secrets":
                _sim.StubWatchCore($"/api/v1/namespaces/{ns}/secrets", new[] { ("ADDED", OpenShiftApiSimulator.MakeSecret(name, ns)) });
                break;
            case "services":
                _sim.StubWatchCore($"/api/v1/namespaces/{ns}/services", new[] { ("ADDED", OpenShiftApiSimulator.MakeService(name, ns)) });
                break;
            case "deployments":
                _sim.StubWatchCore($"/apis/apps/v1/namespaces/{ns}/deployments", new[] { ("ADDED", OpenShiftApiSimulator.MakeDeployment(name, ns)) });
                break;
            case "routes":
                _sim.StubWatchCustom("route.openshift.io", "v1", ns, "routes", new[] { ("ADDED", OpenShiftApiSimulator.MakeRoute(name, ns, "rt.example.com")) });
                break;
            default:
                throw new NotSupportedException($"Unknown watch plural '{plural}'.");
        }
    }

    // ─── When ─────────────────────────────────────────────────────────────────

    [When("I watch {string} in namespace {string} with auto-resume disabled")]
    public async Task WhenIWatchResource(string plural, string ns)
    {
        var options = new WatchOptions { AutoResume = false };
        _watchCount = plural switch
        {
            "configmaps" => await CountWatchAsync(_client.ConfigMaps.WatchAsync(ns, options)),
            "secrets" => await CountWatchAsync(_client.Secrets.WatchAsync(ns, options)),
            "services" => await CountWatchAsync(_client.Services.WatchAsync(ns, options)),
            "deployments" => await CountWatchAsync(_client.Deployments.WatchAsync(ns, options)),
            "routes" => await CountWatchAsync(_client.Routes.WatchAsync(ns, options)),
            _ => throw new NotSupportedException($"Unknown watch plural '{plural}'."),
        };
    }

    [When("I get and list namespaced generic widgets and get a cluster widget")]
    public async Task WhenIGenericGetList()
    {
        try
        {
            var nsRef = new GenericResourceRef { Group = "example.com", Version = "v1", Plural = "widgets", Namespace = Ns, Name = "nw1" };
            var got = await _client.Generic.GetAsync(nsRef);
            Assert.Equal("nw1", got.GetProperty("metadata").GetProperty("name").GetString());

            var list = await _client.Generic.ListAsync(new GenericResourceRef { Group = "example.com", Version = "v1", Plural = "widgets", Namespace = Ns });
            Assert.NotEmpty(list.Items);

            var clusterRef = new GenericResourceRef { Group = "example.com", Version = "v1", Plural = "widgets", Name = "cw1" };
            var cluster = await _client.Generic.GetAsync(clusterRef);
            Assert.Equal("cw1", cluster.GetProperty("metadata").GetProperty("name").GetString());
        }
        catch (Exception ex)
        {
            _error = ex;
        }
    }

    [When("I exercise full CRUD on {string} for {string}")]
    public async Task WhenIExerciseCrud(string plural, string name)
    {
        try
        {
            switch (plural)
            {
                case "configmaps":
                    var cm = new ConfigMap { Metadata = Md(name, Ns) };
                    await _client.ConfigMaps.CreateAsync(cm);
                    _readName = (await _client.ConfigMaps.GetAsync(name, Ns)).Metadata.Name;
                    await _client.ConfigMaps.ListAsync(Ns);
                    await _client.ConfigMaps.ReplaceAsync(cm);
                    await _client.ConfigMaps.PatchAsync(name, Ns, Patch());
                    await _client.ConfigMaps.DeleteAsync(name, Ns, DeletePropagationPolicy.Foreground);
                    break;
                case "secrets":
                    var sec = new Secret { Metadata = Md(name, Ns), Type = "Opaque" };
                    await _client.Secrets.CreateAsync(sec);
                    _readName = (await _client.Secrets.GetAsync(name, Ns)).Metadata.Name;
                    await _client.Secrets.ListAsync(Ns);
                    await _client.Secrets.ReplaceAsync(sec);
                    await _client.Secrets.PatchAsync(name, Ns, Patch());
                    await _client.Secrets.DeleteAsync(name, Ns);
                    break;
                case "services":
                    var svc = new Service { Metadata = Md(name, Ns), Type = "ClusterIP" };
                    await _client.Services.CreateAsync(svc);
                    _readName = (await _client.Services.GetAsync(name, Ns)).Metadata.Name;
                    await _client.Services.ListAsync(Ns);
                    await _client.Services.ReplaceAsync(svc);
                    await _client.Services.PatchAsync(name, Ns, Patch());
                    await _client.Services.DeleteAsync(name, Ns, DeletePropagationPolicy.Orphan);
                    break;
                case "deployments":
                    var dep = new Deployment { Metadata = Md(name, Ns), Replicas = 1 };
                    await _client.Deployments.CreateAsync(dep);
                    _readName = (await _client.Deployments.GetAsync(name, Ns)).Metadata.Name;
                    await _client.Deployments.ListAsync(Ns);
                    await _client.Deployments.ReplaceAsync(dep);
                    await _client.Deployments.PatchAsync(name, Ns, Patch());
                    await _client.Deployments.DeleteAsync(name, Ns);
                    break;
                case "deploymentconfigs":
                    var dc = new Deployment { Metadata = Md(name, Ns), Replicas = 1 };
                    await _client.DeploymentConfigs.CreateAsync(dc);
                    _readName = (await _client.DeploymentConfigs.GetAsync(name, Ns)).Metadata.Name;
                    await _client.DeploymentConfigs.ListAsync(Ns);
                    await _client.DeploymentConfigs.ReplaceAsync(dc);
                    await _client.DeploymentConfigs.PatchAsync(name, Ns, Patch());
                    await _client.DeploymentConfigs.DeleteAsync(name, Ns);
                    break;
                case "pods":
                    var pod = new Pod { Metadata = Md(name, Ns), Phase = "Running" };
                    await _client.Pods.CreateAsync(pod);
                    _readName = (await _client.Pods.GetAsync(name, Ns)).Metadata.Name;
                    await _client.Pods.ListAsync(Ns);
                    await _client.Pods.ReplaceAsync(pod);
                    await _client.Pods.PatchAsync(name, Ns, Patch());
                    await _client.Pods.DeleteAsync(name, Ns);
                    break;
                case "projects":
                    var proj = new Project { Metadata = Md(name, null) };
                    await _client.Projects.CreateAsync(proj);
                    _readName = (await _client.Projects.GetAsync(name)).Metadata.Name;
                    await _client.Projects.ListAsync();
                    await _client.Projects.ReplaceAsync(proj);
                    await _client.Projects.PatchAsync(name, null, Patch());
                    await _client.Projects.DeleteAsync(name);
                    break;
                default:
                    throw new NotSupportedException($"Unknown plural '{plural}'.");
            }
        }
        catch (Exception ex)
        {
            _error = ex;
        }
    }

    [When("I exercise read, list, and patch on routes for {string}")]
    public async Task WhenIExerciseRoute(string name)
    {
        try
        {
            _readName = (await _client.Routes.GetAsync(name, Ns)).Metadata.Name;
            await _client.Routes.ListAsync(Ns);
            await _client.Routes.PatchAsync(name, Ns, Patch());
        }
        catch (Exception ex)
        {
            _error = ex;
        }
    }

    [When("I watch deploymentconfigs in namespace {string} with auto-resume disabled")]
    public async Task WhenIWatchDc(string ns)
    {
        _watchCount = await CountWatchAsync(_client.DeploymentConfigs.WatchAsync(ns, new WatchOptions { AutoResume = false }));
    }

    [When("I watch projects with auto-resume disabled")]
    public async Task WhenIWatchProjects()
    {
        _watchCount = await CountWatchAsync(_client.Projects.WatchAsync(options: new WatchOptions { AutoResume = false }));
    }

    [When("I create and delete a generic widget {string} and list cluster widgets")]
    public async Task WhenIGenericCreateDeleteList(string name)
    {
        try
        {
            var nsRef = new GenericResourceRef { Group = "example.com", Version = "v1", Plural = "widgets", Namespace = Ns };
            var body = JsonSerializer.SerializeToElement(OpenShiftApiSimulator.MakeCustom("example.com", "v1", "Widget", name, Ns));
            await _client.Generic.CreateAsync(nsRef, body);
            await _client.Generic.DeleteAsync(new GenericResourceRef { Group = "example.com", Version = "v1", Plural = "widgets", Namespace = Ns, Name = name });
            var clusterList = await _client.Generic.ListAsync(new GenericResourceRef { Group = "example.com", Version = "v1", Plural = "widgets" });
            Assert.NotEmpty(clusterList.Items);
        }
        catch (Exception ex)
        {
            _error = ex;
        }
    }

    private static async Task<int> CountWatchAsync<T>(IAsyncEnumerable<WatchEvent<T>> stream)
    {
        var count = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await foreach (var _ in stream.WithCancellation(cts.Token))
                count++;
        }
        catch (OperationCanceledException)
        {
            // Safety net against a non-terminating stream.
        }
        return count;
    }

    // ─── Then ─────────────────────────────────────────────────────────────────

    [Then("every CRUD operation succeeds")]
    public void ThenEveryCrudSucceeds()
    {
        Assert.True(_error is null, _error?.ToString());
    }

    [Then("the read-back resource is named {string}")]
    public void ThenReadBackNamed(string name)
    {
        Assert.Equal(name, _readName);
    }

    [Then("I receive {int} workload watch event")]
    public void ThenIReceiveNWorkloadWatchEvents(int count)
    {
        Assert.Equal(count, _watchCount);
    }
}
