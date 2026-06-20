using System.Text.Json;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Errors;
using OpenSharp.Core.Generic;
using OpenSharp.Core.Operations;
using OpenSharp.Core.Resources;
using OpenSharp.SystemTests.Support;
using Reqnroll;

namespace OpenSharp.SystemTests.Steps;

/// <summary>
/// Step definitions for US2: node list/get/cordon/uncordon and generic reach into the core
/// (legacy) API group.
/// </summary>
[Binding]
public sealed class NodeSteps
{
    private readonly OpenShiftApiSimulator _sim;
    private readonly OpenShiftClient _client;

    private IReadOnlyList<Node>? _nodes;
    private Node? _node;
    private JsonElement? _coreItem;
    private int _watchCount;
    private Exception? _error;

    public NodeSteps(ScenarioContext ctx)
    {
        _sim = ctx.Get<OpenShiftApiSimulator>();
        _client = ctx.Get<OpenShiftClient>();
    }

    // ─── Given ───────────────────────────────────────────────────────────────

    [Given("the cluster has nodes {string}")]
    public void GivenClusterHasNodes(string names)
    {
        var nodes = names.Split(',').Select(n => OpenShiftApiSimulator.MakeNode(n.Trim())).ToArray();
        _sim.StubNodeList(nodes);
        foreach (var n in names.Split(','))
            _sim.StubGetNode(n.Trim(), OpenShiftApiSimulator.MakeNode(n.Trim()));
    }

    [Given("node {string} accepts cordon and uncordon")]
    public void GivenNodeAcceptsCordon(string name)
    {
        _sim.StubGetNode(name, OpenShiftApiSimulator.MakeNode(name));
        _sim.StubPatchNode(name, OpenShiftApiSimulator.MakeNode(name, unschedulable: true));
    }

    [Given("node {string} does not exist")]
    public void GivenNodeMissing(string name)
    {
        _sim.StubNotFound($"/api/v1/nodes/{name}");
    }

    [Given("namespace {string} serves a core {string} named {string}")]
    public void GivenCoreResource(string ns, string plural, string name)
    {
        var kind = char.ToUpperInvariant(plural[0]) + plural[1..].TrimEnd('s');
        _sim.StubCoreNamespacedGet(plural, ns, name, OpenShiftApiSimulator.MakeCoreObject(kind, name, ns));
    }

    [Given("a node watch emits events {string}")]
    public void GivenNodeWatch(string events)
    {
        var parsed = events.Split(',')
            .Select(e => e.Split(':'))
            .Select(p => (Type: p[0].Trim(), Name: p[1].Trim()))
            .ToList();
        _sim.StubWatchNodes(parsed);
    }

    // ─── When ─────────────────────────────────────────────────────────────────

    [When("I list nodes")]
    public async Task WhenIListNodes()
    {
        _nodes = (await _client.Nodes.ListAsync()).Items;
    }

    [When("I get node {string}")]
    public async Task WhenIGetNode(string name)
    {
        _node = await _client.Nodes.GetAsync(name);
    }

    [When("I cordon node {string}")]
    public async Task WhenICordon(string name)
    {
        await _client.Nodes.CordonAsync(name);
    }

    [When("I uncordon node {string}")]
    public async Task WhenIUncordon(string name)
    {
        await _client.Nodes.UncordonAsync(name);
    }

    [When("I attempt to get node {string}")]
    public async Task WhenIAttemptGetNode(string name)
    {
        try { _node = await _client.Nodes.GetAsync(name); }
        catch (Exception ex) { _error = ex; }
    }

    [When("I get core resource {string} named {string} in namespace {string}")]
    public async Task WhenIGetCore(string plural, string name, string ns)
    {
        var reference = new GenericResourceRef { Group = "", Version = "v1", Plural = plural, Namespace = ns, Name = name };
        _coreItem = await _client.Generic.GetAsync(reference);
    }

    [When("I watch nodes with auto-resume disabled")]
    public async Task WhenIWatchNodes()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await foreach (var _ in _client.Nodes.WatchAsync(options: new WatchOptions { AutoResume = false }, ct: cts.Token))
                _watchCount++;
        }
        catch (OperationCanceledException)
        {
            // Safety net against a non-terminating stream.
        }
    }

    // ─── Then ─────────────────────────────────────────────────────────────────

    [Then("the node list contains {int} items")]
    public void ThenNodeListContains(int count)
    {
        Assert.NotNull(_nodes);
        Assert.Equal(count, _nodes.Count);
    }

    [Then("node {string} is schedulable")]
    public void ThenNodeSchedulable(string name)
    {
        Assert.NotNull(_node);
        Assert.Equal(name, _node.Metadata.Name);
        Assert.False(_node.Unschedulable);
    }

    [Then("the node patch request count is {int}")]
    public void ThenNodePatchCount(int count)
    {
        Assert.Equal(count, _sim.CountRequests("PATCH", "/nodes/"));
    }

    [Then("a node not-found error is raised")]
    public void ThenNodeNotFound()
    {
        Assert.IsType<OpenShiftNotFoundException>(_error);
    }

    [Then("the core resource name is {string}")]
    public void ThenCoreResourceName(string name)
    {
        Assert.NotNull(_coreItem);
        Assert.Equal(name, _coreItem.Value.GetProperty("metadata").GetProperty("name").GetString());
    }

    [Then("I receive {int} node watch events")]
    public void ThenNodeWatchEvents(int count)
    {
        Assert.Equal(count, _watchCount);
    }
}
