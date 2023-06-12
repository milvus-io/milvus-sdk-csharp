using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    ///<inheritdoc/>
    public Task CreateDatabaseAsync(string dbName, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not support in MilvusRestClient");
    }

    ///<inheritdoc/>
    public Task<IEnumerable<string>> ListDatabasesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not support in MilvusRestClient");
    }

    ///<inheritdoc/>
    public Task DropDatabaseAsync(string dbName, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not support in MilvusRestClient");
    }
}
