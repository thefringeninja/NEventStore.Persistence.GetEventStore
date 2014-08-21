namespace NEventStore.Persistence.GetEventStore.Tests
{
    using System.Net;
    using EventStore.ClientAPI;

    public class TestGetEventStorePersistenceFactory : IPersistenceFactory
    {
        public TestGetEventStorePersistenceFactory()
        {
            //LogManager.SetLogFactory(x => new ConsoleLogger());
        }
        public IPersistStreams Build()
        {
            return EmbeddedGetEventStorePersistenceFactory.InMemory(new GetEventStoreJsonSerializer()).Build();
        }
    }

}
