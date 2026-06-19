using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Errors;
using OpenSharp.Core.Operations;
using OpenSharp.Core.Resources;
using OpenSharp.SystemTests.Support;
using Reqnroll;

namespace OpenSharp.SystemTests.Steps;

/// <summary>
/// Step definitions for US1 features: listing projects, listing/enumerating pods,
/// and surfacing typed authentication errors.
/// </summary>
[Binding]
public sealed class ReadSteps
{
    private readonly ScenarioContext _ctx;
    private readonly OpenShiftApiSimulator _sim;
    private readonly OpenShiftClient _client;

    private IReadOnlyList<Project>? _projects;
    private IReadOnlyList<Pod>? _pods;
    private Exception? _thrownException;

    public ReadSteps(ScenarioContext ctx)
    {
        _ctx = ctx;
        _sim = ctx.Get<OpenShiftApiSimulator>();
        _client = ctx.Get<OpenShiftClient>();
    }

    // ─── Given ───────────────────────────────────────────────────────────────

    [Given("the cluster has projects {string}")]
    public void GivenClusterHasProjects(string names)
    {
        var projects = names.Split(',')
            .Select(n => OpenShiftApiSimulator.MakeProject(n.Trim()))
            .ToArray();
        _sim.StubProjectList(projects);
        foreach (var n in names.Split(','))
        {
            _sim.StubGetProject(n.Trim(), OpenShiftApiSimulator.MakeProject(n.Trim()));
        }
    }

    [Given("the cluster has no projects")]
    public void GivenClusterHasNoProjects()
    {
        _sim.StubProjectList(Array.Empty<object>());
    }

    [Given("namespace {string} has pods {string} on page 1 and {string} on page 2")]
    public void GivenPodsOnTwoPages(string ns, string page1Names, string page2Names)
    {
        const string tok = "page2-token";
        var page1 = page1Names.Split(',').Select(n => OpenShiftApiSimulator.MakePod(n.Trim(), ns)).ToArray();
        var page2 = page2Names.Split(',').Select(n => OpenShiftApiSimulator.MakePod(n.Trim(), ns)).ToArray();
        _sim.StubPodList(ns, page1, continueToken: tok);
        _sim.StubPodListPage2(ns, page2, tok);
    }

    [Given("namespace {string} has pods {string} on a single page")]
    public void GivenPodsOnOnePage(string ns, string names)
    {
        var pods = names.Split(',').Select(n => OpenShiftApiSimulator.MakePod(n.Trim(), ns)).ToArray();
        _sim.StubPodList(ns, pods);
    }

    [Given("namespace {string} has {int} pods across pages of {int}")]
    public void GivenLargePagedPods(string ns, int total, int pageSize)
    {
        _sim.StubLargePodList(ns, total, pageSize);
    }

    [Given("the cluster returns 401 Unauthorized for project requests")]
    public void GivenUnauthorizedForProjects()
    {
        _sim.StubUnauthorized("/apis/project.openshift.io/v1/projects");
    }

    [Given("the cluster returns 401 Unauthorized for pod requests in namespace {string}")]
    public void GivenUnauthorizedForPods(string ns)
    {
        _sim.StubUnauthorized($"/api/v1/namespaces/{ns}/pods");
    }

    // ─── When ─────────────────────────────────────────────────────────────────

    [When("I list all projects")]
    public async Task WhenIListAllProjects()
    {
        var page = await _client.Projects.ListAsync();
        _projects = page.Items;
    }

    [When("I get the project named {string}")]
    public async Task WhenIGetProjectNamed(string name)
    {
        var project = await _client.Projects.GetAsync(name);
        _projects = [project];
    }

    [When("I enumerate all pods in namespace {string}")]
    public async Task WhenIEnumeratePodsInNamespace(string ns)
    {
        var list = new List<Pod>();
        await foreach (var pod in _client.Pods.EnumerateAsync(ns))
            list.Add(pod);
        _pods = list;
    }

    [When("I list pods in namespace {string}")]
    public async Task WhenIListPodsInNamespace(string ns)
    {
        var page = await _client.Pods.ListAsync(ns);
        _pods = page.Items;
    }

    [When("I attempt to list projects")]
    public async Task WhenIAttemptToListProjects()
    {
        try { await _client.Projects.ListAsync(); }
        catch (Exception ex) { _thrownException = ex; }
    }

    [When("I attempt to list pods in namespace {string}")]
    public async Task WhenIAttemptToListPodsInNamespace(string ns)
    {
        try { await _client.Pods.ListAsync(ns); }
        catch (Exception ex) { _thrownException = ex; }
    }

    // ─── Then ─────────────────────────────────────────────────────────────────

    [Then("the result contains {int} projects")]
    public void ThenResultContainsNProjects(int count)
    {
        Assert.NotNull(_projects);
        Assert.Equal(count, _projects.Count);
    }

    [Then("the project names include {string}")]
    public void ThenProjectNamesInclude(string name)
    {
        Assert.NotNull(_projects);
        Assert.Contains(_projects, p => p.Metadata.Name == name);
    }

    [Then("the project name is {string}")]
    public void ThenProjectNameIs(string name)
    {
        Assert.NotNull(_projects);
        Assert.Single(_projects);
        Assert.Equal(name, _projects[0].Metadata.Name);
    }

    [Then("I receive {int} pods total")]
    public void ThenIPodsTotalN(int count)
    {
        Assert.NotNull(_pods);
        Assert.Equal(count, _pods.Count);
    }

    [Then("the result contains {int} pods")]
    public void ThenResultContainsNPods(int count)
    {
        Assert.NotNull(_pods);
        Assert.Equal(count, _pods.Count);
    }

    [Then("the pod names include {string}")]
    public void ThenPodNamesInclude(string name)
    {
        Assert.NotNull(_pods);
        Assert.Contains(_pods, p => p.Metadata.Name == name);
    }

    [Then("an OpenShiftAuthenticationException is thrown")]
    public void ThenAuthExceptionIsThrown()
    {
        Assert.NotNull(_thrownException);
        Assert.IsType<OpenShiftAuthenticationException>(_thrownException);
    }
}
