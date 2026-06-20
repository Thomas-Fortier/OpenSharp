using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace OpenSharp.SystemTests.Support;

/// <summary>
/// Wraps a WireMock server that simulates the OpenShift/Kubernetes REST API.
/// Each test or scenario creates an instance via <see cref="Start"/>, stubs
/// the responses it needs, and disposes when done.
/// </summary>
public sealed class OpenShiftApiSimulator : IDisposable
{
    private readonly WireMockServer _server;

    private OpenShiftApiSimulator(WireMockServer server) => _server = server;

    /// <summary>Starts the simulator on a random port.</summary>
    public static OpenShiftApiSimulator Start() =>
        new(WireMockServer.Start(new WireMockServerSettings { Port = 0, UseSSL = false }));

    /// <summary>Base URL the KubernetesClient should connect to.</summary>
    public string BaseUrl => _server.Url!;

    /// <summary>Removes all registered stubs so the simulator can be reused across scenarios.</summary>
    public void Reset() => _server.Reset();

    // ─── Projects (cluster-scoped) ──────────────────────────────────────────

    /// <summary>
    /// Stubs GET /apis/project.openshift.io/v1/projects to return
    /// a ProjectList containing <paramref name="projects"/>.
    /// </summary>
    public void StubProjectList(IEnumerable<object> projects, string? continueToken = null)
    {
        object meta = continueToken is not null
            ? new { resourceVersion = "100", @continue = continueToken }
            : new { resourceVersion = "100" };

        _server
            .Given(Request.Create()
                .WithPath("/apis/project.openshift.io/v1/projects")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    apiVersion = "project.openshift.io/v1",
                    kind = "ProjectList",
                    metadata = meta,
                    items = projects,
                })));
    }

    /// <summary>Stubs GET /apis/project.openshift.io/v1/projects/{name}.</summary>
    public void StubGetProject(string name, object project)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/apis/project.openshift.io/v1/projects/{name}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(project)));
    }

    // ─── Pods (namespace-scoped) ─────────────────────────────────────────────

    /// <summary>
    /// Stubs GET /api/v1/namespaces/{namespace}/pods to return a PodList.
    /// Supply <paramref name="continueToken"/> to simulate a paged first page.
    /// </summary>
    public void StubPodList(string @namespace, IEnumerable<object> pods, string? continueToken = null)
    {
        object meta = continueToken is not null
            ? new { resourceVersion = "100", @continue = continueToken }
            : new { resourceVersion = "100" };

        _server
            .Given(Request.Create()
                .WithPath($"/api/v1/namespaces/{@namespace}/pods")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    apiVersion = "v1",
                    kind = "PodList",
                    metadata = meta,
                    items = pods,
                })));
    }

    /// <summary>Stubs a second page for pod listing (matched by the continue query param).</summary>
    public void StubPodListPage2(string @namespace, IEnumerable<object> pods, string continueToken)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/api/v1/namespaces/{@namespace}/pods")
                .WithParam("continue", continueToken)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    apiVersion = "v1",
                    kind = "PodList",
                    metadata = new { resourceVersion = "101" },
                    items = pods,
                })));
    }

    /// <summary>
    /// Stubs a large, fully-paged pod listing of <paramref name="total"/> pods served in
    /// pages of <paramref name="pageSize"/>, each linking to the next via a continue token.
    /// Used to verify bounded-memory enumeration over very large result sets (SC-006).
    /// </summary>
    public void StubLargePodList(string @namespace, int total, int pageSize)
    {
        var pageCount = (int)Math.Ceiling(total / (double)pageSize);
        for (var page = 0; page < pageCount; page++)
        {
            var start = page * pageSize;
            var count = Math.Min(pageSize, total - start);
            var pods = Enumerable.Range(start, count)
                .Select(i => MakePod($"pod-{i}", @namespace))
                .ToArray();

            var isLast = page == pageCount - 1;
            object meta = isLast
                ? new { resourceVersion = "100" }
                : new { resourceVersion = "100", @continue = $"tok-{page + 1}" };
            var body = JsonSerializer.Serialize(new
            {
                apiVersion = "v1",
                kind = "PodList",
                metadata = meta,
                items = pods,
            });

            // Page 0 matches the first request (no continue token); later pages match their
            // specific continue value and take precedence as more-specific, later mappings.
            var request = Request.Create().WithPath($"/api/v1/namespaces/{@namespace}/pods").UsingGet();
            if (page > 0)
                request = request.WithParam("continue", $"tok-{page}");

            _server.Given(request).RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
        }
    }

    // ─── Routes (namespace-scoped) ───────────────────────────────────────────

    /// <summary>Stubs GET .../routes/{name}.</summary>
    public void StubGetRoute(string @namespace, string name, object route)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/apis/route.openshift.io/v1/namespaces/{@namespace}/routes/{name}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(route)));
    }

    /// <summary>Stubs POST .../routes to create a route and echo it back.</summary>
    public void StubCreateRoute(string @namespace, object createdRoute)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/apis/route.openshift.io/v1/namespaces/{@namespace}/routes")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(createdRoute)));
    }

    /// <summary>Stubs PUT .../routes/{name}.</summary>
    public void StubReplaceRoute(string @namespace, string name, object updatedRoute)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/apis/route.openshift.io/v1/namespaces/{@namespace}/routes/{name}")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(updatedRoute)));
    }

    /// <summary>Stubs DELETE .../routes/{name}.</summary>
    public void StubDeleteRoute(string @namespace, string name)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/apis/route.openshift.io/v1/namespaces/{@namespace}/routes/{name}")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { kind = "Status", status = "Success" })));
    }

    // ─── Error scenarios ─────────────────────────────────────────────────────

    /// <summary>Stubs the given path to return HTTP 401 Unauthorized.</summary>
    public void StubUnauthorized(string path)
    {
        _server
            .Given(Request.Create().WithPath(path).UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    kind = "Status",
                    apiVersion = "v1",
                    status = "Failure",
                    message = "Unauthorized",
                    reason = "Unauthorized",
                    code = 401,
                })));
    }

    /// <summary>Stubs the given path to return HTTP 409 Conflict.</summary>
    public void StubConflict(string path, string message = "AlreadyExists")
    {
        _server
            .Given(Request.Create().WithPath(path).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(409)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    kind = "Status",
                    apiVersion = "v1",
                    status = "Failure",
                    message,
                    reason = "AlreadyExists",
                    code = 409,
                })));
    }

    /// <summary>Stubs the given path to return HTTP 404 Not Found.</summary>
    public void StubNotFound(string path)
    {
        _server
            .Given(Request.Create().WithPath(path).UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    kind = "Status",
                    apiVersion = "v1",
                    status = "Failure",
                    message = "Not found",
                    reason = "NotFound",
                    code = 404,
                })));
    }

    /// <summary>
    /// Stubs the routes collection and item paths to return HTTP 404 with the standard
    /// Kubernetes "could not find the requested resource" body, simulating a plain
    /// (non-OpenShift) cluster where the Route API group is not served (FR-015).
    /// </summary>
    public void StubRouteApiUnavailable(string @namespace)
    {
        var body = JsonSerializer.Serialize(new
        {
            kind = "Status",
            apiVersion = "v1",
            status = "Failure",
            message = "the server could not find the requested resource",
            reason = "NotFound",
            code = 404,
        });

        _server
            .Given(Request.Create()
                .WithPath(new WireMock.Matchers.WildcardMatcher(
                    $"/apis/route.openshift.io/v1/namespaces/{@namespace}/routes*"))
                .UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    // ─── Pod logs / workloads (US3) ──────────────────────────────────────────

    /// <summary>Stubs GET .../pods/{name}/log to return the supplied log text.</summary>
    public void StubPodLog(string @namespace, string name, string logText)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/api/v1/namespaces/{@namespace}/pods/{name}/log")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/plain")
                .WithBody(logText));
    }

    /// <summary>Stubs PATCH .../deployments/{name} (used by scale and rollout restart).</summary>
    public void StubPatchDeployment(string @namespace, string name, int replicas = 1)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/apis/apps/v1/namespaces/{@namespace}/deployments/{name}")
                .UsingPatch())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(MakeDeployment(name, @namespace, replicas))));
    }

    /// <summary>Stubs GET .../deployments/{name}.</summary>
    public void StubGetDeployment(string @namespace, string name, int replicas = 1)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/apis/apps/v1/namespaces/{@namespace}/deployments/{name}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(MakeDeployment(name, @namespace, replicas))));
    }

    // ─── Watch (US4) ──────────────────────────────────────────────────────────

    /// <summary>
    /// Stubs the namespaced pod watch endpoint (GET .../pods?watch=true) to return a
    /// newline-delimited stream of watch events, then close. Each tuple is (type, podName).
    /// </summary>
    public void StubWatchPods(string @namespace, IEnumerable<(string Type, string Name)> events)
    {
        var body = string.Join("\n", events.Select(e =>
            JsonSerializer.Serialize(new
            {
                type = e.Type,
                @object = MakePod(e.Name, @namespace),
            }))) + "\n";

        _server
            .Given(Request.Create()
                .WithPath($"/api/v1/namespaces/{@namespace}/pods")
                .WithParam("watch", "true")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    // ─── Generic escape hatch (US4) ──────────────────────────────────────────

    /// <summary>Stubs GET for a namespaced custom resource collection.</summary>
    public void StubGenericList(string group, string version, string @namespace, string plural, IEnumerable<object> items)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/apis/{group}/{version}/namespaces/{@namespace}/{plural}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    apiVersion = $"{group}/{version}",
                    kind = "List",
                    metadata = new { resourceVersion = "100" },
                    items,
                })));
    }

    /// <summary>Stubs GET for a single namespaced custom resource.</summary>
    public void StubGenericGet(string group, string version, string @namespace, string plural, string name, object body)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/apis/{group}/{version}/namespaces/{@namespace}/{plural}/{name}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(body)));
    }

    /// <summary>Stubs POST to create a namespaced custom resource, echoing the body back.</summary>
    public void StubGenericCreate(string group, string version, string @namespace, string plural, object created)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/apis/{group}/{version}/namespaces/{@namespace}/{plural}")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(created)));
    }

    /// <summary>Stubs DELETE for a single namespaced custom resource.</summary>
    public void StubGenericDelete(string group, string version, string @namespace, string plural, string name)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/apis/{group}/{version}/namespaces/{@namespace}/{plural}/{name}")
                .UsingDelete())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new { kind = "Status", status = "Success" })));
    }

    // ─── Generic full-CRUD coverage helpers ─────────────────────────────────

    private void Json(WireMock.RequestBuilders.IRequestBuilder request, int status, string body) =>
        _server.Given(request).RespondWith(Response.Create()
            .WithStatusCode(status)
            .WithHeader("Content-Type", "application/json")
            .WithBody(body));

    /// <summary>
    /// Stubs list/get/create/replace/patch/delete for a resource collection rooted at
    /// <paramref name="collectionPath"/>, returning <paramref name="item"/> for single-item
    /// operations and a one-item list for the collection.
    /// </summary>
    public void StubFullCrud(string collectionPath, string name, object item)
    {
        var itemPath = $"{collectionPath}/{name}";
        var itemBody = JsonSerializer.Serialize(item);
        var listBody = JsonSerializer.Serialize(new
        {
            apiVersion = "v1",
            kind = "List",
            metadata = new { resourceVersion = "100" },
            items = new[] { item },
        });
        // A DELETE body with no "status" field deserializes cleanly whether the typed client
        // expects a V1Status or the resource type itself (whose "status" is an object).
        var deleteBody = JsonSerializer.Serialize(new { apiVersion = "v1", kind = "Status", metadata = new { }, code = 200 });

        Json(Request.Create().WithPath(collectionPath).UsingGet(), 200, listBody);
        Json(Request.Create().WithPath(collectionPath).UsingPost(), 201, itemBody);
        Json(Request.Create().WithPath(itemPath).UsingGet(), 200, itemBody);
        Json(Request.Create().WithPath(itemPath).UsingPut(), 200, itemBody);
        Json(Request.Create().WithPath(itemPath).UsingPatch(), 200, itemBody);
        Json(Request.Create().WithPath(itemPath).UsingDelete(), 200, deleteBody);
    }

    /// <summary>
    /// Stubs a watch on a namespaced custom-object collection (route, deploymentconfig, …) to
    /// return a newline-delimited stream of events then close.
    /// </summary>
    public void StubWatchCustom(string group, string version, string @namespace, string plural,
        IEnumerable<(string Type, object Object)> events)
    {
        var body = string.Join("\n", events.Select(e =>
            JsonSerializer.Serialize(new { type = e.Type, @object = e.Object }))) + "\n";

        Json(Request.Create()
            .WithPath($"/apis/{group}/{version}/namespaces/{@namespace}/{plural}")
            .WithParam("watch", "true")
            .UsingGet(), 200, body);
    }

    /// <summary>
    /// Stubs a watch on a core (typed) namespaced collection — e.g. configmaps, secrets,
    /// services, deployments — returning a newline-delimited event stream then closing.
    /// </summary>
    public void StubWatchCore(string collectionPath, IEnumerable<(string Type, object Object)> events)
    {
        var body = string.Join("\n", events.Select(e =>
            JsonSerializer.Serialize(new { type = e.Type, @object = e.Object }))) + "\n";

        Json(Request.Create()
            .WithPath(collectionPath)
            .WithParam("watch", "true")
            .UsingGet(), 200, body);
    }

    /// <summary>Stubs a watch on a cluster-scoped custom-object collection (projects, …).</summary>
    public void StubWatchClusterCustom(string group, string version, string plural,
        IEnumerable<(string Type, object Object)> events)
    {
        var body = string.Join("\n", events.Select(e =>
            JsonSerializer.Serialize(new { type = e.Type, @object = e.Object }))) + "\n";

        Json(Request.Create()
            .WithPath($"/apis/{group}/{version}/{plural}")
            .WithParam("watch", "true")
            .UsingGet(), 200, body);
    }

    /// <summary>Stubs GET for a single cluster-scoped custom resource.</summary>
    public void StubGenericClusterGet(string group, string version, string plural, string name, object body)
    {
        Json(Request.Create()
            .WithPath($"/apis/{group}/{version}/{plural}/{name}")
            .UsingGet(), 200, JsonSerializer.Serialize(body));
    }

    /// <summary>Stubs GET for a cluster-scoped custom resource collection (e.g. cluster-scoped widgets).</summary>
    public void StubGenericClusterList(string group, string version, string plural, IEnumerable<object> items)
    {
        Json(Request.Create()
            .WithPath($"/apis/{group}/{version}/{plural}")
            .UsingGet(), 200, JsonSerializer.Serialize(new
            {
                apiVersion = $"{group}/{version}",
                kind = "List",
                metadata = new { resourceVersion = "100" },
                items,
            }));
    }

    /// <summary>Builds a minimal core ConfigMap JSON object.</summary>
    public static object MakeConfigMap(string name, string @namespace) => new
    {
        apiVersion = "v1",
        kind = "ConfigMap",
        metadata = new { name, @namespace, resourceVersion = "1", uid = Guid.NewGuid().ToString() },
        data = new Dictionary<string, string> { ["key"] = "value" },
    };

    /// <summary>Builds a minimal core Secret JSON object.</summary>
    public static object MakeSecret(string name, string @namespace) => new
    {
        apiVersion = "v1",
        kind = "Secret",
        metadata = new { name, @namespace, resourceVersion = "1", uid = Guid.NewGuid().ToString() },
        type = "Opaque",
        data = new Dictionary<string, string> { ["token"] = Convert.ToBase64String("s3cr3t"u8.ToArray()) },
    };

    /// <summary>Builds a minimal core Service JSON object.</summary>
    public static object MakeService(string name, string @namespace) => new
    {
        apiVersion = "v1",
        kind = "Service",
        metadata = new { name, @namespace, resourceVersion = "1", uid = Guid.NewGuid().ToString() },
        spec = new
        {
            type = "ClusterIP",
            clusterIP = "10.0.0.1",
            ports = new[] { new { name = "http", port = 80, targetPort = "8080", protocol = "TCP" } },
        },
    };

    /// <summary>Builds a minimal apps.openshift.io/v1 DeploymentConfig JSON object.</summary>
    public static object MakeDeploymentConfig(string name, string @namespace, int replicas = 1) => new
    {
        apiVersion = "apps.openshift.io/v1",
        kind = "DeploymentConfig",
        metadata = new { name, @namespace, resourceVersion = "1", uid = Guid.NewGuid().ToString() },
        spec = new { replicas },
        status = new { replicas, availableReplicas = replicas, readyReplicas = replicas },
    };

    /// <summary>Builds a minimal custom-resource JSON object for generic-resource coverage.</summary>
    public static object MakeCustom(string group, string version, string kind, string name, string? @namespace = null) => new
    {
        apiVersion = $"{group}/{version}",
        kind,
        metadata = new { name, @namespace, resourceVersion = "1" },
    };

    // ─── Feature 002 shared helpers (selectors, patch, delete capture) ───────

    /// <summary>
    /// Stubs a namespaced custom-object list that returns <paramref name="matching"/> when the
    /// request carries <c>labelSelector=<paramref name="labelSelector"/></c>, and
    /// <paramref name="all"/> otherwise. Pass <paramref name="namespace"/> = <see langword="null"/>
    /// for the all-namespaces (cluster) collection path.
    /// </summary>
    public void StubGenericListFiltered(string group, string version, string? @namespace, string plural,
        string labelSelector, IEnumerable<object> matching, IEnumerable<object> all)
    {
        var path = @namespace is null
            ? $"/apis/{group}/{version}/{plural}"
            : $"/apis/{group}/{version}/namespaces/{@namespace}/{plural}";

        string Body(IEnumerable<object> items) => JsonSerializer.Serialize(new
        {
            apiVersion = $"{group}/{version}",
            kind = "List",
            metadata = new { resourceVersion = "100" },
            items,
        });

        // Filtered match registered after the base so it takes precedence for the selector query.
        Json(Request.Create().WithPath(path).UsingGet(), 200, Body(all));
        Json(Request.Create().WithPath(path).WithParam("labelSelector", labelSelector).UsingGet(), 200, Body(matching));
    }

    /// <summary>Stubs PATCH on a namespaced custom-object item, echoing <paramref name="patched"/>.</summary>
    public void StubGenericPatch(string group, string version, string @namespace, string plural, string name, object patched)
    {
        Json(Request.Create()
            .WithPath($"/apis/{group}/{version}/namespaces/{@namespace}/{plural}/{name}")
            .UsingPatch(), 200, JsonSerializer.Serialize(patched));
    }

    /// <summary>Stubs PATCH on a namespaced custom-object item to return HTTP 422 (validation failure).</summary>
    public void StubGenericPatchInvalid(string group, string version, string @namespace, string plural, string name)
    {
        Json(Request.Create()
            .WithPath($"/apis/{group}/{version}/namespaces/{@namespace}/{plural}/{name}")
            .UsingPatch(), 422, JsonSerializer.Serialize(new
            {
                kind = "Status", apiVersion = "v1", status = "Failure",
                message = "invalid patch", reason = "Invalid", code = 422,
            }));
    }

    /// <summary>Number of received requests whose method and path match (for asserting a call occurred).</summary>
    public int CountRequests(string method, string pathContains) =>
        _server.LogEntries.Count(e =>
            e.RequestMessage is { } rm &&
            string.Equals(rm.Method, method, StringComparison.OrdinalIgnoreCase) &&
            (rm.Path?.Contains(pathContains, StringComparison.OrdinalIgnoreCase) ?? false));

    // ─── Nodes & core-group (US2) ────────────────────────────────────────────

    /// <summary>Stubs GET /api/v1/nodes returning a NodeList of <paramref name="nodes"/>.</summary>
    public void StubNodeList(IEnumerable<object> nodes)
    {
        Json(Request.Create().WithPath("/api/v1/nodes").UsingGet(), 200, JsonSerializer.Serialize(new
        {
            apiVersion = "v1",
            kind = "NodeList",
            metadata = new { resourceVersion = "100" },
            items = nodes,
        }));
    }

    /// <summary>Stubs the node watch endpoint (GET /api/v1/nodes?watch=true) with a stream of events.</summary>
    public void StubWatchNodes(IEnumerable<(string Type, string Name)> events)
    {
        var body = string.Join("\n", events.Select(e =>
            JsonSerializer.Serialize(new { type = e.Type, @object = MakeNode(e.Name) }))) + "\n";
        Json(Request.Create().WithPath("/api/v1/nodes").WithParam("watch", "true").UsingGet(), 200, body);
    }

    /// <summary>Stubs GET /api/v1/nodes/{name}.</summary>
    public void StubGetNode(string name, object node) =>
        Json(Request.Create().WithPath($"/api/v1/nodes/{name}").UsingGet(), 200, JsonSerializer.Serialize(node));

    /// <summary>Stubs PATCH /api/v1/nodes/{name}, echoing <paramref name="node"/>.</summary>
    public void StubPatchNode(string name, object node) =>
        Json(Request.Create().WithPath($"/api/v1/nodes/{name}").UsingPatch(), 200, JsonSerializer.Serialize(node));

    /// <summary>Stubs GET for a namespaced core (legacy group) resource at /api/v1/namespaces/{ns}/{plural}/{name}.</summary>
    public void StubCoreNamespacedGet(string plural, string @namespace, string name, object body) =>
        Json(Request.Create().WithPath($"/api/v1/namespaces/{@namespace}/{plural}/{name}").UsingGet(),
            200, JsonSerializer.Serialize(body));

    /// <summary>Builds a minimal core Node JSON object.</summary>
    public static object MakeNode(string name, bool unschedulable = false) => new
    {
        apiVersion = "v1",
        kind = "Node",
        metadata = new { name, resourceVersion = "1", uid = Guid.NewGuid().ToString() },
        spec = new { unschedulable },
        status = new
        {
            conditions = new[] { new { type = "Ready", status = "True", reason = "KubeletReady" } },
            nodeInfo = new { kubeletVersion = "v1.28.3" },
        },
    };

    /// <summary>Builds a minimal core-group object for generic-reach stubs.</summary>
    public static object MakeCoreObject(string kind, string name, string? @namespace = null) => new
    {
        apiVersion = "v1",
        kind,
        metadata = new { name, @namespace, resourceVersion = "1" },
    };

    // ─── Cluster info & discovery (US3) ──────────────────────────────────────

    /// <summary>Stubs GET /version (with or without a trailing slash) returning the cluster's reported version.</summary>
    public void StubVersion(string gitVersion) =>
        Json(Request.Create().WithPath(new WireMock.Matchers.WildcardMatcher("/version*")).UsingGet(), 200,
            JsonSerializer.Serialize(new
            {
                major = "1", minor = "28", gitVersion, gitCommit = "deadbeef", platform = "linux/amd64",
            }));

    /// <summary>Stubs GET /apis/{group}/{version} discovery to advertise the given resource plurals.</summary>
    public void StubApiResources(string group, string version, IEnumerable<string> plurals) =>
        Json(Request.Create().WithPath($"/apis/{group}/{version}").UsingGet(), 200, JsonSerializer.Serialize(new
        {
            kind = "APIResourceList",
            apiVersion = "v1",
            groupVersion = $"{group}/{version}",
            resources = plurals.Select(p => new { name = p, singularName = "", namespaced = true, kind = "X", verbs = new[] { "get", "list" } }),
        }));

    /// <summary>Stubs GET /apis/{group}/{version} discovery to return HTTP 404 (group not served).</summary>
    public void StubApiGroupNotFound(string group, string version) =>
        Json(Request.Create().WithPath($"/apis/{group}/{version}").UsingGet(), 404, JsonSerializer.Serialize(new
        {
            kind = "Status", apiVersion = "v1", status = "Failure", message = "not found", reason = "NotFound", code = 404,
        }));

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Builds a minimal project JSON object for use in stubs.</summary>
    public static object MakeProject(string name, string phase = "Active") => new
    {
        apiVersion = "project.openshift.io/v1",
        kind = "Project",
        metadata = new { name, resourceVersion = "1", uid = Guid.NewGuid().ToString() },
        status = new { phase },
    };

    /// <summary>Builds a minimal pod JSON object for use in stubs.</summary>
    public static object MakePod(string name, string @namespace, string phase = "Running") => new
    {
        apiVersion = "v1",
        kind = "Pod",
        metadata = new { name, @namespace, resourceVersion = "1", uid = Guid.NewGuid().ToString() },
        status = new { phase },
        spec = new { containers = Array.Empty<object>() },
    };

    /// <summary>Builds a minimal route JSON object for use in stubs.</summary>
    public static object MakeRoute(string name, string @namespace, string host) => new
    {
        apiVersion = "route.openshift.io/v1",
        kind = "Route",
        metadata = new { name, @namespace, resourceVersion = "1", uid = Guid.NewGuid().ToString() },
        spec = new
        {
            host,
            to = new { kind = "Service", name = "my-svc", weight = 100 },
        },
    };

    /// <summary>Builds a minimal apps/v1 Deployment JSON object for use in stubs.</summary>
    public static object MakeDeployment(string name, string @namespace, int replicas = 1) => new
    {
        apiVersion = "apps/v1",
        kind = "Deployment",
        metadata = new { name, @namespace, resourceVersion = "1", uid = Guid.NewGuid().ToString() },
        spec = new { replicas, selector = new { matchLabels = new { app = name } } },
        status = new { replicas, availableReplicas = replicas, readyReplicas = replicas },
    };

    /// <inheritdoc/>
    public void Dispose() => _server.Dispose();
}
