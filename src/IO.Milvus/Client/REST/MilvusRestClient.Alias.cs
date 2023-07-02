using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using IO.Milvus.ApiSchema;

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
        _log.LogDebug("Create alias {0}", collectionName);

        using HttpRequestMessage request = CreateAliasRequest
            .Create(collectionName, alias, dbName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            _log.LogError(e, "Create alias failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task DropAliasAsync(
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Drop alias {0}", alias);

        using HttpRequestMessage request = DropAliasRequest
            .Create(alias, dbName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            _log.LogError(e, "Drop alias failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task AlterAliasAsync(
        string collectionName,
        string alias,
        string dbName = Constants.DEFAULT_DATABASE_NAME,
        CancellationToken cancellationToken = default)
    {
        _log.LogDebug("Alter alias {0}", alias);

        using HttpRequestMessage request = AlterAliasRequest
            .Create(collectionName, alias, dbName)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            _log.LogError(e, "Alter alias failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }
}
