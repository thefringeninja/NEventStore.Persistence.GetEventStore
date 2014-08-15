using System;

namespace NEventStore.Persistence.GetEventStore
{
    internal class GetEventStoreCheckpoint : ICheckpoint
    {
        private readonly int _eventNumber;

        public GetEventStoreCheckpoint(int eventNumber)
        {
            if (_eventNumber < 0) throw new ArgumentOutOfRangeException("eventNumber");
            _eventNumber = eventNumber;
        }

        public int CompareTo(ICheckpoint other)
        {
            var otherCheckpoint = other as GetEventStoreCheckpoint;
            if (otherCheckpoint == null)
                throw new ArgumentException(
                    string.Format("Expected an ICheckpoint of type {0}, got {1} instead.",
                        typeof (GetEventStoreCheckpoint), other.GetType()), "other");

            return _eventNumber.CompareTo(otherCheckpoint._eventNumber);
        }

        public string Value
        {
            get { return _eventNumber.ToString(); }
        }

        public static implicit operator GetEventStoreCheckpoint(int eventNumber)
        {
            return new GetEventStoreCheckpoint(eventNumber);
        }

        public static implicit operator int(GetEventStoreCheckpoint checkpoint)
        {
            return checkpoint._eventNumber;
        }

        public static GetEventStoreCheckpoint Parse(string checkpointToken)
        {
            int eventNumber;

            if (string.IsNullOrWhiteSpace(checkpointToken))
                return 0;

            if (false == Int32.TryParse(checkpointToken, out eventNumber))
            {
                throw new ArgumentException(string.Format("Expected an integer, got '{0}' instead", checkpointToken),
                    "checkpointToken");
            }

            return eventNumber;
        }
    }
}