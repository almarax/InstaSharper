using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using InstaSharper.Classes;
using InstaSharper.Classes.Android.DeviceInfo;
using InstaSharper.Classes.Models;
using InstaSharper.Classes.ResponseWrappers;
using InstaSharper.Classes.ResponseWrappers.BaseResponse;
using InstaSharper.Converters;
using InstaSharper.Converters.Json;
using InstaSharper.Helpers;
using InstaSharper.Logger;
using Newtonsoft.Json;

namespace InstaSharper.API.Processors
{
    public class MessagingProcessor : IMessagingProcessor
    {
        private readonly AndroidDevice _deviceInfo;
        private readonly IHttpRequestProcessor _httpRequestProcessor;
        private readonly IInstaLogger _logger;
        private readonly UserSessionData _user;

        public MessagingProcessor(AndroidDevice deviceInfo, UserSessionData user,
            IHttpRequestProcessor httpRequestProcessor, Func<object, IInstaLogger> loggerFactory)
        {
            _deviceInfo = deviceInfo;
            _user = user;
            _httpRequestProcessor = httpRequestProcessor;
            _logger = loggerFactory(this);
        }

        public async Task<IResult<InstaDirectInboxContainer>> GetDirectInboxAsync()
        {
            try
            {
                var directInboxUri = UriCreator.GetDirectInboxUri();
                var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetDefaultRequest(HttpMethod.Get, directInboxUri, _deviceInfo));
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaDirectInboxContainer>(response, json);
                var inboxResponse = JsonConvert.DeserializeObject<InstaDirectInboxContainerResponse>(json);
                var converter = ConvertersFabric.Instance.GetDirectInboxConverter(inboxResponse);
                return Result.Success(converter.Convert());
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaDirectInboxContainer>(exception.Message);
            }
        }

        public async Task<IResult<InstaDirectInboxThread>> GetDirectInboxThreadAsync(string threadId)
        {
            try
            {
                var directInboxUri = UriCreator.GetDirectInboxThreadUri(threadId);
                var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetDefaultRequest(HttpMethod.Get, directInboxUri, _deviceInfo));
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaDirectInboxThread>(response, json);
                var threadResponse = JsonConvert.DeserializeObject<InstaDirectInboxThreadResponse>(json,
                    new InstaThreadDataConverter());
                var converter = ConvertersFabric.Instance.GetDirectThreadConverter(threadResponse);
                return Result.Success(converter.Convert());
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaDirectInboxThread>(exception.Message);
            }
        }

        public async Task<IResult<InstaDirectInboxThreadList>> SendLinkMessage(InstaMessageLink message, params long[] recipients)
        {
            var threads = new InstaDirectInboxThreadList();
            try
            {
                var directSendMessageUri = UriCreator.GetDirectSendLinkMessageUri();
                
                var fields = new Dictionary<string, string>
                {
                    {"link_text", message.Text}, 
                    {"link_urls", $"[\"{message.Url}\"]"},
                    {"action", "send_item"}
                };

                if (recipients == null || recipients.Length < 1)
                    return Result.Fail<InstaDirectInboxThreadList>("Please provide at least one recipient.");
                    
                fields.Add("recipient_users", "[[" + string.Join(",", recipients) + "]]");
                
                var response = await _httpRequestProcessor.SendAsync(() =>
                {
                    var request = HttpHelper.GetDefaultRequest(HttpMethod.Post, directSendMessageUri, _deviceInfo);
                    request.Content = new FormUrlEncodedContent(fields);
                    return request;
                });
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaDirectInboxThreadList>(response, json);
                var result = JsonConvert.DeserializeObject<InstaSendDirectMessageResponse>(json);
                if (!result.IsOk()) return Result.Fail<InstaDirectInboxThreadList>(result.Status);
                threads.AddRange(result.Threads.Select(thread =>
                    ConvertersFabric.Instance.GetDirectThreadConverter(thread).Convert()));
                return Result.Success(threads);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaDirectInboxThreadList>(exception);
            }
        }
        
        public async Task<IResult<InstaDirectInboxThreadList>> SendLinkMessage(InstaMessageLink message, params string[] threads)
        {
            var threadList = new InstaDirectInboxThreadList();
            try
            {
                var directSendMessageUri = UriCreator.GetDirectSendLinkMessageUri();
                var fields = new Dictionary<string, string>
                {
                    {"link_text", message.Text}, 
                    {"link_urls", $"[\"{message.Url}\"]"},
                    {"action", "send_item"}
                };

                if (threads == null || threads.Length < 1)
                 return Result.Fail<InstaDirectInboxThreadList>("Please provide at least one recipient.");
                fields.Add("thread_ids", "[" + string.Join(",", threads) + "]");

                
                var response = await _httpRequestProcessor.SendAsync(() =>
                {
                    var request = HttpHelper.GetDefaultRequest(HttpMethod.Post, directSendMessageUri, _deviceInfo);
                    request.Content = new FormUrlEncodedContent(fields);
                    return request;
                });
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaDirectInboxThreadList>(response, json);
                var result = JsonConvert.DeserializeObject<InstaSendDirectMessageResponse>(json);
                if (!result.IsOk()) return Result.Fail<InstaDirectInboxThreadList>(result.Status);
                threadList.AddRange(result.Threads.Select(thread =>
                    ConvertersFabric.Instance.GetDirectThreadConverter(thread).Convert()));
                return Result.Success(threadList);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaDirectInboxThreadList>(exception);
            }
        }

        public async Task<IResult<InstaDirectInboxThreadList>> ShareMedia(string mediaId, InstaMediaType mediaType,
            params string[] threads)
        {
            var threadList = new InstaDirectInboxThreadList();
            try
            {
                var directSendMessageUri = UriCreator.GetShareMediaUri(mediaType);
                
                var fields = new Dictionary<string, string>
                {
                    {"media_id", mediaId}, 
                    {"unified_broadcast_format", "1"},
                    {"action", "send_item"}
                };

                if (threads == null || threads.Length < 1)
                    return Result.Fail<InstaDirectInboxThreadList>("Please provide at least one thread.");
                fields.Add("thread_ids", "[" + string.Join(",", threads) + "]");

                var response = await _httpRequestProcessor.SendAsync(() =>
                {
                    var request = HttpHelper.GetDefaultRequest(HttpMethod.Post, directSendMessageUri, _deviceInfo);
                    request.Content = new FormUrlEncodedContent(fields);
                    return request;
                });
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaDirectInboxThreadList>(response, json);
                var result = JsonConvert.DeserializeObject<InstaSendDirectMessageResponse>(json);
                if (!result.IsOk()) return Result.Fail<InstaDirectInboxThreadList>(result.Status);
                threadList.AddRange(result.Threads.Select(thread =>
                    ConvertersFabric.Instance.GetDirectThreadConverter(thread).Convert()));
                return Result.Success(threadList);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaDirectInboxThreadList>(exception);
            }
        }

        public async Task<IResult<InstaDirectInboxThreadList>> SendDirectMessage(string recipients, string threadIds,
            string text)
        {
            var threads = new InstaDirectInboxThreadList();
            try
            {
                var directSendMessageUri = UriCreator.GetDirectSendTextMessageUri();
                
                var fields = new Dictionary<string, string>
                {
                    {"text", text},
                    {"action", "send_item"}
                };
                if (!string.IsNullOrEmpty(recipients))
                    fields.Add("recipient_users", "[[" + recipients + "]]");
                else
                    return Result.Fail<InstaDirectInboxThreadList>("Please provide at least one recipient.");
                if (!string.IsNullOrEmpty(threadIds))
                    fields.Add("thread_ids", "[" + threadIds + "]");

                var response = await _httpRequestProcessor.SendAsync(() =>
                {
                    var request = HttpHelper.GetDefaultRequest(HttpMethod.Post, directSendMessageUri, _deviceInfo);
                    request.Content = new FormUrlEncodedContent(fields);
                    return request;
                });
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaDirectInboxThreadList>(response, json);
                var result = JsonConvert.DeserializeObject<InstaSendDirectMessageResponse>(json);
                if (!result.IsOk()) return Result.Fail<InstaDirectInboxThreadList>(result.Status);
                threads.AddRange(result.Threads.Select(thread =>
                    ConvertersFabric.Instance.GetDirectThreadConverter(thread).Convert()));
                return Result.Success(threads);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaDirectInboxThreadList>(exception);
            }
        }
        
        public async Task<IResult<BaseStatusResponse>> DeclineAllPendingDirectThreads()
        {
            try
            {
                var uri = UriCreator.GetDeclineAllPendingThreadsUri();
                
                var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetDefaultRequest(HttpMethod.Post, uri, _deviceInfo));
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<BaseStatusResponse>(response, json);
                var result = JsonConvert.DeserializeObject<BaseStatusResponse>(json);
                return !result.IsOk() 
                    ? Result.Fail<BaseStatusResponse>(result.Status) 
                    : Result.Success(result);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<BaseStatusResponse>(exception);
            }
        }
        
        public async Task<IResult<BaseStatusResponse>> ApprovePendingDirectThread(string threadId)
        {
            try
            {
                var uri = UriCreator.GetApproveThreadUri(threadId);
                var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetDefaultRequest(HttpMethod.Post, uri, _deviceInfo));
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<BaseStatusResponse>(response, json);
                var result = JsonConvert.DeserializeObject<BaseStatusResponse>(json);
                return !result.IsOk() 
                    ? Result.Fail<BaseStatusResponse>(result.Status) 
                    : Result.Success(result);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<BaseStatusResponse>(exception);
            }
        }

        public async Task<IResult<InstaRecipients>> GetRecentRecipientsAsync()
        {
            try
            {
                var userUri = UriCreator.GetRecentRecipientsUri();
                var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetDefaultRequest(HttpMethod.Get, userUri, _deviceInfo));
                var json = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaRecipients>(response, json);
                var responseRecipients = JsonConvert.DeserializeObject<InstaRecentRecipientsResponse>(json);
                var converter = ConvertersFabric.Instance.GetRecipientsConverter(responseRecipients);
                return Result.Success(converter.Convert());
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaRecipients>(exception.Message);
            }
        }

        public async Task<IResult<InstaRecipients>> GetRankedRecipientsAsync()
        {
            try
            {
                var userUri = UriCreator.GetRankedRecipientsUri();
                var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetDefaultRequest(HttpMethod.Get, userUri, _deviceInfo));
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaRecipients>(response, json);
                var responseRecipients = JsonConvert.DeserializeObject<InstaRankedRecipientsResponse>(json);
                var converter = ConvertersFabric.Instance.GetRecipientsConverter(responseRecipients);
                return Result.Success(converter.Convert());
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaRecipients>(exception.Message);
            }
        }
    }
}