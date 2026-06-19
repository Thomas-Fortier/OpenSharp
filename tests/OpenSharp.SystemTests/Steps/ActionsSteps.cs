using OpenSharp.Core.Errors;
using OpenSharp.Core.Operations;
using OpenSharp.Core.Resources;
using OpenSharp.SystemTests.Support;
using Reqnroll;

namespace OpenSharp.SystemTests.Steps;

/// <summary>
/// Step definitions for US3 features: reading and following logs, scaling and rolling out
/// workloads, and (live-only) executing commands inside containers.
/// </summary>
[Binding]
public sealed class ActionsSteps
{
    private readonly OpenShiftApiSimulator _sim;
    private readonly OpenShiftClient _client;

    private string? _logs;
    private List<string>? _logLines;
    private ExecResult? _execResult;
    private Exception? _thrownException;

    public ActionsSteps(ScenarioContext ctx)
    {
        _sim = ctx.Get<OpenShiftApiSimulator>();
        _client = ctx.Get<OpenShiftClient>();
    }

    // ─── Given ───────────────────────────────────────────────────────────────

    [Given("pod {string} in namespace {string} has log lines {string}")]
    public void GivenPodHasLogLines(string name, string ns, string lines)
    {
        var text = string.Join("\n", lines.Split(',').Select(l => l.Trim())) + "\n";
        _sim.StubPodLog(ns, name, text);
    }

    [Given("deployment {string} in namespace {string} accepts scale and rollout operations")]
    public void GivenDeploymentAcceptsScaleRollout(string name, string ns)
    {
        _sim.StubPatchDeployment(ns, name);
        _sim.StubGetDeployment(ns, name);
    }

    [Given("a live cluster has a running pod {string} in namespace {string}")]
    public void GivenLivePod(string name, string ns)
    {
        // Intentionally empty: the @live hook skips this scenario unless OPENSHARP_LIVE is set.
    }

    // ─── When ─────────────────────────────────────────────────────────────────

    [When("I read logs for pod {string}")]
    public async Task WhenIReadLogs(string name)
    {
        _logs = await _client.Pods.ReadLogsAsync(name, "test-ns", new LogOptions());
    }

    [When("I follow logs for pod {string}")]
    public async Task WhenIFollowLogs(string name)
    {
        _logLines = new List<string>();
        await foreach (var line in _client.Pods.FollowLogsAsync(name, "test-ns", new LogOptions()))
            _logLines.Add(line);
    }

    [When("I scale deployment {string} to {int} replicas")]
    public async Task WhenIScaleDeployment(string name, int replicas)
    {
        await _client.Deployments.ScaleAsync(name, "test-ns", replicas);
    }

    [When("I attempt to scale deployment {string} to {int} replicas")]
    public async Task WhenIAttemptToScaleDeployment(string name, int replicas)
    {
        try { await _client.Deployments.ScaleAsync(name, "test-ns", replicas); }
        catch (Exception ex) { _thrownException = ex; }
    }

    [When("I trigger a rollout restart of deployment {string}")]
    public async Task WhenIRolloutRestart(string name)
    {
        await _client.Deployments.RolloutRestartAsync(name, "test-ns");
    }

    [When("I exec command {string} in pod {string}")]
    public async Task WhenIExec(string command, string name)
    {
        var request = new ExecRequest { Command = command.Split(',').Select(c => c.Trim()).ToList() };
        _execResult = await _client.Pods.ExecAsync(name, "test-ns", request);
    }

    // ─── Then ─────────────────────────────────────────────────────────────────

    [Then("the logs contain {string}")]
    public void ThenLogsContain(string fragment)
    {
        Assert.NotNull(_logs);
        Assert.Contains(fragment, _logs);
    }

    [Then("I receive the log lines {string}")]
    public void ThenIReceiveLogLines(string expected)
    {
        Assert.NotNull(_logLines);
        var want = expected.Split(',').Select(l => l.Trim()).ToList();
        Assert.Equal(want, _logLines);
    }

    [Then("the workload operation completes without error")]
    public void ThenWorkloadCompletes()
    {
        Assert.Null(_thrownException);
    }

    [Then("a workload validation error is raised")]
    public void ThenWorkloadValidationError()
    {
        Assert.NotNull(_thrownException);
        Assert.IsType<OpenShiftValidationException>(_thrownException);
    }

    [Then("the exec stdout contains {string}")]
    public void ThenExecStdoutContains(string fragment)
    {
        Assert.NotNull(_execResult);
        Assert.Contains(fragment, _execResult.StdOut);
    }

    [Then("the exec exit code is {int}")]
    public void ThenExecExitCodeIs(int code)
    {
        Assert.NotNull(_execResult);
        Assert.Equal(code, _execResult.ExitCode);
    }
}
