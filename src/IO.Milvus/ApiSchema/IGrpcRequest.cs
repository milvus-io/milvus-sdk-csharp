namespace IO.Milvus.ApiSchema;

/// <summary>
/// Interface that generate a gRPC request
/// </summary>
/// <typeparam name="TGrpcRequest">gRPC request type</typeparam>
internal interface IGrpcRequest<TGrpcRequest>
{
    /// <summary>
    /// Generate a a gRPC request
    /// </summary>
    /// <returns></returns>
    TGrpcRequest BuildGrpc();
}
