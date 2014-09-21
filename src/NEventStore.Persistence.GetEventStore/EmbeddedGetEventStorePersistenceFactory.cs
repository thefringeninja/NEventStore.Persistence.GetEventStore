using System.Net;
using EventStore.Core.Services.TimerService;

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
        private readonly Action _cleanup;
        internal EmbeddedGetEventStorePersistenceFactory(ISerialize serializer, EmbeddedVNodeBuilder builder, Func<EmbeddedVNodeBuilder, EmbeddedVNodeBuilder> customize = null, Action cleanup = null)
        {
            Guard.AgainstNull(serializer, "serializer");
            Guard.AgainstNull(builder, "builder");

            customize = customize ?? (_ => _);
            
            _cleanup = cleanup ?? (() => { });

            _projections = CreateProjectionsSubsystem();

            builder = customize(builder).AddCustomSubsystem(_projections);

            _serializer = serializer;
            
            _node = builder;
        }

        public IPersistStreams Build()
        {
            var buildConnection = BuildConnectionBuilder(_node, _projections);

            var dropAction = BuildDropAction();

            return new GetEventStorePersistenceEngine(buildConnection, dropAction, _serializer);
        }

        private Action BuildDropAction()
        {
            return () =>
            {
                var wait = new ManualResetEventSlim(false);

                _node.MainBus.Subscribe(
                    new AdHocHandler<SystemMessage.BecomeShutdown>(m => wait.Set()));

                _node.Stop();

                if (!wait.Wait(20000))
                    throw new TimeoutException("Node has not shut down in 20 seconds.");

                _cleanup();
            };
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
    }
}