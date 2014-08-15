using System;

namespace NEventStore.Persistence.GetEventStore
{
    internal static class Format
    {
        internal static string EventStoreStreamId(string bucketId, string streamId)
        {
            return String.Format("{0}.{1}", bucketId, streamId);
        }
    }
}