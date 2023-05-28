# milvus-sdk-csharp

<div class="column" align="middle">
  <a href="https://milvusio.slack.com/archives/C053HTUQGUC"><img src="https://img.shields.io/badge/Join-Slack-orange?logo=slack&amp;logoColor=white&style=flat-square"></a>
  <img src="https://img.shields.io/nuget/v/io.milvus"/>
  <img src="https://img.shields.io/nuget/dt/io.milvus"/>
</div>

<div align="middle">
    <img src="milvussharp.png"/>
</div>

C# SDK for [Milvus](https://github.com/milvus-io/milvus).

## Getting Started

**Visual Studio**

Visual Studio 2019  or higher

**Supported Net versions:**
* .NET Core 2.1
* .NET Framework 4.6.1
* Mono 5.4
* Xamarin.iOS 10.14
* Xamarin.Android 8.0
* Universal Windows Platform 10.0.16299
* Unity 2018.1

**NuGet**

IO.Milvus is delivered via NuGet package manager. You can find the package here:
https://www.nuget.org/packages/IO.Milvus/

*There are currently two implementations, one based on grpc on branch 2.2, and the other based on restfulapi and grpc.*

### Jupyter Notebooks 📙

You can find Jupyter notebooks in the [docs/notebooks](./docs/notebooks) folder.

* [00.Settings.ipynb](./docs/notebooks/00.Settings.ipynb)
* [01.Connect to milvus.ipynb](./docs/notebooks/01.Connect%20to%20milvus.ipynb)
* [02.Create a Collection.ipynb](./docs/notebooks/02.Create%20a%20Collection.ipynb)
* [03.Create a Partition.ipynb](./docs/notebooks/03.Create%20a%20Partition.ipynb)
* [04.Insert Vectors.ipynb](./docs/notebooks/04.Insert%20Vectors.ipynb)
* [05.Build an Index on Vectors.ipynb](./docs/notebooks/05.Build%20an%20Index%20on%20Vectors.ipynb)
* [06.Search.ipynb](./docs/notebooks/06.Search.ipynb)
* [07.Query.ipynb](./docs/notebooks/07.Query.ipynb)
* [08.Calculate Distance.ipynb](./docs/notebooks/08.Calculate%20Distance.ipynb)

> Requirements: C# notebooks require .NET 7 and the VS Code Polyglot extension.


### How to use in .net framework

* Windows 11 or later, Windows Server 2022 or later.
* A reference to System.Net.Http.WinHttpHandler version 6.0.1 or later.
* Configure WinHttpHandler on the channel using GrpcChannelOptions.HttpHandler.

```c#
        public GrpcChannel CreateChannel(string address)
        {
#if NET461_OR_GREATER
            return GrpcChannel.ForAddress(address,new GrpcChannelOptions
            {
                HttpHandler = new WinHttpHandler()
            });
#else
            return GrpcChannel.ForAddress(address);
#endif
        }

        public MilvusServiceClient CreateClient()
        {
            IMilvusClient client = new IO.Milvus.Client.gRPC.MilvusGrpcClient(
                "{Endpoint}",
                {Port},
                "{Usename}",
                "Password",
                grpcChannel:CreateChannel($"{Endpoint}:{Port}")
            );
        }
```

[More details](https://learn.microsoft.com/en-us/aspnet/core/grpc/netstandard?view=aspnetcore-7.0)