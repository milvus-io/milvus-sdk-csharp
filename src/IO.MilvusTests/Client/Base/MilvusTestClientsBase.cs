using IO.Milvus.Param;
using IO.Milvus.Client;
using Grpc.Net.Client;

namespace IO.MilvusTests.Client.Base;

public abstract class MilvusTestClientsBase
{
    public MilvusTestClientsBase()
    {
       MilvusClients =  MilvusConfig
            .Load()
            .Select(c => c.CreateClient())
            .ToList();
    }

    public IReadOnlyList<IMilvusClient2> MilvusClients { get; }

    public GrpcChannel CreateChannel(ConnectParam connectParam)
    {
#if NET461_OR_GREATER
        return GrpcChannel.ForAddress(connectParam.GetAddress(), new GrpcChannelOptions
        {
            HttpHandler = new WinHttpHandler()
        });
#else
        return GrpcChannel.ForAddress(connectParam.GetAddress());
#endif
    }
}