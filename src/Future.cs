﻿namespace Futures;

using Futures.Awaiters;
using Futures.Internals;


public class Future : Future<object>, ICompletableFuture
{
}


public class Future<T> : ICompletableFuture<T>
{
    private FutureState _state = FutureState.Pending;
    private T? _result;
    private Exception? _exception;
    private readonly Mutex _mutex = new();
    private readonly List<FutureAwaiter<T>> _awaiters = new();

    public T? GetResult() => GetResult(Timeout.InfiniteTimeSpan);

    public T? GetResult(TimeSpan timeout)
    {
        Monitor.Enter(_mutex);
        if (_state is FutureState.Cancelled)
        {
            throw new CancelledFutureException();
        }
        if (_state is FutureState.Finished)
        {
            var result = ResultUnsafe();
            Monitor.Exit(_mutex);
            return result;
        }

        // Pending or Running => wait
        Monitor.Wait(_mutex, timeout);

        if (_state is FutureState.Cancelled)
        {
            throw new CancelledFutureException();
        }
        if (_state is FutureState.Finished)
        {
            var result = ResultUnsafe();
            Monitor.Exit(_mutex);
            return result;
        }
        throw new TimeoutException();
    }

    public bool Cancel()
    {
        Monitor.Enter(_mutex);
        if (_state is FutureState.Pending)
        {
            _state = FutureState.Cancelled;
            Monitor.PulseAll(_mutex);
            Monitor.Exit(_mutex);
            return true;
        }
        var state = _state;
        Monitor.Exit(_mutex);
        return state is FutureState.Cancelled;
    }

    private T? ResultUnsafe() => _exception is null ? _result : throw _exception;

    FutureState ICompletableFuture<T>.State => _state;
    void ILockable.Acquire() => Monitor.Enter(_mutex);
    void ILockable.Release() => Monitor.Exit(_mutex);

    void ICompletableFuture<T>.SubscribeUnsafe(FutureAwaiter<T> awaiter) => _awaiters.Add(awaiter);
    void ICompletableFuture<T>.UnsubscribeUnsafe(FutureAwaiter<T> awaiter) => _awaiters.Remove(awaiter);

    void ICompletableFuture<T>.SetResult(T result) => this.Finish(x => x._result = result);
    void ICompletableFuture<T>.SetException(Exception exception) => this.Finish(x => x._exception = exception);

    private void Finish(Action<Future<T>> f)
    {
        Monitor.Enter(_mutex);
        if (_state is FutureState.Cancelled || _state is FutureState.Finished)
        {
            throw new InvalidFutureStateException();
        }
        f(this);
        _state = FutureState.Finished;
        _awaiters.ForEach(x => x.AddFailure(this));
        Monitor.PulseAll(_mutex);
        Monitor.Exit(_mutex);
    }
}
