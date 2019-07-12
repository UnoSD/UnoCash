using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnoCash.Core
{
    static class AsyncLinq
    {
        internal static Task<IEnumerable<T>> WhereAsync<T>(this Task<IEnumerable<T>> task,
                                                           Func<T, bool> predicate) => 
            task.Map(collection => collection.Where(predicate));

        internal static Task<IEnumerable<TOut>> SelectAsync<TIn, TOut>(this Task<IEnumerable<TIn>> functor,
                                                                       Func<TIn, TOut> func) =>
            functor.Map(collection => collection.Select(func));
    }
}
