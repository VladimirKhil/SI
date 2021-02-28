using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SICore.Network
{
    public sealed class Lock: IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly string _name;

        public Lock(string name)
        {
            _name = name;
        }

        public void WithLock(Action action, CancellationToken cancellationToken = default)
        {
            _semaphore.Wait(cancellationToken);
            try
            {
                action();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async ValueTask WithLockAsync(Action action, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                action();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public T WithLock<T>(Func<T> func, CancellationToken cancellationToken = default)
        {
            _semaphore.Wait(cancellationToken);
            try
            {
                return func();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async ValueTask<T> WithLockAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                return func();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async void WithLock(Func<Task> asyncAction, CancellationToken cancellationToken = default)
        {
            _semaphore.Wait(cancellationToken);
            try
            {
                await asyncAction();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async ValueTask WithLockAsync(Func<Task> asyncAction, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await asyncAction();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async ValueTask<T> WithLockAsync<T>(Func<Task<T>> asyncFunc, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                return await asyncFunc();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async ValueTask<bool> TryLockAsync(
            Action action,
            int millisecondsTimeout,
            bool force = false,
            CancellationToken cancellationToken = default)
        {
            var lockAquired = await _semaphore.WaitAsync(millisecondsTimeout, cancellationToken);

            if (!lockAquired)
            {
                Trace.TraceError($"Cannot aquire lock {_name} in {millisecondsTimeout}!");
            }

            try
            {
                if (lockAquired || force)
                {
                    action();
                }
            }
            finally
            {
                if (lockAquired)
                {
                    _semaphore.Release();
                }
            }

            return lockAquired;
        }

        public async ValueTask<(T, bool)> TryLockAsync<T>(
            Func<T> func,
            int millisecondsTimeout,
            bool force = false,
            CancellationToken cancellationToken = default)
        {
            var lockAquired = await _semaphore.WaitAsync(millisecondsTimeout, cancellationToken);

            if (!lockAquired)
            {
                Trace.TraceError($"Cannot aquire lock {_name} in {millisecondsTimeout}!");
            }

            try
            {
                if (lockAquired || force)
                {
                    return (func(), lockAquired);
                }
            }
            finally
            {
                if (lockAquired)
                {
                    _semaphore.Release();
                }
            }

            return (default, lockAquired);
        }

        public async ValueTask<bool> TryLockAsync(
            Func<Task> asyncAction,
            int millisecondsTimeout,
            bool force = false,
            CancellationToken cancellationToken = default)
        {
            var lockAquired = false;
            try
            {
                lockAquired = await _semaphore.WaitAsync(millisecondsTimeout, cancellationToken);
            }
            catch (ObjectDisposedException)
            {

            }

            if (!lockAquired)
            {
                Trace.TraceError($"Cannot aquire lock {_name} in {millisecondsTimeout}!");
            }

            try
            {
                if (lockAquired || force)
                {
                    await asyncAction();
                }
            }
            finally
            {
                if (lockAquired)
                {
                    _semaphore.Release();
                }
            }

            return lockAquired;
        }

        public void Dispose() => _semaphore.Dispose();
    }
}
