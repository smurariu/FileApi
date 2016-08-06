using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace FileStorage.Controllers
{
    public class FileController : ApiController
    {
        [HttpPut]
        [Route("api/File/{*filepath}")]
        public HttpResponseMessage CreateFile(string filepath)
        {
            HttpResponseMessage result = new HttpResponseMessage();
            result.StatusCode = HttpStatusCode.BadRequest;

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
                        result.StatusCode = HttpStatusCode.PreconditionFailed;
                    }
                    else
                    {
                        if (File.Exists(Path.Combine("files", filepath)) == false)
                        {
                            using (FileStream fileStream = new FileStream(Path.Combine("files", filepath), FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                if (fileStream != null)
                                {
                                    result.StatusCode = HttpStatusCode.Created;
                                    result.Headers.Add("Server", Environment.MachineName);
                                    fileStream.SetLength(length);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
                }
            }

            return result;
        }

        [HttpPut]
        [Route("api/File/{*folderPath}")]
        public HttpResponseMessage CreateFolder(string folderPath, string restype)
        {
            HttpResponseMessage result = new HttpResponseMessage();
            result.StatusCode = HttpStatusCode.BadRequest;


            if (restype == "directory")
            {
                if (new DirectoryInfo("files").Exists == false)
                {
                    Directory.CreateDirectory("files");
                }
            }

            string parentFolderPath = Path.GetDirectoryName(folderPath);

            if (Directory.Exists(Path.Combine("files", parentFolderPath)) == false)
            {
                result.StatusCode = HttpStatusCode.PreconditionFailed;
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(Path.Combine("files", folderPath));
                    result.StatusCode = HttpStatusCode.Created;
                    result.Headers.Add("Server", Environment.MachineName);
                }
                catch (Exception ex)
                {
                    result = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
                }
            }

            return result;
        }


        [HttpPut]
        [Route("api/File/{*filepath}")]
        public async Task<HttpResponseMessage> PutRange(string filepath, string comp)
        {
            HttpResponseMessage result = new HttpResponseMessage();
            result.StatusCode = HttpStatusCode.BadRequest;

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
                                result.StatusCode = HttpStatusCode.NotFound;
                            }
                            else if (writeLength > 4 * 1024 * 1024) //4Mb
                            {
                                result.StatusCode = HttpStatusCode.RequestEntityTooLarge;
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
                                                result.StatusCode = HttpStatusCode.Created;
                                            }
                                        }
                                        else
                                        {
                                            await WriteBytes(filePath, startPosition, ms);
                                            result.StatusCode = HttpStatusCode.Created;
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

        [HttpGet]
        [Route("api/File/{*filepath}")]
        public HttpResponseMessage GetFile(string filepath)
        {
            HttpResponseMessage result = new HttpResponseMessage();
            result.StatusCode = HttpStatusCode.OK;

            try
            {
                if (new DirectoryInfo("files").Exists == false)
                {
                    Directory.CreateDirectory("files");
                }

                string folderPath = Path.GetDirectoryName(filepath);

                if (Directory.Exists(Path.Combine("files", folderPath)) == false)
                {
                    result.StatusCode = HttpStatusCode.NotFound;
                }
                else
                {
                    if (File.Exists(Path.Combine("files", filepath)) == false)
                    {
                        result.StatusCode = HttpStatusCode.NotFound;
                    }
                    else
                    {
                        IEnumerable<string> headXMSRangeList;
                        IEnumerable<string> headRangeList;
                        Request.Headers.TryGetValues("x-ms-range", out headXMSRangeList);
                        Request.Headers.TryGetValues("Range", out headRangeList);

                        string headRange = null;
                        if (headXMSRangeList != null && headXMSRangeList.Any())
                        {
                            headRange = headXMSRangeList.FirstOrDefault();
                        }
                        else if (headRangeList != null)
                        {
                            headRange = headRangeList.FirstOrDefault();
                        }

                        long startPosition = 0;
                        long endPosition = -1;

                        if (headRange != null)
                        {
                            string rangeHeader = headRange.Replace("bytes=", "");
                            string[] range = rangeHeader.Split('-');
                            long.TryParse(range[0], out startPosition);
                            if (range[1].Trim().Length > 0)
                            {
                                long.TryParse(range[1], out endPosition);
                            }
                        }

                        //build the response
                        //http://www.strathweb.com/2013/01/asynchronously-streaming-video-with-asp-net-web-api/

                        FileStreaming fileStreaming = new FileStreaming(Path.Combine("files", filepath), startPosition, endPosition);
                        result.Content = new PushStreamContent((a, b, c) => { fileStreaming.WriteToStream(a, b, c); }, new MediaTypeHeaderValue("application/octet-stream"));
                        result.Headers.Add("Server", Environment.MachineName);
                        result.Headers.Add("Accept-Ranges", "bytes");
                        result.Content.Headers.LastModified = File.GetLastWriteTime(Path.Combine("files", filepath));

                        if (headRange != null)
                        {
                            result.Content.Headers.ContentRange = new ContentRangeHeaderValue(startPosition, endPosition);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }

            return result;
        }

        private static async Task WriteBytes(string filePath, long startPosition, Stream content)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.Read))
            {
                content.Seek(0, SeekOrigin.Begin);
                fs.Seek(startPosition, SeekOrigin.Begin);
                await content.CopyToAsync(fs);
            }
        }
    }

    public class FileStreaming
    {
        private readonly string _filepath;
        private long _startPosition;
        private long _endPosition;

        public FileStreaming(string filepath, long startPosition, long endPosition)
        {
            _filepath = filepath;
            _startPosition = startPosition;
            _endPosition = endPosition;
        }

        public async void WriteToStream(Stream outputStream, HttpContent content, TransportContext context)
        {
            try
            {
                var buffer = new byte[65536];

                using (var file = File.Open(_filepath, FileMode.Open, FileAccess.Read))
                {
                    file.Position = _startPosition;
                    if (_endPosition == -1 || _endPosition > file.Length)
                    {
                        _endPosition = file.Length;
                    }

                    var bytesRead = 1;
                    var readLength = _endPosition - _startPosition;

                    while (readLength > 0 && bytesRead > 0)
                    {
                        bytesRead = file.Read(buffer, 0, Math.Min((int)/*don't like this at all*/readLength, buffer.Length));
                        await outputStream.WriteAsync(buffer, 0, bytesRead);
                        readLength -= bytesRead;
                    }
                }
            }
            catch (HttpException ex)
            {
                Console.WriteLine(ex);
                return;
            }
            finally
            {
                outputStream.Close();
            }
        }
    }
}
