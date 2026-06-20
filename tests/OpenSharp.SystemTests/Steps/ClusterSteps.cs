using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Errors;
using OpenSharp.Core.Operations;
using OpenSharp.Core.Resources;
using OpenSharp.SystemTests.Support;
using Reqnroll;

namespace OpenSharp.SystemTests.Steps;

/// <summary>Step definitions for US3: cluster information and resource-type availability discovery.</summary>
[Binding]
public sealed class ClusterSteps
{
    private readonly OpenShiftApiSimulator _sim;
    private readonly OpenShiftClient _client;

    private OpenShiftClient? _override;
    private ClusterInfo? _info;
    private bool? _available;
    private Exception? _error;

    public ClusterSteps(ScenarioContext ctx)
    {
        _sim = ctx.Get<OpenShiftApiSimulator>();
        _client = ctx.Get<OpenShiftClient>();
    }

    private OpenShiftClient Client => _override ?? _client;

    // ─── Given ───────────────────────────────────────────────────────────────

    [Given("the cluster reports version {string}")]
    public void GivenClusterVersion(string version) => _sim.StubVersion(version);

    [Given("the cluster version endpoint is unavailable")]
    public void GivenVersionUnavailable()
    {
        // Point a throwaway client at a closed port so the version call fails to connect.
        var factory = new WireMockConnectionFactory("http://127.0.0.1:1");
        _override = new OpenShiftClient(factory, Options.Create(new OpenShiftClientOptions()), NullLogger<OpenShiftClient>.Instance);
    }

    [Given("the cluster serves resources {string} in group {string} version {string}")]
    public void GivenServesResources(string plurals, string group, string version) =>
        _sim.StubApiResources(group, version, plurals.Split(',').Select(p => p.Trim()));

    [Given("the cluster does not serve group {string} version {string}")]
    public void GivenGroupNotServed(string group, string version) => _sim.StubApiGroupNotFound(group, version);

    // ─── When ─────────────────────────────────────────────────────────────────

    [When("I get cluster info")]
    public async Task WhenIGetInfo() => _info = await Client.Cluster.GetInfoAsync();

    [When("I attempt to get cluster info")]
    public async Task WhenIAttemptGetInfo()
    {
        try { _info = await Client.Cluster.GetInfoAsync(); }
        catch (Exception ex) { _error = ex; }
    }

    [When("I check availability of {string} in group {string} version {string}")]
    public async Task WhenICheckAvailability(string plural, string group, string version) =>
        _available = await Client.Cluster.IsResourceTypeAvailableAsync(group, version, plural);

    // ─── Then ─────────────────────────────────────────────────────────────────

    [Then("the cluster server version is {string}")]
    public void ThenServerVersion(string version)
    {
        Assert.NotNull(_info);
        Assert.Equal(version, _info.ServerVersion);
    }

    [Then("the cluster endpoint is set")]
    public void ThenEndpointSet()
    {
        Assert.NotNull(_info);
        Assert.False(string.IsNullOrEmpty(_info.ApiServerEndpoint));
    }

    [Then("the cluster is reachable")]
    public void ThenReachable()
    {
        Assert.NotNull(_info);
        Assert.True(_info.Reachable);
    }

    [Then("a cluster connection error is raised")]
    public void ThenConnectionError() => Assert.IsType<OpenShiftConnectionException>(_error);

    [Then("the resource type is available")]
    public void ThenAvailable() => Assert.True(_available);

    [Then("the resource type is not available")]
    public void ThenNotAvailable() => Assert.False(_available);
}
