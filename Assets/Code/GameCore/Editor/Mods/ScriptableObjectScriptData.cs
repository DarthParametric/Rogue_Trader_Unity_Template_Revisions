using System;
using UnityEngine;

namespace Code.GameCore.Editor.Mods
{
	[Serializable]
	public class ScriptableObjectScriptData
	{
		[SerializeField]
		public string Guid;
		[SerializeField]
		public long FileId;

		public ScriptableObjectScriptData(string guid, long fileId)
		{
			Guid = guid;
			FileId = fileId;
		}
	}
}