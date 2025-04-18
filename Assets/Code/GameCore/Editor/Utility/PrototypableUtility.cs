using System;
using System.Collections.Generic;
using Assets.Editor;
using Kingmaker.AI;
using Kingmaker.AI.Blueprints;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using UnityEditor;
using UnityEngine;

namespace Editors
{
    public static class PrototypableUtility
    {
        public static void CreateInheritedAsset(BlueprintScriptableObject blueprint)
        {
            // todo: [bp] fix this when prototyping is back
            var path = BlueprintsDatabase.GetAssetPath(blueprint);
            // serialize
            var json = JsonUtility.ToJson(blueprint);
            // make new ID
            json = json.Replace(blueprint.AssetGuid, Guid.NewGuid().ToString("N"));

            // deserialize back
            var copy = (BlueprintScriptableObject)JsonUtility.FromJson(json, blueprint.GetType());

            // set proto but do not sync
            copy.SetPrototype(blueprint, true, false);

            // wrap and save to a new path
            var newPath = BlueprintsDatabase.GenerateUniqueAssetPath(path);
            BlueprintsDatabase.CreateAsset(copy, newPath);

            // set proto (after saving so that it actually exists in the BD)
            copy.SetPrototype(blueprint, true);
            // BlueprintsDatabase.Save(copy.AssetGuid); can't do this, race with indexing server


            EditorApplication.delayCall += () => BlueprintProjectView.Ping(copy);
        }

        public static void SyncWithChildren(BlueprintScriptableObject blueprint)
        {
            try
            {
                if (EditorUtility.DisplayCancelableProgressBar("Syncing children...", "Searching assets", 0f))
                {
                    return;
                }

                int count = 0;
                var guids = BlueprintsDatabase.SearchByType(blueprint.GetType());
                foreach (var pair in guids)
                {
                    count++;
                    float progress = 1f * count / guids.Count;
                    if (EditorUtility.DisplayCancelableProgressBar("Syncing children...", "Looking for children",
                            progress))
                    {
                        return;
                    }

                    var asset = BlueprintsDatabase.LoadById<BlueprintScriptableObject>(pair.Item1);

                    while (asset && asset.PrototypeLink != null)
                    {
                        if (asset.PrototypeLink == blueprint)
                        {
                            BlueprintEditorWrapper.SyncWithProto(asset);
                            break;
                        }

                        asset = asset.PrototypeLink as BlueprintScriptableObject;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static void SyncWithChildrenDeep(BlueprintScriptableObject blueprint)
        {
            try
            {
                if (EditorUtility.DisplayCancelableProgressBar("Syncing children...", "Searching assets", 0f))
                {
                    return;
                }

                int count = 0;
                var guids = BlueprintsDatabase.SearchByType(blueprint.GetType());
                List<BlueprintScriptableObject> children = new();
                foreach (var pair in guids)
                {
                    count++;
                    float progress = 1f * count / guids.Count;
                    if (EditorUtility.DisplayCancelableProgressBar("Syncing children...", "Looking for children",
                            progress))
                    {
                        return;
                    }

                    var asset = BlueprintsDatabase.LoadById<BlueprintScriptableObject>(pair.Item1);

                    while (asset && asset.PrototypeLink != null)
                    {
                        if (asset.PrototypeLink == blueprint)
                        {
                            children.Add(asset);
                            BlueprintEditorWrapper.SyncWithProto(asset);
                            break;
                        }

                        asset = asset.PrototypeLink as BlueprintScriptableObject;
                    }
                }

                foreach (var child in children)
                {
                    SyncWithChildrenDeep(child);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static void UpdateAllBlueprintsOfType<T>(Action<T> blueprintUpdate) where T : BlueprintScriptableObject
        {
            try
            {
                if (EditorUtility.DisplayCancelableProgressBar("Updating blueprints...", "Searching assets", 0f))
                {
                    return;
                }

                var guids = BlueprintsDatabase.SearchByType(typeof(T));
                List<string> updatedAssetGuids = new();
                foreach (var pair in guids)
                {
                    float progress = 1f * updatedAssetGuids.Count / guids.Count;
                    if (EditorUtility.DisplayCancelableProgressBar("Updating blueprints...", "Updating",
                            progress))
                    {
                        return;
                    }

                    var asset = BlueprintsDatabase.LoadById<T>(pair.Item1);

                    if (updatedAssetGuids.Contains(asset.AssetGuid))
                    {
                        continue;
                    }

                    asset.BlueprintsUpdateUpward(updatedAssetGuids, blueprintUpdate);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void BlueprintsUpdateUpward<T>(
            this T blueprint, List<string> updatedGuids, Action<T> blueprintUpdate) where T : BlueprintScriptableObject
        {
            if (blueprint.PrototypeLink != null)
            {
                if (blueprint.PrototypeLink is T parent && !updatedGuids.Contains(parent.AssetGuid))
                {
                    parent.BlueprintsUpdateUpward(updatedGuids, blueprintUpdate);
                }

                BlueprintEditorWrapper.SyncWithProto(blueprint);
            }

            blueprintUpdate(blueprint);
            updatedGuids.Add(blueprint.AssetGuid);
        }
    }
}