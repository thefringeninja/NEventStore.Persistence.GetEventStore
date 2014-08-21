
namespace NEventStore.Persistence.GetEventStore
{
    using System;
    using System.Net;
    using EventStore.ClientAPI;
    using NEventStore.Serialization;

    public class TcpGetEventStorePersistenceFactory : IPersistenceFactory
    {
        private readonly Func<IEventStoreConnection> _connectionBuilder;
        private readonly ISerialize _serializer;

        private TcpGetEventStorePersistenceFactory(Func<IEventStoreConnection> connectionBuilder, ISerialize serializer)
        {
            Guard.AgainstNull(connectionBuilder, "connectionBuilder");
            Guard.AgainstNull(serializer, "serializer");

            _connectionBuilder = connectionBuilder;
            _serializer = serializer;
        }

        public TcpGetEventStorePersistenceFactory(ConnectionSettings connectionSettings, IPEndPoint tcpEndPoint, 
            string connectionName, ISerialize serializer)
            : this(() => EventStoreConnection.Create(connectionSettings, tcpEndPoint, connectionName), serializer)
        {

        }

        public TcpGetEventStorePersistenceFactory(ConnectionSettings connectionSettings, ClusterSettings clusterSettings,
            string connectionName, ISerialize serializer)
            : this(() => EventStoreConnection.Create(connectionSettings, clusterSettings, connectionName), serializer)
        {
            _serializer = serializer;
        }

        public IPersistStreams Build()
        {
            return new GetEventStorePersistenceEngine(
                    _connectionBuilder, 
                    () => { },
                    _serializer);
        }
    }
}