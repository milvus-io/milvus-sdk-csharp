
using Grpc.Core;
using Grpc.Net.Client;
using IO.Milvus.Grpc;
using IO.Milvus.Param;
using System;
#if NET461_OR_GREATER
using System.Net.Http;
#endif

namespace IO.Milvus.Client
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// <see href="https://docs.microsoft.com/zh-cn/aspnet/core/grpc/?view=aspnetcore-6.0"/>
    /// </remarks>
    public class MilvusServiceClient:AbstractMilvusGrpcClient
    {
        public MilvusServiceClient(ConnectParam connectParam)
        {
            connectParam.Check();
            defaultCallOptions = new CallOptions();
            defaultCallOptions.WithHeaders(new Metadata()
            {
                {"authorization",connectParam.Authorization }
            });

#if NET461_OR_GREATER
            var httpHandler = new HttpClientHandler();
            httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            channel = GrpcChannel.ForAddress(connectParam.GetAddress(),new GrpcChannelOptions { HttpHandler = httpHandler });
#else
            channel = GrpcChannel.ForAddress(connectParam.GetAddress());
#endif
            
            client = new MilvusService.MilvusServiceClient(channel);
        }

        /// <summary>
        /// Create a grpc's auto-generate client without any wrapper.
        /// </summary>
        /// <param name="connectParam">use <see cref="ConnectParam.Create(string, int, string, string)"/> to create a connectparam</param>
        /// <returns><see cref="MilvusService.MilvusServiceClient"/></returns>
        public static MilvusService.MilvusServiceClient CreateGrpcDefaultClient(ConnectParam connectParam)
        {
            connectParam.Check();
            var channel = GrpcChannel.ForAddress(connectParam.GetAddress());
            return new MilvusService.MilvusServiceClient(channel);
        }
    }
}
