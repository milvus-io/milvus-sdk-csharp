using System.IO;
using Nuke.Common;
using Nuke.Common.Tools.Docker;
using Serilog;
using Nuke.DockerCompose;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using System.Threading;
using System;
using System.Net.Http;
using System.Threading.Tasks;

partial class Build
{
    const string MilvusYmlName = "milvus-standalone-docker-compose.yml";

    [Parameter]
    string MilvusYmlFileAddress = "https://github.com/milvus-io/milvus/releases/download/v2.2.10/milvus-standalone-docker-compose.yml";

    Target DownloadYml => _ => _ 
        .DependsOn(Compile)
        .Executes(async() =>{
            await DownloadFileAsync(MilvusYmlFileAddress, MilvusYmlName);

            var file = new FileInfo(MilvusYmlName);
            if(file.Exists){                
                Log.Information(file.FullName);
            }
            else{
                Log.Error("Failed download milvus yml file");
            }
        });

    Target ComposeUp => _ => _
        .TriggeredBy(DownloadYml)
        .Executes(() =>{
            DockerComposeTasks.Up(s => s.SetFile(MilvusYmlName));
        });

    Target RunUnitTests => _ => _
        .TriggeredBy(DownloadYml)
        .Executes(() =>{
            //Generate milvus client config file

            //Waiting milvus is ready
            Thread.Sleep(TimeSpan.FromSeconds(20));

            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration.Release)
                .EnableNoBuild()
                .SetResultsDirectory(TestResultsDirectory));
        });

    Target ComposeDown => _ => _
        .TriggeredBy(RunUnitTests)
        .Executes(() =>{
            DockerComposeTasks.Down(s => s.SetFile(MilvusYmlName));
        });

    private async Task DownloadFileAsync(string url, string fileName){
        var uri = new Uri(url);
        using var client = new HttpClient();
        var response = await client.GetAsync(uri);
        using var fs = new FileStream(
            fileName,
            FileMode.CreateNew);
        await response.Content.CopyToAsync(fs);
    }
}
