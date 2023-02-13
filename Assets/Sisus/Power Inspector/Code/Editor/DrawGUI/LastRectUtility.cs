using UnityEngine;
using System.Reflection;

namespace Sisus
{
	/// <summary> Utility class for setting text color of inspector titlebar in active GUI.skin. </summary>
	public static class LastRectUtility
	{
		public static readonly Rect DefaultRect = new Rect(0f, 0f, 1f, 1f);

		private static FieldInfo cursorField;
		private static FieldInfo currentField;
		private static FieldInfo topLevelField;

		private static bool setupSuccessful;

		static LastRectUtility()
		{
			currentField = typeof(GUILayoutUtility).GetField("current", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			if(currentField == null)
			{
				#if DEV_MODE
				Debug.LogWarning("LastRectUtility Setup failed: Could not find field GUILayoutUtility.current.");
				#endif
				return;
			}

			var layoutCache = currentField.GetValue(null);
			if(layoutCache == null)
			{
				#if DEV_MODE
				Debug.LogWarning("LastRectUtility Setup failed: GUILayoutUtility.current value was null.");
				#endif
				return;
			}

			var layoutCacheType = layoutCache.GetType();
			topLevelField = layoutCacheType.GetField("topLevel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if(topLevelField == null)
			{
				#if DEV_MODE
				Debug.LogWarning("LastRectUtility Setup failed: Could not find field LayoutCache.topLevel.");
				#endif
				return;
			}

			var topLevelGroup = topLevelField.GetValue(layoutCache);
			if(topLevelGroup == null)
			{
				#if DEV_MODE
				Debug.LogWarning("LastRectUtility Setup failed: GUILayoutUtility.current.topLevelField value was null.");
				#endif
				return;
			}

			cursorField = topLevelGroup.GetType().GetField("m_Cursor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if(cursorField == null)
			{
				#if DEV_MODE
				Debug.LogWarning("LastRectUtility Setup failed: Could not find field GUILayoutGroup.m_Cursor");
				#endif
				return;
			}

			setupSuccessful = true;
		}

		public static bool TryGetLastRect(out Rect lastRect)
		{
			switch (Event.current.type)
			{
				case EventType.Layout:
				case EventType.Used:
					lastRect = DefaultRect;
					return false;
                default:
					if(!setupSuccessful)
					{
						lastRect = GUILayoutUtility.GetLastRect();
						return lastRect != DefaultRect;
					}
					var layoutCache = currentField.GetValue(null);
					if(layoutCache == null)
					{
						lastRect = GUILayoutUtility.GetLastRect();
						return lastRect != DefaultRect;
					}

					var topLevelGroup = topLevelField.GetValue(layoutCache);
					if(topLevelGroup == null)
					{
						lastRect = GUILayoutUtility.GetLastRect();
						return lastRect != DefaultRect;
					}

					int cursor = (int)cursorField.GetValue(topLevelGroup);
					if(cursor <= 0)
					{
						lastRect = DefaultRect;
						return false;
					}

					lastRect = GUILayoutUtility.GetLastRect();
					return true;
			}
		}
	}
}