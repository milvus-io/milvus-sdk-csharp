using System.Runtime.CompilerServices;

using Grpc.Core;

using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Responses.Utility;

/// <summary>
/// The result of a dump-messages operation, consumed as an <see cref="IAsyncEnumerable{T}" /> of
/// <see cref="DumpMessageInfo" />.
/// </summary>
public sealed class DumpMessagesResp : IAsyncEnumerable<DumpMessageInfo>
{
    private readonly Func<CancellationToken, IAsyncEnumerable<DumpMessageInfo>> _messages;

    internal DumpMessagesResp(Func<CancellationToken, IAsyncEnumerable<DumpMessageInfo>> messages)
    {
        _messages = messages;
    }

    /// <inheritdoc />
    public IAsyncEnumerator<DumpMessageInfo> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
        => _messages(cancellationToken).GetAsyncEnumerator(cancellationToken);
}

/// <summary>
/// Executes the server-streaming dump-messages RPC, converting the raw responses into
/// <see cref="DumpMessageInfo" /> values.
/// </summary>
internal static class DumpMessagesReader
{
    public static async IAsyncEnumerable<DumpMessageInfo> ReadAsync(
        MilvusClientV2 client,
        Grpc.DumpMessagesRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await client.EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        using AsyncServerStreamingCall<Grpc.DumpMessagesResponse> call =
            client.GrpcClient.DumpMessages(request, client.CallOptionsForStreaming(cancellationToken));

        var enumerator = call.ResponseStream.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            bool hasNext;
            try
            {
                hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
            }
            catch (RpcException ex)
            {
                // Grpc.Core surfaces a caller-supplied cancellation on a streaming call as
                // StatusCode.Cancelled; surface it as OperationCanceledException so cancellation is not
                // mistaken for a transport failure (DumpMessagesReq.EndTimetick==0 streams until cancelled).
                // A CANCELLED status with the caller's token not cancelled is a server/transport abort, so it
                // must not be masked as client cancellation -- wrap it like any other RpcException.
                if (ex.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("The dump-messages stream was cancelled.", ex, cancellationToken);
                }

                // A mid-stream transport failure (including a server-initiated CANCELLED when the caller's
                // token is not cancelled) surfaces as a raw RpcException; wrap it into MilvusException so
                // await-foreach consumers get the same uniform error surface as every other facade method.
                throw new MilvusException(
                    MilvusErrorCode.UnexpectedError, $"RPC failed: {ex.StatusCode} {ex.Status.Detail}",
                    ex.StatusCode, ex);
            }

            if (!hasNext)
            {
                yield break;
            }

            Grpc.DumpMessagesResponse response = enumerator.Current;
            if (response.ResponseCase == Grpc.DumpMessagesResponse.ResponseOneofCase.Status)
            {
                var code = (MilvusErrorCode)response.Status.Code;
                if (code != MilvusErrorCode.Success)
                {
                    throw new MilvusException(code, response.Status.Reason);
                }

                continue;
            }

            if (response.Message is not null)
            {
                yield return DumpMessageInfo.FromGrpc(response.Message);
            }
        }
    }
}
