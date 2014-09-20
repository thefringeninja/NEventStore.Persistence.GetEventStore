using System.Net;

namespace NEventStore.Persistence.GetEventStore
{
    using System;
    using System.IO;
    using System.Threading;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.Embedded;
    using EventStore.Common.Options;
    using EventStore.Core;
    using EventStore.Core.Bus;
    using EventStore.Core.Messages;
    using EventStore.Core.Messaging;
    using EventStore.Projections.Core.Messages;
    using EventStore.Projections.Core.Services.Processing;
    using NEventStore.Serialization;

    public class EmbeddedGetEventStorePersistenceFactory : IPersistenceFactory
    {
        private readonly ISerialize _serializer;
        private readonly ClusterVNode _node;
        private readonly ಠ_ಠProjectionsSubsystem _projections;

        private EmbeddedGetEventStorePersistenceFactory(ISerialize serializer, EmbeddedVNodeBuilder builder, Func<EmbeddedVNodeBuilder, EmbeddedVNodeBuilder> customize = null)
        {
            Guard.AgainstNull(serializer, "serializer");
            Guard.AgainstNull(builder, "builder");

            customize = customize ?? (_ => _);

            _projections = CreateProjectionsSubsystem();

            builder = customize(builder).AddCustomSubsystem(_projections);

            _serializer = serializer;
            
            _node = builder;
        }

        private static EmbeddedVNodeBuilder SingleNode()
        {
            var ipEndPoint = new IPEndPoint(IPAddress.None, 0);
            return EmbeddedVNodeBuilder
                .AsSingleNode()
                .WithInternalTcpOn(ipEndPoint)
                .WithInternalHttpOn(ipEndPoint);
        }

        public static EmbeddedGetEventStorePersistenceFactory OnDisk(ISerialize serializer, string database = null)
        {
            Guard.Against<ArgumentException>(database == String.Empty, "database");
            return new EmbeddedGetEventStorePersistenceFactory(serializer, SingleNode()
                .RunOnDisk(ResolveDbPath(database, DateTime.UtcNow)));
        }

        public static EmbeddedGetEventStorePersistenceFactory InMemory(ISerialize serializer)
        {
            return new EmbeddedGetEventStorePersistenceFactory(serializer, SingleNode()
                .RunInMemory());
        }

        public IPersistStreams Build()
        {
            var connectionBuilder = BuildConnectionBuilder(_node, _projections);

            return new GetEventStorePersistenceEngine(connectionBuilder, () =>
            {
                var wait = new ManualResetEventSlim(false);

                _node.MainBus.Subscribe(
                    new AdHocHandler<SystemMessage.BecomeShutdown>(m => wait.Set()));

                _node.Stop();

                if (!wait.Wait(20000))
                    throw new TimeoutException("Node has not shut down in 20 seconds.");
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

        private static ಠ_ಠProjectionsSubsystem CreateProjectionsSubsystem()
        {
            return new ಠ_ಠProjectionsSubsystem(3, ProjectionType.All);
        }
        
        private static string ResolveDbPath(string optionsPath, DateTime startupTimeStamp)
        {
            if (String.IsNullOrEmpty(optionsPath))
                optionsPath = Path.Combine(Path.GetTempPath(),
                    "EventStore",
                    string.Format("{0:yyyy-MM-dd_HH.mm.ss.ffffff}-EmbeddedNode", startupTimeStamp));
            return Path.GetFullPath(optionsPath);
        }
    }
}