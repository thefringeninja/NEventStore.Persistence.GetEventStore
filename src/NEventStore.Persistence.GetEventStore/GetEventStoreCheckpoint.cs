namespace NEventStore.Persistence.GetEventStore
{
    using System;

    internal class GetEventStoreCheckpoint : ICheckpoint
    {
        private readonly int _eventNumber;

        public GetEventStoreCheckpoint(int eventNumber)
        {
            Guard.Against<ArgumentOutOfRangeException>(_eventNumber < 0, "eventNumber");
            _eventNumber = eventNumber;
        }

        public int CompareTo(ICheckpoint other)
        {
            if (other == null) return 1;
            
            var otherCheckpoint = other as GetEventStoreCheckpoint;

            Guard.Against<ArgumentException>(
                otherCheckpoint == null,
                string.Format("Expected an ICheckpoint of type {0}, got {1} instead.",
                    typeof (GetEventStoreCheckpoint), other.GetType()), "other");

            return _eventNumber.CompareTo(otherCheckpoint._eventNumber);
        }

        public string Value
        {
            get { return _eventNumber.ToString(); }
        }

        public static implicit operator GetEventStoreCheckpoint(int? eventNumber)
        {
            return eventNumber.HasValue ? new GetEventStoreCheckpoint(eventNumber.Value) : null;
        }

        public static implicit operator int?(GetEventStoreCheckpoint checkpoint)
        {
            return checkpoint == null ? default(int?) : checkpoint._eventNumber;
        }

        public static GetEventStoreCheckpoint Parse(string checkpointToken)
        {
            int eventNumber;

            if (string.IsNullOrWhiteSpace(checkpointToken))
                return null;

            if (false == Int32.TryParse(checkpointToken, out eventNumber))
            {
                throw new ArgumentException(string.Format("Expected an integer, got '{0}' instead", checkpointToken),
                    "checkpointToken");
            }

            return eventNumber;
        }
    }
}