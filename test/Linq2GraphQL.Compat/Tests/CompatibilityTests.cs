using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Xunit;

namespace Linq2GraphQL.Compat;

/// <summary>
/// An app that updates the runtime packages and keeps the client the baseline release generated must behave as
/// before: every scenario must send the same requests, and return or throw the same, as on the baseline packages.
/// A deliberate change goes into accepted-differences.json, keyed by scenario, with the reason.
/// </summary>
public class CompatibilityTests(CompatibilityRuns runs) : IClassFixture<CompatibilityRuns>
{
    public static TheoryData<string> Scenarios => new(CompatScenarios.All.Select(scenario => scenario.Name));

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Scenario_BehavesAsOnTheBaseline(string scenario)
    {
        var baseline = runs.Baseline[scenario];
        var current = runs.Current[scenario];

        if (runs.AcceptedDifferences.ContainsKey(scenario))
        {
            Assert.True(baseline != current,
                $"{scenario} is in accepted-differences.json but behaves as on the baseline again. Remove it.");
            return;
        }

        if (baseline != current)
        {
            Assert.Fail($"""
                {scenario} behaves differently than on the baseline packages. If that is intended, add it to
                accepted-differences.json with the reason. Every record is in {runs.OutputDirectory}.

                Baseline:
                {baseline}

                Current:
                {current}
                """);
        }
    }

    [Fact]
    public void AcceptedDifferences_AreScenarios()
    {
        Assert.Empty(runs.AcceptedDifferences.Keys.Except(runs.Current.Keys));
    }
}

/// <summary>Runs the scenarios once on this code and once, in its own process, on the baseline packages.</summary>
public sealed class CompatibilityRuns : IAsyncLifetime
{
    private static readonly TimeSpan BaselineTimeout = TimeSpan.FromMinutes(5);

    public IReadOnlyDictionary<string, string> Baseline { get; private set; }

    public IReadOnlyDictionary<string, string> Current { get; private set; }

    public IReadOnlyDictionary<string, string> AcceptedDifferences { get; } = ReadAcceptedDifferences();

    public string OutputDirectory { get; } = AppContext.BaseDirectory;

    public async Task InitializeAsync()
    {
        var baseline = RunBaselineAsync(Path.Combine(OutputDirectory, "compat-baseline.json"));
        Current = await CompatRunner.RunAllAsync();
        await File.WriteAllTextAsync(Path.Combine(OutputDirectory, "compat-current.json"),
            JsonSerializer.Serialize(Current));
        Baseline = await baseline;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private static async Task<IReadOnlyDictionary<string, string>> RunBaselineAsync(string outputFile)
    {
        var runner = typeof(CompatibilityRuns).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "BaselineRunner").Value!;
        if (!File.Exists(runner))
        {
            throw new FileNotFoundException("The baseline runner is not built; build Linq2GraphQL.Compat.Baseline.",
                runner);
        }

        File.Delete(outputFile);
        var startInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            ArgumentList = { runner, outputFile },
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(BaselineTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            throw new TimeoutException($"The baseline runner did not finish in {BaselineTimeout}.");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"The baseline runner failed with exit code {process.ExitCode}.\n{await output}\n{await error}");
        }

        return JsonSerializer.Deserialize<SortedDictionary<string, string>>(await File.ReadAllTextAsync(outputFile))!;
    }

    private static IReadOnlyDictionary<string, string> ReadAcceptedDifferences(
        [CallerFilePath] string callerPath = "")
    {
        var path = Path.Combine(Path.GetDirectoryName(callerPath)!, "accepted-differences.json");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))!;
    }
}
