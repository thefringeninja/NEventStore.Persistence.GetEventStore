namespace NEventStore.MirroringSample
{
    using System;
    using System.Collections.Generic;

    internal static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source) action(item);
        }
    }
}