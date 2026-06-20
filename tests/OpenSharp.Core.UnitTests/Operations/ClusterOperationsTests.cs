using k8s.Models;
using OpenSharp.Core.Operations;
using OpenSharp.Core.Resources;

namespace OpenSharp.Core.UnitTests.Operations;

/// <summary>Tests for cluster capability-discovery matching and the <see cref="ClusterInfo"/> model.</summary>
public sealed class ClusterOperationsTests
{
    private static V1APIResourceList ListWith(params string[] names) => new()
    {
        Resources = names.Select(n => new V1APIResource
        {
            Name = n, SingularName = "", Namespaced = true, Kind = "X", Verbs = ["get", "list"],
        }).ToList(),
    };

    [Fact]
    public void Serves_True_WhenResourcePresent()
    {
        Assert.True(ClusterOperations.Serves(ListWith("routes", "builds"), "routes"));
    }

    [Fact]
    public void Serves_False_WhenResourceAbsent()
    {
        Assert.False(ClusterOperations.Serves(ListWith("builds"), "routes"));
    }

    [Fact]
    public void Serves_False_WhenNoResources()
    {
        Assert.False(ClusterOperations.Serves(new V1APIResourceList(), "routes"));
    }

    [Fact]
    public void ClusterInfo_ExposesEndpointVersionReachable()
    {
        var info = new ClusterInfo { ApiServerEndpoint = "https://api.example.com:6443", ServerVersion = "v1.28.3", Reachable = true };
        Assert.Equal("https://api.example.com:6443", info.ApiServerEndpoint);
        Assert.Equal("v1.28.3", info.ServerVersion);
        Assert.True(info.Reachable);
    }
}
