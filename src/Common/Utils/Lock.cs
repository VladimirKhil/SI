using System.Diagnostics;

namespace Utils;

/// <summary>
/// Provides an await-friendly locking mechanism.
/// </summary>
public sealed class Lock : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

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
            try
            {
                _semaphore.Release();
            }
            catch (ObjectDisposedException)
            {

            }
        }
    }

    public void WithLock(Action action, int millisecondsTimeout)
    {
        _semaphore.Wait(millisecondsTimeout);

        try
        {
            action();
        }
        finally
        {
            try
            {
                _semaphore.Release();
            }
            catch (ObjectDisposedException)
            {

            }
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
            try
            {
                _semaphore.Release();
            }
            catch (ObjectDisposedException)
            {

            }
        }
    }

    public async ValueTask WithLockAsync(Action action, int millisecondsTimeout)
    {
        await _semaphore.WaitAsync(millisecondsTimeout);

        try
        {
            action();
        }
        finally
        {
            try
            {
                _semaphore.Release();
            }
            catch (ObjectDisposedException)
            {

            }
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

    public T WithLock<T>(Func<T> func, int millisecondsTimeout)
    {
        _semaphore.Wait(millisecondsTimeout);

        try
        {
            return func();
        }
        finally
        {
            try
            {
                _semaphore.Release();
            }
            catch (ObjectDisposedException)
            {

            }
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
            try
            {
                _semaphore.Release();
            }
            catch (ObjectDisposedException)
            {

            }
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
            try
            {
                _semaphore.Release();
            }
            catch (ObjectDisposedException)
            {

            }
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
            try
            {
                _semaphore.Release();
            }
            catch (ObjectDisposedException)
            {

            }
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
                try
                {
                    _semaphore.Release();
                }
                catch (ObjectDisposedException)
                {

                }
            }
        }

        return lockAquired;
    }

    public async ValueTask<(T?, bool)> TryLockAsync<T>(
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
                try
                {
                    _semaphore.Release();
                }
                catch (ObjectDisposedException)
                {

                }
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
                try
                {
                    _semaphore.Release();
                }
                catch (ObjectDisposedException)
                {

                }
            }
        }

        return lockAquired;
    }

    public void Dispose() => _semaphore.Dispose();
}
