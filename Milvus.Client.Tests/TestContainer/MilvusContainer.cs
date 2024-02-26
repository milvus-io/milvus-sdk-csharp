using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;

namespace Milvus.Client.Tests.TestContainer;

public class MilvusContainer(MilvusConfiguration configuration, ILogger logger)
    : DockerContainer(configuration, logger);
