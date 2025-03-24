using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Go2Web
{
    class SearchEngine
    {
        private const string HOST = "lite.duckduckgo.com";
        private const int PORT = 443; 
        private const string SEARCH_URL = "/lite/?q=";
        private const int MAX_RESULTS = 10;

        public static async Task<string> Search(string searchTerm)
        {
            string encodedSearchTerm = Uri.EscapeDataString(searchTerm);
            string request = "GET " + SEARCH_URL + encodedSearchTerm + " HTTP/1.1\r\n" +
                             "Host: " + HOST + "\r\n" +
                             "Connection: close\r\n" +
                             "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36\r\n" +
                             "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8\r\n" +
                             "Accept-Language: en-US,en;q=0.5\r\n" +
                             "Accept-Encoding: identity\r\n" +
                             "\r\n";

            try
            {
                using (TcpClient tcpClient = new TcpClient(HOST, PORT))
                using (SslStream sslStream = new SslStream(tcpClient.GetStream(), false, 
                       new RemoteCertificateValidationCallback(ValidateServerCertificate)))
                {
                    await sslStream.AuthenticateAsClientAsync(HOST);
                    
                    byte[] requestBytes = Encoding.ASCII.GetBytes(request);
                    await sslStream.WriteAsync(requestBytes, 0, requestBytes.Length);
                    await sslStream.FlushAsync();
                    
                    using (StreamReader reader = new StreamReader(sslStream, Encoding.UTF8))
                    {
                        string response = await reader.ReadToEndAsync();
                        return ExtractSearchResults(response, searchTerm);
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, 
                                                     X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true; 
        }

        private static string ExtractSearchResults(string html, string searchTerm)
        {
            var resultStringBuilder = new StringBuilder();
            
            string searchQuery = searchTerm;

            resultStringBuilder.AppendLine($"Search Results for: \"{searchQuery}\"");
            resultStringBuilder.AppendLine("================================================");
            
            html = System.Net.WebUtility.HtmlDecode(html);

            string resultPattern = @"<tr class=""result(?:odd|even)?"".*?>.*?<a.*?href=""([^""]+)"".*?>(.*?)</a>.*?<td class=""result-snippet"">(.*?)</td>";
            var matches = Regex.Matches(html, resultPattern, RegexOptions.Singleline);

            if (matches.Count == 0)
            {
                resultPattern = @"<div class=""links_main"">\s*<a.*?href=""([^""]+)"".*?>(.*?)</a>.*?<div class=""snippet"">(.*?)</div>";
                matches = Regex.Matches(html, resultPattern, RegexOptions.Singleline);
            }

            if (matches.Count == 0)
            {
                resultPattern = @"<a class=""result-link"" href=""([^""]+)"".*?>(.*?)</a>.*?<a class=""result-snippet"">(.*?)</a>";
                matches = Regex.Matches(html, resultPattern, RegexOptions.Singleline);
            }

            int count = 0;

            foreach (Match match in matches)
            {
                if (count >= MAX_RESULTS) break;

                if (match.Groups.Count >= 4)
                {
                    string rawUrl = match.Groups[1].Value;
                    string title = CleanText(match.Groups[2].Value);
                    string snippet = CleanText(match.Groups[3].Value);
                    string url = ExtractActualUrl(rawUrl);

           
                    if (url.Contains("/y.js") || url.Contains("/kw/") || 
                        url.Contains("javascript:") || url.EndsWith(".css") || 
                        url.EndsWith(".js") || url.StartsWith("#") || 
                        url.Contains("/about/") || url.Contains("/settings/"))
                        continue;

                    resultStringBuilder.AppendLine($"Result #{count + 1}");
                    resultStringBuilder.AppendLine($"Title: {title}");
                    resultStringBuilder.AppendLine($"URL: {url}");
                    resultStringBuilder.AppendLine($"Description: {snippet}");
                    resultStringBuilder.AppendLine("------------------------------------------------");

                    count++;
                }
            }

            if (count == 0)
            {
                var titles = Regex.Matches(html, @"<a[^>]*href=[""'][^""']+[""'][^>]*>((?:(?!</a>).)+)</a>", RegexOptions.Singleline);
                var urls = Regex.Matches(html, @"<a[^>]*href=[""']([^""']+)[""']", RegexOptions.Singleline);

                for (int i = 0; i < Math.Min(titles.Count, MAX_RESULTS); i++)
                {
                    if (i < urls.Count)
                    {
                        string url = urls[i].Groups[1].Value;
                        url = ExtractActualUrl(url);

                        if (url.Contains("javascript:") || url.EndsWith(".css") || url.EndsWith(".js") || 
                            url.StartsWith("#") || url.Contains("about") || url.Contains("settings"))
                            continue;

                        string title = CleanText(titles[i].Groups[1].Value);
                        if (string.IsNullOrWhiteSpace(title) || title.Length < 3)
                            continue;

                        resultStringBuilder.AppendLine($"Result #{count + 1}");
                        resultStringBuilder.AppendLine($"Title: {title}");
                        resultStringBuilder.AppendLine($"URL: {url}");
                        resultStringBuilder.AppendLine("------------------------------------------------");

                        count++;
                        if (count >= MAX_RESULTS) break;
                    }
                }
            }

            return count == 0 ? "No search results could be extracted. DuckDuckGo may have changed their page structure." : resultStringBuilder.ToString();
        }

        private static string ExtractActualUrl(string ddgUrl)
        {
            ddgUrl = System.Net.WebUtility.HtmlDecode(ddgUrl);
            
            if (ddgUrl.StartsWith("//"))
            {
                ddgUrl = "https:" + ddgUrl;
            }
            
            if (ddgUrl.Contains("/l/?"))
            {
                var match = Regex.Match(ddgUrl, @"uddg=([^&]+)");
                if (match.Success)
                {
                    try {
                
                        string extractedUrl = Uri.UnescapeDataString(match.Groups[1].Value);
                        
                 
                        if (!extractedUrl.StartsWith("http://") && !extractedUrl.StartsWith("https://"))
                        {
                            extractedUrl = "https://" + extractedUrl;
                        }
                        
                        return extractedUrl;
                    }
                    catch {
        
                        return ddgUrl;
                    }
                }
            }
            

            if (!ddgUrl.StartsWith("/") && !ddgUrl.StartsWith("http://") && !ddgUrl.StartsWith("https://"))
            {
                ddgUrl = "https://" + ddgUrl;
            }
            
            return ddgUrl;
        }

        private static string CleanText(string text)
        {
      
            text = Regex.Replace(text, @"<[^>]+>", " ");
            

            text = System.Net.WebUtility.HtmlDecode(text);
            
        
            text = Regex.Replace(text, @"\s+", " ").Trim();
            
            return text;
        }
    }
}
