using System.IO;
using Nuke.Common;
using Serilog;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using System.Threading;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using IO.MilvusTests;
using System.Collections.Generic;
using Nuke.Common.IO;
using System.Text.Json;
using System.Diagnostics;
using Nuke.Common.Tooling;

partial class Build
{
    const string MilvusYmlName = "milvus-standalone-docker-compose.yml";

    [Parameter]
    string MilvusYmlFileAddress = "https://github.com/milvus-io/milvus/releases/download/v2.2.10/milvus-standalone-docker-compose.yml";

    Target DownloadYml => _ => _ 
        .DependsOn(Compile)
        .Executes(async() =>{
            var file = new FileInfo(MilvusYmlName);
            if(file.Exists)
            {
                file.Delete();
            }

            await DownloadFileAsync(MilvusYmlFileAddress, MilvusYmlName);

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
            var process = Process.Start(new ProcessStartInfo(){
                FileName = "docker-compose",
                Arguments = $"-f {MilvusYmlName} up"
             });
            
            //Waiting milvus is ready
            Thread.Sleep(TimeSpan.FromSeconds(20));
        });

    AbsolutePath TestDir => RootDirectory / "src" / "IO.MilvusTests" / "bin" / "Release" / "net7.0" / "milvusclients.json";

    Target Test => _ => _        
        .DependsOn(ComposeUp)
        .Executes(() =>{
            //Generate milvus client config file
            List<MilvusConfig> configs = new List<MilvusConfig>{
                new MilvusConfig(){
                    Endpoint = "http://localhost",
                    Port = 19530,
                    ConnectionType = "grpc" ,
                    Username = "root",
                    Password = "milvus"
                },
                new MilvusConfig(){
                    Endpoint = "http://localhost",
                    Port = 9091,
                    ConnectionType = "rest" ,
                    Username = "root",
                    Password = "milvus"
                },
            };

            string configText = JsonSerializer.Serialize(configs);
            Log.Logger.Information(configText);
            TestDir.WriteAllText(configText);

            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration.Release)
                .EnableNoBuild()                
                .When(true, _ => _
                    .SetLoggers("trx")
                    .SetResultsDirectory(TestResultsDirectory)));
        });

    Target ComposeDown => _ => _
        .AssuredAfterFailure()
        .TriggeredBy(Test)
        .After(Test)
        .Executes(() =>{
            var process = Process.Start(new ProcessStartInfo(){
                FileName = "docker-compose",
                Arguments = $"-f {MilvusYmlName} down"
             });
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
