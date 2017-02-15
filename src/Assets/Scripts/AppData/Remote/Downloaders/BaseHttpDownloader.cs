using System;
using System.IO;
using System.Net;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public sealed class BaseHttpDownloader : IBaseHttpDownloader
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(BaseHttpDownloader));

        private readonly IHttpWebRequestAdapterFactory _httpWebRequestAdapterFactory;

        private bool _downloadHasBeenCalled;
        private long _bytesRangeStart;
        private long _bytesRangeEnd;

        private string _url;
        private IHttpWebRequestAdapter _request;
        private IHttpWebResponseAdapter _response;

        public BaseHttpDownloader(int timeout) :
            this(new HttpWebRequestAdapterFactory(timeout))
        {
        }

        public BaseHttpDownloader(IHttpWebRequestAdapterFactory httpWebRequestAdapterFactory)
        {
            AssertChecks.ArgumentNotNull(httpWebRequestAdapterFactory, "httpWebRequestAdapterFactory");

            DebugLogger.LogConstructor();

            _httpWebRequestAdapterFactory = httpWebRequestAdapterFactory;

            _bytesRangeStart = 0;
            _bytesRangeEnd = -1;
        }

        public void SetBytesRange(long bytesRangeStart, long bytesRangeEnd)
        {
            DebugLogger.Log("Setting bytes range.");

            DebugLogger.LogVariable(bytesRangeStart, "bytesRangeStart");
            DebugLogger.LogVariable(bytesRangeEnd, "bytesRangeEnd");

            _bytesRangeStart = bytesRangeStart;
            _bytesRangeEnd = bytesRangeEnd;
        }

        public Stream GetDownloadStream(string url, CancellationToken cancellationToken)
        {
            AssertChecks.MethodCalledOnlyOnce(ref _downloadHasBeenCalled, "Download");
            Checks.ArgumentNotNullOrEmpty(url, "url");

            DebugLogger.Log("Downloading.");
            DebugLogger.LogVariable(url, "url");

            _url = url;

            CreateRequest();

            cancellationToken.ThrowIfCancellationRequested();

            RetrieveResponse();
            VerifyResponse();
            return RetrieveResponseStream();
        }

        private void CreateRequest()
        {
            DebugLogger.Log("Creating request.");

            _request = _httpWebRequestAdapterFactory.CreateGetRequest(_url);
            _request.AddRange(_bytesRangeStart, _bytesRangeEnd);
        }

        private void RetrieveResponse()
        {
            DebugLogger.Log("Retrieving response from request.");

            try
            {
                _response = _request.GetResponse();
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.Timeout)
                {
                    throw new BaseHttpDownloaderException(string.Format("Timeout <{0}>.", _url), BaseHttpDownloaderExceptionType.Timeout);
                }

                throw new BaseHttpDownloaderException(string.Format("Connection error '{0}' <{1}>.", webException.Status,
                    _url), BaseHttpDownloaderExceptionType.NetworkError, webException);
            }
        }

        private void VerifyResponse()
        {
            DebugLogger.Log("Veryfing response.");

            if (_response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new BaseHttpDownloaderException(string.Format("Resource not found <{0}>.", _url),
                    BaseHttpDownloaderExceptionType.ResourceNotFound);
            }

            if (_response.StatusCode != HttpStatusCode.OK && _response.StatusCode != HttpStatusCode.PartialContent)
            {
                throw new BaseHttpDownloaderException(
                    string.Format("Unexpected server response '{0} ({1})' <{2}>. ", _response.StatusCode,
                        (int) _response.StatusCode,
                        _url),
                    BaseHttpDownloaderExceptionType.UnexpectedServerResponse);
            }
        }

        private Stream RetrieveResponseStream()
        {
            DebugLogger.Log("Retrieving stream.");

            return _response.GetResponseStream();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BaseHttpDownloader()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_response != null)
                {
                    _response.Dispose();
                }
            }
        }
    }
}