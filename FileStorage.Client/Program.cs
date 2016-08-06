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
            TestServer();

            Console.ReadLine();
        }


        /// <summary>
        /// Test the server by generating a random file
        /// uploading it, downloading it and comparing 
        /// the results.
        /// </summary>
        private static void TestServer()
        {
            string randomFileName = Guid.NewGuid().ToString() + ".bin";

            string sentMd5 = GenerateTestData(randomFileName);
            string receivedMd5 = String.Empty;

            Stopwatch sw = Stopwatch.StartNew();

            StorageClient.CreateFolder("testData");
            StorageClient.UploadFile(randomFileName, "testData");

            sw.Stop();

            FileInfo fi = new FileInfo(randomFileName);
            Console.WriteLine(Environment.NewLine + "Transferred " + fi.Length + " bytes in " + sw.ElapsedMilliseconds + " ms.");

            File.Delete(randomFileName);

            StorageClient.GetFile("testData/" + randomFileName, randomFileName);

            using (FileStream receivedFileStream = File.OpenRead(randomFileName))
            {
                receivedMd5 = ComputeMd5(receivedFileStream);
            }

            //compare MD5s
            if (String.CompareOrdinal(receivedMd5, sentMd5) == 0)
            {
                Console.WriteLine("Test OK!");
            }
            else
            {
                Console.WriteLine("Test failed!");
            }

            File.Delete(randomFileName);
        }

        /// <summary>
        /// Generates a file with random content and 
        /// returns the md5 hash of the gnenerated file
        /// </summary>
        /// <param name="filename">The name of the file to generate</param>
        /// <param name="sizeInMb">The size in mb</param>
        /// <returns>The md5 hash of the generated file</returns>
        static string GenerateTestData(string filename, int sizeInMb = 4)
        {
            string computedMd5 = String.Empty;

            // Note: block size must be a factor of 1MB to avoid rounding errors :)
            const int blockSize = 1024 * 8;
            const int blocksPerMb = (1024 * 1024) / blockSize;
            byte[] data = new byte[blockSize];
            Random rng = new Random();
            using (FileStream stream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                for (int i = 0; i < sizeInMb * blocksPerMb; i++)
                {
                    rng.NextBytes(data);
                    stream.Write(data, 0, data.Length);
                }

                computedMd5 = ComputeMd5(stream);
            }

            return computedMd5;
        }

        static string ComputeMd5(Stream inputStream)
        {
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            inputStream.Seek(0, SeekOrigin.Begin);
            byte[] md5Hash = md5.ComputeHash(inputStream);
            return Convert.ToBase64String(md5Hash);
        }
    }
}
