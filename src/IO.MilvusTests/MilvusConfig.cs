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

    [JsonPropertyName("apikey")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("type")]
    public string? ConnectionType { get; set; }

    [JsonPropertyName("skip")]
    public bool Skip { get; set; }

    public static IEnumerable<MilvusConfig> Load()
    {
        string? dir = Path.GetDirectoryName(typeof(MilvusConfig).Assembly.Location);

        if (string.IsNullOrWhiteSpace(dir))
        {
            throw new DirectoryNotFoundException();
        }

        var file = Path.Combine(
            dir, 
            "milvusclients.json");

        if (!File.Exists(file))
        {
            throw new FileNotFoundException(file);
        }

        var configs = JsonSerializer.Deserialize<IList<MilvusConfig>>(File.ReadAllText(file));

        if (configs == null)
        {
            throw new NullReferenceException("Cannot load milvusclients");
        }

        return configs.Where(p => !p.Skip);
    }

    public override string ToString() => $"{Endpoint}:{Port}";
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
            if (!string.IsNullOrWhiteSpace(config.ApiKey))
            {
                return new MilvusGrpcClient(config.Endpoint, config.ApiKey);
            }
            else
            {
                return new MilvusGrpcClient(config.Endpoint, config.Port,config.Username,config.Password);
            }
        }
        else
        {
            throw new NotSupportedException($"Connection type {config.ConnectionType} is not supported.");
        }
    }
}