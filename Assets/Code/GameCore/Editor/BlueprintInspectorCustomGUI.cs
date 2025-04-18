using System;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Owlcat.Runtime.Core.Utility;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
    public abstract class BlueprintInspectorCustomGUI
    {
        public virtual void OnEnable(BlueprintInspector ed) { }
        public virtual void OnHeader(SimpleBlueprint bp) { }
        public virtual void OnBeforeComponents(SimpleBlueprint bp) { }
        public virtual void OnFooter(SimpleBlueprint bp) { }

        // if true, nothing but header is called and the whole inspector is not rendered
        public virtual bool TotalOverride
            => false;

        private static readonly Dictionary<Type, Type> CustomAdditions = new();

        static BlueprintInspectorCustomGUI()
        {
            var customGuis = TypeCache.GetTypesDerivedFrom<BlueprintInspectorCustomGUI>().Where(t => !t.IsAbstract);

            foreach (var customGUI in customGuis)
            {
                var attr = customGUI.GetAttribute<BlueprintCustomEditorAttribute>();
                if (attr == null) 
                    continue;
                var inspectedType = attr.InspectedType;
                CustomAdditions[inspectedType] = customGUI;
            }
        }
        
        public static BlueprintInspectorCustomGUI GetForType(Type t)
        {
            if (t == typeof(SimpleBlueprint) || t.BaseType==null)
                return null;
            
            if(CustomAdditions.TryGetValue(t, out var gui))
            {
                return (BlueprintInspectorCustomGUI)Activator.CreateInstance(gui);
            }

            return GetForType(t.BaseType);
        }
    }
}