using System.Runtime.CompilerServices;

namespace Linq2GraphQL.Tests;

/// <summary>
/// Compares text against a checked-in snapshot under Snapshots/. A missing snapshot is written and the test
/// fails, so new output is always reviewed. Set LINQ2GRAPHQL_UPDATE_SNAPSHOTS=1 to rewrite changed snapshots.
/// </summary>
internal static class Snapshot
{
    public static void Match(string actual, string name, [CallerFilePath] string callerPath = "")
    {
        var path = Path.Combine(Path.GetDirectoryName(callerPath)!, "Snapshots", name + ".txt");
        var update = Environment.GetEnvironmentVariable("LINQ2GRAPHQL_UPDATE_SNAPSHOTS") == "1";
        actual = actual.ReplaceLineEndings("\n");

        if (!File.Exists(path) || update)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var existing = File.Exists(path) ? File.ReadAllText(path).ReplaceLineEndings("\n") : null;
            File.WriteAllText(path, actual);
            if (existing == null)
            {
                Assert.Fail($"Snapshot {name} did not exist and was written to {path}. Review it and run again.");
            }

            return;
        }

        Assert.Equal(File.ReadAllText(path).ReplaceLineEndings("\n"), actual);
    }
}
