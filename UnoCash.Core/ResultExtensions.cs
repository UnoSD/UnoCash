using System;

namespace UnoCash.Core
{
    public static class ResultExtensions
    {
        public static IResult<T> RTap<T>(this IResult<T> result, Action<T> action) =>
            result.Map(r =>
            {
                action(r);
                return r;
            });

        public static IResult<T> Success<T>(this T value) =>
            new SuccessResult<T>(value);

        public static IResult<T> Failure<T>(this string value) =>
            new FailureResult<T>(value);

        public static IResult<TOut> Bind<TIn, TOut>(this IResult<TIn> item, Func<TIn, IResult<TOut>> func) =>
            item.Match(func,
                       r => r.Failure<TOut>());

        public static TOut Match<TIn, TOut>(this IResult<TIn> value,
                                            Func<TIn, TOut> onSuccess,
                                            Func<string, TOut> onFailure) =>
            value is SuccessResult<TIn> success ? onSuccess(success.Success) :
            value is FailureResult<TIn> failure ? onFailure(failure.Failure) :
            //value is null                        ? throw new NullReferenceException() : 
            throw new Exception("Bad implementation, do not implement IResult into " +
                                "classes other than SuccessResult and FailureResult");

        public static IResult<TOut> Map<TIn, TOut>(this IResult<TIn> value, Func<TIn, TOut> func) =>
            value.Match(s => func(s).Success(),
                        e => e.Failure<TOut>());
    }
}