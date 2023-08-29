# milvus-sdk-csharp

<div class="column" align="middle">
  <a href="https://milvusio.slack.com/archives/C053HTUQGUC"><img src="https://img.shields.io/badge/Join-Slack-orange?logo=slack&amp;logoColor=white&style=flat-square"></a>
  <img src="https://img.shields.io/nuget/v/milvus.client"/>
  <img src="https://img.shields.io/nuget/dt/milvus.client"/>
</div>

<div align="middle">
    <img src="milvussharp.png"/>
</div>

C# SDK for [Milvus](https://github.com/milvus-io/milvus).

**Supported Net versions:**
* .NET Core 2.1+
* .NET Framework 4.6.1+

**[NuGet](https://www.nuget.org/packages/Milvus.Client/)**

Milvus.Client is delivered via NuGet package manager.

| Nuget version | Branch | Description | Milvus version
| --- | --- | --- | --- |
| v2.2.2 | main | Support grpc only | 2.2.x |
| v2.2.1 | main | Support restfulapi and grpc[**Obsolete**] | 2.2.x |
| v2.2.0 | 2.2 | Support grpc only[**Obsolete**] | 2.2.x |

## Docs 📚

* [Quick Start](./docs/readme.md)
* [Milvus docs](https://milvus.io/docs)

### Jupyter Notebooks 📙

You can find Jupyter notebooks in the [docs/notebooks](./docs/notebooks) folder.

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/milvus-io/milvus-sdk-csharp)

* [00.Settings.ipynb](./docs/notebooks/00.Settings.ipynb)
* [01.Connect to milvus.ipynb](./docs/notebooks/01.Connect%20to%20milvus.ipynb)
* [02.Create a Collection.ipynb](./docs/notebooks/02.Create%20a%20Collection.ipynb)
* [03.Create a Partition.ipynb](./docs/notebooks/03.Create%20a%20Partition.ipynb)
* [04.Insert Vectors.ipynb](./docs/notebooks/04.Insert%20Vectors.ipynb)
* [05.Build an Index on Vectors.ipynb](./docs/notebooks/05.Build%20an%20Index%20on%20Vectors.ipynb)
* [06.Search.ipynb](./docs/notebooks/06.Search.ipynb)
* [07.Query.ipynb](./docs/notebooks/07.Query.ipynb)

> Requirements: C# notebooks require .NET 7 and the VS Code Polyglot extension.
