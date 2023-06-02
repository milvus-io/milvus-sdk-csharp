using IO.Milvus.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IO.Milvus;

/// <summary>
/// Converter fields_data to <see cref="IList{Field}"/>.
/// </summary>
public class MilvusFieldConverter : JsonConverter<IList<Field>>
{
    ///<inheritdoc/>
    public override IList<Field> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = new List<Field>();
        DeserializePropertyList<Field>(ref reader,list);
        return list;
    }

    ///<inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IList<Field> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    private static void DeserializePropertyList<T>(ref Utf8JsonReader reader, IList<T> list)
    {
        Verify.NotNull(list, nameof(list));

        if (reader.TokenType == JsonTokenType.PropertyName) reader.Read();

        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();
        if (reader.TokenType == JsonTokenType.StartObject) throw new JsonException();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (TryCastValue(ref reader, typeof(T), out Object item))
            {
                list.Add((T)item);
            }

            System.Diagnostics.Debug.Assert(reader.TokenType != JsonTokenType.StartArray);
            System.Diagnostics.Debug.Assert(reader.TokenType != JsonTokenType.StartObject);
            System.Diagnostics.Debug.Assert(reader.TokenType != JsonTokenType.PropertyName);
        }

        if (list.Count == 0) throw new JsonException("Empty array found.");

        System.Diagnostics.Debug.Assert(reader.TokenType == JsonTokenType.EndArray);
    }

    private static bool TryCastValue(ref Utf8JsonReader reader, Type vtype, out Object value)
    {
        value = null;

        if (reader.TokenType == JsonTokenType.EndArray) return false;
        if (reader.TokenType == JsonTokenType.EndObject) return false;
        // if (reader.TokenType == JsonToken.EndConstructor) return false;

        if (reader.TokenType == JsonTokenType.PropertyName) reader.Read();

        // untangle nullable
        var ntype = Nullable.GetUnderlyingType(vtype);
        if (ntype != null) vtype = ntype;

        if (vtype == typeof(String)) { value = reader.GetString(); return true; }
        if (vtype == typeof(Boolean)) { value = reader.GetBoolean(); return true; }
        if (vtype == typeof(SByte)) { value = reader.GetInt16(); return true; }
        if (vtype == typeof(Int16)) { value = reader.GetInt16(); return true; }
        if (vtype == typeof(Int32)) { value = reader.GetInt32(); return true; }
        if (vtype == typeof(Int64)) { value = reader.GetInt64(); return true; }
        if (vtype == typeof(UInt16)) { value = reader.GetUInt16(); return true; }
        if (vtype == typeof(UInt32)) { value = reader.GetUInt32(); return true; }
        if (vtype == typeof(UInt64)) { value = reader.GetUInt64(); return true; }
        if (vtype == typeof(Single)) { value = reader.GetSingle(); return true; }
        if (vtype == typeof(Double)) { value = reader.GetDouble(); return true; }
        if (vtype == typeof(Decimal)) { value = reader.GetDecimal(); return true; }

        if (vtype == typeof(Field))
        {
            value = DeserializeField(ref reader);
            return true;
        }

        throw new NotImplementedException($"Can't deserialize {vtype}");
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

        reader.Read();
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            var name = reader.GetString();

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
                        var fieldTypeName = reader.GetString();

                        switch (fieldTypeName)
                        {
                            case "Scalars": 
                                {
                                    reader.Read();reader.Read();
                                    reader.Read();reader.Read();
                                    var scalarTypeName = reader.GetString();
                                    reader.Read();reader.Read();
                                    switch (scalarTypeName)
                                    {
                                        case "BoolData":
                                            DeserializePropertyList<bool>(ref reader,boolData);                                            
                                            break;
                                        case "BytesData":
                                            DeserializePropertyList<byte>(ref reader,bytesData);
                                            break;
                                        case "IntData":
                                            DeserializePropertyList<int>(ref reader,intData);
                                            break;
                                        case "FloatData":
                                            DeserializePropertyList<float>(ref reader,floatData);
                                            break;
                                        case "DoubleData":
                                            DeserializePropertyList<double>(ref reader,doubleData);
                                            break;
                                        case "StringData":
                                            DeserializePropertyList<string>(ref reader,stringData);
                                            break;
                                        case "LongData":
                                            DeserializePropertyList<long>(ref reader,longData);
                                            break;
                                        default:
                                            throw new JsonException($"Unexpected property {scalarTypeName}");
                                    }
                                    reader.Read(); reader.Read();
                                    reader.Read(); reader.Read();
                                }
                                break;
                            default:
                                break;
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
                field = Field.Create<bool>(fieldName, boolData);
                break;
            case MilvusDataType.Int8:
                field = Field.Create<sbyte>(fieldName, intData.Select(s => (sbyte)s).ToList());
                break;
            case MilvusDataType.Int16:
                field = Field.Create<Int16>(fieldName, intData.Select(s => (Int16)s).ToList());;
                break;
            case MilvusDataType.Int32:
                field = Field.Create<int>(fieldName, intData);
                break;
            case MilvusDataType.Int64:
                field = Field.Create<long>(fieldName, longData);
                break;
            case MilvusDataType.Float:
                field = Field.Create<float>(fieldName, floatData);
                break;
            case MilvusDataType.Double:
                field = Field.Create<double>(fieldName, doubleData);
                break;
            case MilvusDataType.VarChar:
                field = Field.CreateVarChar(fieldName, stringData);
                break;
            case MilvusDataType.BinaryVector:
            case MilvusDataType.FloatVector:
            case MilvusDataType.String:
            default:
                throw new JsonException($"Unexpected milvus datatype {dataType}");
        }

        field.FieldId = fieldId;

        return field;
    }

    private static Object DeserializeUnknownObject(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.PropertyName) reader.Read();

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<Object>();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                list.Add(DeserializeUnknownObject(ref reader));
            }

            return list;
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var dict = new Dictionary<String, Object>();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var key = reader.GetString();

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
    public static Object GetAnyValue(this in Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null: return null;
            case JsonTokenType.True: return true;
            case JsonTokenType.False: return false;
            case JsonTokenType.String: return reader.GetString();
            case JsonTokenType.Number: return reader.GetDecimal();
            case JsonTokenType.PropertyName: return reader.GetString();
            default: throw new NotImplementedException();
        }
    }
}