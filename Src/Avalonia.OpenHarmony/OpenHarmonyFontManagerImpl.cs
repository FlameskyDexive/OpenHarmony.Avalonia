using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;

using SkiaSharp;

namespace Avalonia.OpenHarmony;

public class TestE : EmbeddedFontCollection
{
    public TestE(Uri key, Uri source) : base(key, source)
    {
    }

    public override bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch,
        [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
    {
        if (familyName == "$Default")
            familyName = "HarmonyOS Sans SC";
        return base.TryGetGlyphTypeface(familyName, style, weight, stretch, out glyphTypeface);
    }
}

internal class OpenHarmonyFontManagerImpl : IFontManagerImpl
{
    private readonly SKFontManager _skFontManager = SKFontManager.Default;

    private readonly TestE _testE = new(
        new Uri("fonts:AvaloniaFonts", UriKind.Absolute),
        new Uri("resm:Avalonia.OpenHarmony.Assets?assembly=Avalonia.OpenHarmony"));

    /// <summary>
    ///     familyName -> 平台 typeface 缓存。
    ///     Avalonia 12 的 FontCollectionBase 公开了 TryAddGlyphTypeface(Stream)，
    ///     其内部回调本实现的 TryCreateGlyphTypeface(Stream)，在该处顺带登记缓存，
    ///     供 IFontManagerImpl.TryCreateGlyphTypeface(familyName)/TryMatchCharacter 映射回 IPlatformTypeface。
    /// </summary>
    private readonly Dictionary<string, List<IPlatformTypeface>> _platformTypefaceCache = new();

    public OpenHarmonyFontManagerImpl()
    {
        FontManager.Current.AddFontCollection(_testE);
        LoadSystemFonts();
    }

    private void LoadSystemFonts()
    {
        foreach (var fontAsset in Directory.GetFiles("/system/fonts"))
            try
            {
                using var stream = File.OpenRead(fontAsset);
                _testE.TryAddGlyphTypeface(stream, out _);
            }
            catch (UnauthorizedAccessException)
            {
                // 鸿蒙应用沙箱下个别系统字体（如 Roboto-Regular.ttf）无读权限，静默跳过即可，可读字体仍正常加载
                OHDebugHelper.Debug($"跳过不可读的系统字体: {fontAsset}");
            }
            catch (Exception e)
            {
                OHDebugHelper.Error("加载字体失败。", e);
            }
    }

    public string GetDefaultFontFamilyName()
    {
        return "HarmonyOS Sans SC";
    }

    public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false)
    {
        return
        [
            "HarmonyOS Sans",
            "HarmonyOS Sans SC",
            "HarmonyOS Sans Condensed",
            "HarmonyOS Sans Digit",
            "Noto Serif",
            "Noto Sans Mono"
        ];
    }

    public bool TryMatchCharacter(int codepoint, FontStyle fontStyle,
        FontWeight fontWeight, FontStretch fontStretch, string? familyName, CultureInfo? culture,
        [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
    {
        platformTypeface = null;

        if (_testE.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, familyName, culture,
                out var match)
            && _platformTypefaceCache.TryGetValue(match.FontFamily.Name, out var candidates)
            && candidates.Count > 0)
        {
            platformTypeface = candidates[0];
            return true;
        }

        return false;
    }

    public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
        FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
    {
        platformTypeface = null;

        if (_platformTypefaceCache.TryGetValue(familyName, out var candidates) && candidates.Count > 0)
        {
            platformTypeface = candidates[0];
            return true;
        }

        return false;
    }

    public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations,
        [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
    {
        var skTypeface = SKTypeface.FromStream(stream);

        if (skTypeface != null)
        {
            platformTypeface = Create(skTypeface, fontSimulations);

            if (!string.IsNullOrEmpty(skTypeface.FamilyName))
            {
                if (!_platformTypefaceCache.TryGetValue(skTypeface.FamilyName, out var list))
                {
                    list = [];
                    _platformTypefaceCache[skTypeface.FamilyName] = list;
                }

                list.Add(platformTypeface);
            }

            return true;
        }

        platformTypeface = null;

        return false;
    }

    private IPlatformTypeface Create(object? skTypeface, object? fontSimulations)
    {
        // Avalonia 12: GlyphTypefaceImpl 已被 internal SkiaTypeface 取代（ctor 不变：SKTypeface + FontSimulations）
        Type? type = null;
        foreach (var alc in AssemblyLoadContext.All)
        {
            foreach (var assembly in alc.Assemblies)
            {
                type = assembly.GetType("Avalonia.Skia.SkiaTypeface");
                if (type != null)
                    break;
            }

            if (type != null)
                break;
        }

        var ctor = type.GetConstructor([typeof(SKTypeface), typeof(FontSimulations)]);
        var obj = RuntimeHelpers.GetUninitializedObject(type);
        ctor.Invoke(obj, [skTypeface, fontSimulations]);
        return (IPlatformTypeface)obj;
    }

    public bool TryGetFamilyTypefaces(string familyName,
        [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
    {
        return _testE.TryGetFamilyTypefaces(familyName, out familyTypefaces);
    }
}
