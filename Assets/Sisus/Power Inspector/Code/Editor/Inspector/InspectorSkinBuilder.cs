using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public static class InspectorSkinBuilder
	{
		public static GUISkin Build(GUIStyles styles)
		{
			if(GUI.skin == null)
			{
				throw new InvalidOperationException("InspectorSkinBuilder.Build should only be called during the OnGUI event.");
			}
			return Build(GUI.skin, styles);
		}

		public static GUISkin Build([NotNull]GUISkin baseSkin, [NotNull]GUIStyles styles)
		{
			var result = styles.AddTo(baseSkin);

			var wordWrappedLabel = new GUIStyle(result.label);
			wordWrappedLabel.wordWrap = true;
			wordWrappedLabel.name = "WordWrappedLabel";

			var wordWrappedMiniLabel = new GUIStyle(result.GetStyle("MiniLabel"));
			wordWrappedMiniLabel.wordWrap = true;
			wordWrappedMiniLabel.name = "WordWrappedMiniLabel";
			
			result.customStyles = result.customStyles.Add(wordWrappedLabel, wordWrappedMiniLabel);

			return result;
		}
	}
}