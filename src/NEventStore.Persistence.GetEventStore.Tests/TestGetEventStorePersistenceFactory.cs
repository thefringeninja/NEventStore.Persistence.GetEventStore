using EventStore.Common.Log;

namespace NEventStore.Persistence.GetEventStore.Tests
{
    public class TestGetEventStorePersistenceFactory : IPersistenceFactory
    {
        public TestGetEventStorePersistenceFactory()
        {
            //LogManager.SetLogFactory(x => new ConsoleLogger());
        }
        public IPersistStreams Build()
        {
            return new EmbeddedGetEventStorePersistenceFactory().Build();
        }
    }

}
