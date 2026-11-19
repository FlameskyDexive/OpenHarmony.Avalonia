using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia.OpenHarmony;

public class OpenHarmonyPlatform
{
    public static OpenHarmonyPlatformOptions Options = new()
    { RenderingMode = [OpenHarmonyPlatformRenderingMode.Software] };

    public static void Initialize()
    {
        var options = AvaloniaLocator.Current.GetService<OpenHarmonyPlatformOptions>() ??
                      new OpenHarmonyPlatformOptions();
        var fontManagerOptions = new FontManagerOptions()
        {
            DefaultFamilyName = "HarmonyOS Sans SC",
        };

        // Avalonia 12: 显式初始化 Dispatcher（legacy IPlatformThreadingInterface 不再被自动适配）
        var threading = new OpenHarmonyPlatformThreading();
        Dispatcher.InitializeUIThreadDispatcher(threading);

        AvaloniaLocator.CurrentMutable
            .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
            .Bind<FontManagerOptions>().ToConstant(fontManagerOptions)
            // .Bind<FontManager>().ToConstant(new FontManager(new OpenHarmonyFontManagerImpl()))
            .Bind<FontManager>().ToConstant(new FontManager(new OpenHarmonyFontManagerImpl()))
            .Bind<IRuntimePlatform>().ToSingleton<OpenHarmonyRuntimePlatform>()
            .Bind<IRenderTimer>().ToSingleton<OpenHarmonyRenderTimer>()
            .Bind<ICursorFactory>().ToSingleton<CursorFactory>()
            .Bind<OpenHarmonyPlatformThreading>().ToConstant(threading)
            .Bind<IKeyboardDevice>().ToSingleton<OpenHarmonyKeyboardDevice>()
            .Bind<IPlatformSettings>().ToSingleton<OpenHarmonyPlatformSettings>();

        // Avalonia 12: Compositor 从 locator 解析 IRenderLoop，缺失则渲染循环不跑（黑屏）。
        // 用我们的手动泵 timer（XComponent 帧回调驱动）组装 DefaultRenderLoop。
        AvaloniaLocator.CurrentMutable
            .Bind<IRenderLoop>()
            .ToConstant(RenderLoop.FromTimer(AvaloniaLocator.Current.GetRequiredService<IRenderTimer>()));

        var platformGraphics = InitializeGraphics(options);
        if (platformGraphics is not null)
            AvaloniaLocator.CurrentMutable.Bind<IPlatformGraphics>().ToConstant(platformGraphics);

        var compositor = new Compositor(platformGraphics);
        AvaloniaLocator.CurrentMutable.Bind<Compositor>().ToConstant(compositor);

        // FontManager.Current.AddFontCollection(new TestE(
        //     new Uri("fonts:AvaloniaSystemFonts"),
        //     new Uri("resm:Avalonia.OpenHarmony.Assets?assembly=Avalonia.OpenHarmony")));
    }

    private static IPlatformGraphics? InitializeGraphics(OpenHarmonyPlatformOptions options)
    {
        foreach (var renderingMode in options.RenderingMode)
        {
            if (renderingMode == OpenHarmonyPlatformRenderingMode.Egl)
                return EglPlatformGraphics.TryCreate(() =>
                {
                    return new EglDisplay(new EglDisplayCreationOptions
                    {
                        Egl = new EglInterface("libEGL.so"),
                        SupportsMultipleContexts = true,
                        SupportsContextSharing = true
                    });
                });

            if (renderingMode == OpenHarmonyPlatformRenderingMode.Software) return null;
        }

        throw new Exception("no render mode");
    }
}

public sealed class OpenHarmonyPlatformOptions
{
    public IReadOnlyList<OpenHarmonyPlatformRenderingMode> RenderingMode { get; set; } =
        [OpenHarmonyPlatformRenderingMode.Egl, OpenHarmonyPlatformRenderingMode.Software];
}

public enum OpenHarmonyPlatformRenderingMode
{
    Software = 1,

    Egl = 2
}
