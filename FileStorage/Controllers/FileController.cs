using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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
        [Route("api/File/{*filepath}")]
        public HttpResponseMessage CreateFile(string filepath)
        {
            HttpResponseMessage result = new HttpResponseMessage();
            result.StatusCode = System.Net.HttpStatusCode.BadRequest;

            long length = 0;

            if (Request.Headers.Contains("x-ms-content-length"))
            {
                length = long.Parse(Request.Headers.GetValues("x-ms-content-length").First());

                try
                {
                    if (new DirectoryInfo("files").Exists == false)
                    {
                        Directory.CreateDirectory("files");
                    }

                    string folderPath = Path.GetDirectoryName(filepath);

                    if (Directory.Exists(Path.Combine("files", folderPath)) == false)
                    {
                        result.StatusCode = System.Net.HttpStatusCode.PreconditionFailed;
                    }
                    else
                    {
                        if (File.Exists(Path.Combine("files", filepath)) == false)
                        {
                            using (FileStream fileStream = new FileStream(Path.Combine("files", filepath), FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                if (fileStream != null)
                                {
                                    result.StatusCode = System.Net.HttpStatusCode.Created;
                                    result.Headers.Add("Server", System.Environment.MachineName);
                                    fileStream.SetLength(length);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = Request.CreateErrorResponse(System.Net.HttpStatusCode.InternalServerError, ex);
                }
            }

            return result;
        }

        [HttpPut]
        [Route("api/File/{*filepath}")]
        public async Task<HttpResponseMessage> PutRange(string filepath, string comp)
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

                            var filePath = Path.Combine("files", filepath);
                            if (File.Exists(filePath) == false)
                            {
                                result.StatusCode = System.Net.HttpStatusCode.NotFound;
                            }
                            else if (writeLength > 4 * 1024 * 1024) //4Mb
                            {
                                result.StatusCode = System.Net.HttpStatusCode.RequestEntityTooLarge;
                            }
                            else if (Request.Headers.GetValues("x-ms-write").First() == "write")
                            {
                                if (writeLength == contentLength)
                                {
                                    byte[] content = new byte[writeLength];
                                    using (MemoryStream ms = new MemoryStream(content))
                                    {
                                        using (Stream stream = await Request.Content.ReadAsStreamAsync())
                                        {
                                            await stream.CopyToAsync(ms);
                                        }

                                        if (Request.Content.Headers.Contains("Content-MD5"))
                                        {
                                            string receivedMd5 = Request.Content.Headers.GetValues("Content-MD5").First();

                                            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                                            byte[] md5Hash = md5.ComputeHash(content);
                                            string computedMd5 = Convert.ToBase64String(md5Hash);

                                            if (String.CompareOrdinal(receivedMd5, computedMd5) == 0)
                                            {
                                                await WriteBytes(filePath, startPosition, ms);
                                                result.StatusCode = System.Net.HttpStatusCode.Created;
                                            }
                                        }
                                        else
                                        {
                                            await WriteBytes(filePath, startPosition, ms);
                                            result.StatusCode = System.Net.HttpStatusCode.Created;
                                        }
                                    }
                                }
                            }
                            else if (Request.Headers.GetValues("x-ms-write").First() == "clear")
                            {
                                if (Request.Content.Headers.Contains("Content-MD5") == false)
                                {
                                    //fill range with 0s
                                    byte[] dataToWrite = new byte[writeLength];
                                    using (MemoryStream ms = new MemoryStream(dataToWrite))
                                    {
                                        await WriteBytes(filePath, startPosition, ms);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private async Task WriteBytes(string filePath, long startPosition, Stream content)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.Read))
            {
                content.Seek(0, SeekOrigin.Begin);
                fs.Seek(startPosition, SeekOrigin.Begin);
                await content.CopyToAsync(fs);
            }
        }

        [HttpGet]
        [Route("api/File/{*filepath}")]
        public HttpResponseMessage GetFile(string filepath)
        {
            HttpResponseMessage result = new HttpResponseMessage();
            result.StatusCode = System.Net.HttpStatusCode.InternalServerError;

            long length = 0;
            try
            {
                if (new DirectoryInfo("files").Exists == false)
                {
                    return result;
                }

                string folderPath = Path.GetDirectoryName(filepath);

                if (Directory.Exists(Path.Combine("files", folderPath)) == false)
                {
                    result.StatusCode = System.Net.HttpStatusCode.NotFound;
                }
                else
                {
                    if (File.Exists(Path.Combine("files", filepath)) == false)
                    {
                        result.StatusCode = System.Net.HttpStatusCode.NotFound;
                    }
                    else
                    {
                        var stream = new FileStream(Path.Combine("files", filepath), FileMode.Open,
                                FileAccess.Read, FileShare.None);
                        result.Content = new StreamContent(stream);

                        result.Headers.Add("Server", System.Environment.MachineName);
                        result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                        result.StatusCode = System.Net.HttpStatusCode.OK;
                    }
                }
            }
            catch (Exception ex)
            {
                result = Request.CreateErrorResponse(System.Net.HttpStatusCode.InternalServerError, ex);
            }

            return result;
        }
    }
}
