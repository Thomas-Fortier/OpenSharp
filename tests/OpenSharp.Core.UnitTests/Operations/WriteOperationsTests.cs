using System.Net;
using System.Runtime.CompilerServices;
using k8s;
using k8s.Autorest;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Errors;
using OpenSharp.Core.Operations;
using OpenSharp.Core.Resources;

namespace OpenSharp.Core.UnitTests.Operations;

/// <summary>
/// Tests for <see cref="WriteOperationsBase{T}"/>: delete propagation-policy mapping and
/// the shared error-translation path used by create/replace/patch/delete (incl. version
/// conflicts).
/// </summary>
public sealed class WriteOperationsTests
{
    private sealed record Record(string Name);

    /// <summary>Minimal concrete write operations used to exercise base-class behaviour.</summary>
    private sealed class StubWriteOps : WriteOperationsBase<Record>
    {
        public StubWriteOps()
            : base(Mock.Of<IKubernetes>(), new OpenShiftClientOptions(), NullLogger.Instance) { }

        public string MapPropagation(DeletePropagationPolicy p) => ToK8sPropagation(p);

        public Task<T> Run<T>(Func<Task<T>> call, string? resourceRef = null) => ExecuteAsync(call, resourceRef);

        public override Task<Record> GetAsync(string name, string? @namespace = null, CancellationToken ct = default) =>
            Task.FromResult(new Record(name));

        public override Task<PagedList<Record>> ListAsync(
            string? @namespace = null, int? limit = null, string? continueToken = null,
            string? labelSelector = null, CancellationToken ct = default) =>
            Task.FromResult(new PagedList<Record> { Items = [] });

        public override Task<Record> CreateAsync(Record resource, CancellationToken ct = default) =>
            Task.FromResult(resource);

        public override Task<Record> ReplaceAsync(Record resource, CancellationToken ct = default) =>
            Task.FromResult(resource);

        public override Task<Record> PatchAsync(string name, string? @namespace, System.Text.Json.JsonDocument patch, CancellationToken ct = default) =>
            Task.FromResult(new Record(name));

        public override Task DeleteAsync(string name, string? @namespace, DeleteOptions options, CancellationToken ct = default) =>
            Task.CompletedTask;

        protected override async IAsyncEnumerable<WatchEvent<Record>> WatchCoreAsync(
            string? @namespace, string? labelSelector, string? resourceVersion,
            [EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    [Theory]
    [InlineData(DeletePropagationPolicy.Background, "Background")]
    [InlineData(DeletePropagationPolicy.Foreground, "Foreground")]
    [InlineData(DeletePropagationPolicy.Orphan, "Orphan")]
    public void ToK8sPropagation_MapsEveryPolicy(DeletePropagationPolicy policy, string expected)
    {
        var ops = new StubWriteOps();
        Assert.Equal(expected, ops.MapPropagation(policy));
    }

    [Fact]
    public async Task Execute_Conflict_ThrowsValidationException()
    {
        var ops = new StubWriteOps();
        var http = MakeHttpException(HttpStatusCode.Conflict, "already exists");

        var ex = await Assert.ThrowsAsync<OpenShiftValidationException>(
            () => ops.Run<Record>(() => throw http, "my-resource"));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("my-resource", ex.ResourceRef);
    }

    [Fact]
    public async Task Execute_UnprocessableEntity_ThrowsValidationException()
    {
        var ops = new StubWriteOps();
        var http = MakeHttpException(HttpStatusCode.UnprocessableEntity, "invalid");

        await Assert.ThrowsAsync<OpenShiftValidationException>(
            () => ops.Run<Record>(() => throw http));
    }

    [Fact]
    public async Task Execute_Connectivity_ThrowsConnectionException()
    {
        var ops = new StubWriteOps();
        await Assert.ThrowsAsync<OpenShiftConnectionException>(
            () => ops.Run<Record>(() => throw new HttpRequestException("no route to host")));
    }

    private static HttpOperationException MakeHttpException(HttpStatusCode code, string message) =>
        new(message)
        {
            Response = new HttpResponseMessageWrapper(
                new HttpResponseMessage(code), message),
        };
}
