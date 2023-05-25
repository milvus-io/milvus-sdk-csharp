using FluentAssertions;
using IO.Milvus.Param;
using IO.Milvus.Client;
using IO.Milvus.Grpc;
using IO.Milvus.Param.Collection;
using IO.Milvus.Param.Dml;
using IO.Milvus.Param.Index;
using Grpc.Net.Client;
using IO.Milvus.Param.Alias;
using IO.Milvus.Param.Partition;
using IO.MilvusTests.Helpers;

namespace IO.MilvusTests.Client.Base;

public abstract class MilvusServiceClientTestsBase
{
    private MilvusServiceClient? _milvusclient;
    protected static Random random = new(DateTime.Now.Second);

    public MilvusServiceClient MilvusClient => _milvusclient ??= DefaultClient();

    public GrpcChannel CreateChannel(ConnectParam connectParam)
    {
#if NET461_OR_GREATER
        return GrpcChannel.ForAddress(connectParam.GetAddress(), new GrpcChannelOptions
        {
            HttpHandler = new WinHttpHandler()
        });
#else
        return GrpcChannel.ForAddress(connectParam.GetAddress());
#endif
    }

    public MilvusServiceClient DefaultClient()
    {
        _milvusclient ??= new MilvusServiceClient(
            HostConfig.ConnectParam, CreateChannel(HostConfig.ConnectParam));

        _milvusclient.Should().NotBeNull();
        _milvusclient.ClientIsReady().Should().BeTrue();

        return MilvusClient;
    }

    public MilvusServiceClient NewClient()
    {
        var client = new MilvusServiceClient(
            HostConfig.ConnectParam, CreateChannel(HostConfig.ConnectParam));

        client.Should().NotBeNull();
        client.ClientIsReady().Should().BeTrue();

        return client;
    }

    public MilvusServiceClient NewErrorClient()
    {
        var client = new MilvusServiceClient(
            ConnectParam.Create(
                host: HostConfig.Host,
                port: HostConfig.Port));

        client.Should().NotBeNull();
        client.ClientIsReady().Should().BeTrue();

        return client;
    }

    #region Helper Methods

    public async Task<R<RpcStatus>> CreateBookCollectionAsync(string collectionName)
    {
        var param = CreateCollectionParam.Create(
            collectionName,
            2,
            new List<FieldType>()
            {
                FieldType.Create(
                    "book_id",
                    DataType.Int64,
                    isPrimaryLey: true),

                FieldType.Create(
                    "book_name",
                    "",
                    DataType.VarChar,
                    0, 256
                ),

                FieldType.Create(
                    "word_count",
                    DataType.Int64),

                FieldType.Create(
                    "book_intro",
                    "",
                    DataType.FloatVector,
                    dimension: 2,
                    0)
            },
            description: "Test book search");

        var r = MilvusClient.CreateCollection(param);

        r.AssertRpcStatus();

        await Task.Delay(500);
        return r;
    }


    public R<RpcStatus> CreatePartition(string collectionName, string partitionName)
    {
        var r = MilvusClient.CreatePartition(CreatePartitionParam.Create(
            collectionName, partitionName));

        return r;
    }

    public async Task<R<RpcStatus>> CreateAliasAsync(string collectionName, string aliasName)
    {
        var param = CreateAliasParam.Create(
            collectionName, aliasName);

        var r = MilvusClient.CreateAlias(param);

        r.AssertRpcStatus();

        await Task.Delay(2000);
        return r;
    }

    public R<RpcStatus> CreateBookCollectionIndex(string collectionName, string fieldName, string indexName)
    {
        var param = CreateIndexParam.Create(
            collectionName, fieldName, indexName, IndexType.IVF_FLAT, MetricType.L2);
        param.ExtraDic.Add("nlist", "1024");

        var r = MilvusClient.CreateIndex(param);

        r.AssertRpcStatus();

        return r;
    }

    public async Task<R<RpcStatus>> LoadCollectionAsync(string collectionName)
    {
        var param = LoadCollectionParam.Create(
            collectionName);

        var r = MilvusClient.LoadCollection(param);

        r.AssertRpcStatus();

        await Task.Delay(10000);
        return r;
    }

    public R<MutationResult> InsertDataToBookCollection(string collectionName, string partitionName)
    {
        var bookIds = new List<long>();
        var wordCounts = new List<long>();
        var bookIntros = new List<List<float>>();
        var bookNames = new List<string>();

        var random = new Random(DateTime.Now.Second);
        for (int i = 0; i < 2000; i++)
        {
            bookIds.Add(i);
            wordCounts.Add(i + 10000);
            bookNames.Add($"Book Name {i}");
            var vector = new List<float>();
            for (int k = 0; k < 2; ++k)
            {
                vector.Add(random.Next());
            }

            bookIntros.Add(vector);
        }

        var insertParam = InsertParam.Create(collectionName, partitionName,
            new List<Field>()
            {
                Field.Create("book_id", bookIds),
                Field.Create("book_name", bookNames),
                Field.Create("word_count", wordCounts),
                Field.CreateBinaryVectors("book_intro", bookIntros),
            });

        var r = MilvusClient.Insert(insertParam);

        return r;
    }

    public R<RpcStatus> LoadPartitions(string collectionName, List<string> partitions)
    {
        var param = LoadPartitionsParam.Create(
            collectionName, partitions);

        var r = MilvusClient.LoadPartitions(param);

        r.AssertRpcStatus();

        return r;
    }

    public R<RpcStatus> CreateAlias(string collectionName, string aliasName)
    {
        return MilvusClient.CreateAlias(CreateAliasParam.Create(collectionName, aliasName));
    }

    public R<RpcStatus> DropAlias(string collectionName, string aliasName)
    {
        var r = MilvusClient.DropAlias(DropAliasParam.Create(collectionName, aliasName));

        return r;
    }

    #endregion
}