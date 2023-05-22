FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["IO.MilvusTests/IO.MilvusTests.csproj", "IO.MilvusTests/"]
COPY ["IO.Milvus/IO.Milvus.csproj", "IO.Milvus/"]
RUN dotnet restore "IO.MilvusTests/IO.MilvusTests.csproj"
RUN dotnet restore "IO.Milvus/IO.Milvus.csproj"
COPY . .
WORKDIR "/src/IO.MilvusTests"
RUN dotnet build "IO.MilvusTests.csproj" --framework net7.0 -c Release

# Run the tests
CMD ["dotnet", "test", "--configuration", "Release", "--no-restore", "--framework", "net7.0"]

