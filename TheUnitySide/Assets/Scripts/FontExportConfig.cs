using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AokanaFontFix.Unity
{
    [CreateAssetMenu(menuName = "Aokana Font Fix/Font Export Config", fileName = "FontExportConfig.asset")]
    public class FontExportConfig : ScriptableObject
    {
        [SerializeField]
        private List<FontEntry> fonts;

        [Serializable]
        public class FontEntry
        {
            public int index;
            public TMP_FontAsset font;
            public Material preset0;
            public Material preset1;
        }

        public List<FontEntry> Fonts { get { return fonts; } }
    }
}