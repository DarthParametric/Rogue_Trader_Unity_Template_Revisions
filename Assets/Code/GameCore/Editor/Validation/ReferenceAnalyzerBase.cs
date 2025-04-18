using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.DialogSystem.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.Validation
{
	public abstract class ReferenceAnalyzerBase
    {
        public BlueprintScriptableObject Target;

        public abstract Type AnalyzedType { get; }

        public abstract void AnalyzeBlueprintReference(object asset, ReferenceGraph.Ref refData);
        public abstract void AnalyzeSceneReferences(GameObject[] gameObjects, ReferenceGraph.Ref refData);

        protected void AnalyzeBlueprint<TObj, TRefInterface>(
            object asset, ReferenceGraph.Ref refData, Func<TObj, TRefInterface, int> mask) where TRefInterface : class where TObj : class
        {
            var reference = asset as TRefInterface;
            if (reference != null)
            {
                Target = Target.ReloadFromInstanceID();
                refData.ReferenceTypeMask = mask(Target as TObj, reference);
            }
            else
            {
                refData.ReferenceTypeMask = 0;
            }
        }

        protected void AnalyzeScene<TObj, TRefInterface>(GameObject[] gameObjects, ReferenceGraph.Ref refData, Func<TObj, TRefInterface, int> mask) where TRefInterface : class where TObj : class
        {
            IEnumerable<GameObject> objs;
            if (string.IsNullOrEmpty(refData.ReferencingObjectName)) // can't reliably parse prefab name from scene file, so check ALL prefab instances in the scene
            {
                objs = gameObjects.Where(PrefabInfo.IsPrefabInstance);
            }
            else
            {
                objs = gameObjects.Where(o => o.name == refData.ReferencingObjectName);
            }
            var comps = new List<TRefInterface>();
            foreach (var o in objs)
            {
                o.GetComponents(comps);
                foreach (var comp in comps)
                {
                    if (HasReference(comp as Object, refData))
                    {
                        refData.ReferenceTypeMask |= mask(Target as TObj, comp);
                    }
                }
            }
        }

        private bool HasReference(Object comp, ReferenceGraph.Ref refData)
        {
            if (comp.GetType().Name != refData.ReferencingObjectType)
                return false;

            var so = new SerializedObject(comp);
            var prop = so.GetIterator();
            while (prop.Next(true))
            {
                if (prop.propertyType==SerializedPropertyType.ObjectReference && prop.objectReferenceValue == Target)
                    return true;
            }
            return false;
        }
    }

    public class ReferenceAnalyzerUnlock : ReferenceAnalyzerBase
    {
        public override Type AnalyzedType => typeof(BlueprintUnlockableFlag);

        public override void AnalyzeBlueprintReference(object asset, ReferenceGraph.Ref refData)
        {
            AnalyzeBlueprint<BlueprintUnlockableFlag, IUnlockableFlagReference>(asset, refData, (a, r) => (int)r.GetUsagesFor(a));
        }

        public override void AnalyzeSceneReferences(GameObject[] gameObjects, ReferenceGraph.Ref refData)
        {
            AnalyzeScene<BlueprintUnlockableFlag, IUnlockableFlagReference>(gameObjects, refData, (a, r) => (int)r.GetUsagesFor(a));
        }
    }
    public class ReferenceAnalyzerSummonPool : ReferenceAnalyzerBase
    {
        public override Type AnalyzedType => typeof(BlueprintSummonPool);

        public override void AnalyzeBlueprintReference(object asset, ReferenceGraph.Ref refData)
        {
            AnalyzeBlueprint<BlueprintUnlockableFlag, IUnlockableFlagReference>(asset, refData, (a, r) => (int)r.GetUsagesFor(a));
        }

        public override void AnalyzeSceneReferences(GameObject[] gameObjects, ReferenceGraph.Ref refData)
        {
            AnalyzeScene<BlueprintUnlockableFlag, IUnlockableFlagReference>(gameObjects, refData, (a, r) => (int)r.GetUsagesFor(a));
        }
    }
    public class ReferenceAnalyzerQuest : ReferenceAnalyzerBase
    {
        public override Type AnalyzedType => typeof(BlueprintQuest);

        public override void AnalyzeBlueprintReference(object asset, ReferenceGraph.Ref refData)
        {
            AnalyzeBlueprint<BlueprintQuest, IQuestReference>(asset, refData, (a, r) => (int)r.GetUsagesFor(a));
        }

        public override void AnalyzeSceneReferences(GameObject[] gameObjects, ReferenceGraph.Ref refData)
        {
            return; // never referenced in scenes
        }
    }
    public class ReferenceAnalyzerQuestObjective : ReferenceAnalyzerBase
    {
        public override Type AnalyzedType => typeof(BlueprintQuestObjective);

        public override void AnalyzeBlueprintReference(object asset, ReferenceGraph.Ref refData)
        {
            AnalyzeBlueprint<BlueprintQuestObjective, IQuestObjectiveReference>(asset, refData, (a, r) => (int)r.GetUsagesFor(a));
        }

        public override void AnalyzeSceneReferences(GameObject[] gameObjects, ReferenceGraph.Ref refData)
        {
            return; // never referenced in scenes
        }
    }

    public class ReferenceAnalyzerDialog : ReferenceAnalyzerBase
    {
        public override Type AnalyzedType => typeof(BlueprintDialog);

        public override void AnalyzeBlueprintReference(object asset, ReferenceGraph.Ref refData)
        {
            AnalyzeBlueprint<BlueprintDialog, IDialogReference>(asset, refData, (a, r) => (int)r.GetUsagesFor(a));
        }

        public override void AnalyzeSceneReferences(GameObject[] gameObjects, ReferenceGraph.Ref refData)
        {
            AnalyzeScene<BlueprintDialog, IDialogReference>(gameObjects, refData, (a, r) => (int)r.GetUsagesFor(a));
        }
    }

    public class ReferenceAnalyzerCutscene : ReferenceAnalyzerBase
    {
        public override Type AnalyzedType => typeof(Cutscene);

        public override void AnalyzeBlueprintReference(object asset, ReferenceGraph.Ref refData)
        {
            AnalyzeBlueprint<Cutscene, ICutsceneReference>(asset, refData, (a, r) => r.GetUsagesFor(a) ? 1 : 0);
        }

        public override void AnalyzeSceneReferences(GameObject[] gameObjects, ReferenceGraph.Ref refData)
        {
            AnalyzeScene<Cutscene, ICutsceneReference>(gameObjects, refData, (a, r) => r.GetUsagesFor(a) ? 1 : 0);
        }
    }
    public class ReferenceAnalyzerEnterPoint : ReferenceAnalyzerBase
    {
        public override Type AnalyzedType => typeof(BlueprintAreaEnterPoint);

        public override void AnalyzeBlueprintReference(object asset, ReferenceGraph.Ref refData)
        {
            AnalyzeBlueprint<BlueprintAreaEnterPoint, IAreaEnterPointReference>(asset, refData, (a, r) => r.GetUsagesFor(a) ? 1 : 0);
        }

        public override void AnalyzeSceneReferences(GameObject[] gameObjects, ReferenceGraph.Ref refData)
        {
            AnalyzeScene<BlueprintAreaEnterPoint, IAreaEnterPointReference>(gameObjects, refData, (a, r) => r.GetUsagesFor(a) ? 1 : 0);
        }
    }
    
    public class ReferenceAnalyzerEtude : ReferenceAnalyzerBase
    {
        public override Type AnalyzedType => typeof(BlueprintEtude);

        public override void AnalyzeBlueprintReference(object asset, ReferenceGraph.Ref refData)
        {
            AnalyzeBlueprint<BlueprintEtude, IEtudeReference>(asset, refData, (a, r) => (int)r.GetUsagesFor(a));
        }

        public override void AnalyzeSceneReferences(GameObject[] gameObjects, ReferenceGraph.Ref refData)
        {
            return; // never referenced in scenes
        }
    }
}