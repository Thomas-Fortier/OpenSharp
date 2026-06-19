using System.Text.Json;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Generic;
using OpenSharp.Core.Operations;
using OpenSharp.Core.Resources;
using OpenSharp.SystemTests.Support;
using Reqnroll;

namespace OpenSharp.SystemTests.Steps;

/// <summary>
/// Step definitions for US4 features: watching pods for change events and reaching
/// unwrapped resource types through the generic escape hatch.
/// </summary>
[Binding]
public sealed class WatchAndGenericSteps
{
    private readonly OpenShiftApiSimulator _sim;
    private readonly OpenShiftClient _client;

    private List<WatchEvent<Pod>>? _watchEvents;
    private PagedList<JsonElement>? _genericList;
    private JsonElement? _genericItem;

    public WatchAndGenericSteps(ScenarioContext ctx)
    {
        _sim = ctx.Get<OpenShiftApiSimulator>();
        _client = ctx.Get<OpenShiftClient>();
    }

    private static GenericResourceRef ParseRef(string gvp, string? @namespace = null, string? name = null)
    {
        var parts = gvp.Split('/');
        return new GenericResourceRef
        {
            Group = parts[0],
            Version = parts[1],
            Plural = parts[2],
            Namespace = @namespace,
            Name = name,
        };
    }

    // ─── Given ───────────────────────────────────────────────────────────────

    [Given("a pod watch in namespace {string} emits events {string}")]
    public void GivenPodWatchEmits(string ns, string events)
    {
        var parsed = events.Split(',')
            .Select(e => e.Split(':'))
            .Select(p => (Type: p[0].Trim(), Name: p[1].Trim()))
            .ToList();
        _sim.StubWatchPods(ns, parsed);
    }

    [Given("the cluster has {string} named {string} in namespace {string}")]
    public void GivenGenericResources(string gvp, string names, string ns)
    {
        var parts = gvp.Split('/');
        var items = names.Split(',').Select(n => MakeWidget(parts[0], parts[1], n.Trim())).ToArray();
        _sim.StubGenericList(parts[0], parts[1], ns, parts[2], items);
        foreach (var n in names.Split(','))
            _sim.StubGenericGet(parts[0], parts[1], ns, parts[2], n.Trim(), MakeWidget(parts[0], parts[1], n.Trim()));
    }

    private static object MakeWidget(string group, string version, string name) => new
    {
        apiVersion = $"{group}/{version}",
        kind = "Widget",
        metadata = new { name, resourceVersion = "1" },
    };

    // ─── When ─────────────────────────────────────────────────────────────────

    [When("I watch pods in namespace {string} with auto-resume disabled")]
    public async Task WhenIWatchPods(string ns)
    {
        _watchEvents = new List<WatchEvent<Pod>>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await foreach (var e in _client.Pods.WatchAsync(ns, new WatchOptions { AutoResume = false }, cts.Token))
                _watchEvents.Add(e);
        }
        catch (OperationCanceledException)
        {
            // Safety net so a non-terminating stream cannot hang the suite.
        }
    }

    [When("I list generic resources {string} in namespace {string}")]
    public async Task WhenIListGeneric(string gvp, string ns)
    {
        _genericList = await _client.Generic.ListAsync(ParseRef(gvp, ns));
    }

    [When("I get generic resource {string} of {string} in namespace {string}")]
    public async Task WhenIGetGeneric(string name, string gvp, string ns)
    {
        _genericItem = await _client.Generic.GetAsync(ParseRef(gvp, ns, name));
    }

    // ─── Then ─────────────────────────────────────────────────────────────────

    [Then("I receive {int} watch events")]
    public void ThenIReceiveNWatchEvents(int count)
    {
        Assert.NotNull(_watchEvents);
        Assert.Equal(count, _watchEvents.Count);
    }

    [Then("watch event {int} is of type {string}")]
    public void ThenWatchEventIsOfType(int oneBasedIndex, string type)
    {
        Assert.NotNull(_watchEvents);
        var expected = Enum.Parse<WatchEventType>(type);
        Assert.Equal(expected, _watchEvents[oneBasedIndex - 1].Type);
    }

    [Then("the generic list contains {int} items")]
    public void ThenGenericListContains(int count)
    {
        Assert.NotNull(_genericList);
        Assert.Equal(count, _genericList.Items.Count);
    }

    [Then("the generic resource name is {string}")]
    public void ThenGenericResourceNameIs(string name)
    {
        Assert.NotNull(_genericItem);
        var actual = _genericItem.Value.GetProperty("metadata").GetProperty("name").GetString();
        Assert.Equal(name, actual);
    }
}
