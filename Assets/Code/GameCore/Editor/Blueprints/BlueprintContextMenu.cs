using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Utility.DotNetExtensions;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
    public class BlueprintContextMenu
    {

        class MenuItem
        {
            public string Name;
            public MethodInfo Method;

            public void Call(object userdata)
            {
                if (Method.GetParameters().Length == 0)
                {
                    Method.Invoke(Method.IsStatic ? null : userdata, null);
                }
                else
                {
                    Method.Invoke(null, new[] {userdata});
                }
            }
        }
        
        static readonly Dictionary<Type, List<MenuItem>> AddMenus = new Dictionary<Type,List<MenuItem>>();
        
        static BlueprintContextMenu()
        {
            foreach (var method in TypeCache.GetMethodsWithAttribute<BlueprintContextMenuAttribute>())
            {
                var attrs = method.GetCustomAttributes<BlueprintContextMenuAttribute>();
                foreach (var attr in attrs)
                {
                    if (string.IsNullOrEmpty(attr?.Name))
                        continue; // sanity check
                    var type = attr.BlueprintType ?? method.DeclaringType;
                    if (type == null || !type.IsOrSubclassOf<SimpleBlueprint>())
                        continue;

                    var item = new MenuItem {Name = attr.Name, Method = method};
                    foreach (var d in TypeCache.GetTypesDerivedFrom(type))
                    {
                        AddMenuItem(d, item);
                    }

                    AddMenuItem(type, item);
                }
            }

            
        }

        private static void AddMenuItem(Type type, MenuItem item)
        {
            var list = AddMenus.Get(type) ?? new List<MenuItem>();
            list.Add(item);
            AddMenus[type] = list;
        }

        public static void AddItemsToMenu(GenericMenu gm, SimpleBlueprint bp)
        {
            var list = AddMenus.Get(bp.GetType());
            if (list?.Count > 0)
            {
                foreach (var menuItem in list)
                {
                    gm.AddItem(new GUIContent(menuItem.Name), false, menuItem.Call, bp);
                }
                gm.AddSeparator("");
            }

            gm.AddItem(new GUIContent("Find references in project..."), false,
                () => ReferencesWindow.ReferencesWindow.FindReferencesInProject(bp));
            gm.AddItem(new GUIContent("Copy/Name"), false, () => GUIUtility.systemCopyBuffer = bp.name);
            gm.AddItem(new GUIContent("Copy/Path"), false, () => GUIUtility.systemCopyBuffer = BlueprintsDatabase.GetAssetPath(bp));
            gm.AddItem(new GUIContent("Copy/Guid"), false, () => GUIUtility.systemCopyBuffer = bp.AssetGuid);
            gm.AddItem(new GUIContent("Copy/Name (Guid)"), false, () => GUIUtility.systemCopyBuffer = $"{bp.name} ({bp.AssetGuid})");
            gm.AddItem(new GUIContent("Copy/Type"), false, () => GUIUtility.systemCopyBuffer = bp.GetType().Name);
            gm.AddItem(new GUIContent("Copy/Contents"), false, () => GUIUtility.systemCopyBuffer = File.ReadAllText(BlueprintsDatabase.GetAssetPath(bp)));

        }
    }
}