using System;
using System.Threading.Tasks;

namespace UnoCash.Shared
{
    public static class DisposableExtensions
    {
        public static TOut Using<T, TOut>(this T disposable, Func<T, TOut> func) where T : IDisposable
        {
            using (disposable)
                return func(disposable);
        }

        public static async Task<TOut> UsingAsync<T, TOut>(this T disposable, Func<T, Task<TOut>> func)
            where T : IDisposable
        {
            using (disposable)
                return await func(disposable).ConfigureAwait(false);
        }
    }
}
