using UnityEngine;

namespace Sisus
{
	/// <summary> Utility class for setting text color of inspector titlebar in active GUI.skin. </summary>
	public static class GUIStyleUtility
	{
		private static GUIStyle titlebarTextOriginal;

		/// <summary> Sets the inspector titlebar text in current GUI.skin to using the given color. </summary>
		public static void SetInspectorTitlebarTextColor(Color color)
		{
			var guiStyle = GUI.skin.GetStyle("IN TitleText");

			if(titlebarTextOriginal == null)
			{
				titlebarTextOriginal = new GUIStyle(guiStyle);
			}

			//if(!titlebarTextNormalColor.HasValue)
			//{
			//	titlebarTextNormalColor = guiStyle.normal.textColor;
			//}

			guiStyle.normal.textColor = color;
			guiStyle.active.textColor = color;
			guiStyle.hover.textColor = color;
			guiStyle.focused.textColor = color;
		}

		/// <summary> Sets the inspector titlebar text in current GUI.skin to using the given color. </summary>
		public static void SetInspectorTitlebarPinged()
		{
			var guiStyle = GUI.skin.GetStyle("IN TitleText");
			guiStyle.SetAllBackgrounds(InspectorPreferences.Styles.PingedHeader.normal.background);
		}

		/// <summary> Sets the inspector titlebar text in current GUI.skin to using the given color. </summary>
		public static void ResetInspectorTitlebarBackground()
		{
			var guiStyle = GUI.skin.GetStyle("IN TitleText");
			guiStyle.SetAllBackgrounds(null);
		}

		public static void SetInspectorTitlebarToggleColor(Color color)
		{
			var guiStyle = GUI.skin.toggle;

			guiStyle.normal.textColor = color;
			guiStyle.active.textColor = color;
		}

		/// <summary> Resets the inspector titlebar text back to using its normal color. </summary>
		public static void ResetInspectorTitlebarTextColor()
		{
			if(titlebarTextOriginal == null)
			{
				#if DEV_MODE
				Debug.LogWarning("ResetInspectorTitlebarTextColor called but titlebarTextOriginal was null (SetInspectorTitlebarTextColor has not been called yet)");
				#endif
				return;
			}

			var guiStyle = GUI.skin.GetStyle("IN TitleText");
			//var color = InspectorUtility.Preferences.theme.PrefixIdleText;
			//guiStyle.normal.textColor = color;
			//guiStyle.active.textColor = color;

			guiStyle.normal.textColor = titlebarTextOriginal.normal.textColor;
			guiStyle.active.textColor = titlebarTextOriginal.active.textColor;
			guiStyle.hover.textColor = titlebarTextOriginal.hover.textColor;
			guiStyle.focused.textColor = titlebarTextOriginal.focused.textColor;
		}

		public static void ResetInspectorTitlebarToggleColor()
		{
			var guiStyle = GUI.skin.toggle;
			var color = InspectorUtility.Preferences.theme.PrefixIdleText;
			guiStyle.normal.textColor = color;
			guiStyle.active.textColor = color;
		}
	}
}