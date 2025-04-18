using System;
using System.Collections.Generic;
using System.Linq;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class CreatorPicker: ValuePicker<AssetCreatorBase>
	{
		private static List<AssetCreatorBase> s_AllCreators;

	    public static AssetCreatorBase GetCreatorForType(Type type)
	    {
	        EnsureCreators();
	        return s_AllCreators.FirstOrDefault(c => c.CanCreateAssetsOfType(type));
	    }

		public static void Button(
			string buttonText,
			Action<AssetCreatorBase> callback,
			bool showNow = false,
			params GUILayoutOption[] options)
		{
		    EnsureCreators();

		    Button(
				GetWindow<CreatorPicker>,
				buttonText,
				()=>s_AllCreators,
				callback,
				showNow,
				GUI.skin.button,
				options
			);
		}

	    private static void EnsureCreators()
	    {
	        if (s_AllCreators == null || s_AllCreators.Count == 0 || !s_AllCreators[0])
	        {
	            var types = TypeCache.GetTypesDerivedFrom(typeof(AssetCreatorBase)).Where(t => !t.IsAbstract);
	            s_AllCreators = types.Select(CreateInstance).Cast<AssetCreatorBase>().OrderBy(c => c.CreatorName).ToList();
	        }
	    }

	    protected override string GetValueName(AssetCreatorBase value)
		{
			return value.CreatorName;
		}
	}
}