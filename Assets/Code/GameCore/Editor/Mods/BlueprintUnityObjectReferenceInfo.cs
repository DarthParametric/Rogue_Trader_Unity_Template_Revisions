using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.GameCore.Editor.Mods
{
	[Serializable]
	public class BlueprintUnityObjectReferenceInfoList
	{
		public List<BlueprintUnityObjectReferenceInfo> dataList = new();

		public void Add(BlueprintUnityObjectReferenceInfo info)
		{
			dataList.Add(info);
		}
	}

	[Serializable]
	public class BlueprintUnityObjectReferenceInfo
	{
		[SerializeField]
		public string BlueprintName;
		[SerializeField]
		public string BlueprintGuid;
		[SerializeField]
		public string FieldName;
		[SerializeField]
		public string ObjectPath;
		[SerializeField]
		public string Guid;
		[SerializeField]
		public long FileId;
		[SerializeField]
		public string ScriptGuid;
		[SerializeField]
		public long ScriptFileId;
		

		public BlueprintUnityObjectReferenceInfo(string blueprintName, string blueprintGuid, string fieldName, string objectPath, string guid, long fileId, string scriptGuid, long scriptFileId)
		{
			BlueprintName = blueprintName;
			BlueprintGuid = blueprintGuid;
			FieldName = fieldName;
			ObjectPath = objectPath;
			Guid = guid;
			FileId = fileId;
			ScriptGuid = scriptGuid;
			ScriptFileId = scriptFileId;
		}

		public override string ToString() => 
			($" bp name: {BlueprintName}, bp guid : {BlueprintGuid}, fieldName : {FieldName} path : {ObjectPath}, guid : {Guid}, Fileid: {FileId},scriptGuid : {ScriptGuid}, scriptFileId: {ScriptFileId}");
		
	}
}