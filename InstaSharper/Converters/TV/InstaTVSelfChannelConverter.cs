﻿/*
 * Developer: Ramtin Jokar [ Ramtinak@live.com ] [ My Telegram Account: https://t.me/ramtinak ]
 * 
 * Github source: https://github.com/ramtinak/InstagramApiSharp
 * Nuget package: https://www.nuget.org/packages/InstagramApiSharp
 * 
 * IRANIAN DEVELOPERS
 */

using System;
using System.Linq;
using InstaSharper.Classes.Models.TV;
using InstaSharper.Classes.ResponseWrappers.TV;

namespace InstaSharper.Converters.TV
{
    internal class InstaTVSelfChannelConverter : IObjectConverter<InstaTVSelfChannel, InstaTVSelfChannelResponse>
    {
        public InstaTVSelfChannelResponse SourceObject { get; set; }

        public InstaTVSelfChannel Convert()
        {
            if (SourceObject == null)
                throw new ArgumentNullException("SourceObject");

            var channel = new InstaTVSelfChannel
            {
                HasMoreAvailable = SourceObject.HasMoreAvailable,
                Id = SourceObject.Id,
                MaxId = SourceObject.MaxId,
                Title = SourceObject.Title,
                Type = SourceObject.Type
            };

            if (SourceObject.Items != null && SourceObject.Items.Any())
            {
                foreach (var item in SourceObject.Items)
                {
                    try
                    {
                        channel.Items.Add(ConvertersFabric.Instance.GetSingleMediaConverter(item).Convert());
                    }
                    catch { }
                }
            }
            if (SourceObject.User != null)
            {
                try
                {
                    channel.User = ConvertersFabric.Instance.GetTVUserConverter(SourceObject.User).Convert();
                }
                catch { }
            }
            return channel;
        }
    }
}
