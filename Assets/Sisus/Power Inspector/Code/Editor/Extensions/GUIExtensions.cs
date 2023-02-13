using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public static class GUIExtensions
	{
		public static void SetAllTextColors([NotNull]this GUIStyle guiStyle, Color color)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(guiStyle != null);
			#endif

			guiStyle.normal.textColor = color;
			guiStyle.onNormal.textColor = color;
			guiStyle.hover.textColor = color;
			guiStyle.onHover.textColor = color;
			guiStyle.focused.textColor = color;
			guiStyle.onFocused.textColor = color;
			guiStyle.active.textColor = color;
			guiStyle.onActive.textColor = color;
		}

		public static void SetAllBackgrounds([NotNull]this GUIStyle guiStyle, Texture2D texture)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(guiStyle != null);
			#endif

			guiStyle.normal.background = texture;
			guiStyle.onNormal.background = texture;
			guiStyle.hover.background = texture;
			guiStyle.onHover.background = texture;
			guiStyle.focused.background = texture;
			guiStyle.onFocused.background = texture;
			guiStyle.active.background = texture;
			guiStyle.onActive.background = texture;
		}
	}
}