using System;
using System.Threading.Tasks;

namespace UnoCash.Core
{
    static class TaskExtensions
    {
        internal static async Task<TOut> Map<TIn, TOut>(this Task<TIn> functor, Func<TIn, TOut> func)
        {
            var result = await functor.ConfigureAwait(false);

            return func(result);
        }
    }
}
