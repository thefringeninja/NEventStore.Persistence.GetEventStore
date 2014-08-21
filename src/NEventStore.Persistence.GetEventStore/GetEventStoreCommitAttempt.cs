namespace NEventStore.Persistence.GetEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using EventStore.ClientAPI;
    using NEventStore.Serialization;

    internal class GetEventStoreCommitAttempt
    {
        internal const string EventType = "NEventStoreCommit";

        private readonly CommitAttempt _attempt;
        private readonly int _expectedVersion;
        private readonly ISerialize _serializer;
        private readonly string _streamId;
        private byte[] _eventData;

        public GetEventStoreCommitAttempt(CommitAttempt attempt, ISerialize serializer)
        {
            _attempt = attempt;
            _serializer = serializer;
            _streamId = Format.EventStoreStreamId(attempt.BucketId, attempt.StreamId);
            _expectedVersion = attempt.CommitSequence == 1
                ? EventStore.ClientAPI.ExpectedVersion.NoStream
                : attempt.CommitSequence - 2;
        }

        public Guid EventId
        {
            get { return _attempt.CommitId; }
        }

        public string StreamId
        {
            get { return _streamId; }
        }

        public int ExpectedVersion
        {
            get { return _expectedVersion; }
        }

        public byte[] EventData
        {
            get { return _eventData ?? (_eventData = Serialize()); }
        }

        public string Type
        {
            get { return EventType; }
        }

        private byte[] Serialize()
        {
            Dto dto = Dto.Create(_attempt);
            return _serializer.Serialize(dto);
        }

        public static implicit operator EventData(GetEventStoreCommitAttempt attempt)
        {
            return new EventData(attempt.EventId, attempt.Type, true, attempt.EventData, null);
        }

        [Serializable]
        [DataContract]
        public class Dto
        {
            [DataMember(Order = 1)] public readonly string BucketId;
            [DataMember(Order = 4)] public readonly Guid CommitId;
            [DataMember(Order = 5)] public readonly int CommitSequence;
            [DataMember(Order = 6)] public readonly DateTime CommitStamp;
            [DataMember(Order = 8)] public readonly ICollection<EventMessage> Events;
            [DataMember(Order = 7)] public readonly IDictionary<string, object> Headers;
            [DataMember(Order = 2)] public readonly string StreamId;
            [DataMember(Order = 3)] public readonly int StreamRevision;

            private Dto()
            {
            }

            private Dto(string bucketId, string streamId, int streamRevision, Guid commitId, int commitSequence,
                DateTime commitStamp, IDictionary<string, object> headers, ICollection<EventMessage> events)
            {
                BucketId = bucketId;
                StreamId = streamId;
                StreamRevision = streamRevision;
                CommitId = commitId;
                CommitSequence = commitSequence;
                CommitStamp = commitStamp;
                Headers = headers;
                Events = events;
            }

            public static Dto Create(CommitAttempt attempt)
            {
                return new Dto(attempt.BucketId, attempt.StreamId, attempt.StreamRevision, attempt.CommitId,
                    attempt.CommitSequence, attempt.CommitStamp, new Dictionary<string, object>(attempt.Headers),
                    new Collection<EventMessage>(attempt.Events.ToList()));
            }
        }
    }
}