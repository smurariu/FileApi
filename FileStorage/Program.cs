using Microsoft.Owin.Hosting;
using System;

namespace FileStorage
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://localhost:8081/";
            
            // Start OWIN host 
            using (WebApp.Start<Startup>(url: baseAddress))
            {
                Console.WriteLine("Press Enter to quit.");
                Console.ReadLine();
            }
        }
    }
}
