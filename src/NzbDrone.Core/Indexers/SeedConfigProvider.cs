using System;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers
{
    public interface ISeedConfigProvider
    {
        TorrentSeedConfiguration GetSeedConfiguration(RemoteBook release);
        TorrentSeedConfiguration GetSeedConfiguration(int indexerId, bool fullSeason);
    }

    public class SeedConfigProvider : ISeedConfigProvider, IHandle<IndexerSettingUpdatedEvent>
    {
        private readonly IIndexerFactory _indexerFactory;
        private readonly ICached<SeedCriteriaSettings> _cache;

        public SeedConfigProvider(IIndexerFactory indexerFactory, ICacheManager cacheManager)
        {
            _indexerFactory = indexerFactory;
            _cache = cacheManager.GetRollingCache<SeedCriteriaSettings>(GetType(), "criteriaByIndexer", TimeSpan.FromHours(1));
        }

        public TorrentSeedConfiguration GetSeedConfiguration(RemoteBook remoteBook)
        {
            if (remoteBook.Release.DownloadProtocol != DownloadProtocol.Torrent)
            {
                return null;
            }

            if (remoteBook.Release.IndexerId == 0)
            {
                return null;
            }

            return GetSeedConfiguration(remoteBook.Release.IndexerId, remoteBook.ParsedBookInfo.Discography);
        }

        public TorrentSeedConfiguration GetSeedConfiguration(int indexerId, bool fullSeason)
        {
            if (indexerId == 0)
            {
                return null;
            }

            var seedCriteria = _cache.Get(indexerId.ToString(), () => FetchSeedCriteria(indexerId));

            if (seedCriteria == null)
            {
                return null;
            }

            var seedConfig = new TorrentSeedConfiguration
            {
                Ratio = seedCriteria.SeedRatio
            };

            var seedTime = fullSeason ? seedCriteria.DiscographySeedTime : seedCriteria.SeedTime;
            if (seedTime.HasValue)
            {
                seedConfig.SeedTime = TimeSpan.FromMinutes(seedTime.Value);
            }

            return seedConfig;
        }

        private SeedCriteriaSettings FetchSeedCriteria(int indexerId)
        {
            try
            {
                var indexer = _indexerFactory.Get(indexerId);
                var torrentIndexerSettings = indexer.Settings as ITorrentIndexerSettings;

                return torrentIndexerSettings?.SeedCriteria;
            }
            catch (ModelNotFoundException)
            {
                return null;
            }
        }

        public void Handle(IndexerSettingUpdatedEvent message)
        {
            _cache.Clear();
        }
    }
}
