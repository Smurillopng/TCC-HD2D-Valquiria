#if UNITY_EDITOR
using System;
using System.IO;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Default fallback drawer for text assets, rendering as plain text with no special syntax formatting.
	/// </summary>
	[Serializable, DrawerForAsset(typeof(TextAsset), true, true)]
	#if UNITY_EDITOR
	[DrawerForAsset(typeof(UnityEditor.DefaultAsset), true, true)] // Display all unknown asset types as plain text?
	#endif
	public class TextAssetDrawer : FormattedTextAssetDrawer<PlainTextSyntaxFormatter>
	{
		/// <inheritdoc />
		protected override bool AllowSyntaxHighlighting
		{
			get
			{
				return false;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static new TextAssetDrawer Create(Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			var result = Create<TextAssetDrawer>(targets, parent, inspector);
			return result;
		}

		/// <inheritdoc />
		protected override void GetHeaderSubtitle(ref GUIContent subtitle)
		{
			subtitle.tooltip = "A text asset.";

			string extension = Path.GetExtension(LocalPath);
			if(extension != null && extension.Length > 1)
			{
				subtitle.text = StringUtils.SplitPascalCaseToWords(extension.Substring(1));
				if(!subtitle.text.EndsWith("Asset"))
				{
					subtitle.text = string.Concat(subtitle.text, " Asset");
				}
			}
			else
			{
				subtitle.text = "";
			}
		}

		/// <inheritdoc />
		protected override PlainTextSyntaxFormatter CreateSyntaxFormatter()
		{
			return PlainTextSyntaxFormatterPool.Pop();
		}

		protected override string GetTextFromFile()
		{
			var fullPath = FullPath;
			if(FileUtility.IsBinaryFile(fullPath))
			{
				return "[Binary File]";
			}
			return File.ReadAllText(fullPath);
		}
	}
}
#endif