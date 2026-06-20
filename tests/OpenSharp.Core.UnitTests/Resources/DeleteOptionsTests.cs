using System.Runtime.CompilerServices;
using System.Text.Json;
using k8s;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Operations;
using OpenSharp.Core.Resources;

namespace OpenSharp.Core.UnitTests.Resources;

/// <summary>
/// Tests for <see cref="DeleteOptions"/> defaults and the grace-period resolution in
/// <see cref="WriteOperationsBase{T}"/> (`Force ⇒ 0`, otherwise the explicit grace period).
/// </summary>
public sealed class DeleteOptionsTests
{
    private sealed record Rec(string Name);

    /// <summary>Minimal write operations subclass that exposes the protected grace-period helper.</summary>
    private sealed class Exposer : WriteOperationsBase<Rec>
    {
        public Exposer() : base(Mock.Of<IKubernetes>(), new OpenShiftClientOptions(), NullLogger.Instance) { }

        public int? Grace(DeleteOptions options) => EffectiveGracePeriod(options);

        public override Task<Rec> GetAsync(string name, string? @namespace = null, CancellationToken ct = default) =>
            Task.FromResult(new Rec(name));

        public override Task<PagedList<Rec>> ListAsync(
            string? @namespace = null, int? limit = null, string? continueToken = null,
            string? labelSelector = null, CancellationToken ct = default) =>
            Task.FromResult(new PagedList<Rec> { Items = [] });

        public override Task<Rec> CreateAsync(Rec resource, CancellationToken ct = default) => Task.FromResult(resource);
        public override Task<Rec> ReplaceAsync(Rec resource, CancellationToken ct = default) => Task.FromResult(resource);
        public override Task<Rec> PatchAsync(string name, string? @namespace, JsonDocument patch, CancellationToken ct = default) =>
            Task.FromResult(new Rec(name));
        public override Task DeleteAsync(string name, string? @namespace, DeleteOptions options, CancellationToken ct = default) =>
            Task.CompletedTask;

        protected override async IAsyncEnumerable<WatchEvent<Rec>> WatchCoreAsync(
            string? @namespace, string? labelSelector, string? resourceVersion,
            [EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    [Fact]
    public void Defaults_AreBackgroundNoGraceNoForce()
    {
        var options = new DeleteOptions();
        Assert.Equal(DeletePropagationPolicy.Background, options.Propagation);
        Assert.Null(options.GracePeriodSeconds);
        Assert.False(options.Force);
    }

    [Fact]
    public void EffectiveGracePeriod_Force_IsZero()
    {
        var ops = new Exposer();
        Assert.Equal(0, ops.Grace(new DeleteOptions { Force = true }));
        // Force overrides an explicit grace period.
        Assert.Equal(0, ops.Grace(new DeleteOptions { Force = true, GracePeriodSeconds = 30 }));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(30)]
    public void EffectiveGracePeriod_NoForce_PassesThroughGracePeriod(int? grace)
    {
        var ops = new Exposer();
        Assert.Equal(grace, ops.Grace(new DeleteOptions { GracePeriodSeconds = grace }));
    }
}
