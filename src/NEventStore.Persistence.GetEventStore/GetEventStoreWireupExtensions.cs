namespace NEventStore.Persistence.GetEventStore
{
    public static class GetEventStoreWireupExtensions
    {
        public static EmbeddedGetEventStoreWireup WithEmbeddedEventStore(this Wireup wireup)
        {
            return new EmbeddedGetEventStoreWireup(wireup);
        }

        public static TcpGetEventStoreWireup WithEventStoreOverTcp(this Wireup wireup)
        {
            return new TcpGetEventStoreWireup(wireup);
        }
    }
}