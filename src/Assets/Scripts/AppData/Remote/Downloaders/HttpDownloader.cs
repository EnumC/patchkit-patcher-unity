﻿using System;
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
    public class HttpDownloader : IDisposable
    {
        private const int RetriesAmount = 100;

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(HttpDownloader));

        private readonly string[] _urls;

        private readonly long _size;

        private readonly int _timeout;

        private readonly FileStream _fileStream;

        private bool _downloadHasBeenCalled;

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        public HttpDownloader(string destinationFilePath, RemoteResource resource, int timeout) : 
            this(destinationFilePath, resource.Urls, resource.Size, timeout)
        {
            Checks.ArgumentValidRemoteResource(resource, "resource");
        }

        public HttpDownloader(string destinationFilePath, string[] urls, long size, int timeout)
        {
            Checks.ArgumentParentDirectoryExists(destinationFilePath, "destinationFilePath");
            AssertChecks.ArgumentNotNull(urls, "urls");
            Checks.ArgumentMoreThanZero(timeout, "timeout");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(destinationFilePath, "destinationFilePath");
            DebugLogger.LogVariable(timeout, "timeout");

            _urls = urls;
            _size = size;
            _timeout = timeout;

            _fileStream = new FileStream(destinationFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        }

        public void Download(CancellationToken cancellationToken)
        {
            AssertChecks.MethodCalledOnlyOnce(ref _downloadHasBeenCalled, "Download");

            DebugLogger.Log("Starting download.");

            var validUrls = new List<string>(_urls);
            validUrls.Reverse();

            int retry = RetriesAmount;

            while (validUrls.Count > 0 && retry > 0)
            {
                for (int i = validUrls.Count - 1; i >= 0 && retry-- > 0; --i)
                {
                    string url = validUrls[i];

                    try
                    {
                        Download(url, cancellationToken);
                        return;
                    }
                    catch (DownloaderException downloaderException)
                    {
                        DebugLogger.LogException(downloaderException);
                        switch (downloaderException.Status)
                        {
                            case DownloaderExceptionStatus.EmptyStream:
                                // try another one
                                break;
                            case DownloaderExceptionStatus.CorruptData:
                                // try another one
                                break;
                            case DownloaderExceptionStatus.NotFound:
                                // remove url and try another one
                                validUrls.Remove(url);
                                break;
                            case DownloaderExceptionStatus.Other:
                                // try another one
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    catch (Exception exception)
                    {
                        DebugLogger.LogException(exception);
                        // try another one
                    }
                }

                DebugLogger.Log("Waiting 10 seconds before trying again...");
                Thread.Sleep(10000);
            }

            if (retry <= 0)
            {
                throw new DownloaderException("Too many retries, aborting.", DownloaderExceptionStatus.Other);
            }

            throw new DownloaderException("Cannot download resource.", DownloaderExceptionStatus.Other);
        }

        private void Download(string url, CancellationToken cancellationToken)
        {
            DebugLogger.Log("Trying to download from " + url);

            ClearFileStream();

            long downloadedBytes = 0;

            BaseHttpDownloader baseHttpDownloader = new BaseHttpDownloader(url, _timeout);
            baseHttpDownloader.DataAvailable += (bytes, length) =>
            {
                _fileStream.Write(bytes, 0, length);

                downloadedBytes += length;
                OnDownloadProgressChanged(downloadedBytes, _size);
            };

            baseHttpDownloader.Download(cancellationToken);
        }

        private void ClearFileStream()
        {
            _fileStream.SetLength(0);
            _fileStream.Flush();
        }

        public void Dispose()
        {
            _fileStream.Dispose();
        }

        protected virtual void OnDownloadProgressChanged(long downloadedBytes, long totalBytes)
        {
            if (DownloadProgressChanged != null) DownloadProgressChanged(downloadedBytes, totalBytes);
        }
    }
}