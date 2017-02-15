using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    /// <summary>
    /// Downloads file through HTTP without any validation (such as hash checking).
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class HttpDownloader : IHttpDownloader
    {
        private const int RetriesAmount = 100;

        private const int BufferSize = 1024;

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(HttpDownloader));

        private readonly string _destinationFilePath;

        private readonly RemoteResource _resource;

        private readonly int _timeout;

        private readonly byte[] _buffer;

        private FileStream _fileStream;

        private bool _downloadHasBeenCalled;

        private bool _disposed;

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        public HttpDownloader(string destinationFilePath, RemoteResource resource, int timeout)
        {
            Checks.ArgumentValidRemoteResource(resource, "resource");
            Checks.ArgumentParentDirectoryExists(destinationFilePath, "destinationFilePath");
            Checks.ArgumentMoreThanZero(timeout, "timeout");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(destinationFilePath, "destinationFilePath");
            DebugLogger.LogVariable(resource, "resource");
            DebugLogger.LogVariable(timeout, "timeout");

            _destinationFilePath = destinationFilePath;
            _resource = resource;
            _timeout = timeout;

            _buffer = new byte[BufferSize];
        }

        private void OpenFileStream()
        {
            if (_fileStream == null)
            {
                _fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            }
        }

        private void CloseFileStream()
        {
            if (_fileStream != null)
            {
                _fileStream.Dispose();
                _fileStream = null;
            }
        }

        public void Download(CancellationToken cancellationToken)
        {
            AssertChecks.MethodCalledOnlyOnce(ref _downloadHasBeenCalled, "Download");

            DebugLogger.Log("Downloading.");

            var validUrls = new List<string>(_resource.Urls);
            validUrls.Reverse();

            int retry = RetriesAmount;

            while (validUrls.Count > 0 && retry > 0)
            {
                for (int i = validUrls.Count - 1; i >= 0 && retry-- > 0; --i)
                {
                    string url = validUrls[i];

                    try
                    {
                        OpenFileStream();

                        Download(url, cancellationToken);

                        CloseFileStream();

                        var validator = new DownloadedResourceValidator();
                        validator.Validate(_destinationFilePath, _resource);

                        return;
                    }
                    catch (DownloadedResourceValidationException validationException)
                    {
                        DebugLogger.LogException(validationException);
                        validUrls.Remove(url);
                    }
                    catch (BaseHttpDownloaderException e)
                    {
                        DebugLogger.LogException(e);
                        switch (e.Type)
                        {
                            case BaseHttpDownloaderExceptionType.UnexpectedServerResponse:
                            case BaseHttpDownloaderExceptionType.Timeout:
                            case BaseHttpDownloaderExceptionType.NetworkError:
                                // try another one
                                break;
                            case BaseHttpDownloaderExceptionType.ResourceNotFound:
                                // remove url and try another one
                                validUrls.Remove(url);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    finally
                    {
                        CloseFileStream();
                    }
                }

                DebugLogger.Log("Waiting 10 seconds before trying again...");
                Thread.Sleep(10000);
            }

            if (retry <= 0)
            {
                throw new ResourceDownloaderException("Too many retries, aborting.");
            }

            throw new ResourceDownloaderException("Cannot download resource.");
        }

        private void Download(string url, CancellationToken cancellationToken)
        {
            DebugLogger.Log(string.Format("Trying to download from {0}", url));

            ClearFileStream();

            long downloadedBytes = 0;

            IBaseHttpDownloader baseHttpDownloader = new BaseHttpDownloader(_timeout);

            using (var downloadStream = baseHttpDownloader.GetDownloadStream(url, cancellationToken))
            {
                int length;
                while ((length = downloadStream.Read(_buffer, 0, BufferSize)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _fileStream.Write(_buffer, 0, length);

                    downloadedBytes += length;
                    OnDownloadProgressChanged(downloadedBytes, _resource.Size);
                }
            }
        }

        private void ClearFileStream()
        {
            _fileStream.SetLength(0);
            _fileStream.Flush();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~HttpDownloader()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(_disposed)
            {
                return;
            }

            DebugLogger.LogDispose();

            if(disposing)
            {
                CloseFileStream();
            }

            _disposed = true;
        }

        protected virtual void OnDownloadProgressChanged(long downloadedBytes, long totalBytes)
        {
            if (DownloadProgressChanged != null) DownloadProgressChanged(downloadedBytes, totalBytes);
        }
    }
}