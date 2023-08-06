# How to use in .net framework

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
            MilvusClient client = new Milvus.Client.MilvusClient(
                "{Endpoint}",
                {Port},
                "{Usename}",
                "Password",
                grpcChannel:CreateChannel($"{Endpoint}:{Port}")
            );
        }
```

[More details](https://learn.microsoft.com/en-us/aspnet/core/grpc/netstandard?view=aspnetcore-7.0)
