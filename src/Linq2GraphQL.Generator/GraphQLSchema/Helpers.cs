using System.Text.RegularExpressions;

namespace Linq2GraphQL.Generator;

public static class Helpers
{

    internal static string SummarySafe(string text)
    {
        if (string.IsNullOrEmpty(text)) { return text; }
        return Regex.Replace(text, @"\r\n?|\n", Environment.NewLine + "/// ");
    }

    internal static string SafeDeprecationReason(string text)
    {
        if (string.IsNullOrEmpty(text)) { return text; }
        return text.Replace("\"", "'");
    }

    internal static string SafeVariableName(string name)
    {
        if (string.IsNullOrEmpty(name)) { return name; }
        var newName = name.ToCamelCase();
        if (Keywords.Contains(newName)) { return "@" + newName; }
        return newName;
    }

    public static readonly HashSet<string> Keywords = [
            "abstract",
            "as",
            "base",
            "bool",
            "break",
            "byte",
            "case",
            "catch",
            "char",
            "checked",
            "class",
            "const",
            "continue",
            "decimal",
            "default",
            "delegate",
            "do",
            "double",
            "else",
            "enum",
            "event",
            "explicit",
            "extern",
            "false",
            "finally",
            "fixed",
            "float",
            "for",
            "foreach",
            "goto",
            "if",
            "implicit",
            "in",
            "int",
            "interface",
            "internal",
            "is",
            "lock",
            "long",
            "namespace",
            "new",
            "null",
            "object",
            "operator",
            "out",
            "override",
            "params",
            "private",
            "protected",
            "public",
            "readonly",
            "ref",
            "return",
            "sbyte",
            "sealed",
            "short",
            "sizeof",
            "static",
            "string",
            "struct",
            "switch",
            "this",
            "throw",
            "true",
            "try",
            "typeof",
            "uint",
            "ulong",
            "unchecked",
            "unsafe",
            "ushort",
            "using",
            "virtual",
            "void",
            "volatile",
            "while",];


    /// <summary>
    /// The built-in GraphQL scalar to CLR type mapping. Copied per generator run so that user
    /// supplied overrides never leak between runs - see <see cref="CreateTypeMapping"/>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string Name, Type type)> DefaultTypeMapping =
        new Dictionary<string, (string Name, Type type)>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "Int", new ValueTuple<string, Type>("int", typeof(int)) },
            { "Float", new ValueTuple<string, Type>("double", typeof(double)) },
            { "String", new ValueTuple<string, Type>("string", typeof(string)) },
            { "Date", new ValueTuple<string, Type>("DateTime", typeof(DateTime)) },
            { "Boolean", new ValueTuple<string, Type>("bool", typeof(bool)) },
            { "Long", new ValueTuple<string, Type>("long", typeof(long)) },
            { "uuid", new ValueTuple<string, Type>("Guid", typeof(Guid)) },
            { "timestamptz", new ValueTuple<string, Type>("DateTimeOffset", typeof(DateTimeOffset)) },
            { "Uri", new ValueTuple<string, Type>("Uri", typeof(Uri)) },
            { "DateTime", new ValueTuple<string, Type>("DateTimeOffset", typeof(DateTimeOffset)) },
            { "Decimal", new ValueTuple<string, Type>("decimal", typeof(decimal)) },
            { "TimeSpan", new ValueTuple<string, Type>("TimeSpan", typeof(TimeSpan)) },
            { "UnsignedByte", new ValueTuple<string, Type>("byte", typeof(byte)) },
            { "Byte", new ValueTuple<string, Type>("sbyte", typeof(sbyte)) },
            { "LocalDate", new ValueTuple<string, Type>("DateOnly", typeof(DateOnly)) },
            { "LocalTime", new ValueTuple<string, Type>("TimeOnly", typeof(TimeOnly)) },
        };

    /// <summary>
    /// The simple CLR types a scalar may be mapped to. Deliberately an allow-list: a mapping target
    /// has to resolve to a real <see cref="Type"/>, because <see cref="CoreType.CSharpType"/> drives
    /// nullability and the input factory template. Keyed by every spelling we accept, valued with
    /// the canonical name to emit.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string Name, Type type)> SupportedTargetTypes =
        BuildSupportedTargetTypes();

    /// <summary>
    /// Names of the supported mapping targets, canonically spelled, for error messages.
    /// </summary>
    public static IEnumerable<string> SupportedTargetTypeNames =>
        SupportedTargetTypes.Values.Select(e => e.Name).Distinct().OrderBy(e => e, StringComparer.Ordinal);

    private static Dictionary<string, (string Name, Type type)> BuildSupportedTargetTypes()
    {
        var result = new Dictionary<string, (string Name, Type type)>(StringComparer.InvariantCultureIgnoreCase);

        void Add(string emittedName, Type type)
        {
            var mapping = new ValueTuple<string, Type>(emittedName, type);
            result[emittedName] = mapping;
            result[type.Name] = mapping;
            result[type.FullName] = mapping;
        }

        Add("bool", typeof(bool));
        Add("byte", typeof(byte));
        Add("sbyte", typeof(sbyte));
        Add("char", typeof(char));
        Add("short", typeof(short));
        Add("ushort", typeof(ushort));
        Add("int", typeof(int));
        Add("uint", typeof(uint));
        Add("long", typeof(long));
        Add("ulong", typeof(ulong));
        Add("float", typeof(float));
        Add("double", typeof(double));
        Add("decimal", typeof(decimal));
        Add("string", typeof(string));
        Add("Guid", typeof(Guid));
        Add("Uri", typeof(Uri));
        Add("DateTime", typeof(DateTime));
        Add("DateTimeOffset", typeof(DateTimeOffset));
        Add("DateOnly", typeof(DateOnly));
        Add("TimeOnly", typeof(TimeOnly));
        Add("TimeSpan", typeof(TimeSpan));

        return result;
    }

    /// <summary>
    /// A fresh, mutable copy of <see cref="DefaultTypeMapping"/> for a single generator run.
    /// </summary>
    public static Dictionary<string, (string Name, Type type)> CreateTypeMapping()
    {
        return new Dictionary<string, (string Name, Type type)>(DefaultTypeMapping,
            StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Turns raw <c>scalarMappings</c> config values into resolved mappings, failing fast on an
    /// unsupported target type. A null or empty value means "opt this scalar out of the built-in
    /// mapping", which makes the generator emit a CustomScalar class for it instead.
    /// </summary>
    public static Dictionary<string, (string Name, Type type)?> ResolveScalarMappings(
        IReadOnlyDictionary<string, string> scalarMappings)
    {
        var result = new Dictionary<string, (string Name, Type type)?>(StringComparer.InvariantCultureIgnoreCase);

        if (scalarMappings == null)
        {
            return result;
        }

        foreach (var (scalarName, targetTypeName) in scalarMappings)
        {
            if (string.IsNullOrWhiteSpace(scalarName))
            {
                throw new GeneratorConfigurationException("A scalar mapping must have a non-empty scalar name.");
            }

            if (string.IsNullOrWhiteSpace(targetTypeName))
            {
                result[scalarName.Trim()] = null;
                continue;
            }

            if (!SupportedTargetTypes.TryGetValue(targetTypeName.Trim(), out var mapping))
            {
                throw new GeneratorConfigurationException(
                    $"Unsupported target type '{targetTypeName}' for scalar '{scalarName}'. Supported target types are: " +
                    $"{string.Join(", ", SupportedTargetTypeNames)}. " +
                    "Use null to generate a CustomScalar class for the scalar instead.");
            }

            result[scalarName.Trim()] = mapping;
        }

        return result;
    }

    /// <summary>
    /// Applies resolved overrides on top of a type mapping table, in place.
    /// </summary>
    public static void ApplyScalarMappings(Dictionary<string, (string Name, Type type)> typeMapping,
        IReadOnlyDictionary<string, (string Name, Type type)?> overrides)
    {
        if (overrides == null)
        {
            return;
        }

        foreach (var (scalarName, mapping) in overrides)
        {
            if (mapping == null)
            {
                typeMapping.Remove(scalarName);
            }
            else
            {
                typeMapping[scalarName] = mapping.Value;
            }
        }
    }

    /// <summary>
    /// The type mapping in force for the current run, falling back to the defaults when no
    /// generator run has been started.
    /// </summary>
    internal static IReadOnlyDictionary<string, (string Name, Type type)> EffectiveTypeMapping =>
        GeneratorSettings.Current?.TypeMapping ?? DefaultTypeMapping;
}
