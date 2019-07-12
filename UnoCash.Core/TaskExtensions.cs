using System;
using System.Threading.Tasks;

namespace UnoCash.Core
{
    static class TaskExtensions
    {
        internal static async Task<TOut> Map<TIn, TOut>(this Task<TIn> functor, Func<TIn, TOut> func)
        {
            // Use ContinueWith (with correct arguments)
            var result = await functor.ConfigureAwait(false);

            return func(result);
        }

        internal static Task<TOut> Bind<TIn, TOut>(this Task<TIn> monad, Func<TIn, Task<TOut>> func) =>
            monad.Map(func)
                 .Unwrap();
    }
}
