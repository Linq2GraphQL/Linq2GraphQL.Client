namespace Linq2GraphQL.Client.AI.Schema;

/// <summary>
/// "Did you mean" suggestions and capped name lists, so every message to the agent says what it can use instead.
/// </summary>
internal static class NameSuggestions
{
    private const int MaxListedNames = 15;

    /// <summary>
    /// " Did you mean `x`?" for a near miss, followed by the available names.
    /// </summary>
    public static string Suggest(string name, IEnumerable<string> candidates)
    {
        var list = candidates.ToList();
        var closest = Closest(name, list);
        var suggestion = closest == null ? "" : $" Did you mean `{closest}`?";
        return list.Count == 0 ? suggestion : $"{suggestion} Available: {List(list)}.";
    }

    public static string Closest(string name, IEnumerable<string> candidates) =>
        candidates
            .Select(e => (Name: e, Distance: Distance(name.ToLowerInvariant(), e.ToLowerInvariant())))
            .Where(e => e.Distance <= Math.Max(2, name.Length / 3))
            .OrderBy(e => e.Distance)
            .Select(e => e.Name)
            .FirstOrDefault();

    public static string List(IEnumerable<string> names)
    {
        var list = names.ToList();
        return list.Count <= MaxListedNames
            ? string.Join(", ", list)
            : string.Join(", ", list.Take(MaxListedNames)) + $" and {list.Count - MaxListedNames} more";
    }

    private static int Distance(string a, string b)
    {
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var current = new int[b.Length + 1];
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }

            previous = current;
        }

        return previous[b.Length];
    }
}
