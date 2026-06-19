using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Operations;
using k8s;

namespace OpenSharp.Core.UnitTests.Operations;

/// <summary>
/// Tests for <see cref="ReadOperationsBase{T}"/>, specifically the
/// <see cref="ReadOperationsBase{T}.EnumerateAsync"/> auto-paging behaviour.
/// </summary>
public sealed class ReadOperationsTests
{
    private sealed class StubRecord
    {
        public required string Name { get; init; }
        public string? ResourceVersion { get; init; }
    }

    /// <summary>
    /// Minimal concrete subclass used solely to exercise the base-class
    /// auto-paging and watch logic without requiring a live Kubernetes client.
    /// </summary>
    private sealed class StubOperations : ReadOperationsBase<StubRecord>
    {
        private readonly Queue<PagedList<StubRecord>> _pages;

        public StubOperations(Queue<PagedList<StubRecord>> pages)
            : base(Mock.Of<IKubernetes>(), new OpenShiftClientOptions(), NullLogger.Instance)
        {
            _pages = pages;
        }

        public override Task<StubRecord> GetAsync(string name, string? @namespace = null, CancellationToken ct = default) =>
            Task.FromResult(new StubRecord { Name = name });

        public override Task<PagedList<StubRecord>> ListAsync(
            string? @namespace = null, int? limit = null, string? continueToken = null,
            string? labelSelector = null, CancellationToken ct = default) =>
            Task.FromResult(_pages.Count > 0 ? _pages.Dequeue() : new PagedList<StubRecord> { Items = [] });

        protected override async IAsyncEnumerable<WatchEvent<StubRecord>> WatchCoreAsync(
            string? @namespace, string? labelSelector, string? resourceVersion,
            [EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
            list.Add(item);
        return list;
    }

    [Fact]
    public async Task EnumerateAsync_SinglePage_ReturnsAllItems()
    {
        var pages = new Queue<PagedList<StubRecord>>();
        pages.Enqueue(new PagedList<StubRecord>
        {
            Items = [new StubRecord { Name = "a" }, new StubRecord { Name = "b" }],
            ContinueToken = null,
        });

        var ops = new StubOperations(pages);
        var results = await CollectAsync(ops.EnumerateAsync());

        Assert.Equal(2, results.Count);
        Assert.Equal("a", results[0].Name);
    }

    [Fact]
    public async Task EnumerateAsync_MultiplePages_FollowsContinuationTokens()
    {
        var pages = new Queue<PagedList<StubRecord>>();
        pages.Enqueue(new PagedList<StubRecord>
        {
            Items = [new StubRecord { Name = "p1-a" }],
            ContinueToken = "tok1",
        });
        pages.Enqueue(new PagedList<StubRecord>
        {
            Items = [new StubRecord { Name = "p2-a" }, new StubRecord { Name = "p2-b" }],
            ContinueToken = null,
        });

        var ops = new StubOperations(pages);
        var results = await CollectAsync(ops.EnumerateAsync());

        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.Name == "p1-a");
        Assert.Contains(results, r => r.Name == "p2-b");
    }

    [Fact]
    public async Task EnumerateAsync_EmptyFirstPage_ReturnsEmpty()
    {
        var pages = new Queue<PagedList<StubRecord>>();
        pages.Enqueue(new PagedList<StubRecord> { Items = [], ContinueToken = null });

        var ops = new StubOperations(pages);
        var results = await CollectAsync(ops.EnumerateAsync());

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAsync_ReturnsNamedResource()
    {
        var ops = new StubOperations(new Queue<PagedList<StubRecord>>());
        var result = await ops.GetAsync("my-resource");
        Assert.Equal("my-resource", result.Name);
    }
}
