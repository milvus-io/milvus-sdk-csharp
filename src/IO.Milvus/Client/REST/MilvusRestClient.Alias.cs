using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    ///<inheritdoc/>
    public async Task CreateAliasAsync(
        string collectionName,
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(alias);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/alias",
            new CreateAliasRequest { CollectionName = collectionName, Alias = alias, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task DropAliasAsync(
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(alias);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/alias", 
            new DropAliasRequest { Alias = alias, DbName = dbName });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task AlterAliasAsync(
        string collectionName,
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(collectionName);
        Verify.NotNullOrWhiteSpace(alias);
        Verify.NotNullOrWhiteSpace(dbName);

        using HttpRequestMessage request = HttpRequest.CreatePatchRequest(
            $"{ApiVersion.V1}/alias", 
            new AlterAliasRequest { CollectionName = collectionName, Alias = alias, DbName = dbName });
        
        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ValidateResponse(responseContent);
    }
}
