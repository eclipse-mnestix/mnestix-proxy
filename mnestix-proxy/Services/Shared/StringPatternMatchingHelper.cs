using System.Text.RegularExpressions;

namespace mnestix_proxy.Services.Shared
{
    public static class StringPatternMatchingHelper
    {
        public static bool CheckForPatternMatch(string input, string pattern)
        {
            pattern = $"^{pattern.Replace("+", ".*")}$";
            var regex = new Regex(pattern);

            return regex.IsMatch(input);
        }
    }
}
