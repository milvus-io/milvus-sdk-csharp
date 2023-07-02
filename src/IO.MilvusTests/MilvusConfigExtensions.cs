using IO.Milvus.Client;
using IO.Milvus.Client.gRPC;
using IO.Milvus.Client.REST;

namespace IO.MilvusTests;

internal static class MilvusConfigExtensions
{
    public static IMilvusClient CreateClient(this MilvusConfig config)
    {
        if (string.Compare(config.ConnectionType,"rest",true) == 0)
        {
            return new MilvusRestClient(config.Endpoint, config.Port);
        }
        else if(string.Compare(config.ConnectionType, "grpc", true) == 0)
        {
            return new MilvusGrpcClient(config.Endpoint, config.Port,config.Username,config.Password);
        }
        else
        {
            throw new NotSupportedException($"Connection type {config.ConnectionType} is not supported.");
        }
    }
}
