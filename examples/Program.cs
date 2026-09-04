namespace Milvus.Examples;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        string example = args.Length > 0 ? args[0] : "SimpleExample";

        // The URI defaults to localhost:19530; override with MILVUS_URI / MILVUS_TOKEN env vars.
        string uri = Environment.GetEnvironmentVariable("MILVUS_URI") ?? "localhost:19530";

        var examples = new Dictionary<string, Func<string, Task>>
        {
            [nameof(SimpleExample)] = SimpleExample.Run,
            [nameof(GeneralExample)] = GeneralExample.Run,
            [nameof(AddFieldExample)] = AddFieldExample.Run,
            [nameof(ArrayFieldExample)] = ArrayFieldExample.Run,
            [nameof(JsonFieldExample)] = JsonFieldExample.Run,
            [nameof(SparseVectorExample)] = SparseVectorExample.Run,
            [nameof(Float16VectorExample)] = Float16VectorExample.Run,
            [nameof(Int8VectorExample)] = Int8VectorExample.Run,
            [nameof(BinaryVectorExample)] = BinaryVectorExample.Run,
            [nameof(UpsertExample)] = UpsertExample.Run,
            [nameof(GroupByExample)] = GroupByExample.Run,
            [nameof(RBACExample)] = RBACExample.Run,
            [nameof(PartitionKeyExample)] = PartitionKeyExample.Run,
            [nameof(DynamicFieldExample)] = DynamicFieldExample.Run,
            [nameof(NullableFieldExample)] = NullableFieldExample.Run,
            [nameof(ConsistencyLevelExample)] = ConsistencyLevelExample.Run,
            [nameof(RunAnalyzerExample)] = RunAnalyzerExample.Run,
            [nameof(AliasExample)] = AliasExample.Run
        };

        if (!examples.TryGetValue(example, out Func<string, Task>? runner))
        {
            Console.WriteLine($"Unknown example '{example}'. Available: {string.Join(", ", examples.Keys.OrderBy(x => x))}");
            return 1;
        }

        try
        {
            await runner(uri);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Example '{example}' failed: {ex}");
            return 1;
        }
    }
}
