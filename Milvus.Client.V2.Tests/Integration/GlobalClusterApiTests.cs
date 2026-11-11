using System.Net.Http;
using System.Threading;
using Grpc.Core;
using Grpc.Net.Client;
using Xunit;

using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Tests.Integration;

[Trait("Category", "Integration")]
public class GlobalClusterApiTests
{
    private const int OldPrimaryPort = 29531;
    private const int NewPrimaryPort = 29532;

    private static string TopologyBody(string endpoint, int version)
        => "{\"code\":0,\"data\":{\"version\":" + version.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ",\"clusters\":[{\"clusterId\":\"primary\",\"endpoint\":\"" + endpoint + "\",\"capability\":3}]}}";

    // A channel handler that routes each request to the mock server bound to its URI port, so a rebuilt failover
    // channel (a different host:port) provably reaches the new primary's in-process TestServer.
    private static GrpcChannelOptions RoutingChannelOptions(
        (int Port, MockMilvusServer Server) oldPrimary, (int Port, MockMilvusServer Server) newPrimary)
        => new()
        {
            HttpHandler = new PortRoutingHandler(new Dictionary<int, HttpMessageHandler>
            {
                [oldPrimary.Port] = oldPrimary.Server.ChannelOptions.HttpHandler!,
                [newPrimary.Port] = newPrimary.Server.ChannelOptions.HttpHandler!
            })
        };

    [Fact]
    public async Task ConnectAsync_with_global_endpoint_fetches_topology_and_connects_to_primary()
    {
        using var primary = new MockMilvusServer();
        using var topologyHandler = new CapturingHandler(_ => { }, TopologyBody($"localhost:{OldPrimaryPort}", 1));
        using MilvusClientV2 client = new(
            new ConnectConfig
            {
                Uri = "https://my.global-cluster.example.com",
                ApiKey = "secret",
                ChannelOptions = RoutingChannelOptions((OldPrimaryPort, primary), (NewPrimaryPort, primary))
            },
            topologyHandler,
            _ => new[] { new SrvTarget(0, 0, 8443, "seed-a") });

        await client.ConnectAsync(TestContext.Current.CancellationToken);

        Assert.Equal($"localhost:{OldPrimaryPort}", client.PrimaryEndpoint);
        Assert.NotNull(primary.Service.LastConnectClientInfo);
        Assert.Equal("CSharp", primary.Service.LastConnectClientInfo!.SdkType);
    }

    [Fact]
    public async Task Failover_rebuilds_channel_to_new_primary_and_routes_to_it()
    {
        using var oldPrimary = new MockMilvusServer();
        using var newPrimary = new MockMilvusServer();

        string currentTopology = TopologyBody($"localhost:{OldPrimaryPort}", 1);
        using var topologyHandler = new CapturingHandler(_ => { }, () => currentTopology);
        using MilvusClientV2 client = new(
            new ConnectConfig
            {
                Uri = "https://my.global-cluster.example.com",
                ApiKey = "secret",
                ChannelOptions = RoutingChannelOptions((OldPrimaryPort, oldPrimary), (NewPrimaryPort, newPrimary))
            },
            topologyHandler,
            _ => new[] { new SrvTarget(0, 0, 8443, "seed-a") });

        await client.ConnectAsync(TestContext.Current.CancellationToken);
        Assert.Equal($"localhost:{OldPrimaryPort}", client.PrimaryEndpoint);
        Assert.NotNull(oldPrimary.Service.LastConnectClientInfo);

        // A topology change promotes a new primary; the next refresh fails the client over to it.
        currentTopology = TopologyBody($"localhost:{NewPrimaryPort}", 2);
        await client.RefreshGlobalTopologyAsync(TestContext.Current.CancellationToken);

        Assert.Equal($"localhost:{NewPrimaryPort}", client.PrimaryEndpoint);

        // The rebuilt channel must actually route to the new primary's mock server: the request lands on
        // newPrimary.Service, not the old one (whose handler would otherwise still serve it if the swap had not
        // rebuilt the channel).
        await client.GetServerVersionAsync(new GetServerVersionReq(), TestContext.Current.CancellationToken);
        Assert.Contains("GetVersion", newPrimary.Service.Requests.Keys);
        Assert.DoesNotContain("GetVersion", oldPrimary.Service.Requests.Keys);
    }

    [Fact]
    public async Task Global_routing_error_triggers_topology_probe_and_is_retried()
    {
        using var primary = new MockMilvusServer();
        int topologyCalls = 0;
        using var topologyHandler = new CapturingHandler(_ => Interlocked.Increment(ref topologyCalls),
            TopologyBody($"localhost:{OldPrimaryPort}", 1));
        using MilvusClientV2 client = new(
            new ConnectConfig
            {
                Uri = "https://my.global-cluster.example.com",
                ApiKey = "secret",
                ChannelOptions = RoutingChannelOptions((OldPrimaryPort, primary), (NewPrimaryPort, primary))
            },
            topologyHandler,
            _ => new[] { new SrvTarget(0, 0, 8443, "seed-a") });

        await client.ConnectAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, topologyCalls); // initial connect fetch only

        // The next call fails with a server-side global-cluster routing error, then succeeds. Unlike a plain
        // server error, a global-cluster client retries it and probes the topology for a primary change.
        primary.Service.FailureStatus = new Milvus.Client.Grpc.Status
        {
            Code = (int)MilvusErrorCode.UnexpectedError,
            Reason = "STREAMING_CODE_REPLICATE_VIOLATION"
        };
        primary.Service.FailNextCalls = 1;

        int callsBefore = primary.Service.TotalCalls; // the Connect RPC from ConnectAsync
        await client.GetServerVersionAsync(new GetServerVersionReq(), TestContext.Current.CancellationToken);

        Assert.Equal(callsBefore + 2, primary.Service.TotalCalls); // routing-error attempt, then success

        // The connectivity-failure probe re-fetches the topology in the background; wait for it to land.
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (Volatile.Read(ref topologyCalls) < 2 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        Assert.True(Volatile.Read(ref topologyCalls) >= 2,
            $"topology probe did not run after the routing error (calls={Volatile.Read(ref topologyCalls)})");
    }

    [Fact]
    public async Task ConnectAsync_with_topology_auth_failure_surfaces_MilvusException()
    {
        using var primary = new MockMilvusServer();
        using var topologyHandler = new CapturingHandler(_ => { }, "{}", System.Net.HttpStatusCode.Unauthorized);
        using MilvusClientV2 client = new(
            new ConnectConfig
            {
                Uri = "https://my.global-cluster.example.com",
                ApiKey = "secret",
                ChannelOptions = RoutingChannelOptions((OldPrimaryPort, primary), (NewPrimaryPort, primary))
            },
            topologyHandler,
            _ => new[] { new SrvTarget(0, 0, 8443, "seed-a") });

        // The internal GlobalClusterApiException must surface at the public boundary as a MilvusException.
        MilvusException exception = await Assert.ThrowsAsync<MilvusException>(
            () => client.ConnectAsync(TestContext.Current.CancellationToken));
        Assert.Contains("authentication failed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unavailable_error_triggers_topology_probe_and_is_retried()
    {
        using var primary = new MockMilvusServer();
        int topologyCalls = 0;
        using var topologyHandler = new CapturingHandler(_ => Interlocked.Increment(ref topologyCalls),
            TopologyBody($"localhost:{OldPrimaryPort}", 1));
        using MilvusClientV2 client = new(
            new ConnectConfig
            {
                Uri = "https://my.global-cluster.example.com",
                ApiKey = "secret",
                ChannelOptions = RoutingChannelOptions((OldPrimaryPort, primary), (NewPrimaryPort, primary))
            },
            topologyHandler,
            _ => new[] { new SrvTarget(0, 0, 8443, "seed-a") });

        await client.ConnectAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, topologyCalls); // initial connect fetch only

        // The next call fails with gRPC UNAVAILABLE once, then succeeds: it is retried and probes the topology.
        primary.Service.RpcFailure = new RpcException(new Status(StatusCode.Unavailable, "unavailable"));
        primary.Service.FailNextCalls = 1;

        int callsBefore = primary.Service.TotalCalls; // the Connect RPC from ConnectAsync
        await client.GetServerVersionAsync(new GetServerVersionReq(), TestContext.Current.CancellationToken);

        Assert.Equal(callsBefore + 2, primary.Service.TotalCalls); // UNAVAILABLE attempt, then success

        // The connectivity-failure probe re-fetches the topology in the background; wait for it to land.
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (Volatile.Read(ref topologyCalls) < 2 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        Assert.True(Volatile.Read(ref topologyCalls) >= 2,
            $"topology probe did not run after UNAVAILABLE (calls={Volatile.Read(ref topologyCalls)})");
    }

    [Fact]
    public async Task Refresh_with_stale_or_unchanged_topology_keeps_primary()
    {
        using var primary = new MockMilvusServer();
        string currentTopology = TopologyBody($"localhost:{OldPrimaryPort}", 1);
        using var topologyHandler = new CapturingHandler(_ => { }, () => currentTopology);
        using MilvusClientV2 client = new(
            new ConnectConfig
            {
                Uri = "https://my.global-cluster.example.com",
                ApiKey = "secret",
                ChannelOptions = RoutingChannelOptions((OldPrimaryPort, primary), (NewPrimaryPort, primary))
            },
            topologyHandler,
            _ => new[] { new SrvTarget(0, 0, 8443, "seed-a") });

        await client.ConnectAsync(TestContext.Current.CancellationToken);
        Assert.Equal($"localhost:{OldPrimaryPort}", client.PrimaryEndpoint);

        // A same-version answer is stale: it must be discarded without touching the channel.
        await client.RefreshGlobalTopologyAsync(TestContext.Current.CancellationToken);
        Assert.Equal($"localhost:{OldPrimaryPort}", client.PrimaryEndpoint);

        // A newer version whose primary is unchanged only advances the recorded version (no rebuild).
        currentTopology = TopologyBody($"localhost:{OldPrimaryPort}", 2);
        await client.RefreshGlobalTopologyAsync(TestContext.Current.CancellationToken);
        Assert.Equal($"localhost:{OldPrimaryPort}", client.PrimaryEndpoint);

        // The channel still routes to the (unchanged) primary.
        await client.GetServerVersionAsync(new GetServerVersionReq(), TestContext.Current.CancellationToken);
        Assert.Contains("GetVersion", primary.Service.Requests.Keys);
    }
}

// Dispatches each gRPC request to the in-process TestServer handler registered for the request's URI port, so a
// channel rebuilt for a different host:port provably routes to the corresponding mock server.
internal sealed class PortRoutingHandler : HttpMessageHandler
{
    private readonly Dictionary<int, HttpMessageInvoker> _byPort = new();

    public PortRoutingHandler(IEnumerable<KeyValuePair<int, HttpMessageHandler>> byPort)
    {
        foreach (KeyValuePair<int, HttpMessageHandler> pair in byPort)
        {
            _byPort[pair.Key] = new HttpMessageInvoker(pair.Value, disposeHandler: false);
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        int port = request.RequestUri?.Port ?? 0;
        if (!_byPort.TryGetValue(port, out HttpMessageInvoker? invoker))
        {
            throw new InvalidOperationException($"No mock server registered for port {port}");
        }

        return invoker.SendAsync(request, cancellationToken);
    }
}
