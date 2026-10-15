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

## SDKs

Two generations of the SDK live in this repository:

| SDK | Assembly | Status |
| --- | --- | --- |
| **V1** — `Milvus.Client` | `Milvus.Client.dll` | Legacy, maintained; [NuGet](https://www.nuget.org/packages/Milvus.Client/) |
| **V2** — `Milvus.Client.V2` | `Milvus.Client.V2.dll` | Current development, DTO-based API aligned with the Java/C++/Rust V2 SDKs |

V1 supports the Milvus versions listed below; V2 targets the 2.6 API surface.

**Supported Net versions:**
* .NET Core 2.1+
* .NET Framework 4.6.1+

**[NuGet](https://www.nuget.org/packages/Milvus.Client/)**

Milvus.Client (V1) is delivered via NuGet package manager.

| Nuget version | Branch | Description | Milvus version
| --- | --- | --- | --- |
| v2.2.2 | main | Support grpc only | 2.2.x |
| v2.2.1 | main | Support restfulapi and grpc[**Obsolete**] | 2.2.x |
| v2.2.0 | 2.2 | Support grpc only[**Obsolete**] | 2.2.x |

## Docs 📚

* [V1 Quick Start](./docs/v1/readme.md)
* [V2 design](./docs/design/v2_design.md)
* [Milvus docs](https://milvus.io/docs)

### Jupyter Notebooks 📙

You can find Jupyter notebooks in the [docs/v1/notebooks](./docs/v1/notebooks) folder.

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/milvus-io/milvus-sdk-csharp)

* [00.Settings.ipynb](./docs/v1/notebooks/00.Settings.ipynb)
* [01.Connect to milvus.ipynb](./docs/v1/notebooks/01.Connect%20to%20milvus.ipynb)
* [02.Create a Collection.ipynb](./docs/v1/notebooks/02.Create%20a%20Collection.ipynb)
* [03.Create a Partition.ipynb](./docs/v1/notebooks/03.Create%20a%20Partition.ipynb)
* [04.Insert Vectors.ipynb](./docs/v1/notebooks/04.Insert%20Vectors.ipynb)
* [05.Build an Index on Vectors.ipynb](./docs/v1/notebooks/05.Build%20an%20Index%20on%20Vectors.ipynb)
* [06.Search.ipynb](./docs/v1/notebooks/06.Search.ipynb)
* [07.Query.ipynb](./docs/v1/notebooks/07.Query.ipynb)

> Requirements: C# notebooks require .NET 7 and the VS Code Polyglot extension.
