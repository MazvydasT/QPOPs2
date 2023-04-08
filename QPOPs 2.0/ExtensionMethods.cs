using System.Text.RegularExpressions;

namespace QPOPs2
{
    public static class ExtensionMethods
    {
        public static string Replace(this string source, Regex regex, string replacement)
        {
            return regex.Replace(source, replacement);
        }

        public static IEnumerable<T> Yield<T>(this T obj)
        {
            yield return obj;
        }
    }
}
