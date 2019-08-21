using System;
using System.Net.Http;
using System.Threading.Tasks;
using InstaSharper.Classes.Android.DeviceInfo;
using InstaSharper.Logger;
using Polly;

namespace InstaSharper.Classes
{
    internal class HttpRequestProcessor : IHttpRequestProcessor
    {
        private int _maxDelay;
        private int _minDelay;
        private readonly Random _random = new Random();
        private TimeSpan _delay 
            => TimeSpan.FromMilliseconds(_random.Next(_minDelay, _maxDelay));
        private readonly IInstaLogger _logger;
        private Policy _polly;

        public HttpRequestProcessor(int minDelay, int maxDelay, HttpClient httpClient, HttpClientHandler httpHandler,
            ApiRequestMessage requestMessage, Func<object, IInstaLogger> loggerFactory, Policy retryPolicy)
        {
            _minDelay = minDelay;
            _maxDelay = maxDelay;
            Client = httpClient;
            HttpHandler = httpHandler;
            RequestMessage = requestMessage;
            _logger = loggerFactory(this);
            _polly = retryPolicy;
        }

        public HttpClientHandler HttpHandler { get; }
        public ApiRequestMessage RequestMessage { get; }
        public HttpClient Client { get; }

        public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> requestMessageFactory)
        {
            if (_maxDelay > 0)
                await Task.Delay(_delay);
            
            var response = await _polly.ExecuteAsync(async () =>
            {
                var msg = requestMessageFactory();
                LogHttpRequest(msg);
                return await Client.SendAsync(msg);
            });
            //var response = await Client.SendAsync(requestMessage);
            LogHttpResponse(response);
            return response;
        }

        public async Task<HttpResponseMessage> GetAsync(Uri requestUri)
        {
            _logger?.LogRequest(requestUri);
            if (_maxDelay > 0)
                await Task.Delay(_delay);
            var response = await Client.GetAsync(requestUri);
            LogHttpResponse(response);
            return response;
        }

        public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> requestMessageFactory,
            HttpCompletionOption completionOption)
        {

            if (_maxDelay > 0)
                await Task.Delay(_delay);
            var response = await _polly.ExecuteAsync(async () =>
            {
                var msg = requestMessageFactory();
                LogHttpRequest(msg);
                return await Client.SendAsync(msg, completionOption);
            });
            return response;
        }

        public async Task<string> SendAndGetJsonAsync(Func<HttpRequestMessage> requestMessageFactory,
            HttpCompletionOption completionOption)
        {
            var requestMessage = requestMessageFactory();
            LogHttpRequest(requestMessage);
            if (_maxDelay > 0)
                await Task.Delay(_delay);
            var response = await Client.SendAsync(requestMessage, completionOption);
            LogHttpResponse(response);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GeJsonAsync(Uri requestUri)
        {
            _logger?.LogRequest(requestUri);
            if (_maxDelay > 0)
                await Task.Delay(_delay);
            var response = await Client.GetAsync(requestUri);
            LogHttpResponse(response);
            return await response.Content.ReadAsStringAsync();
        }

        private void LogHttpRequest(HttpRequestMessage request)
        {
            _logger?.LogRequest(request);
        }

        private void LogHttpResponse(HttpResponseMessage request)
        {
            _logger?.LogResponse(request);
        }

        public void SetDelay(int min, int max)
        {
            _minDelay = min;
            _maxDelay = max;
        }
    }
}