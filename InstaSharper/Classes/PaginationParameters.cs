using System;

namespace InstaSharper.Classes
{
    public class PaginationParameters
    {
        private PaginationParameters()
        { }

        public string NextId { get; set; } = string.Empty;
        public int MaximumItemsToLoad { get; private set; } = int.MaxValue;
        public int ItemsLoaded { get; set; }
        public int MaximumPagesToLoad { get; private set; }
        public int PagesLoaded { get; set; }
        //public static PaginationParameters Empty => MaxPagesToLoad(int.MaxValue);
        public static PaginationParameters MaxPagesToLoad(int maxPagesToLoad)
        {
            return new PaginationParameters { MaximumPagesToLoad = maxPagesToLoad };
        }
        public static PaginationParameters MaxItemsToLoad(int maxItemsToLoad)
        {
            return new PaginationParameters { MaximumItemsToLoad = maxItemsToLoad, MaximumPagesToLoad = (int)Math.Ceiling((decimal)maxItemsToLoad / 9) };
        }
        public PaginationParameters StartFromId(string nextId)
        {
            NextId = nextId;
            return this;
        }
    }
}