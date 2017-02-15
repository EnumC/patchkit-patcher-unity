using System.Net;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public class HttpWebRequestAdapter : IHttpWebRequestAdapter
    {
        private readonly HttpWebRequest _httpWebRequest;

        public HttpWebRequestAdapter(HttpWebRequest httpWebRequest)
        {
            AssertChecks.ArgumentNotNull(httpWebRequest, "httpWebRequest");

            _httpWebRequest = httpWebRequest;
        }

        public void AddRange(long start, long end)
        {
            _httpWebRequest.AddRange(start, end);
        }

        public IHttpWebResponseAdapter GetResponse()
        {
            // Why catch and retreive response from exception - http://stackoverflow.com/a/14385202

            WebResponse response;

            try
            {
                response = _httpWebRequest.GetResponse();
            }
            catch (WebException webException)
            {
                if (webException.Response != null && webException.Status == WebExceptionStatus.ProtocolError)
                {
                    response = webException.Response;
                }
                else
                {
                    throw;
                }
            }

            return new HttpWebResponseAdapter((HttpWebResponse)response);
        }
    }
}