﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DownloadManager.Core.Http;
using DownloadManager.Core.Managers;
using DownloadManager.Core.Storage;
using NSubstitute;
using NUnit.Framework;

namespace DownloadManager.Core.UnitTests
{
    public class StubHttpMessageHandler : HttpMessageHandler
    {
        public StubHttpMessageHandler(HttpResponseMessage responseMessage)
        {
            ResponseMessage = responseMessage;
        }

        public HttpResponseMessage ResponseMessage { get; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(ResponseMessage);
        }
    }
    
    public class DelayHttpMessageHandler : HttpMessageHandler
    {
        public DelayHttpMessageHandler(HttpResponseMessage responseMessage)
        {
            ResponseMessage = responseMessage;
        }

        public HttpResponseMessage ResponseMessage { get; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
			await Task.Delay(TimeSpan.FromSeconds(1));
            return ResponseMessage;
        }
    }


    [TestFixture]
    public class TestHttpService
    {
        [Test]
        public void DownloadFileAsync_ReturnsObservable()
        {
            IStorage storage = Substitute.For<IStorage>();
            IHttpService httpService = new HttpService();
            MemoryStream ms = new MemoryStream();
            var observable = httpService.DownloadFileAsync("http://testsite.com", "TestFile", ms);
            Assert.NotNull(observable);
        }

        [Test]
        public async Task DownloadFileAsync_OnErrorWhenGetFails()
        {
            byte[] byteData = new byte[4096 * 5];
            Random random = new Random();
            random.NextBytes(byteData);

            MemoryStream ms = new MemoryStream();

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new ByteArrayContent(byteData, 0, byteData.Length)
            };
            StubHttpMessageHandler testmessage = new StubHttpMessageHandler(httpResponseMessage);
            HttpClient stubHttpClient = new HttpClient(testmessage);
            IHttpService httpService = new HttpService( stubHttpClient);

            var observable = httpService.DownloadFileAsync("http://testsite.com", "TestFile", ms);
            TaskCompletionSource<HttpStatusCodeException> errorResult = new TaskCompletionSource<HttpStatusCodeException>();
            
            observable.Subscribe(d =>
                { }, exception =>
                {
                    errorResult.SetResult((HttpStatusCodeException)exception); 
                });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            cancellationTokenSource.Token.Register(() =>
                { errorResult.TrySetCanceled(); });

            HttpStatusCodeException result = await errorResult.Task;

            Assert.NotNull(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task DownloadFileAsync_ReportsCorrectProgressWhenContentLengthIsNotKnown()
        {
            byte[] byteData = new byte[4096*5];
            Random random = new Random();
            random.NextBytes(byteData);

            MemoryStream ms = new MemoryStream();

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(byteData, 0, byteData.Length)
            };
            StubHttpMessageHandler testmessage = new StubHttpMessageHandler(httpResponseMessage);
            HttpClient stubHttpClient = new HttpClient(testmessage);
            IHttpService httpService = new HttpService(stubHttpClient);

            var observable = httpService.DownloadFileAsync("http://testsite.com", "TestFile", ms);
            TaskCompletionSource<bool> updateResult = new TaskCompletionSource<bool>();

            observable.Subscribe(d =>
            {
                var x = d;
                if (d > 0)
                    updateResult.SetResult(true);
            }, exception => {  });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            cancellationTokenSource.Token.Register(() =>
            { updateResult.TrySetCanceled(); });

            var result = await updateResult.Task;

            Assert.True(result);
        }

        [Test]
        public async Task DownloadFileAsync_ReportsCorrectProgressWhenContentLengthIsKnown()
        {
            byte[] byteData = new byte[4096 * 5];
            Random random = new Random();
            random.NextBytes(byteData);
            
            MemoryStream ms = new MemoryStream();
            
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(byteData, 0, byteData.Length)
            };
            StubHttpMessageHandler testmessage = new StubHttpMessageHandler(httpResponseMessage);
            HttpClient stubHttpClient = new HttpClient(testmessage);
            IHttpService httpService = new HttpService(stubHttpClient);

            var observable = httpService.DownloadFileAsync("http://testsite.com", "TestFile", ms);
            TaskCompletionSource<bool> updateResult = new TaskCompletionSource<bool>();
            int count = 0;
            observable.Subscribe(d =>
            {
                count++;
                if (d >= 1.0)
                    updateResult.SetResult(true);
            }, exception => { });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            cancellationTokenSource.Token.Register(() =>
            { updateResult.TrySetCanceled(); });

            var result = await updateResult.Task;

            Assert.AreEqual(7, count);
        }
        
        [Test]
        public async Task DownloadFileAsync_Dispose_CancelsDownload(){
			byte[] byteData = new byte[4096 * 20];
            Random random = new Random();
            random.NextBytes(byteData);

            MemoryStream ms = new MemoryStream();

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(byteData, 0, byteData.Length)
            };
            DelayHttpMessageHandler testmessage = new DelayHttpMessageHandler(httpResponseMessage);
            HttpClient stubHttpClient = new HttpClient(testmessage);
            IHttpService httpService = new HttpService(stubHttpClient);

            var observable = httpService.DownloadFileAsync("http://testsite.com", "TestFile", ms);
            TaskCompletionSource<bool> updateResult = new TaskCompletionSource<bool>();
            int count = 0;
            var disp = observable.Subscribe(d =>
            {
                count++;
				if (d > 0.0)
					updateResult.TrySetResult(true);
            }, exception => { });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            cancellationTokenSource.Token.Register(() =>
            { updateResult.TrySetCanceled(); });

            var result = await updateResult.Task;
			disp.Dispose();
			
			Assert.AreNotEqual(22, count);
		}
    }
}
