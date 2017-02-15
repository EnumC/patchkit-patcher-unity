using System;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public class ResourceDownloaderException : Exception
    {
        public ResourceDownloaderException(string message) : base(message)
        {
        }

        public ResourceDownloaderException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}