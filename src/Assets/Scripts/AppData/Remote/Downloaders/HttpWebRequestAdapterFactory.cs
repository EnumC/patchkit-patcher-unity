using System.Net;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public class HttpWebRequestAdapterFactory : IHttpWebRequestAdapterFactory
    {
        private readonly int _timeout;

        public HttpWebRequestAdapterFactory(int timeout)
        {
            _timeout = timeout;

            ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, errors) => true;
            ServicePointManager.DefaultConnectionLimit = 65535;
        }

        public IHttpWebRequestAdapter CreateGetRequest(string url)
        {
            var webRequest = (HttpWebRequest) WebRequest.Create(url);
            webRequest.Timeout = _timeout;
            webRequest.Method = "GET";

            return new HttpWebRequestAdapter(webRequest);
        }
    }
}