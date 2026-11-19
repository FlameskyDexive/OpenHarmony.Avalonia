using System.Diagnostics;

using Avalonia.Rendering;

namespace Avalonia.OpenHarmony;

public class OpenHarmonyRenderTimer : IRenderTimer
{
    private static readonly Stopwatch s_sw = Stopwatch.StartNew();
    public bool RunsInBackground => false;

    // Avalonia 12: IRenderTimer.Tick 由事件改为可空属性
    public Action<TimeSpan>? Tick { get; set; }

    public void Render()
    {
        var timespan = s_sw.Elapsed;
        s_sw.Restart();
        Tick?.Invoke(timespan);
    }
}
