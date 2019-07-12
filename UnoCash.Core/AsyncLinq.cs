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

        internal static IEnumerable<T> Unfold<TState, T>(this TState state,
                                                         Func<TState, (T, TState)> generator, 
                                                         Func<TState, bool> stopCondition)
        {
            if (stopCondition(state))
                yield break;
            
            var (value, newState) = generator(state);

            yield return value;

            foreach (var nextValue in Unfold(newState, generator, stopCondition))
                yield return nextValue;
        }

        internal static IEnumerable<T> Unfold<TState, T>(this TState state,
                                                         Func<TState, (T, TState)?> generator)
        {
            var nullable = generator(state);

            if (!nullable.HasValue)
                yield break;
            
            var (value, newState) = nullable.Value;

            yield return value;

            foreach (var nextValue in Unfold(newState, generator))
                yield return nextValue;
        }
    }
}
