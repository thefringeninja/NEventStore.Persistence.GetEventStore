namespace NEventStore.MirroringSample
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using NEventStore.Client;
    using NEventStore.MirroringSample.Tweets;
    using NEventStore.Persistence.GetEventStore;
    using NEventStore.Persistence.Sql.SqlDialects;
    using Newtonsoft.Json;

    internal class Mirroror : IDisposable
    {
        private readonly EventStoreClient _client;
        private readonly IStoreEvents _mirror;
        private IDisposable _subscription;

        public Mirroror(IStoreEvents master, IStoreEvents mirror)
        {
            _mirror = mirror;
            _client = new EventStoreClient(master.Advanced, 50);
        }

        public void Dispose()
        {
            _subscription.Dispose();
            _client.Dispose();
        }

        public void Start()
        {
            _subscription = _client.Subscribe(null, commit =>
            {
                var attempt = new CommitAttempt(commit.BucketId, commit.StreamId, commit.StreamRevision, commit.CommitId,
                    commit.CommitSequence, commit.CommitStamp, commit.Headers, commit.Events)
                {
                    Headers =
                    {
                        {"CheckpointToken", commit.CheckpointToken}
                    }
                };

                return _mirror.Advanced.Commit(attempt);
            });
        }

        public void Stop()
        {
            if (_subscription != null)
            {
                _subscription.Dispose();
            }
        }
    }

    internal class MainProgram : IDisposable
    {
        private readonly IStoreEvents _master;
        private readonly IStoreEvents _mirror;
        private readonly EventStoreClient _mirrorClient;
        private readonly Mirroror _replicationPipeine;

        public MainProgram(IStoreEvents master, IStoreEvents mirror)
        {
            _master = master;
            _mirror = mirror;
            _replicationPipeine = new Mirroror(_master, _mirror);
            _mirrorClient = new EventStoreClient(_mirror.Advanced, 50);
        }

        public IObservable<ICommit> CommitsFromMirror
        {
            get { return _mirrorClient.FromCheckpoint(); }
        }

        public void Dispose()
        {
            _replicationPipeine.Dispose();
            _mirrorClient.Dispose();
        }

        private static void Main(string[] args)
        {
            IStoreEvents master = SqliteInMemory().Build();
            IStoreEvents mirror = EventStoreInMemory().Build();
            using (var program = new MainProgram(master, mirror))
            {
                using (program.CommitsFromMirror.SelectMany(
                    commit => from eventMessage in commit.Events
                        select eventMessage.Body)
                    .OfType<TweetMessage>()
                    .Select(message => message.Text)
                    .Subscribe(Console.WriteLine))
                {
                    Task.Run(() => program.Run());

                    Console.ReadLine();
                }
            }
        }

        public async Task Run()
        {
            _replicationPipeine.Start();
            await PopulateMaster();
            Console.WriteLine("Done populating master");
        }

        private Task PopulateMaster()
        {
            IEventStream stream = _master.CreateStream("tweets");

            IEnumerable<Task> tasks = ReadTweets().Select(tweet =>
            {
                var message = new TweetMessage(tweet);
                stream.Add(new EventMessage {Body = message});
                return stream.CommitChanges(Guid.NewGuid());
            });

            return Task.WhenAll(tasks);
        }

        private IEnumerable<dynamic> ReadTweets()
        {
            return new Twitter();
        }

        private static Wireup SqliteInMemory()
        {
            const string databasePath = "NEventStore.db";

            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }

            return Wireup.Init()
                .UsingSqlPersistence("master", "System.Data.SQLite",
                    string.Format("Data Source={0};Version=3;New=True;", databasePath))
                .WithDialect(new SqliteDialect())
                .InitializeStorageEngine()
                // the default jsonserializer doesn't do what I want
                .UsingCustomSerialization(
                    new GetEventStoreJsonSerializer(
                        c => c.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor))
                ;
        }

        private static Wireup EventStoreInMemory()
        {
            return Wireup.Init()
                .UsingEmbeddedEventStore()
                .InMemory()
                .CustomizeJsonSerailizationWith(
                    c => c.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor)
                .InitializeStorageEngine()
                ;
        }
    }
}