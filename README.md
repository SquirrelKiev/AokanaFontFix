# AokanaFontFix

BepInEx plugin for replacing dialogue fonts in NekoNyan's Aokana. Tested on `1.23 (R18+PE)`, and with `BepInEx 6.0.0-be.793`.

Intented for replacing their use of faux italics with a real copy of italics, but can be used to replace the dialogue font with whatever font you want.

## Building

`/AokanaFontFix/` is the plugin side of things. `/TheUnitySide/` is for building the font assetbundle (requires Unity 2018.2.19f1).
Can build the plugin, copy that to your plugin directory, then build the font assetbundle, and copy that to your config directory. assetbundle must be called `moe.enonibobble.aokanafontfix.assetbundle`.

Currently just replaces the 1st font (NotoSansCJK-Medium) with SourceSans3-Medium(Italic), which I believe is what the latin glyphs are based on? Swapping in other fonts is relatively painless however.

### Swapping the fonts

Add your desired font ttf/otf/whatever to the Unity project (`Assets/Fonts/YourFontName/Raw` should do),
then generate a font asset via `Window > TextMeshPro > Font Asset Creator`. I believe the below settings match NekoNyan's, but do tweak accordingly to your font.
The SourceSans3-Medium copy I've generated for example is 1024x1024, with a subset of the characters limited to what the font contains.

* Atlas Resolution: 4096x4096
* Character Set: Characters from File
* Character File: Assets/Fonts/characters.txt
* Font Style: Normal, 0
* Render Mode: Distance Field 16

After generating and saving that, create two new material presets for that font, Preset 0 and Preset 1. (Right click the embedded font's header in the inspector, Create Material Preset)
Preset 0 is the variant used when Text Border is set to OFF, and preset 1 is when set to ON.

If you're trying to match NekoNyan's settings, Preset 0 you can just leave at the defaults,
and Preset 1 should be set to `Face > Dilate` of 0.8, and `Outline > Thickness` of the same. Make sure to also set `Outline > Color` to white.
Tweak accordingly for your font of course. Can always check it against the text in TestScene.

After that, you can now add a new entry to Fonts, set index to whichever font you want to replace (index matches to the number in the settings menu, plus one.), and assign Font, Preset 0, and Preset 1 accordingly.

Finally, export the font bundle via `Tools > Export Font Asset Bundle...`, and copy it to your config directory.

You should now be able to load up Aokana, switch to the font in settings, and see your font!