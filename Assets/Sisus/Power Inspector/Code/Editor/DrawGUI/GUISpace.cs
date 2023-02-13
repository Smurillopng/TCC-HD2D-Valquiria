using System;
using UnityEngine;

namespace Sisus
{
	public static class GUISpace
	{
		private static Space current;

		public static Space Current
		{
			get
			{
				return current;
			}

			set
			{
				current = value;
			}
		}

		public static Vector2 ConvertPoint(Vector2 point, Space toSpace)
		{
			return ConvertPoint(point, Current, toSpace);
		}

		public static Vector2 ConvertPoint(Vector2 point, Space fromSpace, Space toSpace)
		{
			switch(fromSpace)
			{
				case Space.Window:
					switch(toSpace)
					{
						case Space.Window:
							// Window to window space:
							return point;
						case Space.Inspector:
							// Window to inspector space:
							// substract difference between window space and inspector space from point.
							return point - (InspectorUtility.InspectorBeginScreenPoint - DrawGUI.OnWindowBeginScreenPoint);
						case Space.Local:
							// Window to local space:
							// substract difference between window space and local space from point.
							return point - (GUIUtility.GUIToScreenPoint(Vector2.zero) - DrawGUI.OnWindowBeginScreenPoint);
						case Space.Screen:
							// Window to screen space:
							// add window begin screen point to window local point.
							return point + DrawGUI.OnWindowBeginScreenPoint;
						default:
							throw new IndexOutOfRangeException();
					}
				case Space.Inspector:
					switch(toSpace)
					{
						case Space.Window:
							// Inspector to window space:
							// substract difference between inspector space and window space from point.
							return point - (DrawGUI.OnWindowBeginScreenPoint - InspectorUtility.InspectorBeginScreenPoint);
						case Space.Inspector:
							// Inspector to inspector space:
							return point;
						case Space.Local:
							// Inspector to local space:
							// substract difference between inspector space and local space from point.
							return point - (GUIUtility.GUIToScreenPoint(Vector2.zero) - InspectorUtility.InspectorBeginScreenPoint);
						case Space.Screen:
							// Inspector to screen space:
							// add inspector begin screen point to inspector local point.
							return point + InspectorUtility.InspectorBeginScreenPoint;
						default:
							throw new IndexOutOfRangeException();
					}
				case Space.Local:
					switch(toSpace)
					{
						case Space.Window:
							// Local to window space:
							return GUIUtility.GUIToScreenPoint(point) - DrawGUI.OnWindowBeginScreenPoint;
						case Space.Inspector:
							// Local to inspector space:
							return GUIUtility.GUIToScreenPoint(point) - InspectorUtility.InspectorBeginScreenPoint;
						case Space.Local:
							// Local to local space:
							return point;
						case Space.Screen:
							// Local to screen space:
							return GUIUtility.GUIToScreenPoint(point);
						default:
							throw new IndexOutOfRangeException();
					}
				case Space.Screen:
					switch(toSpace)
					{
						case Space.Window:
							// Screen to window space:
							return point - DrawGUI.OnWindowBeginScreenPoint;
						case Space.Inspector:
							// Screen to inspector space:
							return point - InspectorUtility.InspectorBeginScreenPoint;
						case Space.Local:
							// Screen to local space:
							return GUIUtility.ScreenToGUIPoint(point);
						case Space.Screen:
							// Screen to screen space:
							return point;
						default:
							throw new IndexOutOfRangeException();
					}
				default:
					throw new IndexOutOfRangeException();
			}
		}

		public static Rect ConvertRect(Rect rect, Space toSpace)
		{
			return ConvertRect(rect, Current, toSpace);
		}

		public static Rect ConvertRect(Rect rect, Space fromSpace, Space toSpace)
		{
			rect.position = ConvertPoint(rect.position, fromSpace, toSpace);
			return rect;
		}
	}
}