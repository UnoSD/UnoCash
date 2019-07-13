using System;
using System.Collections.Generic;

namespace UnoCash.Core
{
    public static class EnumeraleExtensions
    {
        public static void Iter<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
        }
    }
}
