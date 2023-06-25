using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Nuke.Common.Tooling;
using Serilog;

namespace Nuke.DockerCompose;

[Serializable]
public class DockerComposeSettings : ToolSettings
{
    public override string ProcessToolPath => DockerComposeTasks.DockerPath;

    internal List<string> FileInternal;
    public IReadOnlyCollection<string> File => FileInternal.AsReadOnly();

    protected override Arguments ConfigureProcessArguments(Arguments arguments)
    {
        arguments.Add("--file {value}", File);
        return base.ConfigureProcessArguments(arguments);
    }
}

public static class DockerComposeSettingsExtensions
{
    [Pure]
    public static T SetFile<T>(this T settings, params string[] files) where T : DockerComposeSettings =>
        SetFile(settings, (IEnumerable<string>)files);

    [Pure]
    public static T SetFile<T>(this T settings, IEnumerable<string> files)
        where T : DockerComposeSettings
    {
        settings = settings.NewInstance();
        settings.FileInternal = files.ToList();
        return settings;
    }
}

[Serializable]
public class DockerComposeUpSettings : DockerComposeSettings
{
    public bool Detach { get; internal set; }

    public DockerComposeUpSettings SetDetach(bool detach)
    {
        Detach = detach;
        return this;
    }

    protected override Arguments ConfigureProcessArguments(Arguments arguments)
    {
        arguments = base.ConfigureProcessArguments(arguments);
        arguments.Add("up")
            .Add("--detach", Detach);
        return arguments;
    }
}

[Serializable]
public class DockerComposeDownSettings : DockerComposeSettings
{
    protected override Arguments ConfigureProcessArguments(Arguments arguments)
    {
        arguments = base.ConfigureProcessArguments(arguments);
        arguments.Add("down");
        return arguments;
    }
}

[Serializable]
public class DockerComposeLogsSettings : DockerComposeSettings
{
    public bool Follow { get; internal set; }

    public DockerComposeLogsSettings SetFollow(bool follow)
    {
        Follow = follow;
        return this;
    }

    protected override Arguments ConfigureProcessArguments(Arguments arguments)
    {
        arguments = base.ConfigureProcessArguments(arguments);
        arguments.Add("logs")
            .Add("--follow", Follow);
        return arguments;
    }
}

public static class DockerComposeTasks
{
    internal static string DockerPath => ToolPathResolver.GetPathExecutable("docker-compose");

    internal static void CustomLogger(OutputType type, string output)
    {
        switch (type)
        {
            case OutputType.Std:
                Log.Information(output);
                break;
            case OutputType.Err:
                {
                    if (output.StartsWith("WARNING!"))
                        Log.Warning(output);
                    else
                        Log.Information(output);
                    //TODO: logging real errors
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public static IReadOnlyCollection<Output> Up(Configure<DockerComposeUpSettings> configure) =>
        Up(configure(new DockerComposeUpSettings()));

    public static IReadOnlyCollection<Output> Up(DockerComposeUpSettings settings = null) =>
        StartProcess(settings ?? new DockerComposeUpSettings());

    public static IReadOnlyCollection<Output> Down(Configure<DockerComposeDownSettings> configure) =>
        Down(configure(new DockerComposeDownSettings()));

    public static IReadOnlyCollection<Output> Down(DockerComposeDownSettings settings = null) =>
        StartProcess(settings ?? new DockerComposeDownSettings());

    public static IReadOnlyCollection<Output> Logs(Configure<DockerComposeLogsSettings> configure) =>
        Logs(configure(new DockerComposeLogsSettings()));

    public static IReadOnlyCollection<Output> Logs(DockerComposeLogsSettings settings = null) =>
        StartProcess(settings ?? new DockerComposeLogsSettings());

    private static IReadOnlyCollection<Output> StartProcess([NotNull] ToolSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        var process = ProcessTasks.StartProcess(settings);
        process.AssertWaitForExit();
        return process.Output;
    }
}