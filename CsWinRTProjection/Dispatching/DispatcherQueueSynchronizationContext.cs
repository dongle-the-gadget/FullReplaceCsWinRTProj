using System;
using System.Threading;
using WinRT;

namespace Windows.System.Dispatching;

/// <summary>
/// DispatcherQueueSyncContext allows developers to await calls and get back onto the
/// UI thread. Needs to be installed on the UI thread through DispatcherQueueSyncContext.SetForCurrentThread
/// </summary>
public partial class DispatcherQueueSynchronizationContext : SynchronizationContext
{
    private readonly DispatcherQueue _queue;

    public DispatcherQueueSynchronizationContext(DispatcherQueue queue)
    {
        _queue = queue;
    }

    /// <inheritdoc/>
    public unsafe override void Post(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);

#if NET5_0_OR_GREATER
        DispatcherQueueProxyHandler* dispatcherQueueProxyHandler = DispatcherQueueProxyHandler.Create(d, state);
        int hResult;

        try
        {
            IDispatcherQueue* dispatcherQueue = (IDispatcherQueue*)((IWinRTObject)_queue).NativeObject.ThisPtr;
            bool success;

            hResult = dispatcherQueue->TryEnqueue(dispatcherQueueProxyHandler, &success);

            GC.KeepAlive(this);
        }
        finally
        {
            dispatcherQueueProxyHandler->Release();
        }

        if (hResult != 0)
            ExceptionHelpers.ThrowExceptionForHR(hResult);
#else
        _queue.TryEnqueue(() => d!(state));
#endif
    }

    /// <inheritdoc/>
    public override void Send(SendOrPostCallback d, object state)
    {
        throw new NotSupportedException("Send not supported");
    }

    /// <inheritdoc/>
    public override SynchronizationContext CreateCopy()
    {
        return new DispatcherQueueSynchronizationContext(_queue);
    }
}