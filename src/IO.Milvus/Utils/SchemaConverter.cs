using IO.Milvus.ApiSchema;
using System.Collections.Generic;

namespace IO.Milvus.Utils;

internal static class SchemaConverter
{
    internal static Grpc.CollectionSchema ConvertCollectionSchema(
        this CollectionSchema collectionSchema)
    {
        Grpc.CollectionSchema grpcCollectionSchema = new Grpc.CollectionSchema()
        {
            Name = collectionSchema.Name,
            Description = collectionSchema.Description,
            AutoID = collectionSchema.AutoId,
        };

        foreach (var field in collectionSchema.Fields)
        {
            grpcCollectionSchema.Fields.Add(ConvertFieldSchema(field));
        }

        return grpcCollectionSchema;
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
        foreach (var parameter in indexParams)
        {
            yield return new Grpc.KeyValuePair()
            {
                Key = parameter.Key,
                Value = parameter.Value
            };
        }
    }
}
