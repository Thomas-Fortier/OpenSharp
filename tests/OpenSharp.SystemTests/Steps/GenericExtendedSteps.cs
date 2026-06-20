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
/// Step definitions for US1: generic list label-selector filtering, generic patch, and
/// force/zero-grace delete. Uses a sample <c>example.com/v1 widgets</c> custom resource.
/// </summary>
[Binding]
public sealed class GenericExtendedSteps
{
    private const string Group = "example.com";
    private const string Version = "v1";
    private const string Plural = "widgets";

    private readonly OpenShiftApiSimulator _sim;
    private readonly OpenShiftClient _client;

    private PagedList<JsonElement>? _list;
    private JsonElement? _patched;
    private Exception? _error;

    public GenericExtendedSteps(ScenarioContext ctx)
    {
        _sim = ctx.Get<OpenShiftApiSimulator>();
        _client = ctx.Get<OpenShiftClient>();
    }

    private static object Widget(string name, string? @namespace, IReadOnlyDictionary<string, string>? labels = null) => new
    {
        apiVersion = $"{Group}/{Version}",
        kind = "Widget",
        metadata = new { name, @namespace, resourceVersion = "1", labels = labels ?? new Dictionary<string, string>() },
    };

    private static object[] Widgets(string csv, string ns, IReadOnlyDictionary<string, string>? labels = null) =>
        csv.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)
            .Select(n => Widget(n, ns, labels)).ToArray();

    private GenericResourceRef Ref(string? ns = null, string? name = null) =>
        new() { Group = Group, Version = Version, Plural = Plural, Namespace = ns, Name = name };

    // ─── Given ───────────────────────────────────────────────────────────────

    [Given("namespace {string} serves widgets {string} where {string} carry label {string}")]
    public void GivenNamespacedWidgetsWithLabel(string ns, string allNames, string matchingNames, string selector)
    {
        _sim.StubGenericListFiltered(Group, Version, ns, Plural, selector,
            Widgets(matchingNames, ns, ParseSelector(selector)), Widgets(allNames, ns));
    }

    [Given("all namespaces serve widgets {string} where {string} carry label {string}")]
    public void GivenAllNamespaceWidgetsWithLabel(string allNames, string matchingNames, string selector)
    {
        _sim.StubGenericListFiltered(Group, Version, null, Plural, selector,
            Widgets(matchingNames, "any", ParseSelector(selector)), Widgets(allNames, "any"));
    }

    [Given("widget {string} in namespace {string} accepts a patch setting label team {string}")]
    public void GivenPatchAccepted(string name, string ns, string team)
    {
        _sim.StubGenericPatch(Group, Version, ns, Plural, name,
            Widget(name, ns, new Dictionary<string, string> { ["team"] = team }));
    }

    [Given("patching widget {string} in namespace {string} is rejected as invalid")]
    public void GivenPatchInvalid(string name, string ns)
    {
        _sim.StubGenericPatchInvalid(Group, Version, ns, Plural, name);
    }

    [Given("widget {string} in namespace {string} can be deleted")]
    public void GivenWidgetDeletable(string name, string ns)
    {
        _sim.StubGenericDelete(Group, Version, ns, Plural, name);
    }

    [Given("deleting widget {string} in namespace {string} returns not found")]
    public void GivenDeleteNotFound(string name, string ns)
    {
        _sim.StubNotFound($"/apis/{Group}/{Version}/namespaces/{ns}/{Plural}/{name}");
    }

    // ─── When ─────────────────────────────────────────────────────────────────

    [When("I list widgets in namespace {string} filtered by {string}")]
    public async Task WhenIListNamespacedFiltered(string ns, string selector)
    {
        _list = await _client.Generic.ListAsync(Ref(ns), labelSelector: selector);
    }

    [When("I list widgets in all namespaces filtered by {string}")]
    public async Task WhenIListAllNamespacesFiltered(string selector)
    {
        _list = await _client.Generic.ListAsync(Ref(), labelSelector: selector);
    }

    [When("I patch widget {string} in namespace {string} setting label team {string}")]
    public async Task WhenIPatch(string name, string ns, string team)
    {
        using var patch = JsonDocument.Parse($"{{\"metadata\":{{\"labels\":{{\"team\":\"{team}\"}}}}}}");
        _patched = await _client.Generic.PatchAsync(Ref(ns, name), patch);
    }

    [When("I attempt to patch widget {string} in namespace {string}")]
    public async Task WhenIAttemptPatch(string name, string ns)
    {
        using var patch = JsonDocument.Parse("{\"metadata\":{\"labels\":{\"team\":\"qa\"}}}");
        try { _patched = await _client.Generic.PatchAsync(Ref(ns, name), patch); }
        catch (Exception ex) { _error = ex; }
    }

    [When("I force-delete widget {string} in namespace {string}")]
    public async Task WhenIForceDelete(string name, string ns)
    {
        await _client.Generic.DeleteAsync(Ref(ns, name), new DeleteOptions { Force = true });
    }

    [When("I attempt to force-delete widget {string} in namespace {string}")]
    public async Task WhenIAttemptForceDelete(string name, string ns)
    {
        try { await _client.Generic.DeleteAsync(Ref(ns, name), new DeleteOptions { Force = true }); }
        catch (Exception ex) { _error = ex; }
    }

    // ─── Then ─────────────────────────────────────────────────────────────────

    [Then("the widget list contains {int} items")]
    public void ThenListContains(int count)
    {
        Assert.NotNull(_list);
        Assert.Equal(count, _list.Items.Count);
    }

    [Then("the patched widget has label team {string}")]
    public void ThenPatchedHasLabel(string team)
    {
        Assert.NotNull(_patched);
        var actual = _patched.Value.GetProperty("metadata").GetProperty("labels").GetProperty("team").GetString();
        Assert.Equal(team, actual);
    }

    [Then("a generic validation error is raised")]
    public void ThenValidationError()
    {
        Assert.IsType<OpenShiftValidationException>(_error);
    }

    [Then("the widget delete request was sent")]
    public void ThenDeleteSent()
    {
        Assert.True(_sim.CountRequests("DELETE", $"/{Plural}/") > 0);
    }

    [Then("a generic not-found error is raised")]
    public void ThenNotFoundError()
    {
        Assert.IsType<OpenShiftNotFoundException>(_error);
    }

    private static Dictionary<string, string> ParseSelector(string selector)
    {
        var kv = selector.Split('=', 2);
        return kv.Length == 2 ? new Dictionary<string, string> { [kv[0]] = kv[1] } : new Dictionary<string, string>();
    }
}
