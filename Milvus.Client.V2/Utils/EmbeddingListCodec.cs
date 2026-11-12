using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Utils;

/// <summary>
/// Encodes the embedding lists of a search (regular or hybrid) into a single placeholder value, keeping the
/// type detection, validation and wire packing in one place so SearchReq and AnnSearchReq cannot drift.
/// </summary>
internal static class EmbeddingListCodec
{
    /// <summary>
    /// Packs <paramref name="embeddingLists" /> into one placeholder value. All lists must share one vector
    /// type (float/binary/int8), each list must contain at least one non-zero-length vector, and all vectors
    /// within a list — and across all lists — must share the same dimension; the placeholder type is derived
    /// from the lists.
    /// </summary>
    internal static Grpc.PlaceholderValue EncodeEmbeddingListPlaceholder(IReadOnlyList<EmbeddingList> embeddingLists)
    {
        Grpc.PlaceholderType placeholderType = Grpc.PlaceholderType.EmbListFloatVector;
        foreach (EmbeddingList embeddingList in embeddingLists)
        {
            if (embeddingList.BinaryVectors.Count > 0)
            {
                placeholderType = Grpc.PlaceholderType.EmbListBinaryVector;
                break;
            }

            if (embeddingList.Int8Vectors.Count > 0)
            {
                placeholderType = Grpc.PlaceholderType.EmbListInt8Vector;
                break;
            }

            if (embeddingList.Vectors.Count > 0)
            {
                placeholderType = Grpc.PlaceholderType.EmbListFloatVector;
                break;
            }
        }

        // All embedding lists must share the same vector type: the placeholder type is derived from the first
        // list and applied to every list, so a mixed request would silently mis-encode (a list of the wrong type
        // contributes an empty placeholder value). A single list must also not mix vector types (the encode
        // switch packs only one category, silently dropping the others). An empty list (no vectors of any type)
        // is rejected too: the encoder would still emit an empty placeholder value and count it towards nq.
        // All lists must also share the same vector dimension (see the per-list check below).
        int? uniformLength = null;
        foreach (EmbeddingList embeddingList in embeddingLists)
        {
            int nonEmptyCollections = (embeddingList.Vectors.Count > 0 ? 1 : 0)
                + (embeddingList.BinaryVectors.Count > 0 ? 1 : 0)
                + (embeddingList.Int8Vectors.Count > 0 ? 1 : 0);
            if (nonEmptyCollections == 0)
            {
                throw new ArgumentException(
                    "An embedding list must contain at least one vector.", nameof(embeddingLists));
            }

            if (nonEmptyCollections > 1)
            {
                throw new ArgumentException(
                    "An embedding list cannot mix float, binary and int8 vectors; each list must carry " +
                    "exactly one vector type.", nameof(embeddingLists));
            }

            Grpc.PlaceholderType listType = embeddingList.BinaryVectors.Count > 0
                ? Grpc.PlaceholderType.EmbListBinaryVector
                : embeddingList.Int8Vectors.Count > 0
                    ? Grpc.PlaceholderType.EmbListInt8Vector
                    : Grpc.PlaceholderType.EmbListFloatVector;

            if (listType != placeholderType)
            {
                throw new ArgumentException(
                    "All embedding lists in a search must carry the same vector type; a list cannot mix " +
                    "float, binary and int8 vectors.", nameof(embeddingLists));
            }

            // Each list is flattened into one placeholder value that the server splits by the schema
            // dimension, so all vectors within one list — and across all lists in one search — must share
            // the same (non-zero) size; otherwise the payload is silently mis-split (or rejected opaquely
            // by the server). A zero-length vector is rejected like an empty list.
            foreach (ReadOnlyMemory<float> vector in embeddingList.Vectors)
            {
                CheckVectorLength(vector.Length, nameof(embeddingLists), ref uniformLength);
            }

            foreach (ReadOnlyMemory<byte> vector in embeddingList.BinaryVectors)
            {
                CheckVectorLength(vector.Length, nameof(embeddingLists), ref uniformLength);
            }

            foreach (ReadOnlyMemory<sbyte> vector in embeddingList.Int8Vectors)
            {
                CheckVectorLength(vector.Length, nameof(embeddingLists), ref uniformLength);
            }
        }

        var embListPlaceholder = new Grpc.PlaceholderValue { Tag = "$0", Type = placeholderType };
        foreach (EmbeddingList embeddingList in embeddingLists)
        {
            switch (placeholderType)
            {
                case Grpc.PlaceholderType.EmbListFloatVector:
                {
                    int byteCount = 0;
                    foreach (ReadOnlyMemory<float> vector in embeddingList.Vectors)
                    {
                        byteCount += vector.Length * sizeof(float);
                    }

                    var bytes = new byte[byteCount];
                    int offset = 0;
                    foreach (ReadOnlyMemory<float> vector in embeddingList.Vectors)
                    {
                        foreach (float value in vector.Span)
                        {
                            byte[] raw = BitConverter.GetBytes(value);
                            bytes[offset++] = BitConverter.IsLittleEndian ? raw[0] : raw[3];
                            bytes[offset++] = BitConverter.IsLittleEndian ? raw[1] : raw[2];
                            bytes[offset++] = BitConverter.IsLittleEndian ? raw[2] : raw[1];
                            bytes[offset++] = BitConverter.IsLittleEndian ? raw[3] : raw[0];
                        }
                    }

                    embListPlaceholder.Values.Add(ByteString.CopyFrom(bytes));
                    break;
                }

                case Grpc.PlaceholderType.EmbListBinaryVector:
                    // One placeholder value per embedding list, holding all the list's binary vectors' bytes
                    // concatenated (matching the float branch and pymilvus's flattened encoding).
                    var binaryBytes = new List<byte>();
                    foreach (ReadOnlyMemory<byte> vector in embeddingList.BinaryVectors)
                    {
                        binaryBytes.AddRange(vector.ToArray());
                    }

                    embListPlaceholder.Values.Add(ByteString.CopyFrom(binaryBytes.ToArray()));
                    break;

                case Grpc.PlaceholderType.EmbListInt8Vector:
                    // One placeholder value per embedding list, holding all the list's int8 vectors.
                    var int8Bytes = new List<byte>();
                    foreach (ReadOnlyMemory<sbyte> vector in embeddingList.Int8Vectors)
                    {
                        foreach (sbyte value in vector.Span)
                        {
                            int8Bytes.Add(unchecked((byte)value));
                        }
                    }

                    embListPlaceholder.Values.Add(ByteString.CopyFrom(int8Bytes.ToArray()));
                    break;

                default:
                    throw new NotSupportedException($"Unsupported embedding list placeholder type {placeholderType}");
            }
        }

        return embListPlaceholder;
    }

    private static void CheckVectorLength(int length, string paramName, ref int? running)
    {
        if (length == 0)
        {
            throw new ArgumentException(
                "An embedding list cannot contain zero-length vectors.", paramName);
        }

        if (running is null)
        {
            running = length;
        }
        else if (length != running)
        {
            throw new ArgumentException(
                "All vectors in one embedding list — and across all lists in one search — must share the " +
                "same dimension.", paramName);
        }
    }
}
