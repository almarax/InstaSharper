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
using InstaSharper.Converters;
using InstaSharper.Converters.Json;
using InstaSharper.Helpers;
using InstaSharper.Logger;
using Newtonsoft.Json;

namespace InstaSharper.API.Processors
{
    public class CommentProcessor : ICommentProcessor
    {
        private readonly AndroidDevice _deviceInfo;
        private readonly IHttpRequestProcessor _httpRequestProcessor;
        private readonly IInstaLogger _logger;
        private readonly UserSessionData _user;

        public CommentProcessor(AndroidDevice deviceInfo, UserSessionData user,
            IHttpRequestProcessor httpRequestProcessor, Func<object, IInstaLogger> loggerFactory)
        {
            _deviceInfo = deviceInfo;
            _user = user;
            _httpRequestProcessor = httpRequestProcessor;
            _logger = loggerFactory(this);
        }

        public async Task<IResult<InstaCommentList>> GetMediaCommentsAsync(string mediaId,
            PaginationParameters paginationParameters)
        {
            try
            {
                var commentsUri = UriCreator.GetMediaCommentsUri(mediaId, paginationParameters.NextId);
                var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetDefaultRequest(HttpMethod.Get, commentsUri, _deviceInfo));
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaCommentList>(response, json);
                var commentListResponse = JsonConvert.DeserializeObject<InstaCommentListResponse>(json);
                var pagesLoaded = 1;

                InstaCommentList Convert(InstaCommentListResponse commentsResponse)
                {
                    return ConvertersFabric.Instance.GetCommentListConverter(commentsResponse).Convert();
                }

                while (commentListResponse.MoreComentsAvailable
                       && !string.IsNullOrEmpty(commentListResponse.NextMaxId)
                       && pagesLoaded < paginationParameters.MaximumPagesToLoad)
                {
                    var nextComments = await GetCommentListWithMaxIdAsync(mediaId, commentListResponse.NextMaxId);
                    if (!nextComments.Succeeded)
                        return Result.Fail(nextComments.Info, Convert(commentListResponse));
                    commentListResponse.NextMaxId = nextComments.Value.NextMaxId;
                    commentListResponse.MoreComentsAvailable = nextComments.Value.MoreComentsAvailable;
                    commentListResponse.Comments.AddRange(nextComments.Value.Comments);
                    pagesLoaded++;
                }

                var converter = ConvertersFabric.Instance.GetCommentListConverter(commentListResponse);
                return Result.Success(converter.Convert());
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaCommentList>(exception);
            }
        }

        public async Task<IResult<InstaComment>> CommentMediaAsync(string mediaId, string text)
        {
            try
            {
                var instaUri = UriCreator.GetPostCommetUri(mediaId);
                var breadcrumb = CryptoHelper.GetCommentBreadCrumbEncoded(text);
                var fields = new Dictionary<string, string>
                {
                    {"user_breadcrumb", breadcrumb},
                    {"idempotence_token", Guid.NewGuid().ToString()},
                    {"_uuid", _deviceInfo.DeviceGuid.ToString()},
                    {"_uid", _user.LoggedInUder.Pk.ToString()},
                    {"_csrftoken", _user.CsrfToken},
                    {"comment_text", text},
                    {"containermodule", "comments_feed_timeline"},
                    {"radio_type", "wifi-none"}
                };
                var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetSignedRequest(HttpMethod.Post, instaUri, _deviceInfo, fields));
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaComment>(response, json);
                var commentResponse = JsonConvert.DeserializeObject<InstaCommentResponse>(json,
                    new InstaCommentDataConverter());
                var converter = ConvertersFabric.Instance.GetCommentConverter(commentResponse);
                return Result.Success(converter.Convert());
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail(exception.Message, (InstaComment) null);
            }
        }

        public async Task<IResult<bool>> DeleteCommentAsync(string mediaId, string commentId)
        {
            try
            {
                var instaUri = UriCreator.GetDeleteCommetUri(mediaId, commentId);
                var fields = new Dictionary<string, string>
                {
                    {"_uuid", _deviceInfo.DeviceGuid.ToString()},
                    {"_uid", _user.LoggedInUder.Pk.ToString()},
                    {"_csrftoken", _user.CsrfToken}
                };
                var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetSignedRequest(HttpMethod.Post, instaUri, _deviceInfo, fields));
                var json = await response.Content.ReadAsStringAsync();
                return response.StatusCode == HttpStatusCode.OK
                    ? Result.Success(true)
                    : Result.UnExpectedResponse<bool>(response, json);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail(exception.Message, false);
            }
        }

        public async Task<IResult<bool>> LikeMediaCommentAsync(long commentId)
        {
            try
            {
                var instaUri = UriCreator.GetMediaCommetLikeUri(commentId);
                var fields = new Dictionary<string, string>
                {
                    {"_uuid", _deviceInfo.DeviceGuid.ToString()},
                    {"_uid", _user.LoggedInUder.Pk.ToString()},
                    {"_csrftoken", _user.CsrfToken}
                };
                var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetSignedRequest(HttpMethod.Post, instaUri, _deviceInfo, fields));
                var json = await response.Content.ReadAsStringAsync();
                return response.StatusCode == HttpStatusCode.OK
                    ? Result.Success(true)
                    : Result.UnExpectedResponse<bool>(response, json);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail(exception.Message, false);
            }
        }
        public async Task<IResult<InstaLikersList>> GetMediaCommentLikersAsync(long commentId)
        {
            try
            {
                var likers = new InstaLikersList();
                var likersUri = UriCreator.GetCommentLikersUri(commentId);
                var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetDefaultRequest(HttpMethod.Get, likersUri, _deviceInfo));
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaLikersList>(response, json);
                var mediaLikersResponse = JsonConvert.DeserializeObject<InstaMediaLikersResponse>(json);
                likers.UsersCount = mediaLikersResponse.UsersCount;
                if (mediaLikersResponse.UsersCount < 1) return Result.Success(likers);
                likers.AddRange(
                    mediaLikersResponse.Users.Select(ConvertersFabric.Instance.GetUserShortConverter)
                        .Select(converter => converter.Convert()));
                return Result.Success(likers);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail<InstaLikersList>(exception);
            }
        }

        private async Task<IResult<InstaCommentListResponse>> GetCommentListWithMaxIdAsync(string mediaId,
            string nextId)
        {
            var commentsUri = UriCreator.GetMediaCommentsUri(mediaId, nextId);
            var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetDefaultRequest(HttpMethod.Get, commentsUri, _deviceInfo));
            var json = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != HttpStatusCode.OK)
                return Result.UnExpectedResponse<InstaCommentListResponse>(response, json);
            var comments = JsonConvert.DeserializeObject<InstaCommentListResponse>(json);
            return Result.Success(comments);
        }
    }
}