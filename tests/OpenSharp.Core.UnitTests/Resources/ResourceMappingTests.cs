using System.Text.Json;
using OpenSharp.Core.Operations;
using OpenSharp.Core.Resources;

namespace OpenSharp.Core.UnitTests.Resources;

/// <summary>
/// Tests that <see cref="MetadataMapper"/> and the JSON-to-model mapping helpers
/// extract all relevant fields from raw Kubernetes API objects.
/// </summary>
public sealed class ResourceMappingTests
{
    // ─── MetadataMapper ──────────────────────────────────────────────────────

    [Fact]
    public void MetadataMapper_ExtractsAllFields()
    {
        var json = """
            {
              "metadata": {
                "name": "my-resource",
                "namespace": "my-ns",
                "uid": "abc-123",
                "resourceVersion": "42",
                "creationTimestamp": "2024-01-15T10:00:00Z",
                "labels": { "app": "web" },
                "annotations": { "note": "value" }
              }
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var meta = MetadataMapper.Map(doc.RootElement);

        Assert.Equal("my-resource", meta.Name);
        Assert.Equal("my-ns", meta.Namespace);
        Assert.Equal("abc-123", meta.Uid);
        Assert.Equal("42", meta.ResourceVersion);
        Assert.Equal("web", meta.Labels["app"]);
        Assert.Equal("value", meta.Annotations["note"]);
    }

    [Fact]
    public void MetadataMapper_MissingOptionalFields_UsesDefaults()
    {
        var json = """{"metadata": {"name": "minimal"}}""";
        using var doc = JsonDocument.Parse(json);
        var meta = MetadataMapper.Map(doc.RootElement);

        Assert.Equal("minimal", meta.Name);
        Assert.Null(meta.Namespace);
        Assert.Null(meta.Uid);
        Assert.Null(meta.ResourceVersion);
        Assert.Empty(meta.Labels);
        Assert.Empty(meta.Annotations);
    }

    [Fact]
    public void MetadataMapper_EmptyMetadata_ReturnsEmptyName()
    {
        var json = """{"metadata": {}}""";
        using var doc = JsonDocument.Parse(json);
        var meta = MetadataMapper.Map(doc.RootElement);

        Assert.Equal(string.Empty, meta.Name);
    }

    // ─── Project resource model ───────────────────────────────────────────────

    [Fact]
    public void Project_HasCorrectProperties()
    {
        var p = new Project
        {
            Metadata = new ResourceMetadata { Name = "proj" },
            DisplayName = "My Project",
            Description = "A project",
            Status = "Active",
        };

        Assert.Equal("proj", p.Metadata.Name);
        Assert.Equal("My Project", p.DisplayName);
        Assert.Equal("Active", p.Status);
    }

    // ─── Pod resource model ───────────────────────────────────────────────────

    [Fact]
    public void Pod_HasCorrectProperties()
    {
        var pod = new Pod
        {
            Metadata = new ResourceMetadata { Name = "web-1", Namespace = "default" },
            Phase = "Running",
            Containers = [],
        };

        Assert.Equal("web-1", pod.Metadata.Name);
        Assert.Equal("Running", pod.Phase);
        Assert.Empty(pod.Containers);
    }
}
