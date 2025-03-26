/*using System;
using System.Text.RegularExpressions;

namespace Go2Web
{
    class UrlHelper
    {
        public static string GetHost(string url)
        {
            Match match = Regex.Match(url, @"https?://([^/]+)");
            return match.Success ? match.Groups[1].Value : url;
        }

        public static string GetPath(string url)
        {
            Match match = Regex.Match(url, @"https?://[^/]+(/.*)?");
            return match.Success && match.Groups[1].Success ? match.Groups[1].Value : "/";
        }

        public static bool IsHttps(string url)
        {
            return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }
    }
}*/