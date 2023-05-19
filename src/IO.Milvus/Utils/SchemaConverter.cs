using Google.Protobuf.Collections;
using IO.Milvus.ApiSchema;
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
        };
        if (!string.IsNullOrEmpty(collectionSchema.Description))
        {
            grpcCollectionSchema.Description = collectionSchema.Description;
        }

        foreach (var field in collectionSchema.Fields)
        {
            grpcCollectionSchema.Fields.Add(ConvertFieldSchema(field));
        }

        return grpcCollectionSchema;
    }

    internal static CollectionSchema ToCollectioSchema(this Grpc.CollectionSchema collectionSchema)
    {
        var milvusCollectionSchema = new CollectionSchema()
        {
            Name = collectionSchema.Name,
            Description = collectionSchema.Description,
            AutoId = collectionSchema.AutoID,
        };

        foreach (var field in collectionSchema.Fields)
        {
            milvusCollectionSchema.Fields.Add(ToFieldSchema(field));
        }

        return milvusCollectionSchema;
    }

    private static FieldType ToFieldSchema(Grpc.FieldSchema fieldType)
    {
        var milvusField = new FieldType(fieldType.Name,(MilvusDataType)fieldType.DataType,fieldType.IsPrimaryKey)
        {
            FieldId = fieldType.FieldID,
        };

        ToParams(milvusField.TypeParams, fieldType.TypeParams);
        ToParams(milvusField.IndexParams, fieldType.IndexParams);

        return milvusField;
    }

    private static void ToParams(
        Dictionary<string, string> typeParams, 
        RepeatedField<Grpc.KeyValuePair> indexParams)
    {
        if (indexParams == null)
        {
            return;
        }
        foreach (var parameter in indexParams)
        {
            typeParams[parameter.Key] = parameter.Value;
        }
    }

    internal static Grpc.FieldSchema ConvertFieldSchema(FieldType fieldType)
    {
        var grpcField = new Grpc.FieldSchema()
        {
            Name = fieldType.Name,
            DataType = ((Grpc.DataType)(int)fieldType.DataType),
            FieldID = fieldType.FieldId,
            IsPrimaryKey = fieldType.IsPrimaryKey,
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
        foreach (var parameter in indexParams)
        {
            yield return new Grpc.KeyValuePair()
            {
                Key = parameter.Key,
                Value = parameter.Value
            };
        }
    }

    internal static Dictionary<string,IList<int>> ToKeyDataPairs(
        this IEnumerable<Grpc.KeyDataPair> keyDataPairs)
    {
        Dictionary<string, IList<int>> dictionary = new();

        foreach (var keyDataPair in keyDataPairs)
        {
            dictionary[keyDataPair.Key] = keyDataPair.Data.Select(p => (int)p).ToList();
        }

        return dictionary;
    }
}
