using Google.Protobuf.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IO.Milvus.Utils;

internal static class SchemaConverter
{
    internal static Grpc.CollectionSchema ConvertCollectionSchema(
        this CollectionSchema collectionSchema)
    {
        Grpc.CollectionSchema grpcCollectionSchema = new Grpc.CollectionSchema()
        {
            Name = collectionSchema.Name,
            AutoID = collectionSchema.AutoId,
            EnableDynamicField = collectionSchema.EnableDynamicField,
        };
        if (!string.IsNullOrEmpty(collectionSchema.Description))
        {
            grpcCollectionSchema.Description = collectionSchema.Description;
        }

        foreach (FieldType field in collectionSchema.Fields)
        {
            grpcCollectionSchema.Fields.Add(ConvertFieldSchema(field));
        }

        grpcCollectionSchema.AutoID = collectionSchema.Fields.Any(static p => p.AutoId);

        return grpcCollectionSchema;
    }

    internal static CollectionSchema ToCollectionSchema(this Grpc.CollectionSchema collectionSchema)
    {
        CollectionSchema milvusCollectionSchema = new()
        {
            Name = collectionSchema.Name,
            Description = collectionSchema.Description,
            AutoId = collectionSchema.AutoID,
            Fields = new List<FieldType>()
        };

        foreach (Grpc.FieldSchema field in collectionSchema.Fields)
        {
            milvusCollectionSchema.Fields.Add(ToFieldSchema(field));
        }

        return milvusCollectionSchema;
    }

    private static FieldType ToFieldSchema(Grpc.FieldSchema fieldType)
    {
        FieldType milvusField = new(fieldType.Name, (MilvusDataType)fieldType.DataType, fieldType.IsPrimaryKey, fieldType.IsDynamic)
        {
            FieldId = fieldType.FieldID,
        };

        ToParams(milvusField.TypeParams, fieldType.TypeParams);
        ToParams(milvusField.IndexParams, fieldType.IndexParams);

        return milvusField;
    }

    private static void ToParams(
        IDictionary<string, string> typeParams,
        RepeatedField<Grpc.KeyValuePair> indexParams)
    {
        if (indexParams == null)
        {
            return;
        }
        foreach (Grpc.KeyValuePair parameter in indexParams)
        {
            typeParams[parameter.Key] = parameter.Value;
        }
    }

    internal static Grpc.FieldSchema ConvertFieldSchema(FieldType fieldType)
    {
        Grpc.FieldSchema grpcField = new()
        {
            Name = fieldType.Name,
            DataType = ((Grpc.DataType)(int)fieldType.DataType),
            FieldID = fieldType.FieldId,
            IsPrimaryKey = fieldType.IsPrimaryKey,
            AutoID = fieldType.AutoId,
        };

        grpcField.TypeParams.AddRange(ConverterParams(fieldType.TypeParams));
        grpcField.IndexParams.AddRange(ConverterParams(fieldType.IndexParams));

        return grpcField;
    }

    internal static IEnumerable<Grpc.KeyValuePair> ConverterParams(
        IEnumerable<KeyValuePair<string, string>> indexParams)
    {
        if (indexParams == null)
        {
            yield break;
        }
        foreach (KeyValuePair<string, string> parameter in indexParams)
        {
            yield return new Grpc.KeyValuePair()
            {
                Key = parameter.Key,
                Value = parameter.Value
            };
        }
    }

    internal static Dictionary<string, IList<int>> ToKeyDataPairs(
        this IEnumerable<Grpc.KeyDataPair> keyDataPairs)
    {
        Dictionary<string, IList<int>> dictionary = new();

        foreach (Grpc.KeyDataPair keyDataPair in keyDataPairs)
        {
            dictionary[keyDataPair.Key] = keyDataPair.Data.Select(static p => (int)p).ToList();
        }

        return dictionary;
    }
}
