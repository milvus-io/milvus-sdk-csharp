using System.Diagnostics;
using Milvus.Client.V2.Types;
using System.Text;

namespace Milvus.Client.V2.Tests;

/// <summary>
/// Manages the lifecycle of the Milvus container used by the system tests, by invoking the
/// <c>milvus_container.py</c> helper script (mirroring the C++ SDK test harness).
/// </summary>
internal sealed class MilvusTestContainer : IAsyncDisposable
{
    private readonly string _scriptPath;
    private readonly string _containerId;

    private MilvusTestContainer(string scriptPath, string containerId, int grpcPort)
    {
        _scriptPath = scriptPath;
        _containerId = containerId;
        GrpcPort = grpcPort;
    }

    /// <summary>
    /// The host to use when connecting to the container.
    /// </summary>
    public string Host => "localhost";

    /// <summary>
    /// The gRPC port the container published on the host.
    /// </summary>
    public int GrpcPort { get; }

    /// <summary>
    /// Starts the Milvus container (plus its MinIO sidecar) and waits until it is ready.
    /// </summary>
    public static async Task<MilvusTestContainer> StartAsync()
    {
        string scriptPath = FindScript();
        int grpcPort = GetIntEnv("MILVUS_GRPC_PORT", 29630);
        int healthPort = GetIntEnv("MILVUS_HEALTH_PORT", 19191);
        int minioPort = GetIntEnv("MILVUS_MINIO_PORT", 19100);
        string image = Environment.GetEnvironmentVariable("MILVUS_IMAGE") ?? string.Empty;

        List<string> args =
        [
            scriptPath,
            "start",
            "--grpc-port", grpcPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--health-port", healthPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--minio-port", minioPort.ToString(System.Globalization.CultureInfo.InvariantCulture)
        ];

        if (!string.IsNullOrWhiteSpace(image))
        {
            args.Add("--image");
            args.Add(image);
        }

        string output = await RunAsync("python3", args, scriptPath);

        string containerId = output.Trim();
        if (containerId.Length == 0)
        {
            throw new InvalidOperationException($"milvus_container.py start returned no container ID. Output: {output}");
        }

        Console.WriteLine($"Milvus test container started: {containerId} (grpc port {grpcPort})");
        return new MilvusTestContainer(scriptPath, containerId, grpcPort);
    }

    public MilvusClientV2 CreateClient()
        => new(new ConnectConfig { Uri = $"{Host}:{GrpcPort}" });

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine($"Stopping Milvus test container: {_containerId[..Math.Min(12, _containerId.Length)]}");
        await RunAsync("python3", [_scriptPath, "stop", _containerId], _scriptPath);
    }

    private static string FindScript()
    {
        string? envPath = Environment.GetEnvironmentVariable("MILVUS_CONTAINER_SCRIPT");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return envPath;
        }

        string local = Path.Combine(AppContext.BaseDirectory, "milvus_container.py");
        if (File.Exists(local))
        {
            return local;
        }

        throw new FileNotFoundException(
            "milvus_container.py not found. Set the MILVUS_CONTAINER_SCRIPT environment variable.", "milvus_container.py");
    }

    private static int GetIntEnv(string name, int defaultValue)
        => int.TryParse(Environment.GetEnvironmentVariable(name), out int value) ? value : defaultValue;

    private static async Task<string> RunAsync(string program, IReadOnlyList<string> args, string? scriptPath = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = program,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = scriptPath is null
                ? AppContext.BaseDirectory
                : Path.GetDirectoryName(Path.GetFullPath(scriptPath)) ?? AppContext.BaseDirectory,
        };

        foreach (string arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start '{program}'.");

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();

        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);

        string stdout = await stdoutTask.ConfigureAwait(false);
        string stderr = await stderrTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"'{program} {string.Join(' ', args)}' exited with code {process.ExitCode}.\n{stderr}");
        }

        return stdout;
    }
}
