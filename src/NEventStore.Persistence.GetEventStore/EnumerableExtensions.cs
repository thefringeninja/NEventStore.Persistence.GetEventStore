using System;
using System.Collections.Generic;

namespace NEventStore.Persistence.GetEventStore
{
    internal static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            Guard.AgainstNull(source, "source");
            Guard.AgainstNull(action, "action");

            foreach (var item in source) action(item);
        }
    }
}