using k8s;
using k8s.Autorest;
using Microsoft.Extensions.Logging;
using OpenSharp.Core.Authentication;
using OpenSharp.Core.Errors;

namespace OpenSharp.Core.Operations;

/// <summary>
/// Shared infrastructure for all resource operation classes: cluster client access,
/// default namespace resolution, and error mapping.
/// </summary>
internal abstract class OperationBase
{
    protected readonly IKubernetes K8s;
    protected readonly OpenShiftClientOptions Options;
    protected readonly ILogger Logger;

    protected OperationBase(IKubernetes k8s, OpenShiftClientOptions options, ILogger logger)
    {
        K8s = k8s;
        Options = options;
        Logger = logger;
    }

    protected string ResolveNamespace(string? @namespace) =>
        @namespace ?? Options.DefaultNamespace ?? "default";

    protected static async Task<T> ExecuteAsync<T>(Func<Task<T>> call, string? resourceRef = null)
    {
        try
        {
            return await call().ConfigureAwait(false);
        }
        catch (HttpOperationException ex)
        {
            throw ErrorMapper.Map(ex, resourceRef);
        }
        catch (HttpRequestException ex)
        {
            throw ErrorMapper.MapConnectivity(ex);
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            throw ErrorMapper.MapConnectivity(ex);
        }
    }

    protected static async Task ExecuteAsync(Func<Task> call, string? resourceRef = null)
    {
        try
        {
            await call().ConfigureAwait(false);
        }
        catch (HttpOperationException ex)
        {
            throw ErrorMapper.Map(ex, resourceRef);
        }
        catch (HttpRequestException ex)
        {
            throw ErrorMapper.MapConnectivity(ex);
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            throw ErrorMapper.MapConnectivity(ex);
        }
    }
}
