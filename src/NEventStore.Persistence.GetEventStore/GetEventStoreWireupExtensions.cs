namespace NEventStore.Persistence.GetEventStore
{
    public static class GetEventStoreWireupExtensions
    {
        public static EmbeddedGetEventStoreWireup UsingEmbeddedEventStore(this Wireup wireup)
        {
            return new EmbeddedGetEventStoreWireup(wireup);
        }

        public static TcpGetEventStoreWireup UsingEventStoreOverTcp(this Wireup wireup)
        {
            return new TcpGetEventStoreWireup(wireup);
        }
    }
}