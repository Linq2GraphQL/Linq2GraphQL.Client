using System.Text.Json;

namespace Linq2GraphQL.Compat;

internal static class BaselineRunner
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 1)
        {
            await Console.Error.WriteLineAsync("Usage: Linq2GraphQL.Compat.Baseline <output file>");
            return 2;
        }

        var records = await CompatRunner.RunAllAsync();
        await File.WriteAllTextAsync(args[0], JsonSerializer.Serialize(records));
        return 0;
    }
}
