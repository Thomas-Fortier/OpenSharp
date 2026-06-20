using k8s.Models;
using OpenSharp.Core.Abstractions;
using OpenSharp.Core.Generic;

namespace OpenSharp.Core.UnitTests.Operations;

/// <summary>Tests for generic-operation request shaping — the <see cref="PatchType"/> mapping.</summary>
public sealed class GenericOperationsTests
{
    [Theory]
    [InlineData(PatchType.Merge, V1Patch.PatchType.MergePatch)]
    [InlineData(PatchType.JsonMerge, V1Patch.PatchType.MergePatch)]
    [InlineData(PatchType.StrategicMerge, V1Patch.PatchType.StrategicMergePatch)]
    [InlineData(PatchType.Json, V1Patch.PatchType.JsonPatch)]
    public void ToK8sPatchType_MapsEachStrategy(PatchType type, V1Patch.PatchType expected)
    {
        Assert.Equal(expected, GenericOperations.ToK8sPatchType(type));
    }
}
