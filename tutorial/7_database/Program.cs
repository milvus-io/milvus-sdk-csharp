using Milvus.Client.V2;
using Milvus.Client.V2.Requests.Collection;
using Milvus.Client.V2.Requests.Database;
using Milvus.Client.V2.Requests.Utility;
using Milvus.Client.V2.Responses.Database;
using Milvus.Client.V2.Types;

const string REPLICA_NUMBER = "database.replica.number";

string uri = Environment.GetEnvironmentVariable("MILVUS_URI") ?? "localhost:19530";
string token = Environment.GetEnvironmentVariable("MILVUS_TOKEN") ?? "root:Milvus";
string database = TutorialDatabaseName();

// MilvusClientV2 connects to `uri` and authenticates database administration with `token`.
Console.WriteLine("Calling MilvusClientV2: connect to Milvus");
using var client = CreateClient(uri, token);
await client.ConnectAsync();
Console.WriteLine("MilvusClientV2 connected");

await PrintDatabases(client, "Databases before the tutorial");

Console.WriteLine($"\nCreating database {database}");
Console.WriteLine("Calling CreateDatabaseAsync");
await client.CreateDatabaseAsync(new CreateDatabaseReq { DatabaseName = database });
Console.WriteLine("CreateDatabaseAsync completed");

// Capture the tutorial result so cleanup still runs if a later operation fails.
Exception? tutorialError = await DemonstrateDatabaseInterfaces(client, database);
Exception? cleanupError = await CleanupDatabase(client, database);

if (cleanupError is not null)
{
    Console.WriteLine($"Failed to clean up {database}: {cleanupError.Message}");
}

if (tutorialError is not null)
{
    throw tutorialError;
}

if (cleanupError is not null)
{
    throw cleanupError;
}

await PrintDatabases(client, "Databases after cleanup");

async Task<Exception?> DemonstrateDatabaseInterfaces(MilvusClientV2 client, string database)
{
    try
    {
        Console.WriteLine("Calling DescribeDatabaseAsync: inspect");
        DescribeDatabaseResp description = await client.DescribeDatabaseAsync(new DescribeDatabaseReq { DatabaseName = database });
        Console.WriteLine($"DescribeDatabaseAsync completed: name={description.DatabaseName}, " +
            $"created_timestamp={description.CreatedTimestamp}");

        Console.WriteLine($"\nSetting {REPLICA_NUMBER}=1");
        Console.WriteLine("Calling AlterDatabasePropertiesAsync");
        await client.AlterDatabasePropertiesAsync(new AlterDatabasePropertiesReq
        {
            DatabaseName = database,
            Properties = { [REPLICA_NUMBER] = "1" }
        });
        Console.WriteLine("AlterDatabasePropertiesAsync completed");

        Console.WriteLine("Calling DescribeDatabaseAsync: read the updated property");
        description = await client.DescribeDatabaseAsync(new DescribeDatabaseReq { DatabaseName = database });
        Console.WriteLine($"Property value: {description.Properties.GetValueOrDefault(REPLICA_NUMBER, "<not set>")}");

        Console.WriteLine($"\nRemoving {REPLICA_NUMBER}");
        Console.WriteLine("Calling DropDatabasePropertiesAsync");
        await client.DropDatabasePropertiesAsync(new DropDatabasePropertiesReq
        {
            DatabaseName = database,
            DeleteKeys = [REPLICA_NUMBER]
        });
        Console.WriteLine("DropDatabasePropertiesAsync completed");

        Console.WriteLine("Calling DescribeDatabaseAsync: verify property removal");
        description = await client.DescribeDatabaseAsync(new DescribeDatabaseReq { DatabaseName = database });
        Console.WriteLine($"Property value after removal: {description.Properties.GetValueOrDefault(REPLICA_NUMBER, "<not set>")}");

        Console.WriteLine($"\nCalling UseDatabaseAsync: select {database}");
        await client.UseDatabaseAsync(new UseDatabaseReq { DatabaseName = database });
        Console.WriteLine("UseDatabaseAsync completed");

        Console.WriteLine("Calling ListCollectionsAsync: collections in the selected database");
        var collections = await client.ListCollectionsAsync(new ListCollectionsReq());
        Console.WriteLine($"Collections in {database}: {string.Join(", ", collections.CollectionNames)}");

        return null;
    }
    catch (Exception ex)
    {
        return ex;
    }
}

async Task<Exception?> CleanupDatabase(MilvusClientV2 client, string database)
{
    try
    {
        Console.WriteLine("Calling UseDatabaseAsync: select default");
        await client.UseDatabaseAsync(new UseDatabaseReq { DatabaseName = "default" });
        Console.WriteLine("UseDatabaseAsync completed");

        Console.WriteLine($"\nCalling DropDatabaseAsync: remove {database}");
        await client.DropDatabaseAsync(new DropDatabaseReq { DatabaseName = database });
        Console.WriteLine("DropDatabaseAsync completed");
        return null;
    }
    catch (Exception ex)
    {
        return ex;
    }
}

async Task PrintDatabases(MilvusClientV2 client, string heading)
{
    Console.WriteLine("Calling ListDatabasesAsync");
    ListDatabasesResp response = await client.ListDatabasesAsync(new ListDatabasesReq());
    Console.WriteLine($"ListDatabasesAsync completed - {heading}: {string.Join(", ", response.DatabaseNames)}");
}

static MilvusClientV2 CreateClient(string uri, string token)
{
    var config = new ConnectConfig { Uri = uri };
    int colon = token.IndexOf(':');
    if (colon > 0)
    {
        config.Username = token[..colon];
        config.Password = token[(colon + 1)..];
    }
    else
    {
        config.ApiKey = token;
    }

    return new MilvusClientV2(config);
}

static string TutorialDatabaseName()
{
    return $"csharp_tutorial_db_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Environment.ProcessId}";
}
