namespace IO.Milvus.ApiSchema;

internal interface IGrpcRequest<TGrpcRequest>
{
    TGrpcRequest BuildGrpc();
}
