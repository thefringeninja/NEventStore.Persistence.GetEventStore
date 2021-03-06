﻿namespace NEventStore.Persistence.GetEventStore
{
    using System;
    using System.Net;
    using EventStore.ClientAPI;

    public class TcpGetEventStoreWireup : GetEventStoreWireup
    {
        private ConnectionSettingsBuilder _connectionSettingsBuilder;
        private string _connectionName;

        public TcpGetEventStoreWireup(Wireup inner) : base(inner)
        {
            _connectionSettingsBuilder = ConnectionSettings.Create();

            WithTcpConnectionTo(new IPEndPoint(IPAddress.Loopback, 1113));
        }

        public TcpGetEventStoreWireup WithTcpConnectionTo(IPEndPoint tcpEndPoint)
        {
            Guard.AgainstNull(tcpEndPoint, "tcpEndPoint");

            WithPersisenceFactory(serializer => new TcpGetEventStorePersistenceFactory(_connectionSettingsBuilder, tcpEndPoint, _connectionName, serializer));

            return this;
        }

        public TcpGetEventStoreWireup WithConnectionSettings(Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureBuilder)
        {
            Guard.AgainstNull(configureBuilder, "configureBuilder");

            _connectionSettingsBuilder = configureBuilder(_connectionSettingsBuilder);

            return this;
        }

        public TcpGetEventStoreWireup WithClusterSettings(ClusterSettings clusterSettings)
        {
            Guard.AgainstNull(clusterSettings, "clusterSettings");

            WithPersisenceFactory(serializer => new TcpGetEventStorePersistenceFactory(_connectionSettingsBuilder, clusterSettings, _connectionName, serializer));

            return this;
        }

        public TcpGetEventStoreWireup WithConnectionName(string connectionName)
        {
            Guard.Against<ArgumentException>(connectionName == String.Empty, "connectionName");

            _connectionName = connectionName;

            return this;
        }
    }
}