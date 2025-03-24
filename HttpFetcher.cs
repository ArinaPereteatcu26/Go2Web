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
            // Remove all HTML tags 
            string noHtml = Regex.Replace(html, "<script.*?</script>|<style.*?</style>|<[^>]+?>", "", RegexOptions.Singleline);

            // Decode HTML entities 
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
                
                if (responseHtml.Contains("301 Moved Permanently") || responseHtml.Contains("302 Found"))
                {
                    string location = ExtractLocationHeader(responseHtml);
                    if (!string.IsNullOrEmpty(location))
                    {
                        return await Fetch(location, contentType);
                    }
                    else
                    {
                        throw new Exception("Redirect location not found.");
                    }
                }
                if (string.IsNullOrEmpty(responseHtml) || !responseHtml.Contains("\r\n\r\n"))
                {
                    throw new Exception("Invalid or empty HTTP response.");
                }

          
                string htmlContent = responseHtml.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.None)[1];

                return ExtractTextFromHtml(htmlContent);
            }
            catch (Exception ex)
            {
                return $"Error fetching URL: {ex.Message}";
            }
        }
        
        private static string ExtractLocationHeader(string response)
        {
            string locationPattern = @"Location:\s*(http[s]?:\/\/[^\s]+)";
            Match match = Regex.Match(response, locationPattern, RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
        
        private static string RemoveSessionIdentifiers(string text)
        {
            string sessionIdPattern = @"[?&](session|id|token|ref|sid|key|guid)=[^\s&]+";
            return Regex.Replace(text, sessionIdPattern, string.Empty, RegexOptions.IgnoreCase);
        }
    }
}