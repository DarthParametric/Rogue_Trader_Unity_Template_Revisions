using System;
using JetBrains.Annotations;
using Kingmaker.Modding;
using OwlcatModification.Editor.Inspector;
using UnityEngine;

namespace OwlcatModification.Editor
{
    [CreateAssetMenu(menuName = "Modification")]
    public class Modification : ScriptableObject
    {
        [Serializable]
        public class SettingsData
        {
                    [Header("Steam Workshop Settings")]
                    public string RelativeThumbnailPath;
                    [TextArea]
                    public string WorkshopDescription;
                    [ReadOnly]
                    public string WorkshopId;
        }

        public OwlcatModificationManifest Manifest = new OwlcatModificationManifest();
        public SettingsData Settings = new SettingsData();
    }
}
