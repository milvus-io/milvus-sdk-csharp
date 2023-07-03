using System;
using System.Linq;
using LibGit2Sharp;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.Xunit;
using Nuke.Common.Utilities.Collections;
using Nuke.Components;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Serilog.Log;

partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Push);

    GitHubActions GitHubActions => GitHubActions.Instance;

    string BranchSpec => GitHubActions?.Ref;

    string BuildNumber => GitHubActions?.RunNumber.ToString();

    string PullRequestBase => GitHubActions?.BaseRef;

    [Parameter("The key to push to Nuget")]
    [Secret]
    readonly string NuGetApiKey;

    [Solution(GenerateProjects = true)]
    readonly Solution Solution;

    [GitVersion(Framework = "net7.0")]
    readonly GitVersion GitVersion;

    [GitRepository]
    readonly GitRepository GitRepository;

    AbsolutePath ArtifactsDirectory => RootDirectory / "Artifacts";

    AbsolutePath TestResultsDirectory => RootDirectory / "TestResults";

    string Version;

    Target Clean => _ => _
        .OnlyWhenDynamic(() => RunAllTargets || HasSourceChanges)
        .Executes(() =>
        {
            ArtifactsDirectory.CreateOrCleanDirectory();
            TestResultsDirectory.CreateOrCleanDirectory();
        });

    bool IsPullRequest => GitHubActions?.IsPullRequest ?? false;

    Target Restore => _ => _
        .DependsOn(Clean)
        .OnlyWhenDynamic(() => RunAllTargets || HasSourceChanges)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution)
                .EnableNoCache());
        });

    Target CalculateVersion => _ => _
    .DependsOn(Restore)
    .OnlyWhenDynamic(() => RunAllTargets || HasSourceChanges)
    .Executes(() =>
    {
        Project mainProject = Solution.GetProject("IO.Milvus");
        string projectVersionString = mainProject.GetProperty<string>("Version");
        Information("ProjectVersion: {0}", projectVersionString);
        ProjectVersion projectVersion = ProjectVersion.Parse(projectVersionString);

        Information("GitVersion: {0}", GitVersion.MajorMinorPatch);
        var tag = GitRepository.Tags.LastOrDefault();
        Information("Tag: {0}", tag);

        if (IsTag)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                if (tag.StartsWith('v'))
                {
                    Version = tag.Substring(1, tag.Length - 1);
                }
                else
                {
                    Version = tag;
                }
            }

            if (string.IsNullOrEmpty(Version))
            {
                Version = GitVersion.MajorMinorPatch;
            }
        }
        else
        {
            Version = projectVersionString;
        }

        Information("Version = {0}", Version);
        ReportSummary(s => s.AddPair("NugetVersion", Version));
    });

    Target Compile => _ => _
        .DependsOn(CalculateVersion)
        .OnlyWhenDynamic(() => RunAllTargets || HasSourceChanges)
        .Executes(() =>
        {
            ReportSummary(s => s
                .WhenNotNull(GitVersion, (_, o) => _
                    .AddPair("Version", Version)));

            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration.Release)
                .EnableNoLogo()
                .EnableNoRestore()
                .SetVersion(Version)
                .SetInformationalVersion(Version));
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .OnlyWhenDynamic(() => RunAllTargets || HasSourceChanges)
        .Executes(() =>
        {
            ReportSummary(s => s
                .AddPair("Packed version", Version));

            DotNetPack(s => s
                .SetProcessWorkingDirectory(RootDirectory / "src")
                .SetProject("IO.Milvus")
                .SetOutputDirectory(ArtifactsDirectory)
                .SetConfiguration(Configuration.Release)
                .EnableNoLogo()
                .EnableNoRestore()
                .EnableContinuousIntegrationBuild() // Necessary for deterministic builds
                .SetVersion(Version));
        });

    Target Push => _ => _
        .DependsOn(Pack)
        .OnlyWhenDynamic(() => IsTag)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            var packages = ArtifactsDirectory.GlobFiles("*.nupkg");

            Assert.NotEmpty(packages);

            DotNetNuGetPush(s => s
                .SetApiKey(NuGetApiKey)
                .EnableSkipDuplicate()
                .SetSource("https://api.nuget.org/v3/index.json")
                .EnableNoSymbols()
                .CombineWith(packages,
                    (v, path) => v.SetTargetPath(path)));
        });

    static bool IsDocumentation(string x) =>
        x.StartsWith("docs") ||
        x.StartsWith("CONTRIBUTING.md") ||
        x.StartsWith("LICENSE") ||
        x.StartsWith("package.json") ||
        x.StartsWith("README.md");

    string[] Changes =>
    Repository.Diff
        .Compare<TreeChanges>(TargetBranch, SourceBranch)
        .Where(x => x.Exists)
        .Select(x => x.Path)
        .ToArray();

    bool HasSourceChanges => Changes.Any(x => !IsDocumentation(x));

    Repository Repository => new(GitRepository.LocalDirectory);

    Tree TargetBranch => Repository.Branches[PullRequestBase].Tip.Tree;

    Tree SourceBranch => Repository.Branches[Repository.Head.FriendlyName].Tip.Tree;

    bool RunAllTargets => true;

    bool IsTag => BranchSpec != null && BranchSpec.Contains("refs/tags", StringComparison.OrdinalIgnoreCase);
}