using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using TwitNetwork.Configuration;

namespace TwitNetwork
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    class Plugin : BasePlugin<PluginConfiguration>, IHasThumbImage, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }


        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
                }
            };
        }

        private Guid _id = new Guid("137437C9-3E1D-4D02-BD83-7025848D81FD");
        public override Guid Id
        {
            get { return _id; }
        }

        public static string PluginName = "TWiT Network";

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        public override string Name
        {
            get { return PluginName; }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static Plugin Instance { get; private set; }

        public override string Description
        {
            get
            {
                return "Watch your favourite netcasts from the TWiT network.";
            }
        }

        public ImageFormat ThumbImageFormat
        {
            get
            {
                return ImageFormat.Png;
            }
        }

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".Images.thumb.png");
        }

    }
}
