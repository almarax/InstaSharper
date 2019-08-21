using System;
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
using InstaRecentActivityConverter = InstaSharper.Converters.Json.InstaRecentActivityConverter;

namespace InstaSharper.API.Processors
{
    public class FeedProcessor : IFeedProcessor
    {
        private readonly AndroidDevice _deviceInfo;
        private readonly IHttpRequestProcessor _httpRequestProcessor;
        private readonly IInstaLogger _logger;
        private readonly UserSessionData _user;

        public FeedProcessor(AndroidDevice deviceInfo, UserSessionData user, IHttpRequestProcessor httpRequestProcessor,
            Func<object, IInstaLogger> loggerFactory)
        {
            _deviceInfo = deviceInfo;
            _user = user;
            _httpRequestProcessor = httpRequestProcessor;
            _logger = loggerFactory(this);
        }

        public async Task<IResult<InstaTagFeed>> GetTagFeedAsync(string tag, PaginationParameters paginationParameters)
        {
            var feed = new InstaTagFeed();
            try
            {
                var userFeedUri = UriCreator.GetTagFeedUri(tag, paginationParameters.NextId);
                var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetDefaultRequest(HttpMethod.Get, userFeedUri, _deviceInfo));
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaTagFeed>(response, json);
                var feedResponse = JsonConvert.DeserializeObject<InstaTagFeedResponse>(json,
                    new InstaTagFeedDataConverter());
                feed = ConvertersFabric.Instance.GetTagFeedConverter(feedResponse).Convert();

                paginationParameters.NextId = feedResponse.NextMaxId;
                paginationParameters.PagesLoaded++;
                paginationParameters.ItemsLoaded += feed.Medias.Count;

                while (feedResponse.MoreAvailable
                       && !string.IsNullOrEmpty(paginationParameters.NextId)
                       && paginationParameters.ItemsLoaded < paginationParameters.MaximumItemsToLoad
                       && paginationParameters.PagesLoaded < paginationParameters.MaximumPagesToLoad)
                {
                    var nextFeed = await GetTagFeedAsync(tag, paginationParameters);
                    if (!nextFeed.Succeeded)
                        return Result.Fail(nextFeed.Info, feed);
                    feed.NextId = paginationParameters.NextId = nextFeed.Value.NextId;
                    feed.Medias.AddRange(nextFeed.Value.Medias);
                    feed.Stories.AddRange(nextFeed.Value.Stories);
                }

                return Result.Success(feed);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail(exception, feed);
            }
        }

        public async Task<IResult<InstaFeed>> GetUserTimelineFeedAsync(PaginationParameters paginationParameters)
        {
            var feed = new InstaFeed();
            try
            {
                var userFeedUri = UriCreator.GetUserFeedUri(paginationParameters.NextId);
                var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetDefaultRequest(HttpMethod.Get, userFeedUri, _deviceInfo));
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaFeed>(response, json);

                var feedResponse = JsonConvert.DeserializeObject<InstaFeedResponse>(json,
                    new InstaFeedResponseDataConverter());
                feed = ConvertersFabric.Instance.GetFeedConverter(feedResponse).Convert();
                paginationParameters.NextId = feed.NextId;
                paginationParameters.PagesLoaded++;
                paginationParameters.ItemsLoaded += feed.Medias.Count;

                while (feedResponse.MoreAvailable
                       && !string.IsNullOrEmpty(paginationParameters.NextId)
                       && paginationParameters.ItemsLoaded < paginationParameters.MaximumItemsToLoad
                       && paginationParameters.PagesLoaded < paginationParameters.MaximumPagesToLoad)
                {
                    var nextFeed = await GetUserTimelineFeedAsync(paginationParameters);
                    if (!nextFeed.Succeeded)
                        return Result.Fail(nextFeed.Info, feed);

                    feed.Medias.AddRange(nextFeed.Value.Medias);
                    feed.Stories.AddRange(nextFeed.Value.Stories);

                    paginationParameters.NextId = nextFeed.Value.NextId;
                    paginationParameters.PagesLoaded++;
                }

                return Result.Success(feed);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail(exception, feed);
            }
        }

        public async Task<IResult<InstaExploreFeed>> GetExploreFeedAsync(PaginationParameters paginationParameters)
        {
            var feed = new InstaExploreFeed();
            try
            {
                var exploreUri = UriCreator.GetExploreUri(paginationParameters.NextId);
                var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetDefaultRequest(HttpMethod.Get, exploreUri, _deviceInfo));
                var json = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    return Result.UnExpectedResponse<InstaExploreFeed>(response, json);
                var feedResponse = JsonConvert.DeserializeObject<InstaExploreFeedResponse>(json,
                    new InstaExploreFeedDataConverter());
                feed = ConvertersFabric.Instance.GetExploreFeedConverter(feedResponse).Convert();
                var nextId = feedResponse.Items.Medias.LastOrDefault(media => !string.IsNullOrEmpty(media.NextMaxId))
                    ?.NextMaxId;
                feed.Medias.PageSize = feedResponse.ResultsCount;
                paginationParameters.NextId = nextId;
                feed.NextId = nextId;
                while (feedResponse.MoreAvailable
                       && !string.IsNullOrEmpty(paginationParameters.NextId)
                       && paginationParameters.ItemsLoaded < paginationParameters.MaximumItemsToLoad
                       && paginationParameters.PagesLoaded < paginationParameters.MaximumPagesToLoad)
                {
                    var nextFeed = await GetExploreFeedAsync(paginationParameters);
                    if (!nextFeed.Succeeded)
                        return Result.Fail(nextFeed.Info, feed);
                    nextId = feedResponse.Items.Medias.LastOrDefault(media => !string.IsNullOrEmpty(media.NextMaxId))
                        ?.NextMaxId;
                    feed.NextId = paginationParameters.NextId = nextId;
                    paginationParameters.PagesLoaded++;
                    paginationParameters.ItemsLoaded += feed.Medias.Count;
                    feed.Medias.AddRange(nextFeed.Value.Medias);
                }

                feed.Medias.Pages = paginationParameters.PagesLoaded;
                return Result.Success(feed);
            }
            catch (Exception exception)
            {
                _logger?.LogException(exception);
                return Result.Fail(exception, feed);
            }
        }

        public async Task<IResult<InstaActivityFeed>> GetFollowingRecentActivityFeedAsync(
            PaginationParameters paginationParameters)
        {
            var uri = UriCreator.GetFollowingRecentActivityUri();
            return await GetRecentActivityInternalAsync(uri, paginationParameters);
        }

        public async Task<IResult<InstaActivityFeed>> GetRecentActivityFeedAsync(
            PaginationParameters paginationParameters)
        {
            var uri = UriCreator.GetRecentActivityUri();
            return await GetRecentActivityInternalAsync(uri, paginationParameters);
        }

        public async Task<IResult<InstaMediaList>> GetLikeFeedAsync(PaginationParameters paginationParameters)
        {
            var instaUri = UriCreator.GetUserLikeFeedUri(paginationParameters.NextId);
            var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetDefaultRequest(HttpMethod.Get, instaUri, _deviceInfo));
            var json = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != HttpStatusCode.OK)
                return Result.UnExpectedResponse<InstaMediaList>(response, json);
            var mediaResponse = JsonConvert.DeserializeObject<InstaMediaListResponse>(json,
                new InstaMediaListDataConverter());

            var mediaList = ConvertersFabric.Instance.GetMediaListConverter(mediaResponse).Convert();
            mediaList.NextId = paginationParameters.NextId = mediaResponse.NextMaxId;
            while (mediaResponse.MoreAvailable
                   && !string.IsNullOrEmpty(paginationParameters.NextId)
                   && paginationParameters.ItemsLoaded < paginationParameters.MaximumItemsToLoad
                   && paginationParameters.PagesLoaded < paginationParameters.MaximumPagesToLoad)
            {
                var result = await GetLikeFeedAsync(paginationParameters);
                if (!result.Succeeded)
                    return Result.Fail(result.Info, mediaList);

                paginationParameters.PagesLoaded++;
                paginationParameters.ItemsLoaded += mediaList.Count;
                mediaList.NextId = paginationParameters.NextId = result.Value.NextId;
                mediaList.AddRange(result.Value);
            }

            mediaList.PageSize = mediaResponse.ResultsCount;
            mediaList.Pages = paginationParameters.PagesLoaded;
            return Result.Success(mediaList);
        }

        private async Task<IResult<InstaRecentActivityResponse>> GetFollowingActivityWithMaxIdAsync(string maxId)
        {
            var uri = UriCreator.GetFollowingRecentActivityUri(maxId);
            var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetDefaultRequest(HttpMethod.Get, uri, _deviceInfo));
            var json = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != HttpStatusCode.OK)
                return Result.UnExpectedResponse<InstaRecentActivityResponse>(response, json);
            var followingActivity = JsonConvert.DeserializeObject<InstaRecentActivityResponse>(json,
                new InstaRecentActivityConverter());
            return Result.Success(followingActivity);
        }

        private async Task<IResult<InstaActivityFeed>> GetRecentActivityInternalAsync(Uri uri,
            PaginationParameters paginationParameters)
        {
            var response = await _httpRequestProcessor.SendAsync(() => HttpHelper.GetDefaultRequest(HttpMethod.Get, uri, _deviceInfo),
                HttpCompletionOption.ResponseContentRead);
            var feed = new InstaActivityFeed();
            var json = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != HttpStatusCode.OK)
                return Result.UnExpectedResponse<InstaActivityFeed>(response, json);
            var feedPage = JsonConvert.DeserializeObject<InstaRecentActivityResponse>(json,
                new InstaRecentActivityConverter());
            feed.IsOwnActivity = feedPage.IsOwnActivity;
            var nextId = feedPage.NextMaxId;
            feed.Items.AddRange(
                feedPage.Stories.Select(ConvertersFabric.Instance.GetSingleRecentActivityConverter)
                    .Select(converter => converter.Convert()));
            paginationParameters.PagesLoaded++;
            feed.NextId = paginationParameters.NextId = feedPage.NextMaxId;
            while (!string.IsNullOrEmpty(nextId)
                   && paginationParameters.ItemsLoaded < paginationParameters.MaximumItemsToLoad
                   && paginationParameters.PagesLoaded < paginationParameters.MaximumPagesToLoad)
            {
                var nextFollowingFeed = await GetFollowingActivityWithMaxIdAsync(nextId);
                if (!nextFollowingFeed.Succeeded)
                    return Result.Fail(nextFollowingFeed.Info, feed);
                nextId = nextFollowingFeed.Value.NextMaxId;
                feed.Items.AddRange(
                    feedPage.Stories.Select(ConvertersFabric.Instance.GetSingleRecentActivityConverter)
                        .Select(converter => converter.Convert()));
                paginationParameters.PagesLoaded++;
                paginationParameters.ItemsLoaded += feed.Items.Count;
                feed.NextId = paginationParameters.NextId = nextId;
            }

            return Result.Success(feed);
        }
    }
}