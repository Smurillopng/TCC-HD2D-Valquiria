using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	#if DEV_MODE && UNITY_EDITOR
	#if UNITY_2018_3_OR_NEWER
	[ExecuteAlways]
	#else
	[ExecuteInEditMode]
	#endif
	#endif
	[CreateAssetMenu]
	public class GUIStyles : ScriptableObject
	{
		public GUIStyle[] styles = new GUIStyle[0];
		
		[Pure]
		public GUISkin AddTo(GUISkin guiSkin)
		{
			var copy = Instantiate(guiSkin);
			copy.customStyles = guiSkin.customStyles.Add(styles);
			return copy;
		}
		
		#if DEV_MODE && UNITY_EDITOR
		public GUISkin getStylesFrom;

		[ContextMenu("Get Unique Custom Styles"), UsedImplicitly]
		private void GetUniqueCustomStyles()
		{
			DrawGUI.OnNextBeginOnGUI(GetUniqueCustomStylesNow, true);
		}

		private void GetUniqueCustomStylesNow()
		{
			GetUniqueCustomStyles(GUI.skin, getStylesFrom);
		}
		
		public void GetUniqueCustomStyles(GUISkin editorSkin, GUISkin customSkin)
		{
			UnityEditor.Undo.RecordObject(this, "Set Styles");

			var unique = new List<GUIStyle>();

			var editorStyles = editorSkin.customStyles;
			var customStyles = customSkin.customStyles;

			for(int c = 0, ccount = customStyles.Length; c < ccount; c++)
			{
				var customStyle = customStyles[c];

				bool found = false;
				for(int e = customStyles.Length - 1; e >= 0; e--)
				{
					var editorStyle = editorStyles[e];
					if(string.Equals(customStyle.name, editorStyle.name))
					{
						found = true;
						break;
					}
				}

				if(!found)
				{
					unique.Add(customStyle);
				}
			}

			styles = unique.ToArray();

			UnityEditor.EditorUtility.SetDirty(this);

			Debug.Log("Found "+unique.Count+" unique styles");
		}
		#endif
	}
}