using System.Diagnostics;

using Avalonia.Threading;

namespace Avalonia.OpenHarmony;

/// <summary>
///     Avalonia 12: IPlatformThreadingInterface 已被 IDispatcherImpl 取代（legacy 适配器不再自动挂接）。
///     语义对齐上游 LegacyDispatcherImpl：Signal 经队列由 Tick() 在循环线程（XComponent 帧回调）上泵；
///     到期定时器一次性触发后失效，由 Dispatcher 重新排程。
/// </summary>
public class OpenHarmonyPlatformThreading : IDispatcherImpl
{
    private readonly Thread LoopThread;

    private readonly Stopwatch Clock = Stopwatch.StartNew();

    private readonly object _timerLock = new();

    private List<Action> list = [];

    private List<Action> list2 = [];

    private System.Threading.Timer? _timer;

    public OpenHarmonyPlatformThreading()
    {
        LoopThread = Thread.CurrentThread;
    }

    public bool CurrentThreadIsLoopThread => LoopThread == Thread.CurrentThread;

    public event Action? Signaled;

    public event Action? Timer;

    public long Now => Clock.ElapsedMilliseconds;

    public void Signal()
    {
        lock (list)
        {
            list.Add(() => Signaled?.Invoke());
        }
    }

    public void UpdateTimer(long? dueTimeInMs)
    {
        lock (_timerLock)
        {
            _timer?.Dispose();
            _timer = null;

            if (dueTimeInMs.HasValue)
            {
                var interval = Math.Max(1, dueTimeInMs.Value - Clock.ElapsedMilliseconds);
                _timer = new System.Threading.Timer(_ =>
                {
                    lock (_timerLock)
                    {
                        _timer = null; // 一次性：触发后失效，Dispatcher 会按需重新排程
                    }

                    // Dispatcher 的 OnOSTimer 会直接执行作业并要求 VerifyAccess，
                    // 因此 Timer 事件必须与 Signaled 一样经 Tick() 泵到循环线程（XComponent 帧回调线程）
                    lock (list)
                    {
                        list.Add(() => Timer?.Invoke());
                    }
                }, null, (int)interval, Timeout.Infinite);
            }
        }
    }

    /// <summary>由 XComponent 帧回调（TopLevelImpl.Render）在循环线程上泵信号。</summary>
    public void Tick()
    {
        lock (list)
        {
            (list2, list) = (list, list2);
        }

        foreach (var action in list2)
            try
            {
                action();
            }
            catch (Exception e)
            {
                OHDebugHelper.Error("IDispatcherImpl.Signaled 处理异常", e);
            }

        list2.Clear();
    }
}

internal class Disposable : IDisposable
{
    private readonly Action Action;

    public Disposable(Action action)
    {
        Action = action;
    }

    public void Dispose()
    {
        Action?.Invoke();
    }
}
