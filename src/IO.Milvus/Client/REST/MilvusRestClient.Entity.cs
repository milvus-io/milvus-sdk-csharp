using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    ///<inheritdoc/>
    public async Task<MilvusMutationResult> InsertAsync(
        string collectionName, 
        IList<Field> fields, 
        string partitionName = "", 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public async Task<MilvusMutationResult> DeleteAsync(
        string collectionName, 
        string expr, 
        string partitionName, 
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    ///<inheritdoc/>
    public async Task<MilvusSearchResult> SearchAsync(
        SearchParameters searchParameters, 
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
