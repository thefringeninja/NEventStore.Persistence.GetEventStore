using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NEventStore.MirroringSample.Tweets
{
    class Twitter : IEnumerable<JObject>
    {
        private static IEnumerable<JObject> GetAll()
        {
            return Enumerable.Range(1, 3).SelectMany(GetPage);
        }
        private static IEnumerable<JObject> GetPage(int pageNum)
        {
            using (var stream = typeof (Twitter).Assembly.GetManifestResourceStream(typeof (Twitter), pageNum + ".json"))
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                var page = JToken.ReadFrom(reader) as JObject;
                var statuses = (JArray) page["statuses"];

                foreach (var status in statuses)
                {
                    yield return (JObject)status;
                }
            }
        }
        public IEnumerator<JObject> GetEnumerator()
        {
            return GetAll().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
