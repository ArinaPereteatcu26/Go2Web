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
                // Use TCP to connect to the server
                using TcpClient tcpClient = new TcpClient(host, port);
                using NetworkStream networkStream = tcpClient.GetStream();
                SslStream sslStream = null;

                // If it's HTTPS, wrap the network stream with SSL
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
                while ((bytesRead = (sslStream != null
                           ? await sslStream.ReadAsync(buffer, 0, buffer.Length)
                           : await networkStream.ReadAsync(buffer, 0, buffer.Length))) > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                }

                string responseHtml = Encoding.ASCII.GetString(memoryStream.ToArray());

                
                if (string.IsNullOrEmpty(responseHtml) || !responseHtml.Contains("\r\n\r\n"))
                {
                    throw new Exception("Invalid or empty HTTP response.");
                }

            }
            catch (Exception ex)
            {
                return $"Error fetching URL: {ex.Message}";
            }
        }
    }
}

