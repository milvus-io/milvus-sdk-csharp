using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using IO.Milvus.ApiSchema;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    #region Collection
    ///<inheritdoc/>
    public Task CreateCollectionAsync(string collectionName, ConsistencyLevel consistencyLevel, IList<FieldType> fieldTypes, int shards_num = 1, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public Task<DescribeCollectionResponse> DescribeCollectionAsync(string collectionName, int collectionId, DateTime? dateTime = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public Task DropCollectionAsync(string collectionName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public Task GetCollectionStatisticsAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public Task<bool> HasCollectionAsync(string collectionName, DateTime? dateTime, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public Task LoadCollectionAsync(string collectionName, int replicNumber = 1, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public Task ReleaseCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public Task ShowCollectionsAsync(IList<string> collectionNames = null, int? type = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    #endregion
}

