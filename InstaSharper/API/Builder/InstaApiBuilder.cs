using System;
using System.Net.Http;
using InstaSharper.Classes;
using InstaSharper.Classes.Android.DeviceInfo;
using InstaSharper.Logger;
using Polly;

namespace InstaSharper.API.Builder
{
    public class InstaApiBuilder : IInstaApiBuilder
    {
        private int _minDelay = 0;
        private int _maxDelay = 0;
        private Policy _retryPolicy = Policy.NoOpAsync();
        private AndroidDevice _device;
        private HttpClient _httpClient;
        private HttpClientHandler _httpHandler = new HttpClientHandler();
        private IHttpRequestProcessor _httpRequestProcessor;
        private Func<object, IInstaLogger> _loggerFactory;
        private ApiRequestMessage _requestMessage;
        private UserSessionData _user;

        private InstaApiBuilder()
        { }

        /// <summary>
        ///     Create new API instance
        /// </summary>
        /// <returns>
        ///     API instance
        /// </returns>
        /// <exception cref="ArgumentNullException">User auth data must be specified</exception>
        public IInstaApi Build()
        {
            if (_user == null)
                throw new ArgumentNullException($"User auth data must be specified");
            if (_httpClient == null)
                _httpClient = new HttpClient(_httpHandler) {BaseAddress = new Uri(InstaApiConstants.INSTAGRAM_URL)};

            if (_requestMessage == null)
            {
                _device = AndroidDeviceGenerator.GetRandomAndroidDevice();
                _requestMessage = new ApiRequestMessage
                {
                    phone_id = _device.PhoneGuid.ToString(),
                    guid = _device.DeviceGuid,
                    password = _user?.Password,
                    username = _user?.UserName,
                    device_id = ApiRequestMessage.GenerateDeviceId()
                };
            }

            if (string.IsNullOrEmpty(_requestMessage.password)) _requestMessage.password = _user?.Password;
            if (string.IsNullOrEmpty(_requestMessage.username)) _requestMessage.username = _user?.UserName;

            if (_device == null && !string.IsNullOrEmpty(_requestMessage.device_id))
                _device = AndroidDeviceGenerator.GetById(_requestMessage.device_id);
            if (_device == null) AndroidDeviceGenerator.GetRandomAndroidDevice();

            if (_httpRequestProcessor == null)
                _httpRequestProcessor =
                    new HttpRequestProcessor(_minDelay, _maxDelay, _httpClient, _httpHandler, _requestMessage, _loggerFactory, _retryPolicy);

            var instaApi = new InstaApi(_user, _loggerFactory, _device, _httpRequestProcessor);
            return instaApi;
        }

        /// <summary>
        ///     Use custom logger
        /// </summary>
        /// <param name="loggerFactory">IInstaLogger implementation</param>
        /// <returns>
        ///     API Builder
        /// </returns>
        public IInstaApiBuilder UseLogger(Func<object, IInstaLogger> loggerFactory)
        {
            _loggerFactory = loggerFactory;
            return this;
        }

        /// <summary>
        ///     Set specific HttpClient
        /// </summary>
        /// <param name="httpClient">HttpClient</param>
        /// <returns>
        ///     API Builder
        /// </returns>
        public IInstaApiBuilder UseHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            return this;
        }

        /// <summary>
        ///     Set custom HttpClientHandler to be able to use certain features, e.g Proxy and so on
        /// </summary>
        /// <param name="handler">HttpClientHandler</param>
        /// <returns>
        ///     API Builder
        /// </returns>
        public IInstaApiBuilder UseHttpClientHandler(HttpClientHandler handler)
        {
            _httpHandler = handler;
            return this;
        }

        /// <summary>
        ///     Specify user login, password from here
        /// </summary>
        /// <param name="user">User auth data</param>
        /// <returns>
        ///     API Builder
        /// </returns>
        public IInstaApiBuilder SetUser(UserSessionData user)
        {
            _user = user;
            return this;
        }

        /// <summary>
        ///     Set custom request message. Used to be able to customize device info.
        /// </summary>
        /// <param name="requestMessage">Custom request message object</param>
        /// <returns>
        ///     API Builder
        /// </returns>
        /// <remarks>
        ///     Please, do not use if you don't know what you are doing
        /// </remarks>
        public IInstaApiBuilder SetApiRequestMessage(ApiRequestMessage requestMessage)
        {
            _requestMessage = requestMessage;
            return this;
        }

        /// <summary>
        ///     Set delay between requests. Useful when API supposed to be used for mass-bombing.
        /// </summary>
        /// <param name="delay">Timespan delay</param>
        /// <returns>
        ///     API Builder
        /// </returns>
        public IInstaApiBuilder SetRequestDelay(int minDelay, int maxDelay)
        {
            _minDelay = minDelay;
            _maxDelay = maxDelay;
            return this;
        }
        /// <summary>
        ///     Set Polly retry policy
        /// </summary>
        /// <param name="delay">policy</param>
        /// <returns>
        ///     API Builder
        /// </returns>
        public IInstaApiBuilder SetRetryPolicy(Policy retryPolicy)
        {
            _retryPolicy = retryPolicy;
            return this;
        }
        /// <summary>
        ///     Creates the builder.
        /// </summary>
        /// <returns></returns>
        public static IInstaApiBuilder CreateBuilder()
        {
            return new InstaApiBuilder();
        }
    }
}