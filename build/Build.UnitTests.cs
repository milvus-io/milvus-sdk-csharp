using System.IO;
using Nuke.Common;
using Serilog;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using System.Threading;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Nuke.Common.IO;
using System.Diagnostics;
using Nuke.Common.Tooling;

partial class Build
{
    const string MilvusYmlName = "milvus-standalone-docker-compose.yml";

    [Parameter]
    string MilvusVersion = "v2.2.10";

    string MilvusYmlFileAddress => $"https://github.com/milvus-io/milvus/releases/download/{MilvusVersion}/milvus-standalone-docker-compose.yml";

    Target DownloadYml => _ => _ 
        .DependsOn(Compile)
        .Executes(async() =>{
            var file = new FileInfo(MilvusYmlName);
            if(file.Exists)
            {
                file.Delete();
            }

            Log.Information(MilvusYmlFileAddress);
            await DownloadFileAsync(MilvusYmlFileAddress, MilvusYmlName);

            file = new FileInfo(MilvusYmlName);
            if(file.Exists){                
                Log.Information(file.FullName);
            }
            else{
                Log.Error("Failed download milvus yml file");
            }
        });

    Target ComposeUp => _ => _
        .DependsOn(DownloadYml)        
        .Executes(() =>{
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker-compose",
                Arguments = $"-f {MilvusYmlName} up --build"
             });
            
            //Waiting milvus is ready
            Thread.Sleep(TimeSpan.FromSeconds(60));
        });

    AbsolutePath TestDir => RootDirectory / "src" / "IO.MilvusTests" / "bin" / "Release" / "net7.0" / "milvusclients.json";

    Target Test => _ => _
        .DependsOn(ComposeUp)
        .Produces(TestResultsDirectory / "*.trx")
        .Executes(() => DotNetTest(s => s
            .SetProjectFile(Solution)
            .SetConfiguration(Configuration.Release)
            .EnableNoBuild()
            .When(true, _ => _
                .SetLoggers("trx")
                .SetResultsDirectory(TestResultsDirectory))));

    Target ComposeDown => _ => _
        .AssuredAfterFailure()
        .TriggeredBy(Test)
        .After(Test)
        .Executes(() => Process.Start(new ProcessStartInfo
        {
            FileName = "docker-compose",
            Arguments = $"-f {MilvusYmlName} down"
        }));

    private static async Task DownloadFileAsync(string url, string fileName){
        var uri = new Uri(url);
        using var client = new HttpClient();
        var response = await client.GetAsync(uri);
        using var fs = new FileStream(
            fileName,
            FileMode.CreateNew);
        await response.Content.CopyToAsync(fs);
    }
}
