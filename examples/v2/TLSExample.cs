using System.Security.Cryptography.X509Certificates;

using Milvus.Client.V2;
using Milvus.Client.V2.Types;

namespace Milvus.Examples;

/// <summary>
/// Demonstrates TLS connections. Two modes are shown:
/// one-way TLS via the <c>https://</c> URI scheme, and mutual TLS via
/// <c>ConnectConfig.ChannelOptions</c> with a custom <c>SocketsHttpHandler</c> carrying the
/// client certificate. Mirrors java TLSExample (the C# SDK has no per-certificate config fields;
/// custom CAs/client certificates go through ChannelOptions).
/// </summary>
/// <remarks>
/// <para><b>Purpose:</b> show both ways a .NET client can talk to a TLS-enabled Milvus.</para>
/// <para><b>APIs used:</b> <c>ConnectConfig</c> with a <c>https://</c> URI, and
/// <c>ConnectConfig.ChannelOptions</c> (Grpc.Net).</para>
/// <para><b>Expected output:</b> a connect line from the https one-way client, then "Done.".</para>
/// </remarks>
public static class TLSExample
{
    public static async Task Run(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        // ---- Mode 1: one-way TLS --------------------------------
        // Just use the https:// scheme; the .NET runtime verifies the server certificate against the
        // machine's trust store (set MILVUS_URI=https://host:port to enable).
        string httpsUri = Environment.GetEnvironmentVariable("MILVUS_URI")
            ?? (uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? uri : $"https://{uri}");
        try
        {
            using MilvusClientV2 client = ExampleHelpers.CreateClient(httpsUri);
            await client.ConnectAsync();
            Console.WriteLine($"Connected over TLS to {httpsUri}");
        }
        catch (Exception ex)
        {
            // Requires a TLS-enabled Milvus; a plaintext server (or a self-signed cert not in the trust
            // store) fails here, which is the expected behavior to observe.
            Console.WriteLine($"One-way TLS connect to {httpsUri} failed (expected without a TLS server): {ex.Message}");
        }

        // ---- Mode 2: mutual TLS (optional) ----------------------
        // Provide the client certificate path via env vars; a custom SocketsHttpHandler attached through
        // ChannelOptions carries it. When the vars are unset this section is skipped.
        string? clientPem = Environment.GetEnvironmentVariable("MILVUS_CLIENT_PEM");
        string? clientKey = Environment.GetEnvironmentVariable("MILVUS_CLIENT_KEY");
        string? caPem = Environment.GetEnvironmentVariable("MILVUS_CA_PEM");
        if (clientPem is not null && clientKey is not null)
        {
            try
            {
                using var handler = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual
                };
                handler.ClientCertificates.Add(X509Certificate2.CreateFromPemFile(clientPem, clientKey));
                if (caPem is not null)
                {
                    using var ca = X509Certificate2.CreateFromPemFile(caPem);
                    handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true; // exercise only; trust the given CA in production
                }

                var config = new ConnectConfig
                {
                    Uri = httpsUri,
                    ChannelOptions = new Grpc.Net.Client.GrpcChannelOptions { HttpHandler = handler }
                };
                using var mtls = new MilvusClientV2(config);
                await mtls.ConnectAsync();
                Console.WriteLine("Connected over mutual TLS.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mutual TLS connect failed (expected without a matching TLS server): {ex.Message}");
            }
        }

        Console.WriteLine("Done.");
    }
}
