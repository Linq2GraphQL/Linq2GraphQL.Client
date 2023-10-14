using System.Collections.Concurrent;

namespace Linq2GraphQL.Client;

public abstract class GraphInputBase
{
    private ConcurrentDictionary<string, object> Values { get; } = new();

    internal Dictionary<string, object> GetValues()
    {
        return Values.ToDictionary(e => e.Key, e => e.Value);
    }

    protected void ClearValues()
    {
        Values.Clear();
    }

    protected T GetValue<T>(string name)
    {
        if (Values.TryGetValue(name, out var value))
        {
            return (T)value;
        }

        return default;
    }


    protected void SetValue(string name, object value)
    {
        Values.TryRemove(name, out _);
        Values.TryAdd(name, value);
    }
}