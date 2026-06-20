using k8s.Models;
using OpenSharp.Core.Operations;

namespace OpenSharp.Core.UnitTests.Operations;

/// <summary>Tests for node cordon/uncordon patch shaping and <c>V1Node</c> mapping.</summary>
public sealed class NodeOperationsTests
{
    [Fact]
    public void UnschedulablePatch_Cordon_SetsTrue()
    {
        Assert.Equal("{\"spec\":{\"unschedulable\":true}}", NodeOperations.UnschedulablePatch(true));
    }

    [Fact]
    public void UnschedulablePatch_Uncordon_SetsFalse()
    {
        Assert.Equal("{\"spec\":{\"unschedulable\":false}}", NodeOperations.UnschedulablePatch(false));
    }

    [Fact]
    public void MapNode_MapsIdentityScheduleConditionsAndKubelet()
    {
        var v1 = new V1Node
        {
            Metadata = new V1ObjectMeta { Name = "node-a", Uid = "u1", ResourceVersion = "7" },
            Spec = new V1NodeSpec { Unschedulable = true },
            Status = new V1NodeStatus
            {
                Conditions = [new V1NodeCondition { Type = "Ready", Status = "True", Reason = "KubeletReady" }],
                NodeInfo = new V1NodeSystemInfo { KubeletVersion = "v1.28.3" },
            },
        };

        var node = NodeOperations.MapNode(v1);

        Assert.Equal("node-a", node.Metadata.Name);
        Assert.Null(node.Metadata.Namespace);
        Assert.True(node.Unschedulable);
        Assert.Equal("v1.28.3", node.KubeletVersion);
        Assert.Single(node.Conditions);
        Assert.Equal("Ready", node.Conditions[0].Type);
        Assert.Equal("True", node.Conditions[0].Status);
        Assert.Equal("KubeletReady", node.Conditions[0].Reason);
    }

    [Fact]
    public void MapNode_DefaultsUnschedulableFalse()
    {
        var v1 = new V1Node { Metadata = new V1ObjectMeta { Name = "n" } };
        var node = NodeOperations.MapNode(v1);
        Assert.False(node.Unschedulable);
        Assert.Empty(node.Conditions);
    }
}
