using Avalonia.Platform;
using Avalonia.Platform.Surfaces;

namespace Avalonia.OpenHarmony;

public class FramebufferManager : IFramebufferPlatformSurface
{
    private readonly TopLevelImpl TopLevelImpl;

    public FramebufferManager(TopLevelImpl topLevelImpl)
    {
        TopLevelImpl = topLevelImpl;
    }

    public IFramebufferRenderTarget CreateFramebufferRenderTarget()
    {
        return new FuncFramebufferRenderTarget(Lock);
    }

    public ILockedFramebuffer Lock(IRenderTarget.RenderTargetSceneInfo sceneInfo,
        out FramebufferLockProperties properties)
    {
        properties = new FramebufferLockProperties(PreviousFrameIsRetained: false);
        return new OpenHarmonyFramebuffer(TopLevelImpl);
    }
}
