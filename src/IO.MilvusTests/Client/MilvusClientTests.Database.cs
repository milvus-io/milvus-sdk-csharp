using FluentAssertions;
using IO.Milvus.Client;
using IO.Milvus;
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
    [Fact]
    public async Task DatabaseTest()
    {
        if (TestEnvironment.IsZillizCloud)
        {
            return;
        }

        //Not support below milvus 2.2.9
        MilvusVersion version = await Client.GetMilvusVersionAsync();
        if (!version.GreaterThan(2, 2, 8))
        {
            return;
        }

        string databaseName = Client.GetType().Name;

        //List original database
        IEnumerable<string> databases = await Client.ListDatabasesAsync();
        databases.Should().NotBeNullOrEmpty();

        //Check if it exists.
        if (databases.Contains(databaseName))
        {
            await Client.DropDatabaseAsync(databaseName);
        }

        //Create database
        await Client.CreateDatabaseAsync(databaseName);
        databases = await Client.ListDatabasesAsync();
        databases.Should().NotBeNullOrEmpty();
        databases.Should().Contain(databaseName);

        //Drop database
        await Client.DropDatabaseAsync(databaseName);
        databases = await Client.ListDatabasesAsync();
        databases.Should().NotBeNullOrEmpty();
        databases.Should().NotContain(databaseName);
    }
}