using FluentAssertions;
using FluentAssertions.Execution;
using IO.Milvus.Grpc;
using IO.Milvus.Param;
using Status = IO.Milvus.Param.Status;

namespace IO.MilvusTests.Utils;

public static class AssertExtensions
{
    public static void Assert(this R<MutationResult> r)
    {
        using (new AssertionScope())
        {
            r.Exception?.Message.Should().BeNull();
            r.Exception.Should().BeNull();
            r.Status.Should().Be(Status.Success);

            r.Data.SuccIndex.Count.Should().BeGreaterThan(0);
        }
    }

    public static void Assert(this R<QueryResults> r)
    {
        r.Exception?.Message.Should().BeNull();
        r.Exception.Should().BeNull();
        r.Status.Should().Be(Status.Success);
    }

    public static void AssertRBool(this R<bool> r)
    {
        r.Should().NotBeNull();

        r.Status.Should().Be(Status.Success);
        r.Exception?.ToString().Should().BeEmpty();
        r.Data.Should().BeTrue();
    }

    public static void AssertRpcStatus(this R<RpcStatus> r)
    {
        using (new AssertionScope())
        {
            r.Should().NotBeNull();

            r.Status.Should().Be(Status.Success);
            r.Exception?.ToString().Should().BeEmpty();

            r.Data.Msg.Should().Be(RpcStatus.SUCCESS_MSG);
        }
    }

    public static void ShouldSuccess<T>(this R<T> r)
    {
        using (new AssertionScope())
        {
            r.Should().NotBeNull();
            r.Exception?.ToString().Should().BeEmpty();
            r.Status.Should().Be(Status.Success);
        }
    }
}