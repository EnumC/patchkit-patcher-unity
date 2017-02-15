using System;
using System.IO;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    /// <summary>
    /// Base HTTP downloader.
    /// </summary>
    public interface IBaseHttpDownloader : IDisposable
    {
        /// <summary>
        /// Sets the bytes range to download. 
        /// Default is whole byte range.
        /// </summary>
        /// <param name="start">The bytes range start.</param>
        /// <param name="end">The bytes range end.</param>
        void SetBytesRange(long start, long end);

        /// <summary>
        /// Returns the download stream.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="BaseHttpDownloaderException">Occurs when there are problems with downloading.</exception>
        Stream GetDownloadStream(string url, CancellationToken cancellationToken);
    }
}