#if UNITY_EDITOR && EDITOR_FIELDS
using Kingmaker.Blueprints.Base;
using System;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Blueprints.Creation;
using Kingmaker.Localization;
using Kingmaker.View;
using Owlcat.Runtime.Core.Utility;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Utility;
using Kingmaker.Localization.Shared;
using UnityEditor;
using UnityEngine;
using Owlcat.Editor.Core.Utility;
using Owlcat.Runtime.Core.Logging;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor
{
    [CustomPropertyDrawer(typeof(SharedStringAsset))]
    public class SharedStringAssetPropertyDrawer : PropertyDrawer
    {
        private static readonly LogChannel Logger =
            LogChannelFactory.GetOrCreate(nameof(SharedStringAssetPropertyDrawer));
        private SerializedObject m_SerializedObject;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var r = new Rect(position) {height = EditorGUIUtility.singleLineHeight};
            if (!property.objectReferenceValue)
            {
                if (fieldInfo.HasAttribute<StringCreateWindowAttribute>())
                {
                    r.width -= 48;
                    if (GUI.Button(new Rect(r.xMax, r.y, 48, r.height), "new", EditorStyles.miniButton))
                    {
                        ShowCreator(property, fieldInfo.GetAttribute<StringCreateWindowAttribute>());
                    }
                }
                else
                {
                    r.width -= 52;
                    if (GUI.Button(new Rect(r.xMax, r.y, 36, r.height), "new", EditorStyles.miniButtonLeft))
                    {
                        SharedStringAsset shared = CreateShared(property, fieldInfo);
                        property.objectReferenceValue = shared;
                    }
                    if (GUI.Button(new Rect(r.xMax + 36, r.y, 16, r.height), "▾", EditorStyles.miniButtonRight))
                    {
                        var shared = CreateSharedWithFolderDialog(property, fieldInfo);
                        if (shared)
                            property.objectReferenceValue = shared;
                    }
                }
            }

            AssetPicker.ShowPropertyField(r, property, fieldInfo, label, typeof(SharedStringAsset));

            // doesn't work anyway
            // var ssa = property.objectReferenceValue as SharedStringAsset;
            // if (ssa && !property.hasMultipleDifferentValues)
            // {
            //     var fr = new Rect(r.x + (EditorGUI.indentLevel) * 10, r.y, 16, r.height);
            //
            //     if (BlueprintEditorWrapper.Unwrap<BlueprintUnit>(property.serializedObject.targetObject))
            //     {
            //         property.isExpanded = true;
            //     }
            //
            //     EditorGUI.Foldout(fr, property.isExpanded, GUIContent.none);
            //     if (property.isExpanded)
            //     {
            //         EditorGUI.indentLevel++;
            //         position.yMin += EditorGUIUtility.singleLineHeight;
            //         if (m_SerializedObject == null || m_SerializedObject.targetObject != ssa)
            //         {
            //             m_SerializedObject = new SerializedObject(ssa);
            //         }
            //         m_SerializedObject.Update();
            //         LocalizationEditorGUI.LocalizedStringPropertyField(
            //             position,
            //             new GUIContent("String"),
            //             m_SerializedObject.FindProperty("String"),
            //             ssa.String);
            //         m_SerializedObject.ApplyModifiedProperties();
            //         EditorGUI.indentLevel--;
            //     }
            // }
        }

        public static SharedStringAsset CreateSharedWithFolderDialog(SerializedProperty property, FieldInfo fieldInfo = null)
        {
            string defaultPath = fieldInfo != null ? GetDefaultPath(property, fieldInfo) : GetDefaultPath(property);

            PFLog.Default.Log("DefPath:" + defaultPath);

            string path = EditorUtility.SaveFilePanel(
                "Select folder for string",
                Path.GetDirectoryName(defaultPath),
                Path.GetFileName(defaultPath),
                "asset");

            if (string.IsNullOrEmpty(path)) 
                return null;
            path = path[(path.IndexOf("/Assets/", StringComparison.Ordinal) + 1)..]; // strip beginning of the absolute path so we get one relative to project root
            return CreateShared(path);
        }

        private static void ShowCreator(SerializedProperty property, StringCreateWindowAttribute attr)
        {
            var creator = attr.Type switch
            {
                StringCreateWindowAttribute.StringType.Bark => ScriptableObject.CreateInstance<BarkStringCreator>(),
                StringCreateWindowAttribute.StringType.UIText => (AssetCreatorBase)ScriptableObject.CreateInstance<UITextStringCreator>(),
                StringCreateWindowAttribute.StringType.MapMarker => ScriptableObject.CreateInstance<MarkerStringCreator>(),
                _ => null
            };
            if (!creator)
                return;

            NewAssetWindow.ShowWindow(creator);
            if (attr.GetNameFromAsset)
                NewAssetWindow.AssetName = property.serializedObject.targetObject.name;

            NewAssetWindow.SetCreationCallback(
                asset =>
                {
                    using (GuiScopes.UpdateObject(property))
                    {
                        property.objectReferenceValue = asset as Object;
                    }
                });
        }

        public static SharedStringAsset CreateShared(SerializedProperty property)
        {
            return CreateShared(GetDefaultPath(property));
        }

        private static SharedStringAsset CreateShared(SerializedProperty property, FieldInfo fieldInfo)
        {
            return CreateShared(GetDefaultPath(property, fieldInfo));
        }

        private static string GetDefaultPath(SerializedProperty property, FieldInfo fieldInfo)
        {
            var template = fieldInfo.GetAttribute<StringCreateTemplateAttribute>();
            if (template == null) 
                return GetDefaultPath(property);
            string assetPath = 
                property.serializedObject.targetObject is ScriptableWrapperBase sw
                    ? BlueprintsDatabase.IdToPath(sw.GetOwnerBlueprintId())
                    : AssetDatabase.GetAssetOrScenePath(property.serializedObject.targetObject);
            if (assetPath.StartsWith(BlueprintsDatabase.DbPathPrefix))
            {
                assetPath = "Assets/Mechanics/" + assetPath.Replace("\\", "/").Replace(".jbp",".asset");
            }
            return template.GetStringPath(property, assetPath);
        }

        public static string GetPathPrefix(SerializedProperty property)
        {
            string name = property.propertyPath.Replace("m_", "");
            if (name.StartsWith(nameof(BlueprintEditorWrapper.Blueprint)))
            {
                name = name[nameof(BlueprintEditorWrapper.Blueprint).Length..];
            }else if (name.StartsWith(nameof(BlueprintComponentEditorWrapper.Component)))
            {
                name = name[nameof(BlueprintComponentEditorWrapper.Component).Length..];
            }

            // try to get path to containing asset
            string path = property.serializedObject.targetObject is ScriptableWrapperBase sw
                ? BlueprintsDatabase.IdToPath(sw.GetOwnerBlueprintId())
                : AssetDatabase.GetAssetPath(property.serializedObject.targetObject);

            // try to get path to the scene with this object
            if (string.IsNullOrEmpty(path))
            {
                var mb = property.serializedObject.targetObject as MonoBehaviour;
                if (mb)
                {
                    var area = Object.FindObjectsOfType<AreaEnterPoint>()
                        .Select(ep => ep.Blueprint)
                        .Where(ep => ep != null)
                        .Select(ep => ep.Area)
                        .FirstOrDefault(a => a != null);
                    if (area != null)
                    {
                        path = BlueprintsDatabase.GetAssetPath(area);
                        name = mb.name + "_" + name;
                    }
                }
            }
            if (string.IsNullOrEmpty(path))
            {
                Logger.Error("Cannot create asset - unable to determine path");
                return path;
            }

            // if we want to place SSA into blueprints folder, select matching assets folder instead
            if (path.StartsWith(BlueprintsDatabase.DbPathPrefix))
            {
                path = "Assets/Mechanics/Blueprints/" + path.Substring(BlueprintsDatabase.DbPathPrefix.Length);
            }

            path = path
                .Replace(".asset", "_")
                .Replace(".jbp", "_")
                .Replace(".prefab", "_")
                .Replace(".unity", "/");
            return path + name;
        }

        private static string GetDefaultPath(SerializedProperty property)
        {
            string path = GetPathPrefix(property);
            path = path.Replace("\\", "/") + ".asset";
            AssetPathUtility.EnsurePathExists(Path.GetDirectoryName(path));
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            return path;
        }

        private static SharedStringAsset CreateShared(string path)
        {
            PFLog.Default.Log("Creating a string asset in " + path);
            string dirPath = Path.GetDirectoryName(path);
            if (dirPath != null)
            {
                Directory.CreateDirectory(dirPath);
            }

            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var asset = ScriptableObject.CreateInstance<SharedStringAsset>();
            asset.String = new LocalizedString();
            AssetDatabase.CreateAsset(asset, path);
            Logger.Log("Created asset @ " + path);
            return asset;

        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight + (property.objectReferenceValue && property.isExpanded ? 100 : 0);
        }
    }
}
#endif