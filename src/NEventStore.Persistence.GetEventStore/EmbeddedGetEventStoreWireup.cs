
namespace NEventStore.Persistence.GetEventStore
{
    using System;
    using EventStore.Core.TransactionLog.Chunks;
    using NEventStore.Serialization;

    public class EmbeddedGetEventStoreWireup : GetEventStoreWireup
    {
        private string _database;
        private int _chunkSize;

        public EmbeddedGetEventStoreWireup(Wireup inner) : base(inner)
        {
            _chunkSize = TFConsts.ChunkSize;
            WithPersisenceFactory(InMemory);
        }

        public EmbeddedGetEventStoreWireup WithDatabaseNamed(string database)
        {
            _database = database;
            return this;
        }

        public EmbeddedGetEventStoreWireup OnDisk(int? chunkSize)
        {
            WithPersisenceFactory(OnDisk);
            return WithChunkSizeOf(chunkSize ?? _chunkSize);
        }

        public EmbeddedGetEventStoreWireup WithChunkSizeOf(int chunkSize)
        {
            Guard.Against<ArgumentOutOfRangeException>(chunkSize < 1, "chunkSize");

            _chunkSize = chunkSize;

            return this;
        }

        public EmbeddedGetEventStoreWireup InMemory()
        {
            WithPersisenceFactory(InMemory);

            return this;
        }

        private EmbeddedGetEventStorePersistenceFactory OnDisk(ISerialize serializer)
        {
            return EmbeddedGetEventStorePersistenceFactory.OnDisk(serializer, _database, _chunkSize);
        }

        private EmbeddedGetEventStorePersistenceFactory InMemory(ISerialize serializer)
        {
            return EmbeddedGetEventStorePersistenceFactory.InMemory(serializer);
        }
    }
}