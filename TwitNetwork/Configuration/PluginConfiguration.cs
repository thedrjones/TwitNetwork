using MediaBrowser.Model.Plugins;
using System;

namespace TwitNetwork.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// The twit.tv API key 
        /// </summary>
        public string AppID { get; set; }

        /// <summary>
        /// The twit.tv API Secret
        /// </summary>
        public string AppKey { get; set; }

        /// <summary>
        /// Enable debug logging
        /// </summary>
        public bool EnableDebugLogging { get; set; }

        public DateTime ShowListUpdated { get; set; }

        public int LimitRequestsPerMinute { get; set; }

        public int LimitCollectionInDays { get; set; }

        public PluginConfiguration()
        {
            AppID = "app-id-goes-here";
            AppKey = "app-key-goes-here";
            EnableDebugLogging = false;
            LimitRequestsPerMinute = 5;
            LimitCollectionInDays = 30;
        }

    }
}
