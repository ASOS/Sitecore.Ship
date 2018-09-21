using System.Linq;

namespace Sitecore.Ship
{
    public static class StringExtensions
    {
        public static string Formatted(this string target, params object[] args)
        {
            return string.Format(target, args);
        }

        public static string[] CsvStringToStringArray(this string inputValue, string[] defaultValue)
        {
            if (string.IsNullOrWhiteSpace(inputValue)) return defaultValue;

            return inputValue.Split(new[] { ',' }).Select(x => x.Trim()).ToArray();
        }

        public static bool ToBool(this string inputValue, bool defaultValue)
        {
            if (!string.IsNullOrWhiteSpace(inputValue))
            {
                bool retValue;
                if (bool.TryParse(inputValue, out retValue))
                {
                    return retValue;
                }
            }
            return defaultValue;
        }
    }
}
