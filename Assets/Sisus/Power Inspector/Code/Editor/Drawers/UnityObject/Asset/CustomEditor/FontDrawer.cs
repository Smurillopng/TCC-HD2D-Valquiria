#if UNITY_EDITOR
using System;
using System.IO;
using Sisus.Attributes;
using UnityEditor;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer use for all fonts and related assets such as Font Material and Font Texture.
	/// </summary>
	[Serializable, DrawerByExtension(".fnt", true), DrawerByExtension(".ttf", true), DrawerByExtension(".otf", true)]
	public class FontDrawer : CustomEditorAssetDrawer
	{
		/// <inheritdoc />
		protected override Editor HeaderEditor
		{
			get
			{
				//this is needed to make the forceVisible unfolding to work
				return Editor;
			}
		}

		/// <inheritdoc />
		protected override void GetHeaderSubtitle(ref GUIContent subtitle)
		{
			var type = Type;
			if(type == Types.Material)
			{
				subtitle.text = "Font Material";
			}
			else if(type == Types.Texture)
			{
				subtitle.text = "Font Texture";
			}
			else 
			{
				string extension = Path.GetExtension(LocalPath);
				if(extension == null)
				{
					subtitle.text = "Font";
					return;
				}
				switch(extension.ToLowerInvariant())
				{
					case ".ttf":
						subtitle.text = "TrueType Font";
						return;
					case ".otf":
						subtitle.text = "OpenType Font";
						return;
					case ".fnt":
						subtitle.text = "Windows Font File";
						return;
					case "":
						subtitle.text = "Font";
						return;
					default:
						subtitle.text = StringUtils.SplitPascalCaseToWords(extension.Substring(1));
						if(!subtitle.text.EndsWith("Asset"))
						{
							subtitle.text = string.Concat(subtitle.text, " Asset");
						}
						return;
				}
			}
		}
	}
}
#endif