namespace Milvus.Client.Tests;

public static class Utils
{
    public static async Task<Version> GetParsedMilvusVersion(this MilvusClient client)
    {
        string version = await client.GetVersionAsync();

        if (version.StartsWith("v", StringComparison.Ordinal))
        {
            version = version[1..];
        }

        int dash = version.IndexOf('-');
        if (dash != -1)
        {
            version = version[..dash];
        }

        return Version.Parse(version);
    }
}
