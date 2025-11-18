using System.Globalization;
using System;

namespace ExamplePlugin
{
    public static class Utils
    {
        public static string CapitalizeAll(string str)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
        }

        public static string ToCamelCase(string str)
        {
            return string.Join("", CapitalizeAll(str).Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        }
    }
}

