using System;
using Kingmaker.ElementsSystem;

namespace Kingmaker.Editor.Elements
{
	internal static class ElementsHelper
	{
		public static string GetCaptionSafe(this Element action)
		{
			try
			{
				return action?.GetCaption() ?? "<NULL>";
			}
			catch (Exception e)
			{
				PFLog.Default.Exception(e);
				return $"Exception : {e.Message}";
			}
		}

		public static string GetDescriptionSafe(this Element action)
		{
			try
			{
				return action?.GetDescription() ?? "<NULL>";
			}
			catch (Exception e)
			{
				PFLog.Default.Exception(e);
				return $"Exception : {e.Message}";
			}
		}
	}
}