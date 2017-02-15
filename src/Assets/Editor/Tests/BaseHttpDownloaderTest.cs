using System;
using System.IO;
using System.Net;
using NUnit.Framework;
using NSubstitute;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;
using PatchKit.Unity.Patcher.Cancellation;

public class BaseHttpDownloaderTest
{
    private byte[] CreateRandomData(int length)
    {
        byte[] data = new byte[length];
        new Random().NextBytes(data);
        return data;
    }

    private class Mock
    {
        public IHttpWebRequestAdapterFactory RequestFactory;
        public IHttpWebRequestAdapter Request;
        public IHttpWebResponseAdapter Response;
    }

    private void FillMockWithRequestFactory(Mock mock)
    {
        mock.RequestFactory = Substitute.For<IHttpWebRequestAdapterFactory>();
        mock.RequestFactory.CreateGetRequest(Arg.Any<string>()).Returns(mock.Request);
    }

    private Mock CreateMock(byte[] data, HttpStatusCode statusCode)
    {
        var mock = new Mock();

        mock.Response = Substitute.For<IHttpWebResponseAdapter>();
        mock.Response.GetResponseStream().Returns(new MemoryStream(data));
        mock.Response.StatusCode.Returns(statusCode);

        mock.Request = Substitute.For<IHttpWebRequestAdapter>();
        mock.Request.GetResponse().Returns(mock.Response);

        FillMockWithRequestFactory(mock);
        return mock;
    }

    private Mock CreateMock(WebException webException)
    {
        var mock = new Mock();

        mock.Request = Substitute.For<IHttpWebRequestAdapter>();
        mock.Request.GetResponse().Returns(x => { throw webException; });

        FillMockWithRequestFactory(mock);

        return mock;
    }

    private void AssertValidDownloadStream(byte[] data, Stream stream)
    {
        byte[] buffer = new byte[1];

        for (int i = 0; i < data.Length; i++)
        {
            int length = stream.Read(buffer, 0, 1);

            Assert.IsTrue(length > 0, string.Format("Unexpected end of stream at byte {0}.", i));

            Assert.AreEqual(data[i], buffer[0], string.Format("Output stream is different at byte {0}.", i));
        }
    }

    [Test]
    public void GetDownloadStream_ForResponseStatusOK_ReturnsDownloadStream()
    {
        var data = CreateRandomData(100);
        var mock = CreateMock(data, HttpStatusCode.OK);

        using (var downloader = new BaseHttpDownloader(mock.RequestFactory))
        {
            using (var downloadStream = downloader.GetDownloadStream("http://dummy_url.com/dummy_file",
                    CancellationToken.Empty))
            {
                AssertValidDownloadStream(data, downloadStream);
            }
        }
    }

    [Test]
    public void GetDownloadStream_ForResponseStatusPartialContent_ReturnsDownloadStream()
    {
        var data = CreateRandomData(100);
        var mock = CreateMock(data, HttpStatusCode.PartialContent);

        using (var downloader = new BaseHttpDownloader(mock.RequestFactory))
        {
            using (var downloadStream = downloader.GetDownloadStream("http://dummy_url.com/dummy_file",
                    CancellationToken.Empty))
            {
                AssertValidDownloadStream(data, downloadStream);
            }
        }
    }

    [Test]
    public void GetDownloadStream_ForResponseStatusNotFound_RaiseDownloaderException()
    {
        var data = CreateRandomData(100);
        var mock = CreateMock(data, HttpStatusCode.NotFound);

        using (var downloader = new BaseHttpDownloader(mock.RequestFactory))
        {
            var e = Assert.Throws<BaseHttpDownloaderException>(
                () => downloader.GetDownloadStream("http://dummy_url.com/dummy_file",
                    CancellationToken.Empty));

            Assert.AreEqual(BaseHttpDownloaderExceptionType.ResourceNotFound, e.Type,
                "Expected other exception type.");
        }
    }

    [Test]
    public void GetDownloadStream_ForResponseStatusForbidden_RaiseDownloaderException()
    {
        var data = CreateRandomData(100);
        var mock = CreateMock(data, HttpStatusCode.Forbidden);

        using (var downloader = new BaseHttpDownloader(mock.RequestFactory))
        {
            var e = Assert.Throws<BaseHttpDownloaderException>(
                () => downloader.GetDownloadStream("http://dummy_url.com/dummy_file",
                    CancellationToken.Empty));

            Assert.AreEqual(BaseHttpDownloaderExceptionType.UnexpectedServerResponse, e.Type,
                "Expected other exception type.");
        }
    }

    [Test]
    public void GetDownloadStream_ForWebExceptionTimeout_RaiseDownloaderException()
    {
        var mock = CreateMock(new WebException(string.Empty, WebExceptionStatus.Timeout));

        using (var downloader = new BaseHttpDownloader(mock.RequestFactory))
        {
            var e = Assert.Throws<BaseHttpDownloaderException>(
                () => downloader.GetDownloadStream("http://dummy_url.com/dummy_file",
                    CancellationToken.Empty));

            Assert.AreEqual(BaseHttpDownloaderExceptionType.Timeout, e.Type,
                "Expected other exception type.");
        }
    }

    [Test]
    public void GetDownloadStream_ForWebExceptionConnectFailure_RaiseDownloaderException()
    {
        var mock = CreateMock(new WebException(string.Empty, WebExceptionStatus.ConnectFailure));

        using (var downloader = new BaseHttpDownloader(mock.RequestFactory))
        {
            var e = Assert.Throws<BaseHttpDownloaderException>(
                () => downloader.GetDownloadStream("http://dummy_url.com/dummy_file",
                    CancellationToken.Empty));

            Assert.AreEqual(BaseHttpDownloaderExceptionType.NetworkError, e.Type,
                "Expected other exception type.");
        }
    }

    [Test]
    public void GetDownloadStream_PreceedBySetBytesRange_RequestsPartialContent()
    {
        var data = CreateRandomData(100);
        var mock = CreateMock(data, HttpStatusCode.PartialContent);

        using (var downloader = new BaseHttpDownloader(mock.RequestFactory))
        {
            downloader.SetBytesRange(20, 40);

            using (downloader.GetDownloadStream("http://dummy_url.com/dummy_file", CancellationToken.Empty))
            {
                mock.Request.Received().AddRange(20, 40);
            }
        }
    }
}