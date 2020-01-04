using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TwitNetwork.TwitApi;

namespace TwitNetwork
{
    public class TwitNetworkChannel : IChannel, IHasCacheKey
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        public TwitNetworkCache _twitNetworkCache;

        public TwitNetworkChannel(IHttpClient httpClient, ILogManager logManager, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _logger = logManager.GetLogger(GetType().Name);
            _jsonSerializer = jsonSerializer;

            // Clean new instance for now, but might be serialized to disk and recalled one day if that is useful.
            _twitNetworkCache = new TwitNetworkCache();
        }

        public string DataVersion
        {
            get
            {
                //Increment as needed to invalidate all caches
                return "8";
            }
        }

        public string Description
        {
            get { return Plugin.Instance.Description; }
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public string GetCacheKey(string userId)
        {
            return Plugin.Instance.Configuration.AppID;
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public string Name
        {
            get { return Plugin.Instance.Name; }
        }

        public string HomePageUrl
        {
            get { return "http://twit.tv"; }
        }


        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Thumb,
                ImageType.Backdrop,
                ImageType.Primary
            };
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Podcast
                },

                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Video
                },

                MaxPageSize = 100,

                DefaultSortFields = new List<ChannelItemSortField>
                {
                    ChannelItemSortField.CommunityRating,
                    ChannelItemSortField.Name,
                    ChannelItemSortField.PremiereDate,
                    ChannelItemSortField.Runtime
                }

            };
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Thumb:
                case ImageType.Backdrop:
                case ImageType.Primary:
                    {
                        var path = GetType().Namespace + ".Images." + type.ToString().ToLower() + ".png";

                        return Task.FromResult(new DynamicImageResponse
                        {
                            Format = ImageFormat.Png,
                            HasImage = true,
                            Stream = GetType().Assembly.GetManifestResourceStream(path)
                        });
                    }
                default:
                    throw new ArgumentException("Unsupported image type: " + type);
            }
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            _logger.Debug("In GetChannelItems");

            var downloader = new TwitNetworkDownloader(_logger, _jsonSerializer, _httpClient);
            var cacheOk = await downloader.PopulateNetworkCache(
                _twitNetworkCache, 
                Plugin.Instance.Configuration.LimitCollectionInDays,
                Plugin.Instance.Configuration.LimitRequestsPerMinute, 
                cancellationToken).ConfigureAwait(false);

            _logger.Debug("Downloader cache result: {0}", cacheOk);

            ChannelItemResult result;

            _logger.Debug("Show ID: " + query.FolderId);

            if (query.FolderId == null)
                result = ParseChannelsInternal(query);
            else
                result = ParseChannelItemsInternal(query);

            return result;
        }


        /// <summary>
        /// Compiled regular expression for performance for replacing HTML Tags.
        /// </summary>
        internal static Regex _htmlRegex = new Regex("<.*?>", RegexOptions.Compiled);

        /// <summary>
        /// Remove HTML from string with compiled Regex.
        /// Also resolves some entities and reforms <li> and <p> tags to maintain line breaks</p> 
        /// </summary>
        internal static string StripTags(string source)
        {
            source = source.Replace("</p>", "</p>\r\n");
            source = source.Replace("<li>", "<li> * ");
            source = source.Replace("</li>", "</li>\r\n");
            source = source.Replace("&amp;", "&");
            source = source.Replace("&lt;", "<");
            source = source.Replace("&gt;", ">");
            return _htmlRegex.Replace(source, string.Empty).Trim();
        }

        private ChannelItemResult ParseChannelsInternal(InternalChannelItemQuery query)
        {
            _logger.Debug("In ParseChannelsInternal");
            var showItems = new List<ChannelItemInfo>();

            foreach(var cacheShowItem in _twitNetworkCache.Shows)
            {
                var thisShow = cacheShowItem.Show;
                showItems.Add(BuildChannelItemInfo(thisShow));
            }

            _logger.Debug("ParseChannelsInternal Completed, {0} items", showItems.Count);
            return new ChannelItemResult
            {
                Items = showItems,
                TotalRecordCount = showItems.Count
            };
        }

        private ChannelItemInfo BuildChannelItemInfo(Show i)
        {
            var latestEpisode = _twitNetworkCache.GetCacheEntry(i.id).LatestEpisode;

            var thisItem = new ChannelItemInfo
            {
                Type = ChannelItemType.Folder,
                Name = i.label.Trim(),
                OriginalTitle = i.label.Trim(),
                Id = string.Format("twit-show-{0}-{1}",i.id,i.shortCode.Replace(" ","-")),
                DateCreated = i.created,
                OfficialRating = i.weight.ToString(),
                CommunityRating = i.weight,
                ImageUrl = i.coverArt.url,
                Overview = StripTags(i.description),
                StartDate = i.created,
                HomePageUrl = string.Format("{0}{1}", "https://twit.tv", i.cleanPath),
                ProductionYear = i.created.Year,
                PremiereDate = i.created,
                People = new List<PersonInfo>(),
                Tags = new List<string>(),
                Genres = new List<string>(),
                Studios = new List<string>()
            };

            thisItem.Studios.Add("TWiT Productions");


            if (i._embedded == null)
                return thisItem;

            if (i._embedded.hasCredits)
            {
                foreach (var credit in i._embedded.credits)
                {
                    var personInfo = new PersonInfo
                    {
                        Name = credit.people.label,
                        Role = credit.roles.label
                    };

                    if (credit.people.hasPicture)
                        personInfo.ImageUrl = credit.people.picture.url;

                    thisItem.People.Add(personInfo);
                }
            }

            if (i._embedded.hasTopics)
            {
                foreach (var topic in i._embedded.topics)
                {
                    if (!thisItem.Tags.Contains(topic.label))
                        thisItem.Tags.Add(topic.label);
                }
            }

            if (i._embedded.hasCategories)
            {
                foreach (var genre in i._embedded.categories)
                {
                    if (!thisItem.Genres.Contains(genre.label))
                        thisItem.Genres.Add(genre.label);
                }
            }
            

            return thisItem;
        }

        private ChannelItemResult ParseChannelItemsInternal(InternalChannelItemQuery query)
        {
            _logger.Debug("In ParseChannelItemsInternal");
            var episodeItems = new List<ChannelItemInfo>();
            var folderId = 0;

            if (!string.IsNullOrEmpty(query.FolderId))
            { 
                var cacheFolderId = query.FolderId.Split('-');
                folderId = int.Parse(cacheFolderId[2]);
            }

            _logger.Debug("Looking for folder {0}",folderId);
            var showItem = _twitNetworkCache.GetCacheEntry(folderId);

            _logger.Debug("Building {0} items for {1} ",showItem.Episodes.Count,showItem.Show.shortCode);
            foreach(var episode in showItem.Episodes)
            {
                _logger.Debug("\t{0} episode {1} [{2}]", showItem.Show.shortCode, episode.episodeNumber, episode.label.Trim());
                episodeItems.Add(BuildChannelMediaInfo(episode));
            }

            return new ChannelItemResult
            {
                Items = episodeItems,
                TotalRecordCount = episodeItems.Count
            };
        }

        private ChannelItemInfo BuildChannelMediaInfo(Episode i)
        {
            var shortCode = i._embedded.shows[0].shortCode;
            var cii = new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.Podcast,
                ImageUrl = i.heroImage.url,
                MediaType = ChannelMediaType.Video,
                Type = ChannelItemType.Media,
                Name = string.Format("{0} {1}: {2}", shortCode, i.episodeNumber, i.label.Trim()),
                Id = string.Format("twit-episode-{0}", i.id),
                DateCreated = i.created,
                Overview = StripTags(i.showNotes),
                DateModified = i.changed,
                IndexNumber = i.episodeNumber,
                PremiereDate = i.airingDate,
                ProductionYear = i.airingDate.Year,
                MediaSources = MediaSourceBuilder(i, out long runTime),
                RunTimeTicks = runTime,
                People = new List<PersonInfo>(),
                Tags = new List<string>(),
                Genres = new List<string>(),
                Studios = new List<string>(),
                HomePageUrl = i.cleanPath,
                OriginalTitle = i.label.Trim()
            };

            cii.Studios.Add("TWiT Productions");

            if (i._embedded == null)
                return cii;

            if (i._embedded.hasCredits)
            {
                foreach (var credit in i._embedded.credits)
                {
                    var personInfo = new PersonInfo
                    {
                        Name = credit.people.label,
                        Role = credit.roles.label
                    };
                    if (credit.people.hasPicture)
                        personInfo.ImageUrl = credit.people.picture.url;

                    cii.People.Add(personInfo);
                }
            }

            if (i._embedded.hasTopics)
            {
                foreach (var topic in i._embedded.topics)
                {
                    if (!cii.Tags.Contains(topic.label))
                        cii.Tags.Add(topic.label);
                }
            }

            if (i._embedded.hasCategories)
            {
                foreach (var genre in i._embedded.categories)
                {
                    if (!cii.Genres.Contains(genre.label))
                        cii.Genres.Add(genre.label);
                }
            }
            return cii;
        }


        private List<MediaSourceInfo> MediaSourceBuilder(Episode i, out long runTime)
        {
            var resultList = new List<MediaSourceInfo>();
            TimeSpan runTimeTicks = new TimeSpan();

            if (i.video_hd != null)
                resultList.Add(BuildMediaSourceInfo(i.video_hd, 1280, 720, out runTimeTicks));
            if (i.video_large != null)
                resultList.Add(BuildMediaSourceInfo(i.video_large, 864, 480, out runTimeTicks));
            if (i.video_small != null)
                resultList.Add(BuildMediaSourceInfo(i.video_small, 640, 368, out runTimeTicks));

            runTime = runTimeTicks.Ticks;

            return resultList;

        }

        private MediaSourceInfo BuildMediaSourceInfo(MediaIdentifier source, int width, int height, out TimeSpan RunTimeTicks)
        {
            var res = new ChannelMediaInfo
            {
                Protocol = MediaProtocol.Http,
                Path = source.mediaUrl,
                Width = width,
                Height = height,
                Container = Container.MP4,
                SupportsDirectPlay = true,
                RunTimeTicks = TimeSpan.TryParse(source.runningTime, out RunTimeTicks) ? RunTimeTicks.Ticks : 0
            };
            return res.ToMediaSource();
        }
    }
}
