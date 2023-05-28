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

    public IReadOnlyList<IMilvusClient> MilvusClients { get; }

    public GrpcChannel CreateChannel(string address)
    {
#if NET461_OR_GREATER
        return GrpcChannel.ForAddress(connectParam.GetAddress(), new GrpcChannelOptions
        {
            HttpHandler = new WinHttpHandler()
        });
#else
        return GrpcChannel.ForAddress(address);
#endif
    }
}