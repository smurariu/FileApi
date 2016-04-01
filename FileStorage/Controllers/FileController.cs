using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace FileStorage.Controllers
{
    public class FileController : ApiController
    {
        [HttpGet]

        public object Get(string id)
        {
            return new string[] { id, "string1", "string2" };
        }

        [HttpPut]
        public HttpResponseMessage CreateFile(string id)
        {
            HttpResponseMessage result = new HttpResponseMessage();

            long length = 0;

            bool isValidRequest = true;
            if (Request.Headers.Contains("x-ms-content-length") == false)
            {
                isValidRequest = false;
            }
            else
            {
                length = long.Parse(Request.Headers.GetValues("x-ms-content-length").First());
            }

            if (isValidRequest == false)
            {
                result.StatusCode = System.Net.HttpStatusCode.BadRequest;
            }
            else
            {
                try
                {
                    if (new DirectoryInfo("files").Exists == false)
                    {
                        Directory.CreateDirectory("files");
                    }

                }
                catch (Exception ex)
                {
                    result = Request.CreateErrorResponse(System.Net.HttpStatusCode.InternalServerError, ex);
                }

                using (FileStream fileStream = new FileStream(Path.Combine("files", id), FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    if (fileStream != null)
                    {
                        result.StatusCode = System.Net.HttpStatusCode.Created;
                        result.Headers.Add("Server", System.Environment.MachineName);
                        fileStream.SetLength(length);
                    }
                }
            }

            return result;
        }

        [HttpPut]

        public HttpResponseMessage PutRange(string id, string comp)
        {
            HttpResponseMessage result = new HttpResponseMessage();
            result.StatusCode = System.Net.HttpStatusCode.BadRequest;

            if (comp == "range")
            {
                if (Request.Headers.Contains("x-ms-range") != false && Request.Headers.Contains("x-ms-write") != false)
                {
                    string specifiedLength = Request.Headers.GetValues("x-ms-content-length").First();
                    string rangeSpecification = Request.Headers.GetValues("x-ms-range").First();
                    if (rangeSpecification.StartsWith("bytes="))
                    {
                        rangeSpecification = rangeSpecification.Replace("bytes=", "");
                        var ranges = rangeSpecification.Split('-');
                        if (ranges.Length == 2)
                        {
                            long startPosition = long.Parse(ranges[0]);
                            long endPosition = long.Parse(ranges[1]);
                            long writeLength = endPosition - startPosition;
                            long contentLength = long.Parse(specifiedLength);

                            if (writeLength == contentLength)
                            {
                                if (Request.Headers.GetValues("x-ms-write").First() == "write")
                                {
                                    var filePath = Path.Combine("files", id);
                                    if (File.Exists(filePath) == false)
                                    {
                                        result.StatusCode = System.Net.HttpStatusCode.NotFound;
                                    }
                                    else if (writeLength > 4 * 1024 * 1024) //4Mb
                                    {
                                        result.StatusCode = System.Net.HttpStatusCode.RequestEntityTooLarge;
                                    }
                                    else
                                    {
                                        var stream = Request.Content.ReadAsStreamAsync().Result;
                                        byte[] content = new byte[writeLength];

                                        stream.Read(content, 0, (int)writeLength);

                                        if (Request.Content.Headers.Contains("Content-MD5"))
                                        {
                                            string receivedMd5 = Request.Content.Headers.GetValues("Content-MD5").First();

                                            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                                            byte[] md5Hash = md5.ComputeHash(content);
                                            string computedMd5 = System.Convert.ToBase64String(md5Hash);

                                            if (String.CompareOrdinal(receivedMd5, computedMd5) == 0)
                                            {
                                                WriteBytes(filePath, startPosition, content);
                                                result.StatusCode = System.Net.HttpStatusCode.Created;
                                            }
                                        }
                                        else
                                        {
                                            WriteBytes(filePath, startPosition, content);
                                            result.StatusCode = System.Net.HttpStatusCode.Created;
                                        }
                                    }
                                }
                                else if (Request.Headers.GetValues("x-ms-write").First() == "clear")
                                {
                                    if (Request.Content.Headers.Contains("Content-MD5")==false)
                                    {

                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private void WriteBytes(string filePath, long startPosition, byte[] content)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.Read))
            {
                fs.Seek(startPosition, SeekOrigin.Begin);
                foreach (byte arrayByte in content)
                {
                    fs.WriteByte(arrayByte);
                }
            }
        }
    }
}
