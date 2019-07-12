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

        internal static Task<IEnumerable<TOut>> SelectManyAsync<TIn, TOut>(this Task<IEnumerable<TIn>> functor,
                                                                           Func<TIn, IEnumerable<TOut>> func) =>
            functor.Map(collection => collection.SelectMany(func));

        internal static Task<IEnumerable<T>> ConcatAsync<T>(this Task<IEnumerable<T>> task,
                                                            IEnumerable<T> source) =>
            task.Map(collection => collection.Concat(source));

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

        internal static async Task<IEnumerable<T>> UnfoldAsync2<TState, T>(
            this TState state,
            Func<TState, Task<(T item, TState state)>> generator,
            Func<TState, Task<bool>> stopCondition)
        {
            if (await stopCondition(state).ConfigureAwait(false))
                return Enumerable.Empty<T>();

            var (item, newState) = await generator(state).ConfigureAwait(false);

            return await UnfoldAsync2(newState, generator, stopCondition).Map(x => x.Cons(item))
                             .ConfigureAwait(false);
        }

        internal static Task<IEnumerable<T>> UnfoldAsync<TState, T>(
            this TState state,
            Func<TState, Task<(T item, TState state)>> generator,
            Func<TState, Task<bool>> stopCondition) =>
            stopCondition(state).Bind(stop => stop ? 
                                              Task.FromResult(Enumerable.Empty<T>()) :
                                              generator(state)
                                                  .Bind(t => UnfoldAsync(t.state, generator, stopCondition)
                                                                .Map(x => x.Cons(t.item))));

        internal static Task<IEnumerable<T>> UnfoldAsync<TState, T>(
            this TState state,
            Func<TState, Task<(T item, TState state)>> generator,
            Func<TState, bool> stopCondition) =>
            UnfoldAsync(state, generator, x => Task.FromResult(stopCondition(x)));
    }
}
