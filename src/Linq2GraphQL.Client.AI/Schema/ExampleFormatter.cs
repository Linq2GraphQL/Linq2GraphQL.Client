using System.Text;
using System.Text.RegularExpressions;
using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace Linq2GraphQL.Client.AI.Schema;

/// <summary>
/// Makes a query built by the typed client read like one a person would write, so the agent can copy and adapt
/// it: variables lose the client's uniqueness suffixes (<c>$name_1_1</c> becomes <c>$name</c>) where that stays
/// unambiguous, and the document is re-printed on one line with regular spacing.
/// </summary>
internal static partial class ExampleFormatter
{
    [GeneratedRegex(@"(_\d+)+$")]
    private static partial Regex UniquenessSuffix();

    public static (string Query, Dictionary<string, object> Variables) Tidy(string query,
        IReadOnlyDictionary<string, object> variables)
    {
        var document = Parser.Parse(query);
        var declared = document.Definitions.OfType<GraphQLOperationDefinition>()
            .SelectMany(e => e.Variables?.Items ?? [])
            .Select(e => e.Variable.Name.StringValue)
            .ToList();

        var renames = new Dictionary<string, string>();
        foreach (var name in declared)
        {
            var plain = UniquenessSuffix().Replace(name, "");
            var clashes = declared.Count(e => UniquenessSuffix().Replace(e, "") == plain) > 1 ||
                          (plain != name && declared.Contains(plain));
            if (plain != name && plain.Length > 0 && !clashes)
            {
                renames[name] = plain;
            }
        }

        foreach (var (from, to) in renames)
        {
            query = Regex.Replace(query, $@"\${Regex.Escape(from)}\b", "$" + to);
        }

        var tidyVariables = variables?.ToDictionary(e => renames.GetValueOrDefault(e.Key, e.Key), e => e.Value);
        var printed = new SDLPrinter().Print(Parser.Parse(query));
        return (OneLine(printed), tidyVariables);
    }

    /// <summary>
    /// Collapses whitespace to single spaces outside string literals.
    /// </summary>
    private static string OneLine(string text)
    {
        var result = new StringBuilder();
        var inString = false;
        var pendingSpace = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inString)
            {
                result.Append(c);
                if (c == '\\' && i + 1 < text.Length)
                {
                    result.Append(text[++i]);
                }
                else if (c == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                pendingSpace = true;
                continue;
            }

            var spaceBefore = c == '{' || pendingSpace && c is not ('(' or ')' or ']' or ':' or ',');
            if (spaceBefore && result.Length > 0 && result[^1] is not ('(' or '[' or '$' or ' '))
            {
                result.Append(' ');
            }

            result.Append(c);
            inString = c == '"';
            pendingSpace = c is ':' or ',';
        }

        return result.ToString();
    }
}
