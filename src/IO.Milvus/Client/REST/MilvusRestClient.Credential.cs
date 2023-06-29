using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using IO.Milvus.ApiSchema;
using System.Text.Json;
using System.Text;
using System;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    ///<inheritdoc/>
    public async Task DeleteCredentialAsync(
        string username, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Delete credential {0}", username);

        using HttpRequestMessage request = DeleteCredentialRequest
            .Create(username)
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Delete credential failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task UpdateCredentialAsync(
        string username, 
        string oldPassword, 
        string newPassword, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Update credential {0}", username);

        using HttpRequestMessage request = UpdateCredentialRequest
            .Create(username,Base64Encode(oldPassword),Base64Encode(newPassword))
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Update credential failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task CreateCredentialAsync(
        string username, 
        string password, 
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("Create credential {0}", username);

        using HttpRequestMessage request = CreateCredentialRequest
            .Create(username, Base64Encode(password))
            .BuildRest();

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "Create credential failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        ValidateResponse(responseContent);
    }

    ///<inheritdoc/>
    public async Task<IList<string>> ListCredUsersAsync(
        CancellationToken cancellationToken = default)
    {
        this._log.LogDebug("List credential users");

        using HttpRequestMessage request = HttpRequest.CreateGetRequest($"{ApiVersion.V1}/credential/users");

        (HttpResponseMessage response, string responseContent) = await this.ExecuteHttpRequestAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            this._log.LogError(e, "List credential users failed: {0}, {1}", e.Message, responseContent);
            throw;
        }

        var data = JsonSerializer.Deserialize<ListCredUsersResponse>(responseContent);

        if (data.Status != null && data.Status.ErrorCode != Grpc.ErrorCode.Success)
        {
            this._log.LogError("Failed list credential users: {0}, {1}", data.Status.ErrorCode, data.Status.Reason);
            throw new Diagnostics.MilvusException(data.Status);
        }

        return data.Usernames;
    }

    #region Private =================================================================================================
    private string Base64Encode(string input)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
    }
    #endregion
}
