using System;
using System.Text;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.Quests;
using Kingmaker.DialogSystem.Blueprints;

namespace Kingmaker.Editor.Validation
{
    public abstract class ReferenceValidatorBase
    {
       // public BlueprintScriptableObject Target;

        public abstract Type AnalyzedType { get; }

        public abstract void ValidateEntry(ReferenceGraph.Entry entry);
        
        protected static T LoadAsset<T>(string guid) where T : SimpleBlueprint
        {
            return BlueprintsDatabase.LoadById<T>(guid);
        }
    }

    public class ReferenceValidatorUnlocks : ReferenceValidatorBase
    {
        public override Type AnalyzedType => typeof(BlueprintUnlockableFlag);

        public override void ValidateEntry(ReferenceGraph.Entry entry)
        {
            var found = (UnlockableFlagReferenceType)entry.FullReferencesMask;
            entry.ValidationState = ReferenceGraph.ValidationStateType.Normal;

            var res = new StringBuilder();
            if (found == UnlockableFlagReferenceType.None)
            {
                res.AppendLine($"Flag {entry.ObjectName} : cannot find actual usages");
	            entry.ValidationState = entry.References.Count > 0 ? ReferenceGraph.ValidationStateType.Error : ReferenceGraph.ValidationStateType.Warning;
            }
			if (found.HasFlag(UnlockableFlagReferenceType.Unlock) && !found.HasFlag(UnlockableFlagReferenceType.Check) && !found.HasFlag(UnlockableFlagReferenceType.CheckValue))
            {
                res.AppendLine($"Flag {entry.ObjectName} : is unlocked but never checked");
                entry.ValidationState = ReferenceGraph.ValidationStateType.Error;
            }
            if (!found.HasFlag(UnlockableFlagReferenceType.Unlock) &&
                (found.HasFlag(UnlockableFlagReferenceType.Check) || found.HasFlag(UnlockableFlagReferenceType.CheckValue)))
            {
                res.AppendLine($"Flag {entry.ObjectName} : is checked but never unlocked");
                entry.ValidationState = ReferenceGraph.ValidationStateType.Error;
            }
            if (!found.HasFlag(UnlockableFlagReferenceType.Unlock) &&
                (found.HasFlag(UnlockableFlagReferenceType.Lock)))
            {
                res.AppendLine($"Flag {entry.ObjectName} : is locked but never unlocked");
                entry.ValidationState = ReferenceGraph.ValidationStateType.Error;
            }
            if (!found.HasFlag(UnlockableFlagReferenceType.SetValue) &&
                (found.HasFlag(UnlockableFlagReferenceType.CheckValue)))
            {
                res.AppendLine($"Flag {entry.ObjectName} : has value checks, but it is never changed");
                entry.ValidationState = ReferenceGraph.ValidationStateType.Error;
            }
            if (found.HasFlag(UnlockableFlagReferenceType.SetValue) &&
                !(found.HasFlag(UnlockableFlagReferenceType.CheckValue)))
            {
                res.AppendLine($"Flag {entry.ObjectName} : has value changes, but it is never checked");
                entry.ValidationState = ReferenceGraph.ValidationStateType.Error;
            }

            entry.ValidationResult = res.ToString();
        }
    }

    public class ReferenceValidatorQuests : ReferenceValidatorBase
    {
        public override Type AnalyzedType => typeof(BlueprintQuest);

        public override void ValidateEntry(ReferenceGraph.Entry entry)
        {
            var found = (QuestReferenceType)entry.FullReferencesMask;
            entry.ValidationState = ReferenceGraph.ValidationStateType.Normal;

            var res = new StringBuilder();

            if (found == QuestReferenceType.None)
            {
                res.AppendLine($"Quest {entry.ObjectName} : cannot find actual usages");
	            entry.ValidationState = entry.References.Count > 0 ? ReferenceGraph.ValidationStateType.Error : ReferenceGraph.ValidationStateType.Warning;
            }

			if (found != QuestReferenceType.None && !found.HasFlag(QuestReferenceType.Complete))
            {
                res.AppendLine($"Quest {entry.ObjectName} : cannot find completing objectives");
                entry.ValidationState = ReferenceGraph.ValidationStateType.Error;
            }

            entry.ValidationResult = res.ToString();
        }
    }

    public class ReferenceValidatorSummonPool : ReferenceValidatorBase
    {
        public override Type AnalyzedType => typeof(BlueprintQuest);

        public override void ValidateEntry(ReferenceGraph.Entry entry)
        {
            var found = (QuestReferenceType)entry.FullReferencesMask;
            entry.ValidationState = ReferenceGraph.ValidationStateType.Normal;
        }
    }

    public class ReferenceValidatorQuestObjectives : ReferenceValidatorBase
    {
        public override Type AnalyzedType => typeof(BlueprintQuestObjective);

        public override void ValidateEntry(ReferenceGraph.Entry entry)
        {
	        var obj = LoadAsset<BlueprintQuestObjective>(entry.ObjectGuid);


			var found = (QuestObjectiveReferenceType)entry.FullReferencesMask;
            entry.ValidationState = ReferenceGraph.ValidationStateType.Normal;

            var res = new StringBuilder();
            if (found == QuestObjectiveReferenceType.None)
            {
                res.AppendLine($"Objective {entry.ObjectName} : cannot find actual usages");
	            entry.ValidationState = entry.References.Count > 0 ? ReferenceGraph.ValidationStateType.Error : ReferenceGraph.ValidationStateType.Warning;
            }
			if (found != QuestObjectiveReferenceType.None && !found.HasFlag(QuestObjectiveReferenceType.Give))
            {
                res.AppendLine($"Objective {entry.ObjectName} : is never given");
                entry.ValidationState = ReferenceGraph.ValidationStateType.Error;
            }
            if (found != QuestObjectiveReferenceType.None && !obj.IsAddendum && !found.HasFlag(QuestObjectiveReferenceType.Complete) && !found.HasFlag(QuestObjectiveReferenceType.Fail))
            {
                res.AppendLine($"Objective {entry.ObjectName} : is never completed or failed");
                entry.ValidationState = ReferenceGraph.ValidationStateType.Error;
            }

            entry.ValidationResult = res.ToString();
        }
    }

    public class ReferenceValidatorDialog : ReferenceValidatorBase
    {
        public override Type AnalyzedType => typeof(BlueprintDialog);

        public override void ValidateEntry(ReferenceGraph.Entry entry)
        {
	        var found = DialogReferenceType.None;

			// ignore dialog references from this same dialog
			foreach (var refData in entry.References)
	        {
		        if (refData.RefChasingAssetGuid != entry.ObjectGuid)
		        {
			        found |= (DialogReferenceType)refData.ReferenceTypeMask;
		        }
	        }

            entry.ValidationState = ReferenceGraph.ValidationStateType.Normal;

            var res = new StringBuilder();
            if (found == DialogReferenceType.None)
            {
                res.AppendLine($"Dialog {entry.ObjectName} : cannot find actual usages");
                entry.ValidationState = entry.References.Count>0?ReferenceGraph.ValidationStateType.Error:ReferenceGraph.ValidationStateType.Warning;
            }

            entry.ValidationResult = res.ToString();
        }
    }

    public class ReferenceValidatorCutscene : ReferenceValidatorBase
    {
        public override Type AnalyzedType => typeof(Cutscene);

        public override void ValidateEntry(ReferenceGraph.Entry entry)
        {
            var found = entry.FullReferencesMask;
            entry.ValidationState = ReferenceGraph.ValidationStateType.Normal;

            var res = new StringBuilder();
            if (found == 0)
            {
                res.AppendLine($"Cutscene {entry.ObjectName} : not played anywhere");
                entry.ValidationState = entry.References.Count > 0 ? ReferenceGraph.ValidationStateType.Error : ReferenceGraph.ValidationStateType.Warning;
            }

            entry.ValidationResult = res.ToString();
        }
    }

    public class ReferenceValidatorEnterPoint : ReferenceValidatorBase
    {
        public override Type AnalyzedType => typeof(BlueprintAreaEnterPoint);

        public override void ValidateEntry(ReferenceGraph.Entry entry)
        {
            var found = entry.FullReferencesMask;
            entry.ValidationState = ReferenceGraph.ValidationStateType.Normal;

            var res = new StringBuilder();
            if (found == 0)
            {
                res.AppendLine($"Enter point {entry.ObjectName} : not used anywhere");
                entry.ValidationState = entry.References.Count > 0 ? ReferenceGraph.ValidationStateType.Error : ReferenceGraph.ValidationStateType.Warning;
            }

            entry.ValidationResult = res.ToString();
        }
    }
    
    public class ReferenceValidatorEtude : ReferenceValidatorBase
    {
        public override Type AnalyzedType => typeof(BlueprintEtude);

        public override void ValidateEntry(ReferenceGraph.Entry entry)
        {
            var obj = LoadAsset<BlueprintEtude>(entry.ObjectGuid);
            
            var found = (EtudeReferenceType)entry.FullReferencesMask;
            entry.ValidationState = ReferenceGraph.ValidationStateType.Normal;

            var res = new StringBuilder();
            if (found == EtudeReferenceType.None)
            {
                res.AppendLine($"Etude {entry.ObjectName} : cannot find actual usages");
	            entry.ValidationState = entry.References.Count > 0 ? ReferenceGraph.ValidationStateType.Error : ReferenceGraph.ValidationStateType.Warning;
            }

            entry.ValidationResult = res.ToString();
        }
    }
}