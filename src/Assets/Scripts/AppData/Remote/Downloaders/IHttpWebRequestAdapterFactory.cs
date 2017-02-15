namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public interface IHttpWebRequestAdapterFactory
    {
        IHttpWebRequestAdapter CreateGetRequest(string url);
    }
}