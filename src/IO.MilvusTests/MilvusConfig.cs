using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

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