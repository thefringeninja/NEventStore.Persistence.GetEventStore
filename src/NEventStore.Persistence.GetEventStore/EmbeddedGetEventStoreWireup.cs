namespace NEventStore.Persistence.GetEventStore
{
    using NEventStore.Serialization;

    public class EmbeddedGetEventStoreWireup : PersistenceWireup
    {
        private string _database;

        public EmbeddedGetEventStoreWireup(Wireup inner) : base(inner)
        {
            Container.Register(
                container => new EmbeddedGetEventStorePersistenceFactory(container.Resolve<ISerialize>(), _database));
        }

        public EmbeddedGetEventStoreWireup WithDatabaseNamed(string database)
        {
            _database = database;
            return this;
        }
    }
}