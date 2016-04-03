using System;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace FileStorage
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new HttpSelfHostConfiguration("http://localhost:8081");

            config.MapHttpAttributeRoutes();

            config.MaxReceivedMessageSize = 4 * 1024 * 1024;

            using (HttpSelfHostServer server = new HttpSelfHostServer(config))
            {
                server.OpenAsync().Wait();
                Console.WriteLine("Press Enter to quit.");
                Console.ReadLine();
            }

        }
    }
}
