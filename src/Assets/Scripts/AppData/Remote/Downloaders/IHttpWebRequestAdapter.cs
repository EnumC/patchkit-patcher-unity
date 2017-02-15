using System.Net;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public interface IHttpWebRequestAdapter
    {
        void AddRange(long start, long end);

        /// <exception cref="WebException">The time-out period for the request expired.</exception>
        /// <exception cref="WebException">An error occurred while processing the request.</exception>
        IHttpWebResponseAdapter GetResponse();
    }
}