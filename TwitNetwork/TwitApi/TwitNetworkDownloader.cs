
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;


namespace TwitNetwork.TwitApi
{
    class TwitNetworkDownloader
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;

        public TwitNetworkDownloader(ILogger logger, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }


        /// <summary>
        /// Chain populates the episode cache.
        /// </summary>
        /// <param name="requestCache">An instance of the TWiT network cache</param>
        /// <param name="requestDays">Number of day to request in the API call</param>
        /// <param name="requestLimit">Number of API calls allowed in a minute</param>
        /// <param name="cancellationToken"></param>
        /// <returns>If a successful cache refresh was performed</returns>
        public async Task<bool> PopulateNetworkCache(TwitNetworkCache requestCache, int requestDays, int requestLimit, CancellationToken cancellationToken)
        {
            TimeSpan updateInterval = (DateTime.Now - requestCache.LastUpdated);
            if (requestDays > requestCache.RequestDays)
            {
                _logger.Info("TWiT network cache does not contain enough requested days");
            }
            else if (updateInterval.TotalHours < 1)
            {
                _logger.Info("TWiT network cache is valid and does not require updating yet");
                return false;
            }
            else
            {
                _logger.Info("TWiT network cache is old and requires an update");
            }



            long requestDaysAgoAsEpoch = GetEpochDate(requestDays);

            var apiUrl = String.Format("https://twit.tv/api/v1.0/episodes?filter[airingDate][value]={0}&filter[airingDate][operator]={1}",requestDaysAgoAsEpoch,">=");
            
            int callCount = 0;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Info("Stopping API requests due to cancellation request");
                    return false;
                }
                // Prevent requesting nothing
                if (string.IsNullOrEmpty(apiUrl))
                {
                    _logger.Debug("Empty apiUrl, halting requests.");
                    return true;
                }

                // Issue the API request and serialize the result for usage
                var responseStream = await TwitNetworkApiRequest(apiUrl).ConfigureAwait(false);
                _logger.Debug("Got responseStream [{0}]",apiUrl);
                var listAllEpisodes = _jsonSerializer.DeserializeFromStream<ListEpisodesResponse>(responseStream);

                _logger.Debug("Deserialized the responseStream, processing Network Update");
               
                try
                {
                    // Process the network cache update
                    if (ProcessNetworkCache(requestCache, listAllEpisodes))
                        _logger.Debug("Successfully updated TWiT Network cache with this call");
                    else
                        _logger.Debug("Unable to update TWiT Network cache with this call");

                    // See if we we have reached the end of the needed API calls
                    if (listAllEpisodes._links.next != null)
                    {
                        listAllEpisodes._links.next.TryGetValue("href", out apiUrl);
                        _logger.Debug("Next href is {0}", apiUrl);
                    }
                    else
                    {
                        _logger.Info("TWiT network cache update is complete");
                        requestCache.MarkCacheComplete(requestDays);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug("Error when processing the network update: {0}", ex.Message);
                    return false;
                }

                // Iterate the counter, then if the next call will exceed the limit, wait a minute before the next call
                callCount++; 
                if ((callCount % requestLimit) == 0)
                {
                    _logger.Debug("Waiting for the next request to be sent due to request limit");
                    for (int iter = 0; iter < 60; iter++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            iter = 60;
                        else
                            Thread.Sleep(1000);
                    }
                }
            }
        }


        public bool ProcessNetworkCache(TwitNetworkCache requestCache, ListEpisodesResponse listAllEpisodes)
        {
            foreach (Episode episode in listAllEpisodes.episodes)
            {
                foreach(Show show in episode._embedded.shows)
                {
                    TwitNetworkShowCache thisShow;
                    if (requestCache.ContainsShow(show.id))
                    {
                        _logger.Debug("Show {0} is already in the network cache", show.label);
                        thisShow = requestCache.GetCacheEntry(show.id);
                    }
                    else
                    {
                        if (requestCache.TryAdd(show))
                        {
                            _logger.Debug("Show {0} has been added to the network cache", show.label);
                            thisShow = requestCache.GetCacheEntry(show.id);
                        }
                        else
                        {
                            _logger.Warn("Show {0} cannot be added to the network cache", show.label);
                            continue;
                        }
                    }

                    if (thisShow.HasEpisode(episode.id))
                    {
                        _logger.Debug("{0} episode {1} ({2}) is already in the network cache", show.label, episode.episodeNumber, episode.label);
                    }
                    else
                    {
                        _logger.Debug("{0} episode {1} ({2}) has been added to the cache", show.label, episode.episodeNumber, episode.label);
                        thisShow.AddEpisode(episode);
                    }
                }
            }
            return true;
        }

        public long GetEpochDate(int requestDays)
        {
            DateTime RequestSince = DateTime.UtcNow.AddDays(-requestDays);
            TimeSpan t = RequestSince - new DateTime(1970, 1, 1);
            return (long)t.TotalSeconds;
        }

        public async Task<Stream> TwitNetworkApiRequest(string url)
        {

            Stream responseStream = new MemoryStream();

            HttpRequestOptions reqOpts = new HttpRequestOptions
            {
                Url = url,
                AcceptHeader = "application/json",
                EnableDefaultUserAgent = true,
                EnableHttpCompression = false,
                EnableKeepAlive = false
            };
            reqOpts.RequestHeaders.Add("app-id", Plugin.Instance.Configuration.AppID);
            reqOpts.RequestHeaders.Add("app-key", Plugin.Instance.Configuration.AppKey);

            DebugCall(reqOpts);
            int attempts = 0;

            while (attempts < 6)
            {
                attempts++;
                try
                {
                    using (var apiResponse = await _httpClient.GetResponse(reqOpts).ConfigureAwait(false))
                    {
                        DebugCall(reqOpts, apiResponse);

                        if (apiResponse.StatusCode == HttpStatusCode.OK)
                        {
                            using (var apiResponseStream = apiResponse.Content)
                            {
                                _logger.Debug("Returning valid API response");
                                apiResponseStream.CopyTo(responseStream);
                                return responseStream;
                            }
                        }
                        else if (apiResponse.StatusCode == HttpStatusCode.InternalServerError)
                        {
                            _logger.Debug("HTTP Response was {0} {1}", (int)apiResponse.StatusCode, apiResponse.StatusCode);
                            using (var apiResponseStream = apiResponse.Content)
                            {
                                _logger.Debug("Reading TWiT error response with Json deserializer");
                                var errorResponse = _jsonSerializer.DeserializeFromStream<InternalServerErrorResponse>(responseStream);

                                if (errorResponse._errors.ContainsKey("message"))
                                {
                                    if (errorResponse._errors["message"].Contains("usage limits are exceeded"))
                                    {
                                        _logger.Debug("API Usage limits have been exceeded, delay before retry");
                                    }
                                    else
                                    {
                                        _logger.Debug("API issue seen, delay before retry: {0}", errorResponse._errors["message"]);
                                    }
                                }
                                else
                                {
                                    _logger.Debug("Unhandled API issue, delay before retry");
                                }
                            }
                            Thread.Sleep(20000);
                        }
                        else
                        {
                            _logger.Info("An unexpected error was returned from the API: {0} {1}", (int)apiResponse.StatusCode, apiResponse.StatusCode);
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message == "InternalServerError")
                    {
                        _logger.Debug("Ignoring InternalServerError {0} attempt {1}", reqOpts.Url, attempts);
                        Thread.Sleep(20000);
                        continue;
                    }
                    _logger.Info("Error during Twit API call attempt {0} {1}", reqOpts.Url, attempts);
                    _logger.Info("\t Exception details: {0}", ex.Message);
                    throw ex;
                }
            }
            _logger.Info("Unable to access twit API after 5 attempts, stopping");
            return null;
        }

        public void DebugCall(HttpRequestOptions apiRequest)
        {
            if (!Plugin.Instance.Configuration.EnableDebugLogging)
                return;

            _logger.Debug("Pre Request Debug:");
            _logger.Debug("Requesting {0}", apiRequest.Url);
            foreach (var header in apiRequest.RequestHeaders)
                _logger.Debug("\t{0}\t{1}", header.Key, header.Value);
        }

        public void DebugCall(HttpRequestOptions apiRequest, HttpResponseInfo apiResponse)
        {
            if (!Plugin.Instance.Configuration.EnableDebugLogging)
                return;

            _logger.Debug("Post Request Debug:");
            ///_logger.Debug("Obtained response from {0}: {1}\tHTTP Status {2}", apiRequest.Url, apiResponse.ContentType, apiResponse.StatusCode);
            _logger.Debug("Obtained response from {0}: {1}\tHTTP Status {2}", apiRequest.Url, "", apiResponse.StatusCode);
            ///_logger.Debug("Response Length: {0}", apiResponse.ContentLength);
            foreach (var header in apiResponse.Headers)
                _logger.Debug("\t{0}\t{1}", header.Key, header.Value);
        }

    }


}