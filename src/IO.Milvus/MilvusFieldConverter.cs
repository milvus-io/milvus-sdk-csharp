using IO.Milvus.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IO.Milvus;

/// <summary>
/// Converter fields_data to <see cref="IList{Field}"/>.
/// </summary>
public sealed class MilvusFieldConverter : JsonConverter<IList<Field>>
{
    /// <inheritdoc />
    public override IList<Field> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        List<Field> list = new();
        DeserializePropertyList(ref reader, list);
        return list;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IList<Field> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    private static void DeserializePropertyList<T>(ref Utf8JsonReader reader, IList<T> list)
    {
        Verify.NotNull(list);

        if (reader.TokenType == JsonTokenType.PropertyName) reader.Read();

        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (TryCastValue(ref reader, out T item))
            {
                list.Add(item);
            }

            System.Diagnostics.Debug.Assert(reader.TokenType != JsonTokenType.StartArray);
            System.Diagnostics.Debug.Assert(reader.TokenType != JsonTokenType.StartObject);
            System.Diagnostics.Debug.Assert(reader.TokenType != JsonTokenType.PropertyName);
        }

        if (list.Count == 0) throw new JsonException("Empty array found.");

        System.Diagnostics.Debug.Assert(reader.TokenType == JsonTokenType.EndArray);
    }

    private static bool TryCastValue<T>(ref Utf8JsonReader reader, out T value)
    {
        if (reader.TokenType is JsonTokenType.EndArray or JsonTokenType.EndObject)
        {
            value = default;
            return false;
        }

        if (reader.TokenType == JsonTokenType.PropertyName)
        {
            reader.Read();
        }

        if (typeof(T) == typeof(string)) { value = (T)(object)reader.GetString(); return true; }
        if (typeof(T) == typeof(bool)) { value = (T)(object)reader.GetBoolean(); return true; }
        if (typeof(T) == typeof(sbyte)) { value = (T)(object)reader.GetInt16(); return true; }
        if (typeof(T) == typeof(short)) { value = (T)(object)reader.GetInt16(); return true; }
        if (typeof(T) == typeof(int)) { value = (T)(object)reader.GetInt32(); return true; }
        if (typeof(T) == typeof(long)) { value = (T)(object)reader.GetInt64(); return true; }
        if (typeof(T) == typeof(ushort)) { value = (T)(object)reader.GetUInt16(); return true; }
        if (typeof(T) == typeof(uint)) { value = (T)(object)reader.GetUInt32(); return true; }
        if (typeof(T) == typeof(ulong)) { value = (T)(object)reader.GetUInt64(); return true; }
        if (typeof(T) == typeof(float)) { value = (T)(object)reader.GetSingle(); return true; }
        if (typeof(T) == typeof(double)) { value = (T)(object)reader.GetDouble(); return true; }
        if (typeof(T) == typeof(decimal)) { value = (T)(object)reader.GetDecimal(); return true; }
        if (typeof(T) == typeof(byte)) { value = (T)(object)reader.GetByte(); return true; }

        if (typeof(T) == typeof(Field))
        {
            value = (T)(object)DeserializeField(ref reader);
            return true;
        }

        throw new NotImplementedException($"Can't deserialize {typeof(T)}");
    }

    private static Field DeserializeField(ref Utf8JsonReader reader)
    {
        Field field = default;

        string fieldName = default;
        MilvusDataType dataType = default;
        long fieldId = default;

        List<bool> boolData = new();
        List<byte> bytesData = new();

        List<int> intData = new();
        List<long> longData = new();
        List<float> floatData = new();
        List<double> doubleData = new();
        List<string> stringData = new();

        long dim = default;
        List<float> floatVector = new();
        List<byte> binaryVector = new();

        reader.Read();
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            string name = reader.GetString();

            reader.Read();

            switch (name)
            {
                case "field_name":
                    fieldName = reader.GetString();
                    break;
                case "type":
                    dataType = (MilvusDataType)reader.GetInt32();
                    break;
                case "field_id":
                    fieldId = reader.GetInt64();
                    break;
                case "Field":
                    {
                        reader.Read();
                        string fieldTypeName = reader.GetString();

                        switch (fieldTypeName)
                        {
                            case "Scalars":
                                {
                                    reader.Read(); reader.Read();
                                    reader.Read(); reader.Read();
                                    string scalarTypeName = reader.GetString();
                                    reader.Read(); reader.Read(); reader.Read();
                                    if (reader.TokenType != JsonTokenType.StartArray)
                                    {
                                        reader.Read(); reader.Read();
                                        break;
                                    };
                                    switch (scalarTypeName)
                                    {
                                        case "BoolData":
                                            DeserializePropertyList(ref reader, boolData);
                                            break;
                                        case "BytesData":
                                            DeserializePropertyList(ref reader, bytesData);
                                            break;
                                        case "IntData":
                                            DeserializePropertyList(ref reader, intData);
                                            break;
                                        case "FloatData":
                                            DeserializePropertyList(ref reader, floatData);
                                            break;
                                        case "DoubleData":
                                            DeserializePropertyList(ref reader, doubleData);
                                            break;
                                        case "StringData":
                                            DeserializePropertyList(ref reader, stringData);
                                            break;
                                        case "LongData":
                                            DeserializePropertyList(ref reader, longData);
                                            break;
                                        default:
                                            throw new JsonException($"Unexpected property {scalarTypeName}");
                                    }
                                    reader.Read(); reader.Read();
                                    reader.Read(); reader.Read();
                                }
                                break;
                            case "Vectors":
                                {
                                    reader.Read(); reader.Read();
                                    dim = DeserializeVector(ref reader, floatVector, binaryVector);
                                    reader.Read();
                                }
                                break;
                            default:
                                throw new JsonException($"Unexpected property {fieldTypeName}");
                        }
                    }
                    break;
                default:
                    throw new JsonException($"Unexpected property {name}");
            }

            reader.Read();
        }

        switch (dataType)
        {
            case MilvusDataType.Bool:
                field = Field.Create(fieldName, boolData);
                break;
            case MilvusDataType.Int8:
                field = Field.Create(fieldName, intData.Select(static s => (sbyte)s).ToList());
                break;
            case MilvusDataType.Int16:
                field = Field.Create(fieldName, intData.Select(static s => (short)s).ToList()); ;
                break;
            case MilvusDataType.Int32:
                field = Field.Create(fieldName, intData);
                break;
            case MilvusDataType.Int64:
                field = Field.Create(fieldName, longData);
                break;
            case MilvusDataType.Float:
                field = Field.Create(fieldName, floatData);
                break;
            case MilvusDataType.Double:
                field = Field.Create(fieldName, doubleData);
                break;
            case MilvusDataType.VarChar:
                field = Field.CreateVarChar(fieldName, stringData);
                break;
            case MilvusDataType.BinaryVector:
                field = Field.CreateFromBytes(
                    fieldName,
#if NET6_0_OR_GREATER
                    CollectionsMarshal.AsSpan(binaryVector),
#else
                    binaryVector.ToArray(),
#endif
                    dim);
                break;
            case MilvusDataType.FloatVector:
                field = Field.CreateFloatVector(fieldName, floatVector, dim);
                break;
            case MilvusDataType.String:
            default:
                throw new JsonException($"Unexpected milvus datatype {dataType}");
        }

        field.FieldId = fieldId;

        return field;
    }

    private static long DeserializeVector(ref Utf8JsonReader reader, List<float> floatVector, List<byte> binaryVector)
    {
        long dim = default;
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            string name = reader.GetString();

            reader.Read();

            switch (name)
            {
                case "dim":
                    dim = reader.GetInt64();
                    break;
                case "Data":
                    {
                        reader.Read();

                        string dataType = reader.GetString();
                        reader.Read(); reader.Read(); reader.Read();
                        if (reader.TokenType != JsonTokenType.StartArray)
                        {
                            break;
                        };
                        switch (dataType)
                        {
                            case "FloatVector":
                                {
                                    DeserializePropertyList(ref reader, floatVector);
                                }
                                break;
                            case "BinaryVector":
                                {
                                    DeserializePropertyList(ref reader, binaryVector);
                                }
                                break;
                            default:
                                throw new JsonException($"Not support: {dataType}");
                        }
                        reader.Read(); reader.Read();
                    }
                    break;

                default:
                    throw new JsonException($"Not support: {name}");
            }

            reader.Read();
        }

        return dim;
    }

    private static object DeserializeUnknownObject(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.PropertyName) reader.Read();

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            List<object> list = new();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                list.Add(DeserializeUnknownObject(ref reader));
            }

            return list;
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            Dictionary<string, object> dict = new();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string key = reader.GetString();

                    dict[key] = DeserializeUnknownObject(ref reader);
                }
                else
                {
                    throw new JsonException();
                }
            }

            return dict;
        }

        System.Diagnostics.Debug.Assert(reader.TokenType != JsonTokenType.None);
        System.Diagnostics.Debug.Assert(reader.TokenType != JsonTokenType.EndArray);
        System.Diagnostics.Debug.Assert(reader.TokenType != JsonTokenType.EndObject);

        return reader.GetAnyValue();
    }
}

internal static class JsonConverterExtension
{
    public static object GetAnyValue(this in Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetDecimal(),
            JsonTokenType.PropertyName => reader.GetString(),
            _ => throw new NotImplementedException(),
        };
    }
}