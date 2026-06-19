using System.Runtime.CompilerServices;
using k8s;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Generic;
using OpenSharp.Core.Operations;
using WatchEventType = OpenSharp.Core.Abstractions.WatchEventType;

namespace OpenSharp.Core.UnitTests.Operations;

/// <summary>
/// Tests for US4 base behaviour: watch event delivery, auto-resume resourceVersion
/// tracking in <see cref="ReadOperationsBase{T}"/>, and <see cref="GenericResourceRef"/>
/// construction.
/// </summary>
public sealed class WatchAndGenericTests
{
    private sealed record Record(string Name, string? ResourceVersion);

    /// <summary>
    /// Test double that serves successive watch "connections" from a queue of event batches
    /// and records the resourceVersion it was asked to resume from on each call.
    /// </summary>
    private sealed class StubWatchOps : ReadOperationsBase<Record>
    {
        private readonly Queue<List<WatchEvent<Record>>> _batches;

        public StubWatchOps(Queue<List<WatchEvent<Record>>> batches)
            : base(Mock.Of<IKubernetes>(), new OpenShiftClientOptions(), NullLogger.Instance) =>
            _batches = batches;

        public List<string?> ResumeTokens { get; } = [];

        public override Task<Record> GetAsync(string name, string? @namespace = null, CancellationToken ct = default) =>
            Task.FromResult(new Record(name, null));

        public override Task<PagedList<Record>> ListAsync(
            string? @namespace = null, int? limit = null, string? continueToken = null,
            string? labelSelector = null, CancellationToken ct = default) =>
            Task.FromResult(new PagedList<Record> { Items = [] });

        protected override string? GetResourceVersion(Record resource) => resource.ResourceVersion;

        protected override async IAsyncEnumerable<WatchEvent<Record>> WatchCoreAsync(
            string? @namespace, string? labelSelector, string? resourceVersion,
            [EnumeratorCancellation] CancellationToken ct)
        {
            ResumeTokens.Add(resourceVersion);
            await Task.CompletedTask;
            if (_batches.Count == 0)
                yield break;
            foreach (var evt in _batches.Dequeue())
                yield return evt;
        }
    }

    private static WatchEvent<Record> Event(WatchEventType type, string name, string? rv) =>
        new() { Type = type, Resource = new Record(name, rv) };

    [Fact]
    public async Task WatchAsync_AutoResumeDisabled_YieldsBatchThenStops()
    {
        var batches = new Queue<List<WatchEvent<Record>>>();
        batches.Enqueue(
        [
            Event(WatchEventType.Added, "p1", "1"),
            Event(WatchEventType.Modified, "p1", "2"),
            Event(WatchEventType.Deleted, "p1", "3"),
        ]);

        var ops = new StubWatchOps(batches);
        var received = new List<WatchEvent<Record>>();
        await foreach (var evt in ops.WatchAsync(options: new WatchOptions { AutoResume = false }))
            received.Add(evt);

        Assert.Equal(3, received.Count);
        Assert.Equal(WatchEventType.Added, received[0].Type);
        Assert.Equal(WatchEventType.Deleted, received[2].Type);
    }

    [Fact]
    public async Task WatchAsync_AutoResume_ResumesFromLastResourceVersion()
    {
        var batches = new Queue<List<WatchEvent<Record>>>();
        batches.Enqueue([Event(WatchEventType.Added, "p1", "7")]);
        batches.Enqueue([Event(WatchEventType.Modified, "p1", "9")]);

        var ops = new StubWatchOps(batches);
        using var cts = new CancellationTokenSource();
        var received = new List<WatchEvent<Record>>();

        await foreach (var evt in ops.WatchAsync(options: new WatchOptions { AutoResume = true }, ct: cts.Token))
        {
            received.Add(evt);
            if (received.Count == 2)
                cts.Cancel();
        }

        Assert.Equal(2, received.Count);
        // First connection starts with no resume token; the second resumes from rv "7".
        Assert.Null(ops.ResumeTokens[0]);
        Assert.Equal("7", ops.ResumeTokens[1]);
    }

    [Fact]
    public async Task WatchAsync_BookmarkUpdatesResumeToken()
    {
        var batches = new Queue<List<WatchEvent<Record>>>();
        batches.Enqueue([Event(WatchEventType.Bookmark, string.Empty, "42")]);
        batches.Enqueue([Event(WatchEventType.Added, "p2", "43")]);

        var ops = new StubWatchOps(batches);
        using var cts = new CancellationTokenSource();
        var received = new List<WatchEvent<Record>>();

        await foreach (var evt in ops.WatchAsync(options: new WatchOptions { AutoResume = true }, ct: cts.Token))
        {
            received.Add(evt);
            if (received.Count == 2)
                cts.Cancel();
        }

        Assert.Equal("42", ops.ResumeTokens[1]);
    }

    [Fact]
    public void GenericResourceRef_ConstructsAllFields()
    {
        var reference = new GenericResourceRef
        {
            Group = "example.com",
            Version = "v1",
            Plural = "widgets",
            Namespace = "ns",
            Name = "w1",
        };

        Assert.Equal("example.com", reference.Group);
        Assert.Equal("v1", reference.Version);
        Assert.Equal("widgets", reference.Plural);
        Assert.Equal("ns", reference.Namespace);
        Assert.Equal("w1", reference.Name);
    }

    [Fact]
    public void GenericResourceRef_ClusterScoped_HasNullNamespace()
    {
        var reference = new GenericResourceRef
        {
            Group = "config.openshift.io",
            Version = "v1",
            Plural = "clusteroperators",
        };

        Assert.Null(reference.Namespace);
        Assert.Null(reference.Name);
    }
}
