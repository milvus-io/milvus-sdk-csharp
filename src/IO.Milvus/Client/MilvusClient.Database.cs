﻿using IO.Milvus.Diagnostics;
using IO.Milvus.Grpc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    /// <summary>
    /// Create a database in milvus.
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// </remarks>
    /// <param name="dbName">Database name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public async Task CreateDatabaseAsync(string dbName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.CreateDatabaseAsync, new CreateDatabaseRequest
        {
            DbName = dbName,
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// List databases.
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// </remarks>
    /// <returns>Databases</returns>
    public async Task<IEnumerable<string>> ListDatabasesAsync(CancellationToken cancellationToken = default)
    {
        ListDatabasesResponse response = await InvokeAsync(_grpcClient.ListDatabasesAsync, new ListDatabasesRequest(), static r => r.Status, cancellationToken).ConfigureAwait(false);

        return response.DbNames;
    }

    /// <summary>
    /// Drops a database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// available in <c>Milvus 2.2.9</c>
    /// </para>
    /// <para>
    /// Note that this method drops all data in the database.
    /// </para>
    /// </remarks>
    /// <param name="dbName">Database name.</param>
    /// <param name="cancellationToken">Cancellation name.</param>
    /// <returns></returns>
    public async Task DropDatabaseAsync(string dbName, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhiteSpace(dbName);

        await InvokeAsync(_grpcClient.DropDatabaseAsync, new DropDatabaseRequest
        {
            DbName = dbName,
        }, cancellationToken).ConfigureAwait(false);
    }
}