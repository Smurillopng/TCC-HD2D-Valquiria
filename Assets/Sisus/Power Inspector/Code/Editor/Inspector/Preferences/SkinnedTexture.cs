using System;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Texture that dynamically changes to match the active Editor Skin
	/// (Default light skin or Pro dark skin).
	/// </summary>
	[Serializable]
	public class SkinnedTexture
	{
		[SerializeField]
		private Texture imageLightSkin = null;

		[SerializeField] private Texture imageDarkSkin = null;

		public SkinnedTexture() { }

		public SkinnedTexture(Texture imageLightSkin, Texture imageDarkSkin)
		{
			this.imageLightSkin = imageLightSkin;
			this.imageDarkSkin = imageDarkSkin;
		}

		public Texture Get()
		{
			return DrawGUI.IsProSkin ? imageDarkSkin : imageLightSkin;
		}

		public static implicit operator Texture(SkinnedTexture skinnedTexture)
		{
			return skinnedTexture.Get();
		}
	}
}