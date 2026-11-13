using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace AokanaFontFix;

// "Class Plugin has BepInPlugin attribute but does not inherit from BaseUnityPlugin" - you're just wrong lol
#pragma warning disable BepInEx002

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Aokana.exe")]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; }

    internal new ManualLogSource Logger { get; private set; }

    private AssetBundle bundle;

    private Dictionary<int, FontReplacement> fonts;
    public TMP_FontAsset CachedBacklogFont { get; private set; }
    public Material CachedBacklogFontMaterial { get; private set; }

    private readonly Dictionary<string, EngineMain.Language> languageCodes =
        new(System.StringComparer.OrdinalIgnoreCase)
        {
            { "en", EngineMain.Language.en },
            { "jp", EngineMain.Language.jp },
            { "cn", EngineMain.Language.cn },
            { "tc", EngineMain.Language.tc },
        };

    private readonly Dictionary<int, HashSet<EngineMain.Language>> supportedFontLanguages = [];

    public void Awake()
    {
        Logger = base.Logger;

        Instance = this;

        if (!TryLoadBundle())
        {
            return;
        }

        LoadFonts();
        BindFontLanguages();
        HarmonyPatch();
    }

    private void LoadFonts()
    {
        var assets = bundle.GetAllAssetNames().Where(x => x.StartsWith("font")).Select(x => int.Parse(x.Substring(4))).ToList();

        fonts = new Dictionary<int, FontReplacement>(assets.Count);
        foreach (int assetIndex in assets)
        {
            var fontAssetId = $"font{assetIndex}";
            var preset0AssetId = $"material{assetIndex}-preset0";
            var preset1AssetId = $"material{assetIndex}-preset1";

            var fontAsset = bundle.LoadAsset<TMP_FontAsset>(fontAssetId);
            var preset0Asset = bundle.LoadAsset<Material>(preset0AssetId);
            var preset1Asset = bundle.LoadAsset<Material>(preset1AssetId);

            if (fontAsset == null || preset0Asset == null || preset1Asset == null)
            {
                Logger.LogError($"font {assetIndex} is broken. " +
                                $"{fontAssetId} == null == {fontAsset == null}, " +
                                $"{preset0AssetId} == null == {preset0Asset == null}, " +
                                $"{preset1AssetId} == null == {preset1Asset == null}");
                continue;
            }

            var preset0 = new Material(preset0Asset);
            var preset1 = new Material(preset1Asset);
            var replacementEntry = new FontReplacement(fontAsset, preset0, preset1);
            fonts.Add(assetIndex, replacementEntry);
        }
    }

    private bool TryLoadBundle()
    {
        string bundlePath = System.IO.Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.assetbundle");

        if (!System.IO.File.Exists(bundlePath))
        {
            Logger.LogFatal($"Could not find font assetbundle! It should be at {bundlePath}");
            return false;
        }

        bundle = AssetBundle.LoadFromFile(bundlePath);

        if (bundle == null)
        {
            Logger.LogFatal("bundle failed to load for some reason?");
            return false;
        }

        return true;
    }

    private static void HarmonyPatch()
    {
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(ChangeFontPatch));
        harmony.PatchAll(typeof(AdvTextShadowUpdatePatch));
        harmony.PatchAll(typeof(ShowBacklogPatch));
        harmony.PatchAll(typeof(KerningFixPatch));
    }

    private void BindFontLanguages()
    {
        for (int id = 0; id < UIAdv.fontname.Length; id++)
        {
            string key = $"Font{id + 1}Languages";
            var setting = Config.Bind("Fonts",
                key,
                id == 0 ? "en" : "",
                $"Comma-separated list of languages this font supports. Options are {languageCodes.Select(x => x.Key).Join()}");

            var languages = new HashSet<EngineMain.Language>();

            foreach (string token in setting.Value.Split(','))
            {
                string code = token.Trim();

                if (code.IsNullOrWhiteSpace())
                {
                    continue;
                }

                if (languageCodes.TryGetValue(code, out var language))
                {
                    languages.Add(language);
                }
                else
                {
                    Logger.LogWarning($"Unknown language '{code}' for font '{key}'.");
                }
            }

            supportedFontLanguages[id] = languages;
        }
    }

    public void CacheExistingBacklogFont(TMP_FontAsset font, Material sharedMaterial)
    {
        CachedBacklogFont = font;
        CachedBacklogFontMaterial = sharedMaterial;
    }

    public bool TryGetFont(int id, EngineMain.Language language, out FontReplacement font)
    {
        font = null;

        return supportedFontLanguages.TryGetValue(id, out var languages)
               && languages.Contains(language)
               && fonts.TryGetValue(id, out font);
    }
}

[HarmonyPatch(typeof(UIAdv), nameof(UIAdv.ChangeFont))]
public static class ChangeFontPatch
{
    public static bool Prefix(int id, UIAdv __instance)
    {
        if (!Plugin.Instance.TryGetFont(id, EngineMain.lang, out var font))
        {
            return true;
        }

        __instance.advtext.font = font.Font;
        __instance.nametext.font = font.Font;
        Plugin.Instance.Logger.LogInfo($"Patched font {id} to {font.Font.name}");
        __instance.AdvTextShadow_Update();

        return false;
    }
}

[HarmonyPatch(typeof(UIAdv), nameof(UIAdv.AdvTextShadow_Update))]
public static class AdvTextShadowUpdatePatch
{
    public static bool Prefix(UIAdv __instance, EngineMain ___engine)
    {
        var text = __instance.advtext;
        if (!Plugin.Instance.TryGetFont(___engine.prefs.font.value, EngineMain.lang, out var font))
        {
            return true;
        }

        var material = ___engine.prefs.fontShadow.value ? font.Preset0 : font.Preset1;
        text.fontMaterial = material;
        Plugin.Instance.Logger.LogInfo($"Patched font {___engine.prefs.font.value} material to {material.name}");
        return false;
    }
}

[HarmonyPatch(typeof(UIAdv), "prepTextTags")]
public static class KerningFixPatch
{
    public static void Postfix(string tx, ref string __result)
    {
        // two can play at that game
        __result = __result.Replace("</i><size=10> </size>", "</i>");
    }
}

[HarmonyPatch(typeof(UIBacklog), nameof(UIBacklog.ShowBacklog))]
public static class ShowBacklogPatch
{
    private static readonly System.Reflection.FieldInfo MessageTextField =
        AccessTools.Field(AccessTools.Field(typeof(UIBacklog), "msgframe").FieldType.GetElementType(), "msgtx");

    private static readonly System.Reflection.FieldInfo NameTextField =
        AccessTools.Field(AccessTools.Field(typeof(UIBacklog), "msgframe").FieldType.GetElementType(), "nametx");

    public static void Prefix(UIBacklog __instance, EngineMain ___engine, System.Array ___msgframe)
    {
        if (___msgframe == null)
        {
            return;
        }

        TMP_FontAsset fontAsset;
        Material fontMaterial;
        if (Plugin.Instance.TryGetFont(___engine.prefs.font.value, EngineMain.lang, out var font))
        {
            fontAsset = font.Font;
            fontMaterial = font.Preset0;
        }
        else
        {
            fontAsset = Plugin.Instance.CachedBacklogFont;
            fontMaterial = Plugin.Instance.CachedBacklogFontMaterial;
        }

        foreach (object frame in ___msgframe)
        {
            if (MessageTextField.GetValue(frame) is TextMeshProUGUI messageText)
            {
                if (Plugin.Instance.CachedBacklogFont == null || Plugin.Instance.CachedBacklogFontMaterial == null)
                {
                    Plugin.Instance.CacheExistingBacklogFont(messageText.font, messageText.fontSharedMaterial);
                }

                if (fontAsset == null || fontMaterial == null)
                {
                    return;
                }

                messageText.font = fontAsset;
                messageText.fontSharedMaterial = fontMaterial;
            }

            if (NameTextField.GetValue(frame) is TextMeshProUGUI nameText)
            {
                if (fontAsset == null || fontMaterial == null)
                {
                    return;
                }

                nameText.font = fontAsset;
                nameText.fontSharedMaterial = fontMaterial;
            }
        }
    }
}

public class FontReplacement(TMP_FontAsset font, Material preset0, Material preset1)
{
    public TMP_FontAsset Font { get; } = font;
    public Material Preset0 { get; } = preset0;
    public Material Preset1 { get; } = preset1;
}