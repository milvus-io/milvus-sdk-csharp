using Milvus.Client.V2.Types;
using Milvus.Client.V2.Utils;

namespace Milvus.Client.V2.Requests.Dml;

/// <summary>
/// Represents a request to upsert (insert-or-update) rows into a collection.
/// </summary>
public sealed class UpsertReq
{
    /// <summary>
    /// The name of the collection to upsert into.
    /// </summary>
    public string CollectionName { get; set; } = "";

    /// <summary>
    /// The field data to upsert; each field contains one value per row.
    /// </summary>
    public IReadOnlyList<FieldData> Data { get; set; } = Array.Empty<FieldData>();

    /// <summary>
    /// An optional partition to upsert into.
    /// </summary>
    public string? PartitionName { get; set; }

    internal Grpc.UpsertRequest ToGrpcUpsertRequest()
    {
        Verify.NotNullOrWhiteSpace(CollectionName);
        Verify.NotNullOrEmpty(Data);

        var request = new Grpc.UpsertRequest
        {
            CollectionName = CollectionName,
            PartitionName = PartitionName ?? "",
            NumRows = (uint)Data[0].RowCount
        };

        // Dynamic fields are aggregated into a single JSON metadata field (mirroring V1).
        Dictionary<string, object?>?[]? dynamicFieldsData = null;
        int rowCount = (int)request.NumRows;

        foreach (FieldData field in Data)
        {
            if (field.IsDynamic)
            {
                dynamicFieldsData ??= new Dictionary<string, object?>[rowCount];
                for (int rowNum = 0; rowNum < rowCount; rowNum++)
                {
                    Dictionary<string, object?> rowDynamicData =
                        dynamicFieldsData[rowNum] ?? (dynamicFieldsData[rowNum] = new Dictionary<string, object?>());
                    rowDynamicData[field.FieldName] = field.GetValueAsObject(rowNum);
                }
            }
            else
            {
                request.FieldsData.Add(field.ToGrpcFieldData());
            }
        }

        if (dynamicFieldsData is not null)
        {
            var encodedJson = new string[rowCount];
            for (int rowNum = 0; rowNum < rowCount; rowNum++)
            {
                encodedJson[rowNum] = System.Text.Json.JsonSerializer.Serialize(dynamicFieldsData[rowNum]);
            }

            request.FieldsData.Add(FieldData.CreateDynamicJson(encodedJson).ToGrpcFieldData());
        }

        return request;
    }
}
