using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Operations;
using Reqnroll;

namespace OpenSharp.SystemTests.Support;

/// <summary>
/// Reqnroll hooks shared across all scenarios.
/// Manages <see cref="OpenShiftApiSimulator"/> lifecycle and wires up the
/// <see cref="OpenShiftClient"/> instance injected into step definitions.
/// </summary>
[Binding]
public sealed class Hooks
{
    private readonly ScenarioContext _ctx;

    public Hooks(ScenarioContext ctx) => _ctx = ctx;

    /// <summary>Before each scenario: start the WireMock simulator and build a client.</summary>
    [BeforeScenario(Order = 0)]
    public void BeforeScenario()
    {
        var simulator = OpenShiftApiSimulator.Start();
        var factory = new WireMockConnectionFactory(simulator.BaseUrl);
        var options = Options.Create(new OpenShiftClientOptions
        {
            DefaultNamespace = "test-ns",
        });
        var client = new OpenShiftClient(factory, options, NullLogger<OpenShiftClient>.Instance);

        _ctx.Set(simulator);
        _ctx.Set(client);
    }

    /// <summary>After each scenario: dispose the simulator.</summary>
    [AfterScenario(Order = 0)]
    public void AfterScenario()
    {
        if (_ctx.TryGetValue(out OpenShiftApiSimulator? simulator))
            simulator?.Dispose();
    }

    /// <summary>
    /// Skip scenarios tagged <c>@live</c> when the <c>OPENSHARP_LIVE</c>
    /// environment variable is not set to a non-empty value. Uses xUnit's dynamic skip
    /// (the generated tests are <c>[SkippableFact]</c>) so an unconfigured run reports the
    /// scenario as <em>skipped</em> rather than failed.
    /// </summary>
    [BeforeScenario("@live", Order = -1)]
    public void SkipIfNotLive()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENSHARP_LIVE")))
            throw new Xunit.SkipException("Set OPENSHARP_LIVE and configure a cluster to run @live scenarios.");
    }
}
