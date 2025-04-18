#if UNITY_EDITOR && EDITOR_FIELDS
using System;
using System.IO;
using Kingmaker.Editor.Utility;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor
{
    public static class StringCreateTemplateAttributeExtensions
    {
    		public static string GetStringPath(this StringCreateTemplateAttribute attribute, SerializedProperty property, string assetPath)
    		{
    			switch (attribute.Type)
    			{
    				case StringCreateTemplateAttribute.StringType.UnitName:
	                    return assetPath
		                    .Replace("/Units/", "/Units/Names/")
		                    .Replace(".asset", "_Name.asset");
    				case StringCreateTemplateAttribute.StringType.ItemText:
	                    return assetPath
		                    .Replace("/Items/", "/Items/Names/")
		                    .Replace("/Equipment/", "/Equipment/Names/")
		                    .Replace(".asset", "_ItemText.asset");
                    case StringCreateTemplateAttribute.StringType.UIText when property.serializedObject.targetObject is MonoBehaviour mb:
                    {
	                    string name = PrefabUtility.GetOutermostPrefabInstanceRoot(mb.gameObject) is {} prefab
		                    ? prefab.name + "_" + mb.name
		                    : mb.name;

	                    return $"Assets/Mechanics/Blueprints/Root/Strings/UITexts/{name}.asset";
                    }
                    case StringCreateTemplateAttribute.StringType.UIText:
                    {
	                    return assetPath
		                    .Replace(".asset", "_UIText.asset")
		                    .Replace(".scene", "_UIText.asset");
                    }
                    case StringCreateTemplateAttribute.StringType.Bark:
                    {
	                    string path = SharedStringAssetPropertyDrawer.GetPathPrefix(property);
	                    path = path.Replace("\\", "/") + ".asset";
	                    AssetPathUtility.EnsurePathExists(Path.GetDirectoryName(path));
	                    path = AssetDatabase.GenerateUniqueAssetPath(path);
	                    
	                    path = path.Replace("/World/Areas/", "/World/Dialogs/");
	                    string dir = Path.GetDirectoryName(path);
	                    string name = Path.GetFileName(path);
	                    path = dir + "/Barks/" + name;
	                    path = path.Replace("\\", "/");
	                    return path;
                    }
                    case StringCreateTemplateAttribute.StringType.MapObject:
                    {
	                    string path = SharedStringAssetPropertyDrawer.GetPathPrefix(property);
	                    path = path.Replace("\\", "/") + ".asset";
	                    AssetPathUtility.EnsurePathExists(Path.GetDirectoryName(path));
	                    path = AssetDatabase.GenerateUniqueAssetPath(path);
	                    
	                    path = path.Replace("/World/Areas/", "/World/Dialogs/");
	                    string dir = Path.GetDirectoryName(path);
	                    string name = Path.GetFileName(path);
	                    path = dir + "/MapObjects/" + name;
	                    path = path.Replace("\\", "/");
	                    return path;
                    }
                    default:
    					throw new ArgumentOutOfRangeException();
    			}
    		}
    }
}
#endif