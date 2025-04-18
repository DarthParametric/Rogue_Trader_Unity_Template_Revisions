using Owlcat.Editor.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace Kingmaker.Editor.Cutscenes
{
	public class GateReactivationDrawer
	{
		readonly List<Vector2> m_StartPoints = new List<Vector2>();
		readonly List<Vector2> m_EndPoints = new List<Vector2>();

		public void Add(Vector2 from, Rect to)
		{
			// select to point at least 24px removed vertically
			m_StartPoints.Add(from);
			var endPoint = to.center;
			if (Mathf.Abs(endPoint.y - from.y) < 24)
			{
				var sign = Mathf.Sign(endPoint.y - from.y);
				endPoint.y = from.y + sign * 24;
			}
			m_EndPoints.Add(endPoint);
		}

		public void Clear()
		{
			m_StartPoints.Clear();
			m_EndPoints.Clear();
		}

		public void Draw()
		{
			for (int ii = 0; ii < m_EndPoints.Count; ii++)
			{
				var from = m_StartPoints[ii];
				var to = m_EndPoints[ii];

				var up = from.y > to.y;
				if (up)
				{
					var r = new Rect(from.x-8, to.y+8, 16, from.y-to.y-8);
                    OwlcatEditorStyles.Instance.ReactivateLinkUp.Draw(r);
				}
				else
				{
					var r = new Rect(from.x - 8, from.y, 16, to.y - from.y - 8);
                    OwlcatEditorStyles.Instance.ReactivateLinkDown.Draw(r);
				}

				var left = from.x > to.x;
				if (left)
				{
					var r = new Rect(to.x, to.y - 8, from.x - to.x + 8, 16);
					var style = up ? OwlcatEditorStyles.Instance.ReactivateLinkUpLeft : OwlcatEditorStyles.Instance.ReactivateLinkDownLeft;
					style.Draw(r);
				}
				else
				{
					var r = new Rect(from.x-8, to.y - 8, to.x - from.x + 8, 16);
					var style = up ? OwlcatEditorStyles.Instance.ReactivateLinkUpRight : OwlcatEditorStyles.Instance.ReactivateLinkDownRight;
					style.Draw(r);
				}
			}
		}
	}
}