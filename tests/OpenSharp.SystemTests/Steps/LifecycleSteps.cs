using OpenSharp.Core.Errors;
using OpenSharp.Core.Operations;
using OpenSharp.Core.Resources;
using OpenSharp.SystemTests.Support;
using Reqnroll;

namespace OpenSharp.SystemTests.Steps;

/// <summary>
/// Step definitions for US2 features: creating, reading, updating, and deleting Routes,
/// and surfacing typed validation errors for conflicts and non-OpenShift targets.
/// </summary>
[Binding]
public sealed class LifecycleSteps
{
    private readonly OpenShiftApiSimulator _sim;
    private readonly OpenShiftClient _client;

    private Route? _route;
    private Exception? _thrownException;

    public LifecycleSteps(ScenarioContext ctx)
    {
        _sim = ctx.Get<OpenShiftApiSimulator>();
        _client = ctx.Get<OpenShiftClient>();
    }

    private static Route NewRoute(string name, string @namespace, string host) => new()
    {
        Metadata = new ResourceMetadata { Name = name, Namespace = @namespace, ResourceVersion = "1" },
        Host = host,
        To = new RouteTarget { Kind = "Service", Name = "my-svc", Weight = 100 },
    };

    // ─── Given ───────────────────────────────────────────────────────────────

    [Given("a route {string} can be created, read, updated, and deleted in namespace {string}")]
    public void GivenRouteLifecycle(string name, string ns)
    {
        _sim.StubCreateRoute(ns, OpenShiftApiSimulator.MakeRoute(name, ns, "my-route.example.com"));
        _sim.StubGetRoute(ns, name, OpenShiftApiSimulator.MakeRoute(name, ns, "my-route.example.com"));
        _sim.StubReplaceRoute(ns, name, OpenShiftApiSimulator.MakeRoute(name, ns, "updated.example.com"));
        _sim.StubDeleteRoute(ns, name);
    }

    [Given("creating route {string} in namespace {string} returns a conflict")]
    public void GivenCreateConflict(string name, string ns)
    {
        _sim.StubConflict($"/apis/route.openshift.io/v1/namespaces/{ns}/routes");
    }

    [Given("the cluster does not serve the OpenShift Route API in namespace {string}")]
    public void GivenRouteApiUnavailable(string ns)
    {
        _sim.StubRouteApiUnavailable(ns);
    }

    // ─── When ─────────────────────────────────────────────────────────────────

    [When("I create a route {string} with host {string}")]
    public async Task WhenICreateRoute(string name, string host)
    {
        _route = await _client.Routes.CreateAsync(NewRoute(name, "test-ns", host));
    }

    [When("I attempt to create a route {string} with host {string}")]
    public async Task WhenIAttemptToCreateRoute(string name, string host)
    {
        try { _route = await _client.Routes.CreateAsync(NewRoute(name, "test-ns", host)); }
        catch (Exception ex) { _thrownException = ex; }
    }

    [When("I get the route {string}")]
    public async Task WhenIGetRoute(string name)
    {
        _route = await _client.Routes.GetAsync(name, "test-ns");
    }

    [When("I replace the route {string} with host {string}")]
    public async Task WhenIReplaceRoute(string name, string host)
    {
        _route = await _client.Routes.ReplaceAsync(NewRoute(name, "test-ns", host));
    }

    [When("I delete the route {string}")]
    public async Task WhenIDeleteRoute(string name)
    {
        await _client.Routes.DeleteAsync(name, "test-ns");
    }

    // ─── Then ─────────────────────────────────────────────────────────────────

    [Then("the resulting route host is {string}")]
    public void ThenResultingRouteHostIs(string host)
    {
        Assert.NotNull(_route);
        Assert.Equal(host, _route.Host);
    }

    [Then("the route operation completes without error")]
    public void ThenRouteOperationCompletes()
    {
        Assert.Null(_thrownException);
    }

    [Then("an OpenShiftValidationException is raised")]
    public void ThenValidationExceptionIsRaised()
    {
        Assert.NotNull(_thrownException);
        Assert.IsType<OpenShiftValidationException>(_thrownException);
    }

    [Then("the route error message mentions {string}")]
    public void ThenRouteErrorMentions(string fragment)
    {
        Assert.NotNull(_thrownException);
        Assert.Contains(fragment, _thrownException.Message);
    }
}
