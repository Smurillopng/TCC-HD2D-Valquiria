using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// GUIContent that dynamically changes its texture to match the active Editor Skin
	/// (Default light skin or Pro dark skin).
	/// </summary>
	[Serializable]
	public class SkinnedLabel
	{
		// Warning "Field x is never assigned to, and will always have its default value null"
		// does not apply here because values are assigned through the Unity inspector
		#pragma warning disable 0649

		// values assigned through the Unity inspector
		[SerializeField, UsedImplicitly]
		private GUIContent label;

		// values assigned through the Unity inspector
		[SerializeField, UsedImplicitly]
		private Texture imageLightSkin;

		// values assigned through the Unity inspector
		[SerializeField, UsedImplicitly]
		private Texture imageDarkSkin;

		#pragma warning restore 0649

		public SkinnedLabel() { }

		public SkinnedLabel(GUIContent setLabel)
		{
			label = setLabel;
		}

		public SkinnedLabel(string text)
		{
			label = GUIContentPool.Create(text);
		}

		public SkinnedLabel(string text, string tooltip)
		{
			label = GUIContentPool.Create(text, tooltip);
		}

		public SkinnedLabel(GUIContent setLabel, Texture setImageLightSkin, Texture setImageDarkSkin)
		{
			label = setLabel;
			imageLightSkin = setImageLightSkin;
			imageDarkSkin = setImageDarkSkin;
		}

		public GUIContent Get()
		{
			var result = GUIContentPool.Create(label);
			result.image = DrawGUI.IsProSkin ? imageDarkSkin : imageLightSkin;
			return result;
		}

		public static implicit operator GUIContent(SkinnedLabel skinnedLabel)
		{
			return skinnedLabel.Get();
		}

		public static implicit operator SkinnedLabel(GUIContent label)
		{
			return new SkinnedLabel(label);
		}
	}
}