using System;
using System.Threading.Tasks;
using System.Text;

namespace Go2Web
{
    class Go2Web
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            string url = null;
            string searchTerm = null;
            string contentType = "text/html,application/json;q=0.9,*/*;q=0.8";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-u" && i + 1 < args.Length)
                {
                    url = args[i + 1];
                    i++; 
                }
                else if (args[i] == "-s" && i + 1 < args.Length)
                {
                    searchTerm = args[i + 1];
                    i++; 
                }
                else if (args[i] == "-h")
                {
                    ShowHelp();
                    return;
                }
            }

            if (!string.IsNullOrEmpty(url))
            {
                // Handle URL fetching
                Console.WriteLine($"Fetching: {url}\n");

                try
                {
                    string response = await HttpFetcher.Fetch(url, contentType);
                    Console.WriteLine($"Response from {url}:\n{response}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching URL: {ex.Message}");
                }
            }
            else if (!string.IsNullOrEmpty(searchTerm))
            {
                // Handle search term processing
                Console.WriteLine($"Searching for: {searchTerm}\n");

                try
                {
                    string searchResults = await SearchEngine.Search(searchTerm);

                    if (string.IsNullOrEmpty(searchResults))
                    {
                        Console.WriteLine("No results found.");
                        return;
                    }

                    Console.WriteLine($"Top results:\n{searchResults}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error performing search: {ex.Message}");
                }
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("go2web -u          # Make an HTTP request to the specified URL and print the response");
            Console.WriteLine("go2web -s  # Search for the term using your favorite search engine and print top 10 results");
            Console.WriteLine("go2web -h               # Show this help.");
        }
    }
}