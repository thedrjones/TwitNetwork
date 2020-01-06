# TwitNetwork
A Emby Server plugin using the TWiT API to access links for all the TWiT Network podcasts

You will currently need to bring you own key. See https://twit.tv/about/developer-program for details.
I would not recommend calling for more than 45 days of content, of course you can call more, but more info in next point.
The standard TWiT API is limited to 5 requests per minute and so is the default configuration. Therefore the plugin will wait a minute before calling it again. 
As you ask for more shows, the time to make all the calls increases. We get 25 episodes per API call, along with their associated metadata. This will collect on the internet channels scheduled job,so you might not notice it much.

This is only the basic functionaity for the moment. Here is a taster of future changes:

* Allow choosing the size of channel thumbnails, as the 1200x1200 option is overkill for most people and will use up bandwidth.
* Create a folder that allows access to the archived shows for inactive shows
* Extend the caching so that we can old old episodes from a cache on disk on restart, reducing the calls needed to keep up to date
* Allow manual ways to access the even older episode in a given show's catalog.

Known issues:

I have tried to map the ChannelInfoItem objects as close as I can, but some don't seem to carry into the application like meta data rich movies. I think this might need a change to the exposed emby references, but we will see.

There is no way to reset the "good" cache status, but changing the number of days to a higher number or restarting Emby will sort that.

Default sorting is not very good, I've only recently started with writing Emby plugins.

Sometimes the embedded application icon will not work (e.g. one of my Rokus).

Credits:

The existing TWiT Emby plugin, plus a couple of others which handle json which I used for ideas.
