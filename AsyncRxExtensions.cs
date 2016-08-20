using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenHeroesUploader
{
    public static class AsyncRxExtensions
    {
        public static IObservable<T> DoAsync<T>(this IObservable<T> src, Func<T, Task> action) =>
            src.SelectMany(async t =>
            {
                await action(t);
                return t;
            });

        public static IObservable<U> SelectAsync<T, U>(this IObservable<T> src, Func<T, Task<U>> transformation) => src.SelectMany(transformation);
    }
}
