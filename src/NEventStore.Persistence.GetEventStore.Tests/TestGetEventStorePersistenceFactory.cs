namespace NEventStore.Persistence.GetEventStore.Tests
{
    using EventStore.Common.Log;
    using NEventStore.Serialization;

    public class TestGetEventStorePersistenceFactory : IPersistenceFactory
    {
        public TestGetEventStorePersistenceFactory()
        {
            //LogManager.SetLogFactory(x => new ConsoleLogger());
        }
        public IPersistStreams Build()
        {
            return EmbeddedGetEventStorePersistenceFactory.InMemory(new JsonSerializer()).Build();
        }
    }

}
