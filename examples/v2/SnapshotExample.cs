using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Dml;
using Milvus.Client.V2.Requests.Dql;
using Milvus.Client.V2.Requests.Snapshot;
using Milvus.Client.V2.Responses.Collection;
using Milvus.Client.V2.Responses.Snapshot;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates collection snapshots: create a snapshot after a flush, list and describe it, pin its data,
/// restore it into a new collection, wait for the restore job, verify the row count, then unpin and drop.
/// Mirrors cpp examples/src/v2/snapshot.cpp.
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show the snapshot lifecycle (<c>CreateSnapshotAsync</c> ... <c>DropSnapshotAsync</c>)
/// and the asynchronous restore flow.</para>
/// <para><b>APIs used:</b> <c>CreateCollectionAsync</c>, <c>InsertAsync</c>, <c>FlushAsync</c>,
/// <c>CreateSnapshotAsync</c>, <c>ListSnapshotsAsync</c>, <c>DescribeSnapshotAsync</c>,
/// <c>PinSnapshotDataAsync</c>, <c>RestoreSnapshotAsync</c>, <c>GetRestoreSnapshotStateAsync</c>,
/// <c>QueryAsync</c>, <c>GetCollectionStatsAsync</c>, <c>UnpinSnapshotDataAsync</c>, <c>DropSnapshotAsync</c>,
/// <c>DropCollectionAsync</c>.</para>
/// <para><b>Expected output:</b> snapshot created/list/described, restore job completes with a matching
/// row count, then "Done.".</para>
/// </remarks>
public static class SnapshotExample
{
    public static async Task Run(string uri)
    {
        using MilvusClientV2 client = ExampleHelpers.CreateClient(uri);
        Console.WriteLine("Connecting...");
        await client.ConnectAsync();

        const string collectionName = "snapshot_example";
        const string restoreCollectionName = "snapshot_example_restore";
        const string snapshotName = "csharp_example_snapshot_backup";
        const int dimension = 4;

        await ExampleHelpers.ResetCollectionAsync(client, collectionName);
        await ExampleHelpers.ResetCollectionAsync(client, restoreCollectionName);

        Console.WriteLine("Creating collection...");
        await client.CreateCollectionAsync(new CreateCollectionReq
        {
            CollectionName = collectionName,
            Schema = new CollectionSchema
            {
                Fields =
                {
                    new FieldSchema("id", DataType.Int64, isPrimaryKey: true),
                    FieldSchema.CreateFloatVector("vector", dimension: dimension)
                }
            }
        });

        Console.WriteLine("Inserting 100 rows...");
        await client.InsertAsync(new InsertReq
        {
            CollectionName = collectionName,
            ColumnsData =
            [
                FieldData.Create("id", Enumerable.Range(0, 100).Select(i => (long)i).ToArray()),
                FieldData.CreateFloatVector("vector", Enumerable.Range(0, 100)
                    .Select(i => new ReadOnlyMemory<float>(new[]
                        { i % 9 / 10f, i % 7 / 10f, i % 5 / 10f, i % 3 / 10f }))
                    .ToArray())
            ]
        });

        Console.WriteLine("Flushing collection...");
        await client.FlushAsync(new Milvus.Client.V2.Requests.Utility.FlushReq { CollectionNames = [collectionName] });

        Console.WriteLine("Creating snapshot...");
        await client.CreateSnapshotAsync(new CreateSnapshotReq
        {
            SnapshotName = snapshotName,
            CollectionName = collectionName,
            Description = "Snapshot example backup"
        });
        Console.WriteLine($"Snapshot '{snapshotName}' created");

        Console.WriteLine("Listing snapshots...");
        ListSnapshotsResp listResp = await client.ListSnapshotsAsync(
            new ListSnapshotsReq { CollectionName = collectionName });
        foreach (string name in listResp.Snapshots)
        {
            Console.WriteLine($"  {name}");
        }

        Console.WriteLine("Describing snapshot...");
        DescribeSnapshotResp describeResp = await client.DescribeSnapshotAsync(new DescribeSnapshotReq
        {
            SnapshotName = snapshotName,
            CollectionName = collectionName
        });
        Console.WriteLine($"  name={describeResp.Name}, collection={describeResp.CollectionName}, " +
            $"partitions={string.Join(",", describeResp.PartitionNames)}, createTs={describeResp.CreateTs}, " +
            $"s3Location={describeResp.S3Location}");

        Console.WriteLine("Pinning snapshot data...");
        PinSnapshotDataResp pinResp = await client.PinSnapshotDataAsync(new PinSnapshotDataReq
        {
            SnapshotName = snapshotName,
            CollectionName = collectionName,
            TtlSeconds = 3600
        });
        Console.WriteLine($"Snapshot data pinned, pinId={pinResp.PinId}");

        Console.WriteLine("Restoring snapshot...");
        RestoreSnapshotResp restoreResp = await client.RestoreSnapshotAsync(new RestoreSnapshotReq
        {
            SnapshotName = snapshotName,
            SourceCollectionName = collectionName,
            TargetCollectionName = restoreCollectionName
        });
        Console.WriteLine($"Restore snapshot job submitted, jobId={restoreResp.JobId}");

        RestoreSnapshotJobInfo jobInfo = await WaitRestoreSnapshotAsync(client, restoreResp.JobId);
        Console.WriteLine($"Restore job state: state={jobInfo.State}, progress={jobInfo.Progress}, " +
            $"reason={jobInfo.Reason}");

        long sourceRowCount = await GetPersistedRowCountAsync(client, collectionName);
        long targetRowCount = await GetPersistedRowCountAsync(client, restoreCollectionName);
        Console.WriteLine($"Source persisted row count={sourceRowCount}, " +
            $"target persisted row count={targetRowCount}");
        if (sourceRowCount != targetRowCount)
        {
            throw new InvalidOperationException(
                $"Restored row count mismatch: source={sourceRowCount}, target={targetRowCount}");
        }

        Console.WriteLine("Unpinning snapshot data...");
        await client.UnpinSnapshotDataAsync(new UnpinSnapshotDataReq { PinId = pinResp.PinId });
        Console.WriteLine($"Snapshot data unpinned, pinId={pinResp.PinId}");

        Console.WriteLine("Dropping snapshot...");
        await client.DropSnapshotAsync(new DropSnapshotReq
        {
            SnapshotName = snapshotName,
            CollectionName = collectionName
        });
        Console.WriteLine($"Snapshot '{snapshotName}' dropped");

        Console.WriteLine("Deleting...");
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = collectionName });
        await client.DropCollectionAsync(new DropCollectionReq { CollectionName = restoreCollectionName });
        Console.WriteLine("Done.");
    }

    private static async Task<RestoreSnapshotJobInfo> WaitRestoreSnapshotAsync(MilvusClientV2 client, long jobId)
    {
        for (int i = 0; i < 60; i++)
        {
            GetRestoreSnapshotStateResp response = await client.GetRestoreSnapshotStateAsync(
                new GetRestoreSnapshotStateReq { JobId = jobId });
            RestoreSnapshotJobInfo jobInfo = response.JobInfo;
            if (jobInfo.State is RestoreSnapshotState.Completed)
            {
                return jobInfo;
            }

            if (jobInfo.State is RestoreSnapshotState.Failed)
            {
                throw new InvalidOperationException($"Restore snapshot failed: {jobInfo.Reason}");
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException("Restore snapshot did not complete within 60 seconds");
    }

    private static async Task<long> GetPersistedRowCountAsync(MilvusClientV2 client, string collectionName)
    {
        GetCollectionStatsResp response = await client.GetCollectionStatsAsync(
            new GetCollectionStatsReq { CollectionName = collectionName });
        return response.RowCount;
    }
}
