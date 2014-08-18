using System;
using System.Net;
using EventStore.ClientAPI;
using NEventStore.Serialization;

namespace NEventStore.Persistence.GetEventStore
{
    public class TcpGetEventStoreWireup : PersistenceWireup
    {
        private ConnectionSettingsBuilder _connectionSettingsBuilder;
        private Func<ISerialize, TcpGetEventStorePersistenceFactory> _factoryFactory;
        private string _connectionName;

        public TcpGetEventStoreWireup(Wireup inner) : base(inner)
        {
            _connectionSettingsBuilder = ConnectionSettings.Create();

            Container.Register(container => _factoryFactory(container.Resolve<ISerialize>()).Build());
        }

        public TcpGetEventStoreWireup WithTcpConnectionTo(IPEndPoint tcpEndPoint)
        {
            _factoryFactory = serializer => new TcpGetEventStorePersistenceFactory(_connectionSettingsBuilder, tcpEndPoint, _connectionName, serializer);

            return this;
        }

        public TcpGetEventStoreWireup WithConnectionSettings(Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureBuilder)
        {
            _connectionSettingsBuilder = configureBuilder(_connectionSettingsBuilder);

            return this;
        }

        public TcpGetEventStoreWireup WithClusterSettings(ClusterSettings clusterSettings)
        {
            _factoryFactory = serializer => new TcpGetEventStorePersistenceFactory(_connectionSettingsBuilder, clusterSettings, _connectionName, serializer);

            return this;
        }

        public TcpGetEventStoreWireup WithConnectionName(string connectionName)
        {
            _connectionName = connectionName;

            return this;
        }
    }
}