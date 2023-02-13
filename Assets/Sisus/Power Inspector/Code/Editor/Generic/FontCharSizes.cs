//#define SAFE_MODE
//#define DEBUG_CACHE_CHAR_ADVANCES_TIME

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public class FontCharSizes
	{
		private const int LastCharacterIndex = char.MaxValue;

		/// <summary> List of all characters that have an advance (width) above zero. </summary>
		private const string AllCharacters = "¿ï¾½¼»º¹¸·¶µ´³²±°¯®­¬«ª©¨§¦¥¤£¢¡ Ÿžœ›š™˜—–•”“’‘ŽŒ‹Š‰ˆ‡†…„ƒ‚€îíôìëêéèçæåäãâáàßÞÝÜÛÚÙØ×ÖÕÔÓÒÑÐÏÎÍÌËÊÉÈÇÆÅÄÃÂ~}|{zyxwvutsrqponmlkjihgfedcba`_^]\\[ZYXWVUTSRQPONMLKJIHGFEDCBA@?>=<;:9876543210/.-,+*)('&%$#\"! \t";

		#if DEV_MODE && DEBUG_CACHE_CHAR_ADVANCES_TIME
		private static readonly ExecutionTimeLogger timer = new ExecutionTimeLogger();
		#endif

		/// <summary>
		/// Cached dictionary containing all characters that have any horizontal width, and their widths
		/// </summary>
		private readonly Dictionary<char,float> charAdvances = new Dictionary<char,float>(LastCharacterIndex);
        
		private bool charAdvancesCached;
		private readonly Font font;
		private readonly int fontSize;

		public Font Font
		{
			get
			{
				return font;
			}
		}

		public int FontSize
		{
			get
			{
				return fontSize;
			}
		}

		public bool Ready
		{
			get
			{
				return charAdvancesCached;
			}
		}

		public FontCharSizes([NotNull]Font setFont)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setFont != null);
			#endif

			font = setFont;
			fontSize = font.fontSize;
			CacheCharAdvances();
		}

		public FontCharSizes([NotNull]Font setFont, int setFontSize)
		{
			//#if DEV_MODE && PI_ASSERTATIONS
			//Debug.Assert(setFont != null);
			//#endif

			if(setFont == null)
			{
				Debug.LogError("FontCharSizes called with null font parameter!");
				setFont = Fonts.Normal;
			}

			font = setFont;
			fontSize = setFontSize;
			CacheCharAdvances();
		}

		/// <summary>
		/// Fast and thread safe way to get horizontal width of a character in the font used int GUI labels
		/// </summary>
		/// <param name="c"></param>
		/// <returns>width of char in font</returns>
		public float GetCharAdvance(char c)
		{
			float result;
			if(charAdvances.TryGetValue(c, out result))
			{
				return result;
			}
			return 0f;
		}

		public float GetLabelHeight(GUIContent label, float maxWidth, float singleLineHeight = DrawGUI.SingleLineHeight)
		{
			#if DEV_MODE
			Debug.Assert(maxWidth > 0f);
			#endif

			if(label.image != null)
			{
				return label.image.height;
			}
			string text = label.text;
			float height = singleLineHeight;
			float width = 0f;
			int lastWhiteSpaceIndex = -1;
			int textLength = text.Length;
			for(int c = 0; c < textLength; c++)
			{
				float advance;
				char character = text[c];
				if(character == ' ')
				{
					lastWhiteSpaceIndex = c;
				}
				else if(character == '\n')
				{
					width = 0f;
					height += singleLineHeight;
					continue;
				}

				if(charAdvances.TryGetValue(text[c], out advance))
				{
					float setWidth = width + advance;
					if(setWidth > maxWidth)
					{
						height += singleLineHeight;

						if(lastWhiteSpaceIndex == -1)
						{
							width = advance;
						}
						else
						{
							c = lastWhiteSpaceIndex;
							lastWhiteSpaceIndex = -1;
							width = 0f;
						}
					}
					else
					{
						width = setWidth;
					}
				}
			}
			return height;
		}

		public float GetLabelWidth(GUIContent label)
		{
			if(label.image != null)
			{
				return label.image.width;
			}
			return GetTextWidth(label.text);
		}

		public float GetTextWidth(string text)
		{
			#if DEV_MODE || SAFE_MODE
			if(!charAdvancesCached)
			{
				#if SAFE_MODE
				CacheCharAdvances();
				#endif

				if(!charAdvancesCached)
				{
					throw new Exception("Unable to generate cache from character advances at this time. Make sure that CacheCharAdvances is called from OnGUI before GetTextWidth is called");
				}
			}
			#endif

			float result = 0f;
			for(int c = text.Length - 1; c >= 0; c--)
			{
				float advance;
				if(charAdvances.TryGetValue(text[c], out advance))
				{
					result += advance;
				}
			}
			
			return result;
		}
		
		/// <summary>
		/// Important: this should be called before CharAdvances calculations are requested
		/// Currently handled by DrawGUI.Setup
		/// </summary>
		private void CacheCharAdvances()
		{
			if(charAdvancesCached)
			{
				return;
			}
			
			#if DEV_MODE && DEBUG_CACHE_CHAR_ADVANCES_TIME
			timer.Start("FontCharSizes.CacheCharAdvances");
			Profiler.BeginSample("FontCharSizes.CacheCharAdvances");
			#endif

			charAdvances.Clear();
			
			#if DEV_MODE && DEBUG_CACHE_CHAR_ADVANCES_TIME
			timer.StartInterval("FontCharSizes.RequestCharactersInTexture");
			Profiler.BeginSample("FontCharSizes.RequestCharactersInTexture");
			#endif

			font.RequestCharactersInTexture(AllCharacters, fontSize, FontStyle.Normal);
			
			#if DEV_MODE && DEBUG_CACHE_CHAR_ADVANCES_TIME
			Profiler.EndSample();
			timer.FinishInterval();
			timer.StartInterval("FontCharSizes.GetCharacterInfos");
			Profiler.BeginSample("FontCharSizes.GetCharacterInfo");
			#endif

			CharacterInfo info;
			for(int i = AllCharacters.Length - 1; i >= 0; i--)
			{
				char c = AllCharacters[i];
				font.GetCharacterInfo(c, out info, fontSize, FontStyle.Normal);
				charAdvances.Add(c, info.advance);
			}

			#if DEV_MODE && DEBUG_CACHE_CHAR_ADVANCES_TIME
			Profiler.EndSample();
			timer.FinishInterval();
			#endif

			if(charAdvances.Count == 0)
			{
				#if DEV_MODE
				Debug.LogError("Tried to cache chars from fron "+font.name+" with fontSize "+fontSize+" but failed with event "+StringUtils.ToString(Event.current));
				#endif
				return;
			}
			
			// for some reason the advance for the tab character seems to always give the result of zero inside a for loop
			// but if we request the character info here separately, it seems to give a result of 6f every time.
			// However, even when a non-zero result is given, it seems to be too small compared to actual rendered width,
			// so we just set the value manually to 28.
			//font.GetCharacterInfo('\t', out info, fontSize, FontStyle.Normal);
			//CharAdvances['\t'] = info.advance;
			charAdvances['\t'] = 28f;
			
			charAdvancesCached = true;

			#if DEV_MODE && DEBUG_CACHE_CHAR_ADVANCES_TIME
			Profiler.EndSample();
			timer.FinishAndLogResults();
			#endif
		}
	}
}