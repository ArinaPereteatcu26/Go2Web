using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Go2Web
{
    class HttpFetcher
    {
        public static string ExtractTextFromHtml(string html)
        {
            string noHtml = Regex.Replace(html, "<script.*?</script>|<style.*?</style>|<[^>]+?>", "", RegexOptions.Singleline);
            
            string extractedText = WebUtility.HtmlDecode(noHtml);
            
            extractedText = Regex.Replace(extractedText, @"[^\w\sĂăÂâÎîȘșŢţ]", "");
            extractedText = Regex.Replace(extractedText, @"([a-z])([A-Z])", "$1 $2");
            extractedText = Regex.Replace(extractedText, @"([a-zA-Z])([ȘȘșș])", "$1 $2");
            extractedText = Regex.Replace(extractedText, @"\s+", " ").Trim();
            extractedText = RemoveSessionIdentifiers(extractedText);

            return extractedText;
        }
        

        public static string DecodeHtmlEntities(string html)
        {
            string decoded = html;
            
            decoded = decoded.Replace("&amp;", "&");
            decoded = decoded.Replace("&lt;", "<");
            decoded = decoded.Replace("&gt;", ">");
            decoded = decoded.Replace("&quot;", "\"");
            decoded = decoded.Replace("&apos;", "'");
            decoded = decoded.Replace("&nbsp;", " ");

            return decoded;
        }

  
        public static async Task<string> Fetch(string url, string contentType)
        {
            Uri uri = new Uri(url);
            string host = uri.Host;
            int port = uri.Scheme == "https" ? 443 : 80;
            string path = uri.AbsolutePath;
            if (string.IsNullOrEmpty(path)) path = "/"; 

            string request = $"GET {path} HTTP/1.1\r\n" +
                             $"Host: {host}\r\n" +
                             $"Accept: {contentType}\r\n" +
                             $"Connection: close\r\n\r\n";

            try
            {
                using TcpClient tcpClient = new TcpClient(host, port);
                using NetworkStream networkStream = tcpClient.GetStream();
                SslStream sslStream = null;
                
                if (port == 443)
                {
                    sslStream = new SslStream(networkStream);
                    await sslStream.AuthenticateAsClientAsync(host);
                }
                
                byte[] requestBytes = Encoding.ASCII.GetBytes(request);
                if (sslStream != null)
                {
                    await sslStream.WriteAsync(requestBytes, 0, requestBytes.Length);
                }
                else
                {
                    await networkStream.WriteAsync(requestBytes, 0, requestBytes.Length);
                }
                
                using MemoryStream memoryStream = new MemoryStream();
                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = (sslStream != null ? await sslStream.ReadAsync(buffer, 0, buffer.Length) : await networkStream.ReadAsync(buffer, 0, buffer.Length))) > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                }

                string responseHtml = Encoding.ASCII.GetString(memoryStream.ToArray());
                
                // Extract headers and body
                string[] responseParts = responseHtml.Split(new string[] { "\r\n\r\n" }, 2, StringSplitOptions.None);
                if (responseParts.Length < 2)
                {
                    throw new Exception("Invalid HTTP response");
                }
                string headers = responseParts[0];
                string body = responseParts[1];

                // Handle Redirects
                if (Regex.IsMatch(headers, @"HTTP/1\.\d 30[1278]"))
                {
                    string redirectUrl = GetRedirectLocation(headers, uri);
                    if (!string.IsNullOrEmpty(redirectUrl))
                    {
                        Console.WriteLine($"Redirecting to: {redirectUrl}");
                        return await Fetch(redirectUrl, contentType);
                    }
                    else
                    {
                        throw new Exception("Redirect location not found.");
                    }
                }

                return ExtractTextFromHtml(body);
            }
            catch (Exception ex)
            {
                return $"Error fetching URL: {ex.Message}";
            }
        }
        
        
        private static string GetRedirectLocation(string headers, Uri baseUri)
        {
            Match locationMatch = Regex.Match(headers, @"Location: (.+)", RegexOptions.IgnoreCase);
            if (locationMatch.Success)
            {
                string redirectUrl = locationMatch.Groups[1].Value.Trim();
                if (!redirectUrl.StartsWith("http"))
                {
                    redirectUrl = new Uri(baseUri, redirectUrl).ToString();
                }
                return redirectUrl;
            }
            return null;
        }
        
        private static string RemoveSessionIdentifiers(string text)
        {
            string sessionIdPattern = @"[?&](session|id|token|ref|sid|key|guid)=[^\s&]+";
            return Regex.Replace(text, sessionIdPattern, string.Empty, RegexOptions.IgnoreCase);
        }
    }
}