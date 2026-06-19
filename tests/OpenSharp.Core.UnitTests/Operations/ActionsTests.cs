using k8s;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Errors;
using OpenSharp.Core.Operations;
using OpenSharp.Core.Resources;

namespace OpenSharp.Core.UnitTests.Operations;

/// <summary>
/// Tests for US3 action option shaping and pre-flight validation: log/exec request models
/// and the negative-replica guard on scale, which is enforced before any API call.
/// </summary>
public sealed class ActionsTests
{
    private static DeploymentOperations NewDeploymentOps(bool isDeploymentConfig) =>
        new(Mock.Of<IKubernetes>(), new OpenShiftClientOptions(), NullLogger.Instance, isDeploymentConfig);

    [Fact]
    public async Task ScaleAsync_NegativeReplicas_ThrowsValidation_Deployment()
    {
        var ops = NewDeploymentOps(isDeploymentConfig: false);
        await Assert.ThrowsAsync<OpenShiftValidationException>(
            () => ops.ScaleAsync("api", "ns", -1));
    }

    [Fact]
    public async Task ScaleAsync_NegativeReplicas_ThrowsValidation_DeploymentConfig()
    {
        var ops = NewDeploymentOps(isDeploymentConfig: true);
        await Assert.ThrowsAsync<OpenShiftValidationException>(
            () => ops.ScaleAsync("api", "ns", -1));
    }

    [Fact]
    public void LogOptions_Defaults_AreUnset()
    {
        var options = new LogOptions();
        Assert.Null(options.Container);
        Assert.False(options.Follow);
        Assert.Null(options.TailLines);
        Assert.False(options.Previous);
        Assert.Null(options.SinceSeconds);
    }

    [Fact]
    public void LogOptions_RoundTripsValues()
    {
        var options = new LogOptions
        {
            Container = "app",
            Follow = true,
            TailLines = 100,
            Previous = true,
            SinceSeconds = 60,
        };

        Assert.Equal("app", options.Container);
        Assert.True(options.Follow);
        Assert.Equal(100, options.TailLines);
        Assert.True(options.Previous);
        Assert.Equal(60, options.SinceSeconds);
    }

    [Fact]
    public void ExecRequest_CarriesCommandAndContainer()
    {
        var request = new ExecRequest
        {
            Command = ["sh", "-c", "echo hi"],
            Container = "main",
        };

        Assert.Equal(["sh", "-c", "echo hi"], request.Command);
        Assert.Equal("main", request.Container);
        Assert.Null(request.Stdin);
    }

    [Fact]
    public void ExecResult_ExposesStreamsAndExitCode()
    {
        var result = new ExecResult { StdOut = "out", StdErr = "err", ExitCode = 2 };

        Assert.Equal("out", result.StdOut);
        Assert.Equal("err", result.StdErr);
        Assert.Equal(2, result.ExitCode);
    }
}
