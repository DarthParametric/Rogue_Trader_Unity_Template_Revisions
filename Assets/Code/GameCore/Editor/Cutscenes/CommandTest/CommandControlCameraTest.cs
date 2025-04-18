using System;
using System.Linq;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Cutscenes.Commands;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.Utility;
using Kingmaker.View;
using Owlcat.Runtime.Core.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public class CommandControlCameraTest : ICommandTest
	{
		private const float MaxFov = 30.0f;
		private const float MinFov = 17.5f;
		private const float NearPlane = 1.0f;
		private const float FarPlane = 400.0f;
		private const string GameCameraRigTest = "GameCameraRigTest";

		private static readonly Vector3 AttachPoint = new(0.0f, 18.724f, 20.112f);
		private static readonly Quaternion AttachRotation = Quaternion.Euler(43.068f, 180.0f, 0.0f);
		private static readonly Quaternion RotationOffset = Quaternion.Euler(-0.115f, 0.0f, 0.0f);

		public Action? GetForCommand(CommandBase command)
		{
			if (command is not CommandControlCamera commandControlCamera)
			{
				return null;
			}

			return () => Test(commandControlCamera);
		}

		private static void Test(CommandControlCamera commandControlCamera)
		{
			if (!commandControlCamera.Move)
			{
				PFLog.Cutscene.Warning("CommandControlCamera move is turned off.");
				return;
			}

			if (commandControlCamera.MoveTarget is not LocatorPosition locatorPosition)
			{
				PFLog.Cutscene.Warning("CommandControlCamera move target is not a locator.");
				return;
			}

			var locator = locatorPosition.Locator.FindViewInEditor() as LocatorView;
			if (locator == null)
			{
				PFLog.Cutscene.Warning("Cannot find move target locator in scene.");
				return;
			}

			var followTransform = GetOrCreateGameCameraRig(locator.gameObject.scene);

			followTransform.UpdatePosition = Vector3Getter.Get(commandControlCamera.MoveTarget);

			if (commandControlCamera.Rotate)
			{
				var floatGetter = FloatGetter.Get(commandControlCamera.RotateTarget);
				followTransform.UpdateRotation = floatGetter == null
					? null
					: () => Quaternion.Euler(0, floatGetter(), 0);
			}
			else
			{
				followTransform.UpdateRotation = null;
			}

			if (commandControlCamera.Zoom)
			{
				var followFov = followTransform.gameObject.AddComponent<FollowFov>();
				followFov.Camera = followTransform.gameObject.GetComponentInChildren<Camera>();
				followFov.UpdateFov = () => Mathf.Lerp(MaxFov, MinFov, commandControlCamera.ZoomTarget);
			}

			followTransform.gameObject.ForAllChildren(o => o.hideFlags = CommandTestHelpers.HideFlags);
		}

		private static FollowTransform GetOrCreateGameCameraRig(Scene scene)
		{
			GameObject? cameraRig = scene.GetRootGameObjects().FirstOrDefault(o => o.name == GameCameraRigTest);
			if (cameraRig != null)
			{
				Object.DestroyImmediate(cameraRig);
			}

			cameraRig = CreateGameCameraRigTest();
			SceneManager.MoveGameObjectToScene(cameraRig, scene);

			var followTransform = cameraRig.GetComponent<FollowTransform>();
			if (followTransform == null)
			{
				followTransform = cameraRig.AddComponent<FollowTransform>();
			}

			return followTransform;
		}

		private static GameObject CreateGameCameraRigTest()
		{
			var cameraRig = CreateGameObject(GameCameraRigTest, null);

			var correct180 = CreateGameObject("Correct180", cameraRig);
			correct180.localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);

			var attachPoint = CreateGameObject("attachPointTest", correct180);
			attachPoint.localPosition = AttachPoint;
			attachPoint.localRotation = AttachRotation;

			var mainCamera = CreateGameObject("MainCameraTest", attachPoint);
			mainCamera.localRotation = RotationOffset;

			var camera = mainCamera.gameObject.AddComponent<Camera>();
			camera.nearClipPlane = NearPlane;
			camera.farClipPlane = FarPlane;
			camera.fieldOfView = MaxFov;

			return cameraRig.gameObject;
		}

		private static Transform CreateGameObject(string name, Transform? parent)
		{
			var go = new GameObject(name);
			var transform = go.transform;
			transform.parent = parent;
			transform.ResetAll();
			return transform;
		}
	}
}