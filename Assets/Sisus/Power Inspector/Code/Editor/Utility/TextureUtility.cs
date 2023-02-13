using UnityEngine;
using JetBrains.Annotations;

namespace Sisus
{
	public class TextureUtility
	{
		[Pure]
		public static Texture2D Resize(Texture2D texture, int width, int height)
		{
			var renderTexture = new RenderTexture(width, height, 24);
			RenderTexture.active = renderTexture;
			Graphics.Blit(texture, renderTexture);
			var result = new Texture2D(width, height, texture.format, false);
			result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			result.Apply();
			return result;
		}
	}
}