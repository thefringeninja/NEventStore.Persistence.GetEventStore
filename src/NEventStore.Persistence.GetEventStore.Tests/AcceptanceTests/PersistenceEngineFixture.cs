using NEventStore.Persistence.GetEventStore.Tests;

// ReSharper disable CheckNamespace

namespace NEventStore.Persistence.AcceptanceTests
// ReSharper restore CheckNamespace
{
    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            _createPersistence = _ => new TestGetEventStorePersistenceFactory().Build();
        }
    }
}