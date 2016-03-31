using System;
using System.IO;
using System.Net;

namespace FileStorage.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            FileInfo fi = new FileInfo(@"D:\Music\Dado.zip");
            using (var fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                for (long i = 0; i < fi.Length; i += 1024 * 1024)
                {
                    PutRange(fs, i, i + 1024 * 1024);
                    Console.Write(i);
                }
            }

            string firstFile = @"D:\Music\Dado.zip";
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
            Console.WriteLine("Hash 1: "+BitConverter.ToString(hash1));
            Console.WriteLine("Hash 2: "+BitConverter.ToString(hash2));


        }

        public static void PutRange(FileStream fs, long start, long end)
        {
            //read range
            byte[] dataToSend = new byte[end - start];

            int bytesRead = 0;
            fs.Seek(start, SeekOrigin.Begin);
            bytesRead = fs.Read(dataToSend, 0, (int)(end - start));

            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create("http://localhost:8081/Api/File/Dado.zip?comp=range");
            webRequest.Method = "PUT";

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
