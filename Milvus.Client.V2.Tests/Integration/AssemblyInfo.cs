using Xunit;

// The integration tests share process-wide singletons (SchemaCache, CollectionTsCache) that are
// mutated across tests (DescribeSchema setup, ts-cache clear/set), so they must not run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
