using System;
using System.IO;
using System.Net;

namespace FileStorage.Client
{
    class StorageClient
    {
        static int kb = 1024;
        static int chunkSize = 1024 * kb;
        static string _apiPath = "http://localhost:8081/Api/File/";

        public static void UploadFile(string filename, string folderPath=null)
        {
            FileInfo fi = new FileInfo(filename);
            string name = fi.Name;

            if (folderPath!=null&&folderPath.Length>0)
            {
                name = Path.Combine(folderPath, name).Replace(@"\", "/");
            }

            using (var fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                //create the file on the server
                StorageClient.CreateFile(name, fi.Length);

                //transfer the file contents
                for (long i = 0; i < fi.Length; i += chunkSize)
                {
                    StorageClient.PutRange(fs, name, i, i + chunkSize);
                }
            }
        }

        internal static void CreateFolder(string folderName)
        {
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create("http://localhost:8081/Api/File/" + folderName + "?restype=directory");
            webRequest.Method = "PUT";

            (webRequest as WebRequest).Headers.Add(HttpRequestHeader.Authorization, "Bearer 5c5d3b905c00fdc9817809e324fbe4ab");
            (webRequest as WebRequest).ContentLength = 0;

            using (HttpWebResponse wr = (HttpWebResponse)webRequest.GetResponse())
            {
                using (Stream response = wr.GetResponseStream())
                {
                    // handle response stream.
                    string responseText = new StreamReader(response).ReadToEnd();
                }
            }
        }

        internal static void CreateFile(string name, long length)
        {
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create("http://localhost:8081/Api/File/" + name);
            webRequest.Method = "PUT";

            (webRequest as WebRequest).Headers.Add(HttpRequestHeader.Authorization, "Bearer 5c5d3b905c00fdc9817809e324fbe4ab");
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

        internal static void PutRange(FileStream fs, string filePath, long start, long end)
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

            (webRequest as WebRequest).Headers.Add(HttpRequestHeader.Authorization, "Bearer 5c5d3b905c00fdc9817809e324fbe4ab");
            (webRequest as WebRequest).Headers.Add("Content-MD5", Convert.ToBase64String(md5Hash));
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
                }
            }

        }

        internal static void GetFile(string serverPath, string localPath)
        {
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(_apiPath + serverPath);
            webRequest.Method = "GET";

            (webRequest as WebRequest).ContentLength = 0;
            //(webRequest as WebRequest).Headers.Add("x-ms-range", "bytes=0-256000");

            using (HttpWebResponse wr = (HttpWebResponse)webRequest.GetResponse())
            {
                using (Stream response = wr.GetResponseStream())
                {
                    // handle response stream.
                    using (Stream s = File.Create(localPath))
                    {
                        response.CopyTo(s);
                    }
                }
                Console.WriteLine("Read Status Code: " + wr.StatusCode);
                Console.WriteLine("Read Header: ");
                foreach (var headKey in wr.Headers.AllKeys)
                {
                    Console.WriteLine(" " + headKey + " = " + wr.Headers.Get(headKey) + "; ");
                }
            }
        }
    }
}
