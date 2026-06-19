using System.Net;
using System.Net.Http;
using k8s.Autorest;
using OpenSharp.Core.Errors;

namespace OpenSharp.Core.UnitTests.Errors;

public sealed class ErrorMapperTests
{
    private static HttpOperationException MakeHttpEx(HttpStatusCode code, string message = "test") =>
        new(message) { Response = new HttpResponseMessageWrapper(new HttpResponseMessage(code), "") };

    // ─── Map(HttpOperationException) ────────────────────────────────────────

    [Fact]
    public void Map_401_ReturnsAuthenticationException()
    {
        var ex = MakeHttpEx(HttpStatusCode.Unauthorized);
        var result = ErrorMapper.Map(ex);
        Assert.IsType<OpenShiftAuthenticationException>(result);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public void Map_403_ReturnsAuthorizationException()
    {
        var ex = MakeHttpEx(HttpStatusCode.Forbidden);
        var result = ErrorMapper.Map(ex, resourceRef: "pods/my-pod");
        var typed = Assert.IsType<OpenShiftAuthorizationException>(result);
        Assert.Equal(403, typed.StatusCode);
        Assert.Equal("pods/my-pod", typed.ResourceRef);
    }

    [Fact]
    public void Map_404_ReturnsNotFoundException()
    {
        var ex = MakeHttpEx(HttpStatusCode.NotFound);
        var result = ErrorMapper.Map(ex, resourceRef: "my-resource");
        var typed = Assert.IsType<OpenShiftNotFoundException>(result);
        Assert.Equal(404, typed.StatusCode);
        Assert.Equal("my-resource", typed.ResourceRef);
    }

    [Fact]
    public void Map_409_ReturnsValidationException()
    {
        var ex = MakeHttpEx(HttpStatusCode.Conflict);
        var result = ErrorMapper.Map(ex);
        var typed = Assert.IsType<OpenShiftValidationException>(result);
        Assert.Equal(409, typed.StatusCode);
    }

    [Fact]
    public void Map_422_ReturnsValidationException()
    {
        var ex = MakeHttpEx(HttpStatusCode.UnprocessableEntity);
        var result = ErrorMapper.Map(ex);
        var typed = Assert.IsType<OpenShiftValidationException>(result);
        Assert.Equal(422, typed.StatusCode);
    }

    [Fact]
    public void Map_500_ReturnsServerException()
    {
        var ex = MakeHttpEx(HttpStatusCode.InternalServerError);
        var result = ErrorMapper.Map(ex);
        var typed = Assert.IsType<OpenShiftServerException>(result);
        Assert.Equal(500, typed.StatusCode);
    }

    [Fact]
    public void Map_503_ReturnsServerException()
    {
        var ex = MakeHttpEx(HttpStatusCode.ServiceUnavailable);
        var result = ErrorMapper.Map(ex);
        Assert.IsType<OpenShiftServerException>(result);
    }

    [Fact]
    public void Map_UnknownStatus_ReturnsServerException()
    {
        var ex = MakeHttpEx(HttpStatusCode.BadRequest);
        var result = ErrorMapper.Map(ex);
        Assert.IsType<OpenShiftServerException>(result);
    }

    [Fact]
    public void Map_PreservesInnerException()
    {
        var ex = MakeHttpEx(HttpStatusCode.Unauthorized);
        var result = ErrorMapper.Map(ex);
        Assert.Same(ex, result.InnerException);
    }

    // ─── MapConnectivity ─────────────────────────────────────────────────────

    [Fact]
    public void MapConnectivity_ReturnsConnectionException()
    {
        var inner = new Exception("network failure");
        var result = ErrorMapper.MapConnectivity(inner);
        Assert.IsType<OpenShiftConnectionException>(result);
        Assert.Same(inner, result.InnerException);
        Assert.Contains("network failure", result.Message);
    }

    // ─── UnsupportedResourceType ──────────────────────────────────────────────

    [Fact]
    public void UnsupportedResourceType_ReturnsValidationException()
    {
        var result = ErrorMapper.UnsupportedResourceType("Route");
        var typed = Assert.IsType<OpenShiftValidationException>(result);
        Assert.Contains("Route", typed.Message);
    }
}
