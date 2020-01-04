using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TwitNetwork.TwitApi
{
    /// <summary>
    /// This is the master index of items we have collected from the API.
    /// The Twit API will return episodes with their associated shows as embedded data, but we want to organise as a hierarchy by show.
    /// </summary>
    public class TwitNetworkCache
    {
        /// <summary>
        /// The key here is the Twit API show ID and the value is the show cache object.
        /// Dictionaries seem quicker to query than Bags.
        /// </summary>
        private ConcurrentDictionary<int, TwitNetworkShowCache> CacheCollection;

        public int RequestDays { get; private set; }

        public int Count => CacheCollection.Count;

        public DateTime LastUpdated { get; private set; } = new DateTime(1970, 1, 1);

        public ICollection<TwitNetworkShowCache> Shows => CacheCollection.Values;

        public TwitNetworkCache()
        {
            RequestDays = 0;
            CacheCollection = new ConcurrentDictionary<int, TwitNetworkShowCache>();
        }

        public void MarkCacheComplete(int lookbackDays)
        {
            RequestDays = lookbackDays;
            LastUpdated = DateTime.Now;
        }

        public bool TryAdd(TwitNetworkShowCache showCache)
        {
            var res = CacheCollection.TryAdd(showCache.Show.id, showCache);
            if (res)
                LastUpdated = DateTime.Now;
            return res;
        }

        public bool TryAdd(Show show)
        {
            var res = CacheCollection.TryAdd(show.id, new TwitNetworkShowCache(show));
            if (res)
                LastUpdated = DateTime.Now;
            return res;
        }

        public bool ContainsShow(int id)
        {
            return CacheCollection.ContainsKey(id);
        }

        public TwitNetworkShowCache GetCacheEntry(int id)
        {
            if (ContainsShow(id))
                return CacheCollection[id];
            else
                return null;
        }

        public TwitNetworkShowCache GetCacheEntry(string id)
        {
            var acutalId = int.Parse(id);
            return GetCacheEntry(acutalId);
        }
    }

    /// <summary>
    /// Contains a cache of shows with their associated episodes.
    /// </summary>
    public class TwitNetworkShowCache
    {
        /// <summary>
        /// This is the the show data object
        /// </summary>
        public Show Show;

        /// <summary>
        /// The key here is the Twit API episode ID and the value is the episode cache object.
        /// Dictionaries seem quicker to query than Bags
        /// </summary>
        private ConcurrentDictionary<int,Episode> EpisodeList;

        public ICollection<Episode> Episodes => EpisodeList.Values;

        public TwitNetworkShowCache(Show show)
        {
            Show = show;
            EpisodeList = new ConcurrentDictionary<int, Episode>();
        }

        public bool HasEpisode(int episodeId)
        {
            return EpisodeList.ContainsKey(episodeId);
        }

        public void AddEpisode(Episode episode)
        {
            if (!EpisodeList.ContainsKey(episode.id))
            {
                EpisodeList.TryAdd(episode.id, episode);
            }
        }

        public Episode LatestEpisode
        {
            get
            {
                return EpisodeList.Count == 0 ? null : EpisodeList.Values.OrderByDescending(x => x.airingDate).FirstOrDefault();
            }
        }
    }

  
}
