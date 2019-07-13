using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnoCash.Core
{
    public static class TypeExtensions
    {
        public static TOut TMap<TIn, TOut>(this TIn value, Func<TIn, TOut> func) =>
            func(value);

        public static Task<T> ToTask<T>(this T value) =>
            Task.FromResult(value);

        internal static T Coalesce<T>(this T toCheck, T replacement, T comparison = default) =>
            toCheck.Equals(comparison) ? replacement : toCheck;

        public static T Tap<T>(this T item, Action<T> func)
        {
            func(item);

            return item;
        }

        public static TOut Match<TOut>(this bool value, Func<TOut> trueCase, Func<TOut> falseCase) =>
            value ?
            trueCase() :
            falseCase();

        public static TOut Match<T, TOut>(this IReadOnlyCollection<T> source,
                                          Func<TOut> empty,
                                          Func<T, TOut> one,
                                          Func<IReadOnlyCollection<T>, TOut> many) =>
            source.Count == 0 ? empty() :
            source.Count == 1 ? one(source.Single()) :
            many(source);

        public static TOut Match<T, TOut>(this T item,
                                          params (Func<T, bool> match, Func<T, TOut> func)[] matches) =>
            matches.First(t => t.match(item)).func(item);
    }
}