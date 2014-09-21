namespace NEventStore.MirroringSample
{
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;

    [DataContract]
    public class TweetMessage
    {
        [DataMember] public readonly DateTime CreatedAt;
        [DataMember] public readonly string Text;
        [DataMember] public readonly long Id;

        public TweetMessage(dynamic tweet)
        {
            const string format = "ddd MMM dd HH:mm:ss zzz yyyy";
            Text = tweet.text;
            Id = tweet.id;
            CreatedAt = DateTime.ParseExact((string)tweet.created_at, format, CultureInfo.InvariantCulture);
        }

        private TweetMessage() { }

        public override string ToString()
        {
            return Text;
        }
    }
}