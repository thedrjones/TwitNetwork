using System;
using System.Collections.Generic;

/// <summary>
/// This is a list of all types seen in the the twit.tv API calls we use.
/// </summary>
namespace TwitNetwork.TwitApi
{
    /// <summary>
    /// This is the response we expect to see when we call for all the episodes
    /// </summary>
    public class ListEpisodesResponse
    {
        public int count { get; set; }
        public List<Episode> episodes { get; set; }
        public Embedded _embedded { get; set; }
        public Links _links { get; set; }

    }

    /// <summary>
    /// This is the response we expect to see when we call for all the shows
    /// </summary>
    public class ListShowsResponse
    {
        public int count { get; set; }
        public List<Show> shows { get; set; }
        public Embedded _embedded { get; set; }
        public Links _links { get; set; }
    }

    /// <summary>
    /// This is the response we would expect with an error
    /// </summary>
    public class InternalServerErrorResponse
    {
        public int _status { get; set; }
        public Dictionary<string,string> _errors { get; set; }
    }


    /// <summary>
    /// Base class for the "shows" object
    /// </summary>
    public partial class Show : BaseIdentifiers
    {
        public string cleanPath { get; set; }
        public string description { get; set; }
        public string descriptionSummary { get; set; }
        public string showNotes { get; set; }
        public string showDate { get; set; }
        public string showContactInfo { get; set; }
        public string tagLine { get; set; }
        public string shortCode { get; set; }
        public List<SubscriptionOptions> hdVideoSubscriptionOptions { get; set; }
        public List<SubscriptionOptions> sdVideoLargeSubscriptionOptions { get; set; }
        public List<SubscriptionOptions> sdVideoSmallSubscriptionOptions { get; set; }
        public List<SubscriptionOptions> audioSubscriptionOptions { get; set; }
        public FileIdentifiers heroImage { get; set; }
        public FileIdentifiers coverArt { get; set; }
        public bool active { get; set; }
        public DateTime created { get; set; }
        public int weight { get; set; }
        public Links _links { get; set; }
        public Embedded _embedded { get; set; }
    }

    public partial class SubscriptionOptions : BaseIdentifiers
    {
        public string self { get; set; }
        public FeedProvider feedProvider { get; set; }
        public string url { get; set; }
        public string type { get; set; }
        public DateTime created { get; set; }
        public DateTime changed { get; set; }
        public int sticky { get; set; }
    }

    public partial class FeedProvider : BaseIdentifiers
    {
        public bool active { get; set; }
        public DateTime created { get; set; }
    }

    public partial class BaseIdentifiers
    {
        public int id { get; set; }
        public string label { get; set; }
        public int ttl { get; set; }
    }

    public partial class FileIdentifiers
    {
        public int fid { get; set; }
        public string url { get; set; }
        public string fileName { get; set; }
        public string mimeType { get; set; }
        public Dictionary<string,string> derivatives { get; set; }
        public int fileSize { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string alt { get; set; }
        public string title { get; set; }
        public int status { get; set; }
        public DateTime changed { get; set; }
        public DateTime created { get; set; }
    }

    public partial class Embedded
    {
        public List<Credits> credits { get; set; }
        public List<Show> shows { get; set; }
        public List<Vocabulary> topics { get; set; }
        public List<Vocabulary> categories { get; set; }

        public bool hasCredits => (credits != null);
        public bool hasShows => (shows != null);
        public bool hasTopics => (topics != null);
        public bool hasCategories => (categories != null);
    }

    public partial class Links
    {
        public Dictionary<string,string> episodes { get; set; }
        public Dictionary<string,string> self { get; set; }
        public Dictionary<string,string> next { get; set; }
        public Dictionary<string,string> previous { get; set; }

        public bool hasNext => (next != null);
        public bool hasPrevious => (previous != null);
        public bool hasEpisodes => (episodes != null);

    }

    public partial class Credits : BaseIdentifiers
    {
        public Roles roles { get; set; }
        public People people { get; set; }
        public DateTime created { get; set; }
        public Links _links { get; set; }
    }

    public partial class Vocabulary : BaseIdentifiers
    {
        public int weight { get; set; }
        public int vid { get; set; }
        public int type { get; set; }
        public string vocabularyName { get; set; }
        public string termPath { get; set; }
        public Links _links { get; set; }
    }

    public partial class Roles : Vocabulary
    {
        public string self { get; set; }
    }

    public partial class People : BaseIdentifiers
    {
        public string self { get; set; }
        public string cleanPath { get; set; }
        public string positionTitle { get; set; }
        public string bio { get; set; }
        public string bioSummary { get; set; }
        public FileIdentifiers picture { get; set; }
        public List<RelatedLinks> relatedLinks { get; set; }
        public int published { get; set; }
        public DateTime created { get; set; }
        public DateTime changed { get; set; }
        public int sticky { get; set; }
        public bool staff { get; set; }

        public bool hasPicture => (picture != null && !string.IsNullOrEmpty(picture.url));
    }
    public partial class RelatedLinks
    {
        public string url { get; set; }
        public string title { get; set; }
        public List<string> attibutes { get; set; }
    }
    public partial class Episode : BaseIdentifiers
    {
        public string cleanPath { get; set; }
        public string episodeSponsors { get; set; }
        public int episodeNumber { get; set; }
        public DateTime airingDate { get; set; }
        public string teaser { get; set; }
        public string showNotes { get; set; }
        public string featured { get; set; }
        public string showNotesFooter { get; set; }
        public string relatedLinks { get; set; }
        public string files { get; set; }
        public FileIdentifiers heroImage { get; set; }
        public MediaIdentifier video_hd { get; set; }
        public MediaIdentifier video_large { get; set; }
        public MediaIdentifier video_small { get; set; }
        public MediaIdentifier video_audio { get; set; }
        public MediaIdentifier video_youtube { get; set; }
        public string published { get; set; }
        public DateTime created { get; set; }
        public DateTime changed { get; set; }
        public Links _links { get; set; }
        public Embedded _embedded { get; set; }

    }

    public partial class MediaIdentifier
    {
        public string mediaUrl { get; set; }
        public string format { get; set; }
        public string guid { get; set; }
        public ulong size { get; set; }
        public DateTime changed { get; set; }
        public string runningTime { get; set; }
        public string hours { get; set; }
        public string minutes { get; set; }
        public string seconds { get; set; }
    }
}
