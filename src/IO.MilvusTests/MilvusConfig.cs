using IO.Milvus.Client;
using IO.Milvus.Client.gRPC;
using IO.Milvus.Client.REST;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IO.MilvusTests;

public enum MilvusConnectionType
{
    [JsonPropertyName("rest")]
    Rest,

    [JsonPropertyName("grpc")]
    Grpc,
}

public sealed class MilvusConfig
{
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("type")]
    public string? ConnectionType { get; set; }

    [JsonIgnore]
    public string? Name { get; private set; }

    [JsonPropertyName("skip")]
    public bool Skip { get; set; }

    public static IEnumerable<MilvusConfig> Load()
    {
        string? dir = Path.GetDirectoryName(typeof(MilvusConfig).Assembly.Location);

        if (string.IsNullOrWhiteSpace(dir))
        {
            yield break;
        }

        var files = Directory.GetFiles(
            dir, 
            "milvusclient*.json", 
            SearchOption.TopDirectoryOnly);

        bool exist = false;
        foreach (var file in files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string data = File.ReadAllText(file);

            var config = JsonSerializer.Deserialize<MilvusConfig>(data);

            if (config == null || config.Skip)
            {
                continue;
            }

            exist = true;
            config.Name = name;
            yield return config;
        }

        if (!exist)
        {
            throw new Exception("Milvus server not found, please config your test server and remove {skip: true}");
        }
    }
}

internal static class MilvusConfigExtensions
{
    public static IMilvusClient CreateClient(this MilvusConfig config)
    {
        if (string.Compare(config.ConnectionType,"rest",true) == 0)
        {
            return new MilvusRestClient(config.Endpoint, config.Port);
        }
        else if(string.Compare(config.ConnectionType, "grpc", true) == 0)
        {
            return new MilvusGrpcClient(config.Endpoint, config.Port,config.Username,config.Password);
        }
        else
        {
            throw new NotSupportedException($"Connection type {config.ConnectionType} is not supported.");
        }
    }
}