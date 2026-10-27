using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ColorCode;

namespace Linq2GraphQL.Docs.Shared;

public static partial class SyntaxHighlighter
{
    public static string ToHtml(string code, string language) => language switch
    {
        "csharp" => new HtmlClassFormatter().GetHtmlString(code, Languages.CSharp),
        "xml" => new HtmlClassFormatter().GetHtmlString(code, Languages.Xml),
        "graphql" => Wrap(Tokenize(code, GraphQLToken())),
        "json" => Wrap(Tokenize(code, JsonToken())),
        "shell" => Wrap(Tokenize(code, ShellToken())),
        _ => Wrap(WebUtility.HtmlEncode(code))
    };

    public static string Label(string language) => language switch
    {
        "csharp" => "C#",
        "graphql" => "GraphQL",
        "json" => "JSON",
        "shell" => "Shell",
        "xml" => "XML",
        _ => "Text"
    };

    private static string Wrap(string html) => $"<pre>{html}</pre>";

    private static string Tokenize(string code, Regex tokens)
    {
        var html = new StringBuilder();
        var position = 0;
        foreach (Match match in tokens.Matches(code))
        {
            html.Append(WebUtility.HtmlEncode(code[position..match.Index]));
            var group = match.Groups.Cast<Group>().Skip(1).First(candidate => candidate.Success);
            html.Append($"<span class=\"{group.Name}\">{WebUtility.HtmlEncode(match.Value)}</span>");
            position = match.Index + match.Length;
        }

        html.Append(WebUtility.HtmlEncode(code[position..]));
        return html.ToString();
    }

    [GeneratedRegex("""(?<comment>#[^\n]*)|(?<string>"(?:[^"\\]|\\.)*")|(?<variable>\$\w+)|(?<keyword>\b(?:query|mutation|subscription|fragment|on|true|false|null)\b)|(?<className>\b[A-Z]\w*\b)|(?<number>-?\b\d+(?:\.\d+)?\b)""")]
    private static partial Regex GraphQLToken();

    [GeneratedRegex("""(?<property>"(?:[^"\\]|\\.)*"(?=\s*:))|(?<string>"(?:[^"\\]|\\.)*")|(?<keyword>\b(?:true|false|null)\b)|(?<number>-?\b\d+(?:\.\d+)?\b)""")]
    private static partial Regex JsonToken();

    [GeneratedRegex("""(?<comment>(?:^|(?<=\s))#[^\n]*)|(?<string>"[^"\n]*"|'[^'\n]*')|(?<keyword>^(?:dotnet|Linq2GraphQL|cd)\b)|(?<variable>(?<=\s)--?[\w-]+=?)""", RegexOptions.Multiline)]
    private static partial Regex ShellToken();
}
