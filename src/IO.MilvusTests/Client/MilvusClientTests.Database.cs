using FluentAssertions;
using IO.Milvus.Client;
using IO.Milvus;
using IO.Milvus.Client.REST;
using IO.MilvusTests.Utils;
using Xunit;

namespace IO.MilvusTests.Client;

public partial class MilvusClientTests
{
    /// <summary>
    /// Database test
    /// </summary>
    /// <remarks>
    /// Available in Milvus 2.2.9
    /// 
    /// <see href="https://milvus.io/docs/manage_databases.md"/>
    /// </remarks>
    [Theory]
    [ClassData(typeof(TestClients))]
    public async Task DatabaseTest(IMilvusClient milvusClient)
    {
        //Not support milvusRestClient
        if (milvusClient is MilvusRestClient || milvusClient.IsZillizCloud())
        {
            return;
        }

        //Not support below milvus 2.2.9
        MilvusVersion version = await milvusClient.GetMilvusVersionAsync();
        if (!version.GreaterThan(2,2,8))
        {
            return;
        }

        string databaseName = milvusClient.GetType().Name;

        //List original database
        IEnumerable<string> databases = await milvusClient.ListDatabasesAsync();
        databases.Should().NotBeNullOrEmpty();

        //Check if it exists.
        if (databases.Contains(databaseName))
        {
            await milvusClient.DropDatabaseAsync(databaseName);
        }

        //Create database
        await milvusClient.CreateDatabaseAsync(databaseName);
        databases = await milvusClient.ListDatabasesAsync();
        databases.Should().NotBeNullOrEmpty();
        databases.Should().Contain(databaseName);

        //Drop database
        await milvusClient.DropDatabaseAsync(databaseName);
        databases = await milvusClient.ListDatabasesAsync();
        databases.Should().NotBeNullOrEmpty();
        databases.Should().NotContain(databaseName);
    }
}