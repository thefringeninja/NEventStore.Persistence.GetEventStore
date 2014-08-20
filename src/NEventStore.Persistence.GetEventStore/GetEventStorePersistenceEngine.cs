using System.Reactive.Linq;

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

            Logger.Warn(Messages.AlreadyDisposed);
            throw new ObjectDisposedException(Messages.AlreadyDisposed);
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
                            Logger.Info(Messages.ConcurrentWriteDetected);

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
                        Logger.Info(Messages.DuplicateCommit);
                        source.SetException(new DuplicateCommitException());
                    }
                    else
                    {
                        Logger.Debug(Messages.CommitPersisted, attempt.CommitId);

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
            _connection.Close();
            Logger.Debug(Messages.ShuttingDownPersistence);
            _disposed = true;
        }

        public IObservable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            ThrowWhenDisposed();

            Logger.Debug(Messages.GettingAllCommitsBetween, streamId, minRevision, maxRevision);
            
            var reader = new EventReader(_connection, _serializer);

            return reader.ReadStream(Format.EventStoreStreamId(bucketId, streamId), minRevision, maxRevision);
        }

        public Task<ICommit> Commit(CommitAttempt attempt)
        {
            ThrowWhenDisposed();

            Logger.Debug(Messages.AttemptingToCommit, attempt.Events.Count, attempt.StreamId, attempt.CommitSequence, attempt.BucketId);
            
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

        public Task Initialize()
        {
            ThrowWhenDisposed();
            
            if (Interlocked.Increment(ref _initialized) > 0) return Task.FromResult(false);;

            Logger.Debug(Messages.InitializingStorage);

            _connection = _buildConnection();
            
            return _connection.ConnectAsync();
        }

        public IObservable<ICommit> GetFrom(string checkpointToken = null)
        {
            ThrowWhenDisposed();

            Logger.Debug(Messages.GettingAllCommitsFromCheckpoint, checkpointToken);

            GetEventStoreCheckpoint checkpoint = GetEventStoreCheckpoint.Parse(checkpointToken);

            var reader = new EventReader(_connection, _serializer);

            return reader.ReadAllFromCheckpoint(checkpoint);
        }

        public ICheckpoint GetCheckpoint(string checkpointToken = null)
        {
            return GetEventStoreCheckpoint.Parse(checkpointToken);
        }

        public Task Purge()
        {
            ThrowWhenDisposed();

            Logger.Warn(Messages.PurgingStorage);

            return Task.FromResult(false);
        }

        public Task Purge(string bucketId)
        {
            ThrowWhenDisposed();

            Logger.Warn(Messages.PurgingBucket, bucketId);

            return Task.FromResult(false);
        }

        public Task Drop()
        {
            ThrowWhenDisposed();

            _dropAction();

            return Task.FromResult(false);
        }

        public Task DeleteStream(string bucketId, string streamId)
        {
            ThrowWhenDisposed();

            Logger.Warn(Messages.DeletingStream, streamId, bucketId);

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

            public IObservable<ICommit> ReadStream(string stream, int minRevision, int maxRevision)
            {
                Guard.AgainstNull(stream, "stream");
                if (maxRevision < minRevision) throw new ArgumentOutOfRangeException("maxRevision");

                const int batchSize = 512;

                int start = 0;

                bool isEndOfStream;

                return Observable.Create<ICommit>(async observer =>
                {
                    do
                    {
                        StreamEventsSlice slice =
                            await _connection.ReadStreamEventsForwardAsync(stream, start, batchSize, true);

                        foreach (ResolvedEvent resolved in slice.Events)
                        {
                            if (resolved.OriginalEvent.EventType.StartsWith("$")) continue;
                            var dto =
                                _serializer.Deserialize<GetEventStoreCommitAttempt.Dto>(resolved.OriginalEvent.Data);

                            var commit = new Commit(dto.BucketId, dto.StreamId, dto.StreamRevision, dto.CommitId,
                                dto.CommitSequence, dto.CommitStamp, resolved.OriginalEventNumber.ToString(),
                                dto.Headers, dto.Events);

                            if (dto.StreamRevision >= minRevision &&
                                (dto.StreamRevision - dto.Events.Count + 1) <= maxRevision)
                            {
                                observer.OnNext(commit);
                            }
                        }
                        start += batchSize;
                        isEndOfStream = slice.IsEndOfStream;
                    } while (false == isEndOfStream);

                    observer.OnCompleted();
                });

            }

            public IObservable<ICommit> ReadAllFromCheckpoint(GetEventStoreCheckpoint checkpoint)
            {
                if (checkpoint == null) throw new ArgumentNullException("checkpoint");

                return ReadStream("$et-" + GetEventStoreCommitAttempt.EventType, checkpoint, Int32.MaxValue);
            }
        }
    }
}