using IO.Milvus.ApiSchema;
using IO.Milvus.Diagnostics;
using IO.Milvus.Utils;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client.REST;

public partial class MilvusRestClient
{
    /// <inheritdoc />
    public async Task DeleteCredentialAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);

        using HttpRequestMessage request = HttpRequest.CreateDeleteRequest(
            $"{ApiVersion.V1}/credential",
            new DeleteCredentialRequest { Username = username });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ValidateResponse(responseContent);
    }

    /// <inheritdoc />
    public async Task UpdateCredentialAsync(
        string username,
        string oldPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);
        Verify.NotNullOrWhiteSpace(oldPassword);
        Verify.NotNullOrWhiteSpace(newPassword);

        using HttpRequestMessage request = HttpRequest.CreatePatchRequest(
            $"{ApiVersion.V1}/credential",
            new UpdateCredentialRequest {  Username = username, OldPassword = Base64Encode(oldPassword), NewPassword = Base64Encode(newPassword) });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ValidateResponse(responseContent);
    }

    /// <inheritdoc />
    public async Task CreateCredentialAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(username);
        Verify.NotNullOrWhiteSpace(password);

        long timestamp = TimestampUtils.GetNowUTCTimestamp();
        using HttpRequestMessage request = HttpRequest.CreatePostRequest(
            $"{ApiVersion.V1}/credential",
            new CreateCredentialRequest
            {
                Username = username,
                Password = Base64Encode(password),
                ModifiedUtcTimestamps = timestamp,
                CreatedUtcTimestamps = timestamp,
            });

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ValidateResponse(responseContent);
    }

    /// <inheritdoc />
    public async Task<IList<string>> ListCredUsersAsync(
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = HttpRequest.CreateGetRequest($"{ApiVersion.V1}/credential/users");

        string responseContent = await ExecuteHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);

        ListCredUsersResponse data = JsonSerializer.Deserialize<ListCredUsersResponse>(responseContent);
        ValidateStatus(data.Status);

        return data.Usernames;
    }

    private static string Base64Encode(string input) => Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
}
