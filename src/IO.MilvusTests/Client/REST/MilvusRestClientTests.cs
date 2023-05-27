﻿using IO.MilvusTests;
using Xunit;

namespace IO.Milvus.Client.REST.Tests;

public class MilvusRestClientTests
{
    [Fact]
    public void MilvusRestClientTest()
    {
        var client = new MilvusRestClient(HostConfig.Host, HostConfig.RestPort);
        Assert.NotNull(client);
    }
}