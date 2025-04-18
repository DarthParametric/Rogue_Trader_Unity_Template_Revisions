using UnityEditor;
using UnityEngine;

namespace Kingmaker.Code.Editor.Utility
{
	public static class PrefabInfo
	{
		public static bool IsPrefabAsset(Object obj)
		{
			var assetType = PrefabUtility.GetPrefabAssetType(obj);
			var instanceStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
			return instanceStatus == PrefabInstanceStatus.NotAPrefab &&
			       (assetType == PrefabAssetType.Regular || assetType == PrefabAssetType.Variant);
		}
		public static bool IsPrefabInstance(Object obj)
		{
			var assetType = PrefabUtility.GetPrefabAssetType(obj);
			var instanceStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
			return instanceStatus == PrefabInstanceStatus.Connected &&
			       (assetType == PrefabAssetType.Regular || assetType == PrefabAssetType.Variant);
		}

		public static bool IsModelAsset(Object obj)
		{
			var assetType = PrefabUtility.GetPrefabAssetType(obj);
			var instanceStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
			return instanceStatus == PrefabInstanceStatus.NotAPrefab &&
			       assetType == PrefabAssetType.Model;
		}
		public static bool IsModelInstance(Object obj)
		{
			var assetType = PrefabUtility.GetPrefabAssetType(obj);
			var instanceStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
			return instanceStatus == PrefabInstanceStatus.Connected &&
			       assetType == PrefabAssetType.Model;
		}
	}
}