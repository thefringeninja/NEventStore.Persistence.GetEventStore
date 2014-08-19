
namespace NEventStore.Persistence.GetEventStore
{
    using System;
    using NEventStore.Serialization;

    public abstract class GetEventStoreWireup : PersistenceWireup
    {
        private Func<ISerialize, IPersistenceFactory> _factoryFactory;

        protected GetEventStoreWireup(Wireup inner) : base(inner)
        {
            Container.Register(container => _factoryFactory(new GetEventStoreJsonSerializer()).Build());
        }

        protected void WithPersisenceFactory(Func<ISerialize, IPersistenceFactory> factoryFactory)
        {
            Guard.AgainstNull(factoryFactory, "factoryFactory");

            _factoryFactory = factoryFactory;
        }
    }
}
