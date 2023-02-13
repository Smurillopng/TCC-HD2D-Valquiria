using System;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class SyntaxHiglighting
	{
		public Color StringColor;
		public Color KeywordColor;
		public Color NumberColor;
		public Color TypeColor;
		public Color CommentColor;
		public Color PreprocessorDirectiveColor;

		[HideInInspector, SerializeField]
		public string StringColorTag;
		[HideInInspector, SerializeField]
		public string KeywordColorTag;
		[HideInInspector, SerializeField]
		public string NumberColorTag;
		[HideInInspector, SerializeField]
		public string TypeColorTag;
		[HideInInspector, SerializeField]
		public string CommentColorTag;
		[HideInInspector, SerializeField]
		public string PreprocessorDirectiveColorTag;
		
		public SyntaxHiglighting() { }

		public SyntaxHiglighting(Color stringColor, Color keywordColor, Color numberColor, Color typeColor, Color commentColor, Color preprocessorDirectiveColor)
		{
			StringColor = stringColor;
			KeywordColor = keywordColor;
			NumberColor = numberColor;
			TypeColor = typeColor;
			CommentColor = commentColor;
			PreprocessorDirectiveColor = preprocessorDirectiveColor;
		}

		public void OnValidate()
		{
			StringColorTag = string.Concat("<color=#", ColorUtility.ToHtmlStringRGB(StringColor), ">");
			KeywordColorTag = string.Concat("<color=#", ColorUtility.ToHtmlStringRGB(KeywordColor), ">");
			NumberColorTag = string.Concat("<color=#", ColorUtility.ToHtmlStringRGB(NumberColor), ">");
			TypeColorTag = string.Concat("<color=#", ColorUtility.ToHtmlStringRGB(TypeColor), ">");
			CommentColorTag = string.Concat("<color=#", ColorUtility.ToHtmlStringRGB(CommentColor), ">");
			PreprocessorDirectiveColorTag = string.Concat("<color=#", ColorUtility.ToHtmlStringRGB(PreprocessorDirectiveColor), ">");
		}
	}
}