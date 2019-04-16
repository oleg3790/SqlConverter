using System.Globalization;

namespace SqlConverter.Extensions
{
    internal static class StringExtensions
    {
        public static string ToTitleCase(this string s)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());
        }
    }
}
