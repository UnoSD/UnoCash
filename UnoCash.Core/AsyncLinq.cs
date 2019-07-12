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
                                                         Func<TState, (T item, TState state)> generator,
                                                         Func<TState, bool> stopCondition) =>
            stopCondition(state) ?
            Enumerable.Empty<T>() :
            generator(state).Unfold(generator, stopCondition);

        internal static IEnumerable<T> Unfold<TState, T>(this (T item, TState state) generated,
                                                         Func<TState, (T, TState)> generator,
                                                         Func<TState, bool> stopCondition) =>
            stopCondition(generated.state) ?
            new [] { generated.item } :
            Unfold(generated.state, generator, stopCondition).Cons(generated.item);

        internal static IEnumerable<T> Unfold<TState, T>(this TState state,
                                                         Func<TState, (T, TState)?> generator) =>
            generator(state).Unfold(generator);

        static IEnumerable<T> Unfold<TState, T>(this (T item, TState state)? nullable,
                                                Func<TState, (T, TState)?> generator) =>
            nullable.HasValue ?
            Unfold(nullable.Value.state, generator).Cons(nullable.Value.item) :
            Enumerable.Empty<T>();

        internal static IEnumerable<T> Cons<T>(this IEnumerable<T> source, T item) =>
            new[] { item }.Concat(source);
    }
}
