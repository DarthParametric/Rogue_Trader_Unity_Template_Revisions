using System.Collections.Generic;
using Kingmaker.Modding;
using UnityEngine;

namespace OwlcatModification.Editor
{
	[CreateAssetMenu(menuName = "Blueprints' Patches")]
	public class BlueprintPatches : ScriptableObject
	{
		public List<OwlcatModificationSettings.BlueprintChangeData> Entries =
			new List<OwlcatModificationSettings.BlueprintChangeData>();
	}
}