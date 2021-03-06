﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace UnoCash.Core
{
    public static class TaskExtensions
    {
        [DebuggerStepThrough]
        public static async Task<T> TTap<T>(this Task<T> task, Action<T> action)
        {
            var result = await task.ConfigureAwait(false);

            action(result);

            return result;
        }

        [DebuggerStepThrough]
        public static async Task<TOut> Map<TIn, TOut>(this Task<TIn> functor, Func<TIn, TOut> func)
        {
            // Use ContinueWith (with correct arguments)
            var result = await functor.ConfigureAwait(false);

            return func(result);
        }

        [DebuggerStepThrough]
        public static Task<TOut> Bind<TIn, TOut>(this Task<TIn> monad, Func<TIn, Task<TOut>> func) =>
            monad.Map(func)
                 .Unwrap();

        [DebuggerStepThrough]
        public static Task<T> MatchAsync<T>(this Task<bool> task,
                                            Func<T> onTrue,
                                            Func<T> onFalse) =>
            task.Map(b => b ? onTrue() : onFalse());
    }
}
