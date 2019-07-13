using System;
using System.Collections.Generic;
using System.Linq;

namespace UnoCash.Core
{
    public static class EnumeraleExtensions
    {
        public static void Iter<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
        }

        public static TOut Match<TKey, TValue, TOut>(this IEnumerable<KeyValuePair<TKey, TValue>> kvps,
                                                     TKey match,
                                                     Func<TValue, TOut> found,
                                                     Func<TOut> notFound) =>
            kvps.FirstOrDefault(kvp => kvp.Key.Equals(match))
                .Match((kvp => kvp.Equals(default(KeyValuePair<TKey, TValue>)), r => notFound()),
                       (kvp => true, kvp => found(kvp.Value)));
    }
}
