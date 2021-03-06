﻿using System.Collections.Generic;
using InstaSharper.Classes.Models.Media;
using InstaSharper.Enums;

namespace InstaSharper.Classes.Models.Direct
{
    public class InstaInboxMedia
    {
        public List<InstaImage> Images { get; set; } = new List<InstaImage>();
        public long OriginalWidth { get; set; }
        public long OriginalHeight { get; set; }
        public InstaMediaType MediaType { get; set; }
        public List<InstaVideo> Videos { get; set; } = new List<InstaVideo>();
    }
}