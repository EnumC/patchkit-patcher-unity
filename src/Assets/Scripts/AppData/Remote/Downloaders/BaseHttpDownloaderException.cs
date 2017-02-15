using System;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public class BaseHttpDownloaderException : Exception
    {
        public readonly BaseHttpDownloaderExceptionType Type;

        public BaseHttpDownloaderException(string message, BaseHttpDownloaderExceptionType type) : base(message)
        {
            Type = type;
        }

        public BaseHttpDownloaderException(string message, BaseHttpDownloaderExceptionType type, Exception innerException) : base(message, innerException)
        {
            Type = type;
        }
    }

    public enum BaseHttpDownloaderExceptionType
    {
        ResourceNotFound,
        UnexpectedServerResponse,
        NetworkError,
        Timeout
    }
}