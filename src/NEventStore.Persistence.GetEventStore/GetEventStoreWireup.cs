namespace NEventStore.Persistence.GetEventStore
{
    using System;
    using NEventStore.Serialization;

    public abstract class GetEventStoreWireup : PersistenceWireup
    {
        private Func<ISerialize, IPersistenceFactory> _factoryFactory;
        private GetEventStoreJsonSerializer.Configure _configure;

        protected GetEventStoreWireup(Wireup inner) : base(inner)
        {
            Container.Register(container => _factoryFactory(new GetEventStoreJsonSerializer(_configure)).Build());
        }

        protected void WithPersisenceFactory(Func<ISerialize, IPersistenceFactory> factoryFactory)
        {
            Guard.AgainstNull(factoryFactory, "factoryFactory");

            _factoryFactory = factoryFactory;
        }

        public GetEventStoreWireup CustomizeJsonSerailizationWith(GetEventStoreJsonSerializer.Configure configure)
        {
            _configure = configure;
            return this;
        }
    }
}
