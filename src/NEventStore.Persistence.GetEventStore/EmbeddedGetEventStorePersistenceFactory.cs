namespace NEventStore.Persistence.GetEventStore
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.Embedded;
    using EventStore.Common.Options;
    using EventStore.Common.Utils;
    using EventStore.Core;
    using EventStore.Core.Authentication;
    using EventStore.Core.Bus;
    using EventStore.Core.Cluster.Settings;
    using EventStore.Core.Messages;
    using EventStore.Core.Messaging;
    using EventStore.Core.Services.Gossip;
    using EventStore.Core.Services.Monitoring;
    using EventStore.Core.TransactionLog.Checkpoint;
    using EventStore.Core.TransactionLog.Chunks;
    using EventStore.Core.TransactionLog.FileNamingStrategy;
    using EventStore.Core.Util;
    using EventStore.Projections.Core.Messages;
    using EventStore.Projections.Core.Services.Processing;
    using NEventStore.Serialization;

    public class EmbeddedGetEventStorePersistenceFactory : IPersistenceFactory
    {
        private readonly string _database;
        private readonly DateTime _startupTimeStamp;
        private readonly ISerialize _serializer;
        private readonly bool _inMemDb;
        private readonly int _chunkSize;

        private EmbeddedGetEventStorePersistenceFactory(ISerialize serializer, int chunkSize = TFConsts.ChunkSize, string database = null, bool inMemDb = false)
        {
            Guard.AgainstNull(serializer, "serializer");
            Guard.Against<ArgumentOutOfRangeException>(chunkSize < 1, "chunkSize");
            Guard.Against<ArgumentException>(database == String.Empty, "database");

            _serializer = serializer;
            _database = database;
            _startupTimeStamp = DateTime.UtcNow;
            _inMemDb = inMemDb;
            _chunkSize = chunkSize;
        }

        public static EmbeddedGetEventStorePersistenceFactory OnDisk(ISerialize serializer, string database = null, int chunkSize = TFConsts.ChunkSize)
        {
            return new EmbeddedGetEventStorePersistenceFactory(serializer, chunkSize, database);
        }

        public static EmbeddedGetEventStorePersistenceFactory InMemory(ISerialize serializer)
        {
            return new EmbeddedGetEventStorePersistenceFactory(serializer, inMemDb: true);
        }

        public IPersistStreams Build()
        {
            string dbPath = Path.GetFullPath(ResolveDbPath(_database));

            ಠ_ಠProjectionsSubsystem projectionsSubsystem = CreateProjectionsSubsystem();

            ClusterVNode node = CreateNode(dbPath, projectionsSubsystem);

            Func<IEventStoreConnection> connectionBuilder = BuildConnectionBuilder(node, projectionsSubsystem);

            return new GetEventStorePersistenceEngine(connectionBuilder, () =>
            {
                var wait = new ManualResetEventSlim(false);

                node.MainBus.Subscribe(
                    new AdHocHandler<SystemMessage.BecomeShutdown>(m => wait.Set()));

                node.Stop();

                if (!wait.Wait(20000))
                    throw new TimeoutException("Node has not shut down in 20 seconds.");

                if (Directory.Exists(dbPath))
                {
                    Directory.Delete(dbPath, true);
                }
            }, _serializer);
        }

        private Func<IEventStoreConnection> BuildConnectionBuilder(ClusterVNode node,
            ಠ_ಠProjectionsSubsystem projectionsSubsystem)
        {
            return () =>
            {
                var wait = new ManualResetEventSlim(false);

                node.MainBus.Subscribe(
                    new AdHocHandler<ProjectionManagementMessage.RequestSystemProjections>(m => wait.Set()));

                node.Start();

                if (!wait.Wait(20000))
                    throw new TimeoutException("Node has not started in 20 seconds.");

                StartProjections(projectionsSubsystem.MainQueue);
                
                return EmbeddedEventStoreConnection.Create(node);
            };
        }

        private void StartProjection(string projection, IPublisher projectionsQueue)
        {
            projectionsQueue.Publish(new ProjectionManagementMessage.Command.Enable(
                new CallbackEnvelope(message =>
                {
                    if (message is ProjectionManagementMessage.NotFound)
                    {
                        StartProjection(projection, projectionsQueue);
                    }
                }), projection, ProjectionManagementMessage.RunAs.System));
        }

        private void StartProjections(IPublisher projectionsQueue)
        {
            StartProjection(ProjectionNamesBuilder.StandardProjections.EventByCategoryStandardProjection,
                projectionsQueue);
            StartProjection(ProjectionNamesBuilder.StandardProjections.EventByTypeStandardProjection, projectionsQueue);
            StartProjection(ProjectionNamesBuilder.StandardProjections.StreamByCategoryStandardProjection,
                projectionsQueue);
            StartProjection(ProjectionNamesBuilder.StandardProjections.StreamsStandardProjection, projectionsQueue);
        }

        private ClusterVNode CreateNode(string dbPath, ಠ_ಠProjectionsSubsystem projections)
        {
            ClusterVNodeSettings clusterVNodeSettings = CreateClusterVNodeSettings();

            var node = new ClusterVNode(new TFChunkDb(CreateDbConfig(dbPath, _chunkSize, -1, TFConsts.ChunksCacheSize, _inMemDb)),
                clusterVNodeSettings, new KnownEndpointGossipSeedSource(new IPEndPoint[0]), true,
                Opts.MaxMemtableSizeDefault, projections);
            return node;
        }

        private static ಠ_ಠProjectionsSubsystem CreateProjectionsSubsystem()
        {
            return new ಠ_ಠProjectionsSubsystem(3, ProjectionType.All);
        }

        protected static TFChunkDbConfig CreateDbConfig(string dbPath, int chunkSize, int cachedChunks, long chunksCacheSize, bool inMemDb)
        {
            ICheckpoint writerChk;
            ICheckpoint chaserChk;
            ICheckpoint epochChk;
            ICheckpoint truncateChk;

            if (inMemDb)
            {
                writerChk = new InMemoryCheckpoint(Checkpoint.Writer);
                chaserChk = new InMemoryCheckpoint(Checkpoint.Chaser);
                epochChk = new InMemoryCheckpoint(Checkpoint.Epoch, -1);
                truncateChk = new InMemoryCheckpoint(Checkpoint.Truncate, -1);
            }
            else
            {
                if (!Directory.Exists(dbPath)) // mono crashes without this check
                    Directory.CreateDirectory(dbPath);

                string writerCheckFilename = Path.Combine(dbPath, Checkpoint.Writer + ".chk");
                string chaserCheckFilename = Path.Combine(dbPath, Checkpoint.Chaser + ".chk");
                string epochCheckFilename = Path.Combine(dbPath, Checkpoint.Epoch + ".chk");
                string truncateCheckFilename = Path.Combine(dbPath, Checkpoint.Truncate + ".chk");
                if (Runtime.IsMono)
                {
                    writerChk = new FileCheckpoint(writerCheckFilename, Checkpoint.Writer, true);
                    chaserChk = new FileCheckpoint(chaserCheckFilename, Checkpoint.Chaser, true);
                    epochChk = new FileCheckpoint(epochCheckFilename, Checkpoint.Epoch, true, initValue: -1);
                    truncateChk = new FileCheckpoint(truncateCheckFilename, Checkpoint.Truncate, true,
                        initValue: -1);
                }
                else
                {
                    writerChk = new MemoryMappedFileCheckpoint(writerCheckFilename, Checkpoint.Writer, true);
                    chaserChk = new MemoryMappedFileCheckpoint(chaserCheckFilename, Checkpoint.Chaser, true);
                    epochChk = new MemoryMappedFileCheckpoint(epochCheckFilename, Checkpoint.Epoch, true,
                        initValue: -1);
                    truncateChk = new MemoryMappedFileCheckpoint(truncateCheckFilename, Checkpoint.Truncate, true,
                        initValue: -1);
                }
            }
            long cache = cachedChunks >= 0
                ? cachedChunks*(long) (chunkSize + ChunkHeader.Size + ChunkFooter.Size)
                : chunksCacheSize;
            var nodeConfig = new TFChunkDbConfig(dbPath,
                new VersionedPatternFileNamingStrategy(dbPath, "chunk-"),
                chunkSize,
                cache,
                writerChk,
                chaserChk,
                epochChk,
                truncateChk,
                inMemDb);
            return nodeConfig;
        }


        private static ClusterVNodeSettings CreateClusterVNodeSettings()
        {
            var fakeEndpoint = new IPEndPoint(IPAddress.Loopback, 0);
            return new ClusterVNodeSettings(Guid.NewGuid(),
                0,
                fakeEndpoint,
                null,
                fakeEndpoint,
                null,
                fakeEndpoint,
                fakeEndpoint,
                new string[0],
                false,
                null,
                1,
                false,
                "whatever",
                new IPEndPoint[] {},
                TFConsts.MinFlushDelayMs,
                1,
                1,
                1,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(2),
                false,
                "",
                false,
                TimeSpan.FromHours(1),
                StatsStorage.None,
                1,
                new InternalAuthenticationProviderFactory(),
                true,
                true,
                true,
                false,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(10));
        }

        private string GetLogsDirectory()
        {
            return ResolveDbPath(_database) + "-logs";
        }

        private string ResolveDbPath(string optionsPath)
        {
            if (String.IsNullOrEmpty(optionsPath))
                return Path.Combine(Path.GetTempPath(),
                    "EventStore",
                    string.Format("{0:yyyy-MM-dd_HH.mm.ss.ffffff}-EmbeddedNode", _startupTimeStamp));
            return optionsPath;
        }
    }
}