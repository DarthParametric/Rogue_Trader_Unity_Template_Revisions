using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.UIElements.Custom.Properties;
using Kingmaker.Editor.UIElements.Custom.PropertyComponents;
using UnityEditor;

namespace Kingmaker.Editor.UIElements.Custom.Prototypable
{
    public class PrototypeProperty : BlueprintReferenceProperty
    {
        public PrototypeProperty(SerializedProperty property, System.Type refType) : base(property, property.GetFieldInfo(), property, refType)
        {
            AddComponent(new FuncTitleProviderComponent(() => "Prototype"));
        }
    }
}