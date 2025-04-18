#if UNITY_EDITOR && EDITOR_FIELDS
using System.Collections.Generic;
using System.Linq;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Designers.EventConditionActionSystem.Events;
using Kingmaker.Editor.EtudesViewer;


namespace Kingmaker.Assets.Code.Editor.EtudesViewer
{
    public class EtudesTreeLoader
    {
        public Dictionary<string, EtudeIdReferences> LoadedEtudes = new Dictionary<string, EtudeIdReferences>();
        public Dictionary<string, ConflictingGroupIdReferences> ConflictingGroups = new Dictionary<string, ConflictingGroupIdReferences>();
        
        private EtudesTreeLoader()
        {
            ReloadBlueprintsTree();
        }
        
        private static EtudesTreeLoader instance;
        
        public static EtudesTreeLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EtudesTreeLoader();
                    
                }
                return instance;
            }
        }

        public void ReloadBlueprintsTree()
        {
            LoadedEtudes = new Dictionary<string, EtudeIdReferences>();
            List<BlueprintEtude> blueprints = BlueprintsDatabase.LoadAllOfType<BlueprintEtude>().ToList();

            foreach (var blueprintEtude in blueprints)
            {
                AddEtudeToLoaded(blueprintEtude);
            }

            foreach (var loadedEtude in LoadedEtudes)
            {
                foreach (var etude in loadedEtude.Value.ChainedId)
                {
                    LoadedEtudes[etude].ChainedTo = loadedEtude.Key;
                }

                foreach (var etude in loadedEtude.Value.LinkedId)
                {
                    LoadedEtudes[etude].LinkedTo = loadedEtude.Key;
                }
            }
        }

        public void UpdateEtude(BlueprintEtude blueprintEtude)
        {
            if (LoadedEtudes.ContainsKey(blueprintEtude.AssetGuid))
            {
                UpdateEtudeData(blueprintEtude);
            }
            else
            {
                AddEtudeToLoaded(blueprintEtude);
            }
        }

        private void UpdateEtudeData(BlueprintEtude blueprintEtude)
        {
            EtudeIdReferences etudeIdReference = PrepareNewEtudeData(blueprintEtude);
            EtudeIdReferences oldEtude = LoadedEtudes[blueprintEtude.AssetGuid];
            //Remove old data
            if (etudeIdReference.ChainedTo!=oldEtude.ChainedTo && !string.IsNullOrEmpty(oldEtude.ChainedTo) && LoadedEtudes[oldEtude.ChainedTo].ChainedId.Contains(blueprintEtude.AssetGuid))
                LoadedEtudes[oldEtude.ChainedTo].ChainedId.Remove(blueprintEtude.AssetGuid);
            if (etudeIdReference.LinkedTo!=oldEtude.LinkedTo &&!string.IsNullOrEmpty(oldEtude.LinkedTo) && LoadedEtudes[oldEtude.LinkedTo].LinkedId.Contains(blueprintEtude.AssetGuid))
                LoadedEtudes[oldEtude.LinkedTo].LinkedId.Remove(blueprintEtude.AssetGuid);
            if (etudeIdReference.ParentId!=oldEtude.ParentId &&!string.IsNullOrEmpty(oldEtude.ParentId) && LoadedEtudes[oldEtude.ParentId].ChildrenId.Contains(blueprintEtude.AssetGuid))
                LoadedEtudes[oldEtude.ParentId].ChildrenId.Remove(blueprintEtude.AssetGuid);
            
            foreach (var etude in oldEtude.ChainedId)
            {
                if (!etudeIdReference.ChainedId.Contains(etude))
                {
                    LoadedEtudes[etude].ChainedTo = null;
                }
            }
            
            foreach (var etude in oldEtude.LinkedId)
            {
                if (!etudeIdReference.LinkedId.Contains(etude))
                {
                    LoadedEtudes[etude].LinkedTo = null;
                }
            }
            
            //Add new data

            etudeIdReference.ChildrenId = oldEtude.ChildrenId;
            etudeIdReference.ChainedTo = oldEtude.ChainedTo;
            etudeIdReference.LinkedTo = oldEtude.LinkedTo;
            if (!string.IsNullOrEmpty(oldEtude.ChainedTo))
                LoadedEtudes[etudeIdReference.ChainedTo].ChainedId.Add(blueprintEtude.AssetGuid);

            if (!string.IsNullOrEmpty(oldEtude.LinkedTo))
                LoadedEtudes[etudeIdReference.LinkedTo].LinkedId.Add(blueprintEtude.AssetGuid);

            LoadedEtudes[blueprintEtude.AssetGuid] = etudeIdReference;

            foreach (var etude in LoadedEtudes[blueprintEtude.AssetGuid].ChainedId)
            {
                LoadedEtudes[etude].ChainedTo = blueprintEtude.AssetGuid;
            }
            
            foreach (var etude in LoadedEtudes[blueprintEtude.AssetGuid].LinkedId)
            {
                LoadedEtudes[etude].LinkedTo = blueprintEtude.AssetGuid;
            }
        }

        private void AddEtudeToLoaded(BlueprintEtude blueprintEtude)
        {
            if (!LoadedEtudes.ContainsKey(blueprintEtude.AssetGuid))
            {
                EtudeIdReferences etudeIdReference = PrepareNewEtudeData(blueprintEtude);

                LoadedEtudes.Add(blueprintEtude.AssetGuid, etudeIdReference);
            }
        }

        public void RemoveEtudeData(string SelectedId)
        {
            if (!LoadedEtudes.ContainsKey(SelectedId))
                return;
            
            EtudeIdReferences etudeToRemove = LoadedEtudes[SelectedId];
            LoadedEtudes[etudeToRemove.ParentId].ChildrenId.Remove(SelectedId);

            if (!string.IsNullOrEmpty(etudeToRemove.LinkedTo))
            {
                LoadedEtudes[etudeToRemove.LinkedTo].LinkedId.Remove(SelectedId);
            }
            
            if (!string.IsNullOrEmpty(etudeToRemove.ChainedTo))
            {
                LoadedEtudes[etudeToRemove.ChainedTo].ChainedId.Remove(SelectedId);
            }

            foreach (var linkedTo in etudeToRemove.LinkedId)
            {
                LoadedEtudes[linkedTo].LinkedTo = null;
            }
            
            foreach (var chainedTo in etudeToRemove.ChainedId)
            {
                LoadedEtudes[chainedTo].ChainedTo = null;
            }
            
            LoadedEtudes.Remove(SelectedId);
        }

        private EtudeIdReferences PrepareNewEtudeData(BlueprintEtude blueprintEtude)
        {
            EtudeIdReferences etudeIdReference = new EtudeIdReferences();
            etudeIdReference.Name = blueprintEtude.name;
            etudeIdReference.ParentId = blueprintEtude.Parent?.Get()?.AssetGuid;
            etudeIdReference.CompleteParent = blueprintEtude.CompletesParent;
            etudeIdReference.Comment = blueprintEtude.Comment;
            etudeIdReference.Priority = blueprintEtude.Priority;
            etudeIdReference.Id = blueprintEtude.AssetGuid;

            foreach (var conflictingGroup in blueprintEtude.ConflictingGroups)
            {
                BlueprintScriptableObject conflictingGroupBlueprint = conflictingGroup.GetBlueprint();

                if (conflictingGroupBlueprint == null)
                    continue;
                
                etudeIdReference.ConflictingGroups.Add(conflictingGroupBlueprint.AssetGuid);

                if (!ConflictingGroups.ContainsKey(conflictingGroupBlueprint.AssetGuid))
                    ConflictingGroups.Add(conflictingGroupBlueprint.AssetGuid,new ConflictingGroupIdReferences());

                ConflictingGroups[conflictingGroupBlueprint.AssetGuid].Name = conflictingGroupBlueprint.name;

                if (!ConflictingGroups[conflictingGroupBlueprint.AssetGuid].Etudes.Contains(blueprintEtude.AssetGuid))
                    ConflictingGroups[conflictingGroupBlueprint.AssetGuid].Etudes.Add(blueprintEtude.AssetGuid);
            }

            if (blueprintEtude.LinkedAreaPart != null)
            {
                etudeIdReference.LinkedArea = blueprintEtude.LinkedAreaPart.AssetGuid;
                etudeIdReference.LinkedAreaName = blueprintEtude.LinkedAreaPart.name;
            }

            if (!string.IsNullOrEmpty(etudeIdReference.ParentId))
            {
                if (!LoadedEtudes.ContainsKey(blueprintEtude.Parent.Get().AssetGuid))
                    AddEtudeToLoaded(blueprintEtude.Parent.Get());

                if (!LoadedEtudes[etudeIdReference.ParentId].ChildrenId.Contains(blueprintEtude.AssetGuid))
                    LoadedEtudes[etudeIdReference.ParentId].ChildrenId.Add(blueprintEtude.AssetGuid);
            }

            etudeIdReference.HasSomeMechanics = blueprintEtude.GetComponents<BlueprintComponent>().Any();

            foreach (var chainedStart in blueprintEtude.StartsOnComplete)
            {
                if (chainedStart.Get() == null)
                    continue;

                etudeIdReference.ChainedId.Add(chainedStart.Get().AssetGuid);
            }

            foreach (var linkedStart in blueprintEtude.StartsWith)
            {
                if (linkedStart.Get() == null)
                    continue;
                    
                
                etudeIdReference.LinkedId.Add(linkedStart.Get().AssetGuid);
            }

            return etudeIdReference;
        }

        public List<T> GetAssetList<T>(string path) where T : SimpleBlueprint
        {
            return BlueprintsDatabase.LoadAllOfType<T>(path).ToList();
        }
    }
}
#endif