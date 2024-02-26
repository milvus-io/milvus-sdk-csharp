using System.Reflection;
using System.Text;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Milvus.Client.Tests.TestContainer;

public class MilvusBuilder : ContainerBuilder<MilvusBuilder, MilvusContainer, MilvusConfiguration>
{
    public const string MilvusImage = "milvusdb/milvus:v2.3.10"; // TODO: Configurable
    public const ushort MilvusGrpcPort = 19530;
    public const ushort MilvusManagementPort = 9091;

    public MilvusBuilder() : this(new MilvusConfiguration())
        => DockerResourceConfiguration = Init().DockerResourceConfiguration;

    private MilvusBuilder(MilvusConfiguration dockerResourceConfiguration) : base(dockerResourceConfiguration)
        => DockerResourceConfiguration = dockerResourceConfiguration;

    public override MilvusContainer Build()
    {
        Validate();
        return new MilvusContainer(DockerResourceConfiguration, TestcontainersSettings.Logger);
    }

    protected override MilvusBuilder Init()
    {
        string etcdYaml = """
                       listen-client-urls: http://0.0.0.0:2379
                       advertise-client-urls: http://0.0.0.0:2379
                       """;

        return base.Init()
            .WithImage(MilvusImage)
            .WithEnvironment("COMMON_STORAGETYPE", "local")
            .WithEnvironment("ETCD_USE_EMBED", "true")
            .WithEnvironment("ETCD_DATA_DIR", "/var/lib/milvus/etcd")
            .WithEnvironment("ETCD_CONFIG_PATH", "/milvus/configs/embedEtcd.yaml")
            .WithResourceMapping(Encoding.UTF8.GetBytes(etcdYaml), "/milvus/configs/embedEtcd.yaml")
            .WithCommand("milvus", "run", "standalone")
            .WithPortBinding(MilvusGrpcPort, true)
            .WithPortBinding(MilvusManagementPort, true)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(h => h
                        .ForPort(MilvusManagementPort)
                        .ForPath("/healthz")));
    }

    protected override MilvusBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
        => Merge(DockerResourceConfiguration, new MilvusConfiguration(resourceConfiguration));

    protected override MilvusBuilder Merge(MilvusConfiguration oldValue, MilvusConfiguration newValue)
        => new(new MilvusConfiguration(oldValue, newValue));

    protected override MilvusConfiguration DockerResourceConfiguration { get; }

    protected override MilvusBuilder Clone(IContainerConfiguration resourceConfiguration)
        => Merge(DockerResourceConfiguration, new MilvusConfiguration(resourceConfiguration));
}
