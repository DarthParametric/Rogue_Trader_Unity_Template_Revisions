using System.Collections.Generic;
using System.Linq;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.Blueprints;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.View.Spawners;
using Kingmaker.Visual.Animation.Kingmaker;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public static class CommandTestHelpers
	{
		public const HideFlags HideFlags = UnityEngine.HideFlags.DontSave | UnityEngine.HideFlags.NotEditable;

		public static void SpawnUnitsFromSpawners(IEnumerable<UnitSpawnerBase> spawners)
		{
			if (!spawners.Any())
			{
				PFLog.Cutscene.Warning("No spawners found in spawn action.");
				return;
			}

			var objectsToFrame = new List<Object>(spawners.Count());
			foreach (var spawner in spawners)
			{
				var unitVisual = SpawnUnitFromSpawner(spawner);
				if (unitVisual == null)
				{
					PFLog.Cutscene.Warning("Failed to get spawner game object from spawner.");
					continue;
				}
				objectsToFrame.Add(unitVisual as Object);

				var animationManager = unitVisual.GetComponentInChildren<UnitAnimationManager>();
				if (animationManager == null)
				{
					PFLog.Cutscene.Warning("UnitAnimationManager not found for " + unitVisual.name);
					continue;
				}

				var animationAction = animationManager.AnimationSet.GetAction(UnitAnimationType.VariantIdle);
				if (animationAction != null && animationAction.ClipWrappers.Any())
				{
					var animationClip = animationAction.ClipWrappers.First().AnimationClip;
					PlayAnimation(animationManager.gameObject, animationClip, DirectorWrapMode.Loop);
				}
				else
				{
					PFLog.Cutscene.Warning("No idle animation found for " + unitVisual.name);
				}
			}

			if (objectsToFrame.Count > 0)
			{
				Selection.objects = objectsToFrame.ToArray();
				SceneView.lastActiveSceneView.FrameSelected();
			}
		}

		public static GameObject? SpawnUnitFromSpawner(UnitSpawnerBase spawner)
		{
			var unit = spawner.Blueprint;
			if (unit == null)
			{
				PFLog.Cutscene.Warning("No unit blueprint in spawner.");
				return null;
			}

			var unitVisual = SpawnUnitVisualIfNeeded(unit, spawner.gameObject);
			if (unitVisual == null)
			{
				return null;
			}

			var follower = unitVisual.GetComponent<FollowTransform>();
			if (follower != null)
			{
				// Disconnect from any transform to reflect spawner position
				follower.UpdatePosition = null;
				follower.UpdateRotation = null;
			}

			return unitVisual;
		}

		public static GameObject? SpawnUnitVisualIfNeeded(BlueprintUnit unit, GameObject spawner)
		{
			var unitEntityView = spawner.GetComponentInChildren<UnitEntityView>();
			if (unitEntityView != null)
			{
				return unitEntityView.gameObject;
			}

			var prefab = unit.Prefab;
			if (prefab == null)
			{
				PFLog.Cutscene.Warning("No prefab in unit blueprint.");
				return null;
			}

			unitEntityView = prefab.LoadObject() as UnitEntityView;
			if (unitEntityView == null)
			{
				PFLog.Cutscene.Warning("Failed to load object from prefab.");
				return null;
			}

			var unitVisual = Object.Instantiate(unitEntityView.gameObject, spawner.transform);
			if (unitVisual == null)
			{
				PFLog.Cutscene.Warning("Failed to instantiate prefab from unit blueprint.");
				return null;
			}

			unitVisual.transform.ResetPosition();
			unitVisual.transform.ResetRotation();
			unitVisual.ForAllChildren(o => o.hideFlags = HideFlags);

			return unitVisual;
		}

		public static void PlayAnimation(GameObject animatorObject, AnimationClip animationClip, DirectorWrapMode mode)
		{
			var animator = animatorObject.GetComponent<Animator>();
			if (animator == null)
			{
				PFLog.Cutscene.Warning("Failed to get animator for " + animatorObject.name);
				return;
			}

			var director = animatorObject.GetComponent<PlayableDirector>();
			if (director == null)
			{
				director = animatorObject.AddComponent<PlayableDirector>();
			}
			director.extrapolationMode = mode;

			var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
			var track = timeline.CreateTrack<AnimationTrack>();
			track.applyAvatarMask = false;
			track.CreateClip(animationClip);

			director.playableAsset = timeline;
			director.SetGenericBinding(track, animator);
			director.Play();

			track.hideFlags = HideFlags;
			timeline.hideFlags = HideFlags;
			animatorObject.ForAllChildren(o => o.hideFlags = HideFlags);
		}
	}
}