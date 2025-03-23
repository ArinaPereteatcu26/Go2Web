using System;

namespace Go2Web
{
    class Go2Web
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "-h")
            {
                ShowHelp();
                return;
            }

            switch (args[0])
            {
                case "-u":
                    Console.WriteLine("Fetching URL... (to be implemented)");
                    break;
                case "-s":
                    Console.WriteLine("Searching... (to be implemented)");
                    break;
                default:
                    ShowHelp();
                    break;
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("go2web -u <URL>         # make an HTTP request to the specified URL and print the response");
            Console.WriteLine("go2web -s <search-term> # make an HTTP request to search the term using your favorite search engine and print top 10 results");
            Console.WriteLine("go2web -h               # Show this help");
        }
    }
}