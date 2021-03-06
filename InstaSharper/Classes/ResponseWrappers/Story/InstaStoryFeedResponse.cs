﻿using System.Collections.Generic;
using InstaSharper.Classes.ResponseWrappers.BaseResponse;
using InstaSharper.Classes.ResponseWrappers.Broadcast;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstaSharper.Classes.ResponseWrappers.Story
{
    public class InstaStoryFeedResponse : BaseStatusResponse
    {
        [JsonProperty("face_filter_nux_version")]
        public int FaceFilterNuxVersion { get; set; }

        [JsonProperty("has_new_nux_story")] public bool HasNewNuxStory { get; set; }

        [JsonProperty("story_ranking_token")] public string StoryRankingToken { get; set; }

        [JsonProperty("sticker_version")] public int StickerVersion { get; set; }

        [JsonProperty("tray")] public List</*InstaReelFeedResponse*/JToken> Tray { get; set; }

        [JsonProperty("broadcasts")] public List<InstaBroadcastResponse> Broadcasts { get; set; }

        [JsonProperty("post_live")] public InstaBroadcastAddToPostLiveContainerResponse PostLives { get; set; }
    }
}