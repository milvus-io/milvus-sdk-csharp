using FluentAssertions;
using IO.Milvus.Client;
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
        string version = await milvusClient.GetVersionAsync();
        var milvusVersion = MilvusVersion.Parse(version);
        if (milvusVersion.Minor <=2 && milvusVersion.Patch < 9)
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

internal class MilvusVersion
{
    public MilvusVersion(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public int Major { get; }

    public int Minor { get; }

    public int Patch { get; }

    public static MilvusVersion Parse(string version)
    {
        var versions = version[1..].Split('.');
        return new MilvusVersion(
            int.Parse(versions[0]),
            int.Parse(versions[1]),
            int.Parse(versions[2]));
    }

    public bool GreaterThan(int major, int minor, int patch)
    {
        if (Major > major)
        {
            return true;
        }
        else if (Minor > minor)
        {
            return true;
        }
        else if (Patch > patch)
        {
            return true;
        }

        return false;
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }
}