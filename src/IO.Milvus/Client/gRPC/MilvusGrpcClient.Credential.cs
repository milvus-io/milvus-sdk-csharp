using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace IO.Milvus.Client.gRPC;

public partial class MilvusGrpcClient
{
    public async Task DeleteCredential(
        string username, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateCredentialAsync(
        string username,
        string oldPassword, 
        string newPassword, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task CreateCredentialAsync(
        string username, 
        string password, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IList<string>> ListCredUsersAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
