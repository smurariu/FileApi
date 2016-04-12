using Owin;
using System;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Tracing;

namespace FileStorage
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            config.Services.Replace(typeof(ITraceWriter), new TraceWriter());
            config.MapHttpAttributeRoutes();
            app.UseWebApi(config);
        }

        private class TraceWriter : ITraceWriter
        {
            public void Trace(HttpRequestMessage request, string category,
                              TraceLevel level, Action<TraceRecord> traceAction)
            {
                TraceRecord traceRecord = new TraceRecord(request, category, level);
                traceAction(traceRecord);
                ShowTrace(traceRecord);
            }

            private void ShowTrace(TraceRecord traceRecord)
            {
                Console.WriteLine(
                    String.Format(
                        "[{0}] [{1}] [{2}] [{3}]: Category=[{4}], Level=[{5}] [{6}] [{7}] [{8}] [{9}]",
                        traceRecord.RequestId.ToString(),
                        traceRecord.Timestamp.ToLongTimeString(),
                        traceRecord.Request == null ? String.Empty : traceRecord.Request.Method.ToString(),
                        traceRecord.Request == null ? String.Empty : traceRecord.Request.RequestUri.ToString(),
                        traceRecord.Category,
                        traceRecord.Level,
                        traceRecord.Kind,
                        traceRecord.Operator,
                        traceRecord.Operation,
                        traceRecord.Exception != null
                            ? traceRecord.Exception.GetBaseException().Message
                            : !string.IsNullOrEmpty(traceRecord.Message)
                                ? traceRecord.Message
                                : string.Empty
                    ));
            }
        }

    }
}
