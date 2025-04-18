using System;
using UnityEngine;

namespace Kingmaker.Editor.Cutscenes
{
	public static class SetColor
	{
		private class BackgroundSetter : IDisposable
		{
			private readonly Color m_Color;

			public BackgroundSetter(Color c)
			{
				m_Color = GUI.backgroundColor;
				GUI.backgroundColor = c;
			}

			public void Dispose()
				=> GUI.backgroundColor = m_Color;
		}

		public static IDisposable Background(Color c)
			=> new BackgroundSetter(c);
	}
}