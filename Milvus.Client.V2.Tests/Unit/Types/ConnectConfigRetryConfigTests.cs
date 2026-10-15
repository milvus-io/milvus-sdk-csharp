using Grpc.Net.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using Milvus.Client.V2.Types;

namespace Milvus.Client.V2.Tests.Unit.Types;

[Trait("Category", "Unit")]
public class ConnectConfigRetryConfigTests
{
    [Fact]
    public void ConnectConfig_has_expected_defaults()
    {
        var config = new ConnectConfig();

        Assert.Equal("", config.Uri);
        Assert.Null(config.Username);
        Assert.Null(config.Password);
        Assert.Null(config.ApiKey);
        Assert.Null(config.Database);
        Assert.Null(config.ConnectTimeout);
        Assert.Null(config.KeepAliveTime);
        Assert.Null(config.KeepAliveTimeout);
        Assert.Null(config.KeepAliveWithoutCalls);
        Assert.Null(config.LoggerFactory);
        Assert.Null(config.ChannelOptions);
        Assert.Null(config.Retry);
    }

    [Fact]
    public void ConnectConfig_properties_are_settable()
    {
        var retry = new RetryConfig { MaxRetryTimes = 5 };
        var config = new ConnectConfig
        {
            Uri = "http://localhost:19530",
            Username = "user",
            Password = "pass",
            ApiKey = "key",
            Database = "default",
            ConnectTimeout = TimeSpan.FromSeconds(30),
            KeepAliveTime = TimeSpan.FromSeconds(20),
            KeepAliveTimeout = TimeSpan.FromSeconds(10),
            KeepAliveWithoutCalls = false,
            LoggerFactory = NullLoggerFactory.Instance,
            ChannelOptions = new GrpcChannelOptions(),
            Retry = retry
        };

        Assert.Equal("http://localhost:19530", config.Uri);
        Assert.Equal("user", config.Username);
        Assert.Equal("pass", config.Password);
        Assert.Equal("key", config.ApiKey);
        Assert.Equal("default", config.Database);
        Assert.Equal(TimeSpan.FromSeconds(30), config.ConnectTimeout);
        Assert.Equal(TimeSpan.FromSeconds(20), config.KeepAliveTime);
        Assert.Equal(TimeSpan.FromSeconds(10), config.KeepAliveTimeout);
        Assert.False(config.KeepAliveWithoutCalls);
        Assert.Same(NullLoggerFactory.Instance, config.LoggerFactory);
        Assert.NotNull(config.ChannelOptions);
        Assert.Same(retry, config.Retry);
    }

    [Fact]
    public void RetryConfig_has_expected_defaults()
    {
        var config = new RetryConfig();

        Assert.Equal(75, config.MaxRetryTimes);
        Assert.Equal(TimeSpan.FromMilliseconds(10), config.InitialBackOff);
        Assert.Equal(TimeSpan.FromSeconds(3), config.MaxBackOff);
        Assert.Equal(3, config.BackOffMultiplier);
        Assert.True(config.RetryOnRateLimit);
        Assert.Null(config.MaxRetryTimeout);
    }

    [Fact]
    public void RetryConfig_properties_are_settable()
    {
        var config = new RetryConfig
        {
            MaxRetryTimes = 10,
            InitialBackOff = TimeSpan.FromMilliseconds(50),
            MaxBackOff = TimeSpan.FromSeconds(5),
            BackOffMultiplier = 2,
            RetryOnRateLimit = false,
            MaxRetryTimeout = TimeSpan.FromMinutes(1)
        };

        Assert.Equal(10, config.MaxRetryTimes);
        Assert.Equal(TimeSpan.FromMilliseconds(50), config.InitialBackOff);
        Assert.Equal(TimeSpan.FromSeconds(5), config.MaxBackOff);
        Assert.Equal(2, config.BackOffMultiplier);
        Assert.False(config.RetryOnRateLimit);
        Assert.Equal(TimeSpan.FromMinutes(1), config.MaxRetryTimeout);
    }
}
