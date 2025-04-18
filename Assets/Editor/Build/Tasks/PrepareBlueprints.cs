using OwlcatModification.Editor.Build.Context;
using System.IO;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;

namespace OwlcatModification.Editor.Build.Tasks
{
	public class PrepareBlueprints : IBuildTask
	{
#pragma warning disable 649
		[InjectContext(ContextUsage.In)]
		private IBuildParameters m_BuildParameters;
		
		[InjectContext(ContextUsage.In)]
		private IModificationParameters m_ModificationParameters;
#pragma warning restore 649
		
		public int Version
			=> 1;
		
		public ReturnCode Run()
		{
			string originDirectory = m_ModificationParameters.BlueprintsPath;
			string destinationDirectory = m_BuildParameters.GetOutputFilePathForIdentifier(BuilderConsts.OutputBlueprints);
			BuilderUtils.CopyFilesWithFoldersStructure(
				originDirectory, destinationDirectory, SearchOption.AllDirectories, i => i.EndsWith(".jbp") || i.EndsWith(".patch") || i.EndsWith(".jbp_patch"));
			return ReturnCode.Success;
		}
	}
}