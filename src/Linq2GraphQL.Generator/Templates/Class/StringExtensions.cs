internal static class StringExtensions
{
    internal static string RemoveFromEnd(this string s, string suffix)
    {
        if (s.EndsWith(suffix))
        {
            return s.Substring(0, s.Length - suffix.Length);
        }

        return s;
    }
}