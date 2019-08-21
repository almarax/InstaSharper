using System;
using System.Net.Http;
using InstaSharper.Classes;
using InstaSharper.Classes.Android.DeviceInfo;
using InstaSharper.Logger;
using Polly;

namespace InstaSharper.API.Builder
{
    public interface IInstaApiBuilder
    {
        /// <summary>
        ///     Create new API instance
        /// </summary>
        /// <returns>API instance</returns>
        IInstaApi Build();

        /// <summary>
        ///     Use custom logger
        /// </summary>
        /// <param name="logger">IInstaLogger implementation</param>
        /// <returns>API Builder</returns>
        IInstaApiBuilder UseLogger(Func<object, IInstaLogger> loggerFactory);

        /// <summary>
        ///     Set specific HttpClient
        /// </summary>
        /// <param name="httpClient">HttpClient</param>
        /// <returns>API Builder</returns>
        IInstaApiBuilder UseHttpClient(HttpClient httpClient);

        /// <summary>
        ///     Set custom HttpClientHandler to be able to use certain features, e.g Proxy and so on
        /// </summary>
        /// <param name="handler">HttpClientHandler</param>
        /// <returns>API Builder</returns>
        IInstaApiBuilder UseHttpClientHandler(HttpClientHandler handler);


        /// <summary>
        ///     Specify user login, password from here
        /// </summary>
        /// <param name="user">User auth data</param>
        /// <returns>API Builder</returns>
        IInstaApiBuilder SetUser(UserSessionData user);

        /// <summary>
        ///     Set custom request message. Used to be able to customize device info.
        /// </summary>
        /// <param name="requestMessage">Custom request message object</param>
        /// <remarks>Please, do not use if you don't know what you are doing</remarks>
        /// <returns>API Builder</returns>
        IInstaApiBuilder SetApiRequestMessage(ApiRequestMessage requestMessage);

        /// <summary>
        ///     Set delay between requests in milliseconds. Useful when API supposed to be used for mass-bombing.
        /// </summary>
        /// <param name="minDelay">min delay in ms</param>
        /// <param name="maxDelay">max delay in ms</param>
        /// <returns>API Builder</returns>
        IInstaApiBuilder SetRequestDelay(int minDelay, int maxDelay);
        /// <summary>
        ///     Set Polly retry policy
        /// </summary>
        /// <param name="delay">policy</param>
        /// <returns>
        ///     API Builder
        /// </returns>
        IInstaApiBuilder SetRetryPolicy(Policy retryPolicy);
    }
}