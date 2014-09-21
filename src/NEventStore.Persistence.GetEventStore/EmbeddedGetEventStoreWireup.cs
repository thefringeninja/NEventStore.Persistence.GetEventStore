
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using EventStore.ClientAPI.Embedded;
using EventStore.Core;
using EventStore.Core.Messaging;
using EventStore.Core.Services.UserManagement;
using EventStore.Projections.Core.Messages;
using EventStore.Projections.Core.Services;

namespace NEventStore.Persistence.GetEventStore
{
    using System;

    public class EmbeddedGetEventStoreWireup : GetEventStoreWireup
    {
        private string _database;
        private readonly IList<Func<EmbeddedVNodeBuilder, EmbeddedVNodeBuilder>> _customizationPipeline;
        private EmbeddedVNodeBuilder _builder;
        private Action _cleanup;
        private readonly IList<Action<ClusterVNode>> _startupTasks;

        private const string UnrollCommitsProjection = @"
fromStream('$et-NEventStoreCommit')
.partitionBy(function(e){
	return e.body.BucketId + '-' + e.body.StreamId;
})
.when({
	NEventStoreCommit: function(s, e) {
		if (!e.body || !e.body.Events) return;
		var events = e.body.Events.$values || e.body.Events || [];
		for (var i=0; i < events.length; i++) {
			var eventMessage = events[i];
			var data = eventMessage.Body;
			var metadata = eventMessage.Headers;
			
			var clrType = data.$type;
			
			if (!clrType) return;
			
			var fullTypeName = clrType.split(',')[0];
			
			var type = fullTypeName.split('.').pop();
			
			if (!type) return;
			
			emit('flattened-' + e.body.BucketId + '.' + e.body.StreamId, type, data, metadata);
		}
	}
});
";

        public EmbeddedGetEventStoreWireup(Wireup inner) : base(inner)
        {
            _customizationPipeline = new List<Func<EmbeddedVNodeBuilder, EmbeddedVNodeBuilder>>();
            _startupTasks = new List<Action<ClusterVNode>>();

            WithPersisenceFactory(serializer =>
            {
                var customize = _customizationPipeline
                    .Aggregate((current, next) => (builder => (next(current(builder)))));

                Action<ClusterVNode> startup = node => _startupTasks.ForEach(task => task(node));

                return new EmbeddedGetEventStorePersistenceFactory(serializer, _builder, customize, startup, _cleanup);
            });

            AsSingleNode()
                .InMemory()
                .WithCustomization(builder =>
                {
                    var ipEndPoint = new IPEndPoint(IPAddress.None, 0);
                    return builder
                        .WithInternalTcpOn(ipEndPoint)
                        .WithInternalHttpOn(ipEndPoint);
                })
                .WithContinuousProjection(UnrollCommitsProjection);
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
            return WithCustomization(builder => builder.RunOnDisk(_database));
        }

        public EmbeddedGetEventStoreWireup InMemory()
        {
            _cleanup = null;
            return WithCustomization(builder => builder.RunInMemory());
        }

        public EmbeddedGetEventStoreWireup WithCustomization(Func<EmbeddedVNodeBuilder, EmbeddedVNodeBuilder> customizeBuilder)
        {
            _customizationPipeline.Add(customizeBuilder);
            return this;
        }

        public EmbeddedGetEventStoreWireup WithStartupTask(Action<ClusterVNode> startupTask)
        {
            _startupTasks.Add(startupTask);
            return this;
        }

        public EmbeddedGetEventStoreWireup WithContinuousProjection(string projection, string name = null)
        {
            name = name ?? "neventstore_projection-" + ComputeProjectionName(projection);
            return WithStartupTask(node => node.MainQueue.Publish(new ProjectionManagementMessage.Command.Post(
                new NoopEnvelope(),
                ProjectionMode.Continuous,
                name,
                new ProjectionManagementMessage.RunAs(SystemAccount.Principal),
                "JS",
                projection,
                true,
                true,
                true)));
        }

        private static string ComputeProjectionName(string projection)
        {
            using (var hash = SHA1.Create())
            {
                var result = hash.ComputeHash(Encoding.UTF8.GetBytes(projection));
                return String.Join(String.Empty, result.Select(b => b.ToString("x2")).Take(4));
            }
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