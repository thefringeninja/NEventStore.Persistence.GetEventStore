
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using EventStore.ClientAPI.Embedded;

namespace NEventStore.Persistence.GetEventStore
{
    using System;

    public class EmbeddedGetEventStoreWireup : GetEventStoreWireup
    {
        private string _database;
        private readonly IList<Func<EmbeddedVNodeBuilder, EmbeddedVNodeBuilder>> _customizationPipeline;
        private EmbeddedVNodeBuilder _builder;
        private Action _cleanup;

        public EmbeddedGetEventStoreWireup(Wireup inner) : base(inner)
        {
            _customizationPipeline = new List<Func<EmbeddedVNodeBuilder, EmbeddedVNodeBuilder>>();
            WithPersisenceFactory(serializer =>
            {
                var customize = _customizationPipeline
                    .Aggregate((current, next) => (builder => (next(current(builder)))));

                return new EmbeddedGetEventStorePersistenceFactory(serializer, _builder, customize, _cleanup);
            });

            AsSingleNode()
                .InMemory()
                .CustomizeClusterWith(builder =>
                {
                    var ipEndPoint = new IPEndPoint(IPAddress.None, 0);
                    return builder
                        .WithInternalTcpOn(ipEndPoint)
                        .WithInternalHttpOn(ipEndPoint);
                });
        }

        public EmbeddedGetEventStoreWireup WithDatabaseNamed(string database)
        {
            Guard.AgainstNull(database, "database");

            _database = ResolveDbPath(database);
            
            return OnDisk();
        }

        public EmbeddedGetEventStoreWireup AsSingleNode()
        {
            _builder = EmbeddedVNodeBuilder.AsSingleNode();
            return this;
        }

        public EmbeddedGetEventStoreWireup OnDisk()
        {
            _cleanup = () =>
            {
                if (Directory.Exists(_database))
                {
                    Directory.Delete(_database, true);
                }
            };
            return CustomizeClusterWith(builder => builder.RunOnDisk(_database));
        }

        public EmbeddedGetEventStoreWireup InMemory()
        {
            _cleanup = null;
            return CustomizeClusterWith(builder => builder.RunInMemory());
        }

        public EmbeddedGetEventStoreWireup CustomizeClusterWith(Func<EmbeddedVNodeBuilder, EmbeddedVNodeBuilder> customizeBuilder)
        {
            _customizationPipeline.Add(customizeBuilder);
            return this;
        }

        private static string ResolveDbPath(string optionsPath)
        {
            if (String.IsNullOrEmpty(optionsPath))
                optionsPath = Path.Combine(Path.GetTempPath(),
                    "EventStore",
                    String.Format("{0:yyyy-MM-dd_HH.mm.ss.ffffff}-EmbeddedNode", DateTime.UtcNow));
            return Path.GetFullPath(optionsPath);
        }
    }
}