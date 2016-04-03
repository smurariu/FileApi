using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace FileStorage.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename = @"D:\Temp\altele\55A3AA64.mp4";

            if (args.Length == 1)
            {
                filename = args[0];
            }

            Stopwatch sw = Stopwatch.StartNew();

            FileInfo fi = new FileInfo(filename);
            using (var fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                //create the file on the server
                CreateFile(fi.Name, fi.Length);

                //transfer the file contents
                for (long i = 0; i < fi.Length; i += 1024 * 1024)
                {
                    PutRange(fs, fi.Name, i, i + 1024 * 1024);
                    Console.Write(i);
                }
            }

            sw.Stop();

            Console.WriteLine(Environment.NewLine + "Transferred " + fi.Length + " bytes in " + sw.ElapsedMilliseconds);
            Console.ReadLine();
        }

        private static void CreateFile(string name, long length)
        {
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create("http://localhost:8081/Api/File/" + name);
            webRequest.Method = "PUT";
            (webRequest as WebRequest).Headers.Add("x-ms-content-length", length.ToString());
            (webRequest as WebRequest).ContentLength = 0;

            using (HttpWebResponse wr = (HttpWebResponse)webRequest.GetResponse())
            {
                using (Stream response = wr.GetResponseStream())
                {
                    // handle response stream.
                    string responseText = new StreamReader(response).ReadToEnd();
                    Console.WriteLine(responseText);
                }
            }


        }

        public static void PutRange(FileStream fs, string filePath, long start, long end)
        {
            //read range calculation
            if (fs.Length < end)
            {
                end = fs.Length;
            }
            int range = (int)(end - start);

            byte[] dataToSend = new byte[range];

            int bytesRead = 0;
            fs.Seek(start, SeekOrigin.Begin);
            bytesRead = fs.Read(dataToSend, 0, range);

            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create("http://localhost:8081/Api/File/" + filePath + "?comp=range");
            webRequest.Method = "PUT";

            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] md5Hash = md5.ComputeHash(dataToSend);

            (webRequest as WebRequest).Headers.Add("Content-MD5", System.Convert.ToBase64String(md5Hash));
            (webRequest as WebRequest).Headers.Add("x-ms-write", "write");
            (webRequest as WebRequest).Headers.Add("x-ms-range", "bytes=" + start + "-" + (start + bytesRead).ToString());
            (webRequest as WebRequest).Headers.Add("x-ms-content-length", bytesRead.ToString());

            using (Stream requestStream = webRequest.GetRequestStream())
            {
                requestStream.Write(dataToSend, 0, dataToSend.Length);
            }

            using (HttpWebResponse wr = (HttpWebResponse)webRequest.GetResponse())
            {
                using (Stream response = wr.GetResponseStream())
                {
                    // handle response stream.
                    string responseText = new StreamReader(response).ReadToEnd();
                    Console.WriteLine(responseText);
                }
            }

        }
    }
}
