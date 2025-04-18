using Kingmaker.Localization.Shared;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public static class SharedStringMenuItems
	{
		private const int Priority = -10100;
		private const string MenuRoot = "Assets/Create/Localization/";

		[MenuItem(MenuRoot + "Shared String", priority = Priority)]
		private static void CreateSharedString()
		{
			NewAssetWindow.ShowWindow(ScriptableObject.CreateInstance<SharedStringCreator>());
		}

		[MenuItem(MenuRoot + "Shared String (Action)", priority = Priority + (int)StringCreateWindowAttribute.StringType.Action)]
		private static void CreateSharedStringAction()
		{
			NewAssetWindow.ShowWindow(ScriptableObject.CreateInstance<ActionStringCreator>());
		}

		[MenuItem(MenuRoot + "Shared String (Bark)", priority = Priority + (int)StringCreateWindowAttribute.StringType.Bark)]
		private static void CreateSharedStringBark()
		{
			NewAssetWindow.ShowWindow(ScriptableObject.CreateInstance<BarkStringCreator>());
		}

		[MenuItem(MenuRoot + "Shared String (Buff)", priority = Priority + (int)StringCreateWindowAttribute.StringType.Buff)]
		private static void CreateSharedStringBuff()
		{
			NewAssetWindow.ShowWindow(ScriptableObject.CreateInstance<BuffStringCreator>());
		}

		[MenuItem(MenuRoot + "Shared String (EntryPoint)", priority = Priority + (int)StringCreateWindowAttribute.StringType.EntryPoint)]
		private static void CreateSharedStringEntryPoint()
		{
			NewAssetWindow.ShowWindow(ScriptableObject.CreateInstance<EntryPointStringCreator>());
		}

		[MenuItem(MenuRoot + "Shared String (Item)", priority = Priority + (int)StringCreateWindowAttribute.StringType.Item)]
		private static void CreateSharedStringItem()
		{
			NewAssetWindow.ShowWindow(ScriptableObject.CreateInstance<ItemStringCreator>());
		}

		[MenuItem(MenuRoot + "Shared String (LocationName)", priority = Priority + (int)StringCreateWindowAttribute.StringType.LocationName)]
		private static void CreateSharedStringLocationName()
		{
			NewAssetWindow.ShowWindow(ScriptableObject.CreateInstance<LocationNameStringCreator>());
		}

		[MenuItem(MenuRoot + "Shared String (Name)", priority = Priority + (int)StringCreateWindowAttribute.StringType.Name)]
		private static void CreateSharedStringName()
		{
			NewAssetWindow.ShowWindow(ScriptableObject.CreateInstance<NameStringCreator>());
		}

		[MenuItem(MenuRoot + "Shared String (Other)", priority = Priority + (int)StringCreateWindowAttribute.StringType.Other)]
		private static void CreateSharedStringOther()
		{
			NewAssetWindow.ShowWindow(ScriptableObject.CreateInstance<OtherStringCreator>());
		}
	}
}