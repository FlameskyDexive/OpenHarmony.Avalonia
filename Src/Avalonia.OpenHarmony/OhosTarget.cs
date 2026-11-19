using System.Runtime.InteropServices;

namespace Avalonia.OpenHarmony;

/// <summary>
///     运行时目标探测（单产物需要自适应时用；常规按 RID 分产物请用编译宏 OHOS_EMULATOR / OHOS_DEVICE）。
///     判定依据：HarmonyOS 模拟器镜像为 x86/x64，真机为 arm64。
/// </summary>
public static class OhosTarget
{
    /// <summary>x86/x64 视为模拟器（DGLES GPU 半虚拟化环境）。</summary>
    public static bool IsEmulator =>
        RuntimeInformation.ProcessArchitecture is Architecture.X86 or Architecture.X64;

    /// <summary>Arm64 视为真机（GPU 直通）。</summary>
    public static bool IsDevice =>
        RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
}
