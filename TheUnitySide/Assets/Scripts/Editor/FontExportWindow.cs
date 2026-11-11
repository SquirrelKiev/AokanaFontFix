using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace AokanaFontFix.Unity
{
    public class FontExportWindow : EditorWindow
    {

        [MenuItem("Tools/Export Font Asset Bundle...")]
        public static void OpenWindow()
        {
            FontExportWindow window = GetWindow<FontExportWindow>("Font Exporter");
            window.Show();
        }

        private FontExportConfig fontConfig;

        public void OnGUI()
        {
            bool exportBundle = false;

            EditorGUILayout.BeginVertical();
            {
                fontConfig = (FontExportConfig)EditorGUILayout.ObjectField(fontConfig, typeof(FontExportConfig), false);
                GUI.enabled = fontConfig != null;
                if (GUILayout.Button("Export!"))
                {
                    exportBundle = true;
                }
                GUI.enabled = fontConfig == null;
            }
            EditorGUILayout.EndVertical();

            if (exportBundle)
            {
                BuildFontBundle(fontConfig);
                EditorUtility.DisplayDialog("*fonts all over the place*", "done'd!!", "swag");
            }
        }

        private void BuildFontBundle(FontExportConfig fontConfig)
        {
            var addressableNames = new string[fontConfig.Fonts.Count*3];
            var assetPaths = new string[fontConfig.Fonts.Count*3];

            int index = 0;
            foreach (var fontEntry in fontConfig.Fonts)
            {
                addressableNames[index] = "font" + fontEntry.index.ToString();
                assetPaths[index] = AssetDatabase.GetAssetPath(fontEntry.font);
                index++;
                addressableNames[index] = "material" + fontEntry.index + "-preset0";
                assetPaths[index] = AssetDatabase.GetAssetPath(fontEntry.preset0);
                index++;
                addressableNames[index] = "material" + fontEntry.index + "-preset1";
                assetPaths[index] = AssetDatabase.GetAssetPath(fontEntry.preset1);
                index++;
            }

            var bundle = new AssetBundleBuild[]
            {
                new AssetBundleBuild()
                {
                    assetBundleName = "moe.enonibobble.aokanafontfix.assetbundle",
                    assetNames = assetPaths,
                    addressableNames = addressableNames
                }
            };

            const string bundleDir = "Assets/AssetBundles/";
            Directory.CreateDirectory(bundleDir);

            BuildPipeline.BuildAssetBundles(bundleDir, bundle, BuildAssetBundleOptions.DeterministicAssetBundle, BuildTarget.StandaloneWindows);
        }
    }
}