using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace NEventStore.Persistence.GetEventStore
{
    public partial class GetEventStorePersistenceEngine
    {
        Task IEventStoreConnection.ConnectAsync()
        {
            return _connection.ConnectAsync();
        }

        void IEventStoreConnection.Close()
        {
            _connection.Close();
        }

        Task<DeleteResult> IEventStoreConnection.DeleteStreamAsync(string stream, int expectedVersion,
            UserCredentials userCredentials)
        {
            return _connection.DeleteStreamAsync(stream, expectedVersion, userCredentials);
        }

        Task<DeleteResult> IEventStoreConnection.DeleteStreamAsync(string stream, int expectedVersion, bool hardDelete,
            UserCredentials userCredentials)
        {
            return _connection.DeleteStreamAsync(stream, expectedVersion, hardDelete, userCredentials);
        }

        Task<WriteResult> IEventStoreConnection.AppendToStreamAsync(string stream, int expectedVersion,
            params EventData[] events)
        {
            return _connection.AppendToStreamAsync(stream, expectedVersion, events);
        }

        Task<WriteResult> IEventStoreConnection.AppendToStreamAsync(string stream, int expectedVersion,
            UserCredentials userCredentials, params EventData[] events)
        {
            return _connection.AppendToStreamAsync(stream, expectedVersion, userCredentials, events);
        }

        Task<WriteResult> IEventStoreConnection.AppendToStreamAsync(string stream, int expectedVersion,
            IEnumerable<EventData> events, UserCredentials userCredentials)
        {
            return _connection.AppendToStreamAsync(stream, expectedVersion, events, userCredentials);
        }

        Task<EventStoreTransaction> IEventStoreConnection.StartTransactionAsync(string stream, int expectedVersion,
            UserCredentials userCredentials)
        {
            return _connection.StartTransactionAsync(stream, expectedVersion, userCredentials);
        }

        EventStoreTransaction IEventStoreConnection.ContinueTransaction(long transactionId,
            UserCredentials userCredentials)
        {
            return _connection.ContinueTransaction(transactionId, userCredentials);
        }

        Task<EventReadResult> IEventStoreConnection.ReadEventAsync(string stream, int eventNumber, bool resolveLinkTos,
            UserCredentials userCredentials)
        {
            return _connection.ReadEventAsync(stream, eventNumber, resolveLinkTos, userCredentials);
        }

        Task<StreamEventsSlice> IEventStoreConnection.ReadStreamEventsForwardAsync(string stream, int start, int count,
            bool resolveLinkTos,
            UserCredentials userCredentials)
        {
            return _connection.ReadStreamEventsForwardAsync(stream, start, count, resolveLinkTos, userCredentials);
        }

        Task<StreamEventsSlice> IEventStoreConnection.ReadStreamEventsBackwardAsync(string stream, int start, int count,
            bool resolveLinkTos,
            UserCredentials userCredentials)
        {
            return _connection.ReadStreamEventsBackwardAsync(stream, start, count, resolveLinkTos, userCredentials);
        }

        Task<AllEventsSlice> IEventStoreConnection.ReadAllEventsForwardAsync(Position position, int maxCount,
            bool resolveLinkTos,
            UserCredentials userCredentials)
        {
            return _connection.ReadAllEventsForwardAsync(position, maxCount, resolveLinkTos, userCredentials);
        }

        Task<AllEventsSlice> IEventStoreConnection.ReadAllEventsBackwardAsync(Position position, int maxCount,
            bool resolveLinkTos,
            UserCredentials userCredentials)
        {
            return _connection.ReadAllEventsBackwardAsync(position, maxCount, resolveLinkTos, userCredentials);
        }

        Task<EventStoreSubscription> IEventStoreConnection.SubscribeToStreamAsync(string stream, bool resolveLinkTos,
            Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
            Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
            UserCredentials userCredentials)
        {
            return _connection.SubscribeToStreamAsync(stream, resolveLinkTos, eventAppeared, subscriptionDropped,
                userCredentials);
        }

        EventStoreStreamCatchUpSubscription IEventStoreConnection.SubscribeToStreamFrom(string stream,
            int? lastCheckpoint, bool resolveLinkTos,
            Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
            Action<EventStoreCatchUpSubscription> liveProcessingStarted,
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
            UserCredentials userCredentials, int readBatchSize)
        {
            if (userCredentials == null) throw new ArgumentNullException("userCredentials");
            return _connection.SubscribeToStreamFrom(stream, lastCheckpoint, resolveLinkTos, eventAppeared,
                liveProcessingStarted, subscriptionDropped, userCredentials, readBatchSize);
        }

        Task<EventStoreSubscription> IEventStoreConnection.SubscribeToAllAsync(bool resolveLinkTos,
            Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
            Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
            UserCredentials userCredentials)
        {
            return _connection.SubscribeToAllAsync(resolveLinkTos, eventAppeared, subscriptionDropped, userCredentials);
        }

        EventStoreAllCatchUpSubscription IEventStoreConnection.SubscribeToAllFrom(Position? lastCheckpoint,
            bool resolveLinkTos, Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
            Action<EventStoreCatchUpSubscription> liveProcessingStarted,
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped,
            UserCredentials userCredentials,
            int readBatchSize)
        {
            return _connection.SubscribeToAllFrom(lastCheckpoint, resolveLinkTos, eventAppeared, liveProcessingStarted,
                subscriptionDropped, userCredentials, readBatchSize);
        }

        Task<WriteResult> IEventStoreConnection.SetStreamMetadataAsync(string stream, int expectedMetastreamVersion,
            StreamMetadata metadata,
            UserCredentials userCredentials)
        {
            return _connection.SetStreamMetadataAsync(stream, expectedMetastreamVersion, metadata, userCredentials);
        }

        Task<WriteResult> IEventStoreConnection.SetStreamMetadataAsync(string stream, int expectedMetastreamVersion,
            byte[] metadata,
            UserCredentials userCredentials)
        {
            return _connection.SetStreamMetadataAsync(stream, expectedMetastreamVersion, metadata, userCredentials);
        }

        Task<StreamMetadataResult> IEventStoreConnection.GetStreamMetadataAsync(string stream,
            UserCredentials userCredentials)
        {
            return _connection.GetStreamMetadataAsync(stream, userCredentials);
        }

        Task<RawStreamMetadataResult> IEventStoreConnection.GetStreamMetadataAsRawBytesAsync(string stream,
            UserCredentials userCredentials)
        {
            return _connection.GetStreamMetadataAsRawBytesAsync(stream, userCredentials);
        }

        Task IEventStoreConnection.SetSystemSettingsAsync(SystemSettings settings, UserCredentials userCredentials)
        {
            return _connection.SetSystemSettingsAsync(settings, userCredentials);
        }

        string IEventStoreConnection.ConnectionName
        {
            get { return _connection.ConnectionName; }
        }

        event EventHandler<ClientConnectionEventArgs> IEventStoreConnection.Connected
        {
            add { _connection.Connected += value; }
            remove { _connection.Connected -= value; }
        }

        public event EventHandler<ClientConnectionEventArgs> Disconnected
        {
            add { _connection.Disconnected += value; }
            remove { _connection.Disconnected -= value; }
        }

        public event EventHandler<ClientReconnectingEventArgs> Reconnecting
        {
            add { _connection.Reconnecting += value; }
            remove { _connection.Reconnecting -= value; }
        }

        public event EventHandler<ClientClosedEventArgs> Closed
        {
            add { _connection.Closed += value; }
            remove { _connection.Closed -= value; }
        }

        public event EventHandler<ClientErrorEventArgs> ErrorOccurred
        {
            add { _connection.ErrorOccurred += value; }
            remove { _connection.ErrorOccurred -= value; }
        }

        public event EventHandler<ClientAuthenticationFailedEventArgs> AuthenticationFailed
        {
            add { _connection.AuthenticationFailed += value; }
            remove { _connection.AuthenticationFailed -= value; }
        }
    }
}