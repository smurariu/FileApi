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

            Stopwatch sw = Stopwatch.StartNew();

            FileInfo fi = new FileInfo(@"D:\Temp\altele\55A3AA64.mp4");
            using (var fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                for (long i = 0; i < fi.Length; i += 1024 * 1024)
                {
                    PutRange(fs, i, i + 1024 * 1024);
                    Console.Write(i);
                }
            }

            sw.Stop();

            Console.WriteLine("Transferred " + fi.Length + " bytes in " + sw.ElapsedMilliseconds);

            string firstFile = @"D:\Temp\altele\55A3AA64.mp4";
            string secondFile = @"D:\Work\www\FileStorage\FileStorage\bin\Debug\files\Dado.zip";

            System.Security.Cryptography.HashAlgorithm ha = System.Security.Cryptography.HashAlgorithm.Create();

            FileStream f1 = new FileStream(firstFile, FileMode.Open);
            FileStream f2 = new FileStream(secondFile, FileMode.Open);
            /* Calculate Hash */
            byte[] hash1 = ha.ComputeHash(f1);
            byte[] hash2 = ha.ComputeHash(f2);
            f1.Close();
            f2.Close();
            /* Show Hash in TextBoxes */
            Console.WriteLine("Hash 1: " + BitConverter.ToString(hash1));
            Console.WriteLine("Hash 2: " + BitConverter.ToString(hash2));


        }

        public static void PutRange(FileStream fs, long start, long end)
        {
            //read range calculation
            if(fs.Length<end)
            {
                end = fs.Length;
            }
            int range = (int)(end - start);

            byte[] dataToSend = new byte[range];

            int bytesRead = 0;
            fs.Seek(start, SeekOrigin.Begin);
            bytesRead = fs.Read(dataToSend, 0, range);

            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create("http://localhost:8081/Api/File/Dado.zip?comp=range");
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
