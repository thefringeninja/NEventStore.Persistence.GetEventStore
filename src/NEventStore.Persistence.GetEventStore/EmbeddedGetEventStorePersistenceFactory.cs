using EventStore.Core.Data;

namespace NEventStore.Persistence.GetEventStore
{
    using System;
    using System.Threading;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.Embedded;
    using EventStore.Common.Options;
    using EventStore.Core;
    using EventStore.Core.Bus;
    using EventStore.Core.Messaging;
    using EventStore.Projections.Core.Messages;
    using EventStore.Projections.Core.Services.Processing;
    using NEventStore.Serialization;

    public class EmbeddedGetEventStorePersistenceFactory : IPersistenceFactory
    {
        private readonly ISerialize _serializer;
        private readonly ClusterVNode _node;
        private readonly Action<ClusterVNode> _startup;
        private readonly ಠ_ಠProjectionsSubsystem _projections;
        private readonly Action _cleanup;
        internal EmbeddedGetEventStorePersistenceFactory(
            ISerialize serializer, 
            EmbeddedVNodeBuilder builder, 
            Func<EmbeddedVNodeBuilder, EmbeddedVNodeBuilder> customize = null, 
            Action<ClusterVNode> startup = null, 
            Action cleanup = null)
        {
            Guard.AgainstNull(serializer, "serializer");
            Guard.AgainstNull(builder, "builder");

            customize = customize ?? (_ => _);
            
            _cleanup = cleanup ?? (() => { });

            _projections = CreateProjectionsSubsystem();

            builder = customize(builder).AddCustomSubsystem(_projections);

            _serializer = serializer;
            
            _node = builder;
            _startup = startup;
        }

        public IPersistStreams Build()
        {
            var buildConnection = BuildConnectionBuilder();

            var dropAction = BuildDropAction();

            return new GetEventStorePersistenceEngine(buildConnection, dropAction, _serializer);
        }

        private Action BuildDropAction()
        {
            return () =>
            {
                var wait = new ManualResetEventSlim(false);

                _node.NodeStatusChanged += (sender, args) =>
                {
                    if (args.NewVNodeState == VNodeState.Shutdown)
                    {
                        wait.Set();
                    }
                };
                _node.Stop();

                if (!wait.Wait(20000))
                    throw new TimeoutException("Node has not shut down in 20 seconds.");

                _cleanup();
            };
        }

        private Func<IEventStoreConnection> BuildConnectionBuilder()
        {
            return () =>
            {
                var wait = new ManualResetEventSlim(false);

                _node.NodeStatusChanged += (sender, args) =>
                {
                    if (args.NewVNodeState == VNodeState.Master)
                    {
                        wait.Set();
                    }
                };

                _node.Start();

                if (!wait.Wait(20000))
                    throw new TimeoutException("Node has not started in 20 seconds.");

                StartProjections(_projections.MainQueue);

                _startup(_node);
                
                return EmbeddedEventStoreConnection.Create(_node);
            };
        }

        private static void StartProjection(string projection, IPublisher projectionsQueue)
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

        private static void StartProjections(IPublisher projectionsQueue)
        {
            StartProjection(ProjectionNamesBuilder.StandardProjections.EventByCategoryStandardProjection,
                projectionsQueue);
            StartProjection(ProjectionNamesBuilder.StandardProjections.EventByTypeStandardProjection, 
                projectionsQueue);
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