namespace NEventStore.Persistence.GetEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.Exceptions;
    using NEventStore.Logging;
    using NEventStore.Serialization;

    public class GetEventStorePersistenceEngine : IPersistStreams
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(GetEventStorePersistenceEngine));

        private readonly Func<IEventStoreConnection> _buildConnection;
        private readonly Action _dropAction;
        private readonly ISerialize _serializer;
        private IEventStoreConnection _connection;
        private int _initialized = -1;
        private bool _disposed;

        public GetEventStorePersistenceEngine(Func<IEventStoreConnection> buildConnection, Action dropAction,
            ISerialize serializer)
        {
            _buildConnection = buildConnection;
            _dropAction = dropAction;
            _serializer = serializer;
        }

        private void ThrowWhenDisposed()
        {
            if (!_disposed)
            {
                return;
            }

            Logger.Warn(Resources.AlreadyDisposed);
            throw new ObjectDisposedException(Resources.AlreadyDisposed);
        }

        private static Action<Task<WriteResult>> AppendToStreamContinuation(TaskCompletionSource<ICommit> source, CommitAttempt attempt)
        {
            return task =>
            {
                if (task.Exception != null)
                {
                    task.Exception.Handle(e =>
                    {
                        if (e is WrongExpectedVersionException)
                        {
                            source.SetException(new ConcurrencyException(e.Message, e));
                            return true;
                        }

                        return false;
                    });
                }
                else
                {
                    // this should mean a duplicate write that was ignored by GES
                    if (task.Result.LogPosition == Position.End)
                    {
                        source.SetException(new DuplicateCommitException());
                    }
                    else
                    {
                        source.SetResult(new Commit(
                            attempt.BucketId,
                            attempt.StreamId,
                            attempt.StreamRevision,
                            attempt.CommitId,
                            attempt.CommitSequence,
                            attempt.CommitStamp,
                            attempt.CommitSequence.ToString(),
                            attempt.Headers,
                            attempt.Events));
                    }
                }
            };
        }


        public void Dispose()
        {
            if (_disposed) return;
            Logger.Info(Resources.DisposingEngine);
            _connection.Close();
            _disposed = true;
        }

        public IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            ThrowWhenDisposed();

            Logger.Debug(Resources.GettingAllCommitsFromRevision, streamId, minRevision, maxRevision);
            
            var reader = new EventReader(_connection, _serializer);

            return reader.ReadStream(Format.EventStoreStreamId(bucketId, streamId), minRevision, maxRevision);
        }

        public Task<ICommit> Commit(CommitAttempt attempt)
        {
            ThrowWhenDisposed();

            Logger.Debug(Resources.AttemptingToCommit, attempt.CommitId, attempt.StreamId, attempt.CommitSequence);
            
            var source = new TaskCompletionSource<ICommit>();

            var getEventStoreCommit = new GetEventStoreCommitAttempt(attempt, _serializer);

            _connection.AppendToStreamAsync(getEventStoreCommit.StreamId, getEventStoreCommit.ExpectedVersion,
                getEventStoreCommit)
                .ContinueWith(AppendToStreamContinuation(source, attempt));

            return source.Task;
        }

        public ISnapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
        {
            throw new NotSupportedException();
        }

        public bool AddSnapshot(ISnapshot snapshot)
        {
            return false;
        }

        public IEnumerable<IStreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
        {
            yield break;
        }

        public bool IsDisposed { get { return _disposed; } }

        public void Initialize()
        {
            ThrowWhenDisposed();
            
            if (Interlocked.Increment(ref _initialized) > 0) return;

            Logger.Debug(Resources.InitializingPersistence);

            _connection = _buildConnection();
            _connection.ConnectAsync().Wait();
        }

        public IEnumerable<ICommit> GetFrom(string checkpointToken = null)
        {
            ThrowWhenDisposed();

            Logger.Debug(Resources.GettingAllCommitsFromCheckpoint, checkpointToken);

            GetEventStoreCheckpoint checkpoint = GetEventStoreCheckpoint.Parse(checkpointToken);

            var reader = new EventReader(_connection, _serializer);

            return reader.ReadAllFromCheckpoint(checkpoint);
        }

        public ICheckpoint GetCheckpoint(string checkpointToken = null)
        {
            return GetEventStoreCheckpoint.Parse(checkpointToken);
        }

        public void Purge()
        {
            ThrowWhenDisposed();

            Logger.Warn(Resources.PurgingStore);
        }

        public void Purge(string bucketId)
        {
            ThrowWhenDisposed();

            Logger.Warn(Resources.PurgingStore);
        }

        public void Drop()
        {
            ThrowWhenDisposed();

            _dropAction();
        }

        public void DeleteStream(string bucketId, string streamId)
        {
            ThrowWhenDisposed();

            Logger.Warn(Resources.DeletingStream, streamId, bucketId);

            throw new NotImplementedException();
        }

        private class EventReader
        {
            private readonly IEventStoreConnection _connection;
            private readonly ISerialize _serializer;

            public EventReader(IEventStoreConnection connection, ISerialize serializer)
            {
                _connection = connection;
                _serializer = serializer;
            }

            public IEnumerable<ICommit> ReadStream(string stream, int minRevision, int maxRevision)
            {
                Guard.AgainstNull(stream, "stream");
                if (maxRevision < minRevision) throw new ArgumentOutOfRangeException("maxRevision");

                const int batchSize = 512;

                int start = 0;

                StreamEventsSlice slice =
                    _connection.ReadStreamEventsForwardAsync(stream, start, batchSize, true).Result;

                do
                {
                    foreach (ResolvedEvent resolved in slice.Events)
                    {
                        if (resolved.OriginalEvent.EventType.StartsWith("$")) continue;
                        var dto = _serializer.Deserialize<GetEventStoreCommitAttempt.Dto>(resolved.OriginalEvent.Data);

                        var commit = new Commit(dto.BucketId, dto.StreamId, dto.StreamRevision, dto.CommitId,
                            dto.CommitSequence, dto.CommitStamp, resolved.OriginalEventNumber.ToString(),
                            dto.Headers, dto.Events);

                        if (dto.StreamRevision >= minRevision &&
                            (dto.StreamRevision - dto.Events.Count + 1) <= maxRevision)
                        {
                            yield return commit;
                        }
                    }
                    start += batchSize;
                } while (false == slice.IsEndOfStream);
            }

            public IEnumerable<ICommit> ReadAllFromCheckpoint(GetEventStoreCheckpoint checkpoint)
            {
                if (checkpoint == null) throw new ArgumentNullException("checkpoint");

                return ReadStream("$et-" + GetEventStoreCommitAttempt.EventType, checkpoint, Int32.MaxValue);
            }
        }
    }
}