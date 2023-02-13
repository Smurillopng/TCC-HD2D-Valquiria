using System;
using System.Text;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Sisus
{
	/// <summary>
	/// A group of CodeBlocks with easy methods to get Width of LineCount
	/// and to convert to Code class which is optimized for line-by-line
	/// rendering of the code.
	/// </summary>
	public class CodeBlockGroup
	{
		private List<CodeBlock> blocks;

		public List<CodeBlock> Elements
		{
			get
			{
				return blocks;
			}
		}

		public CodeBlockGroup()
		{
			blocks = new List<CodeBlock>(100);
		}
		
		public void Add(CodeBlock add)
		{
			blocks.Add(add);
		}

		/// <summary>
		/// Returns line count of code blocks.
		/// </summary>
		/// <returns> Line count, with a minumum value of 1. </returns>
		public int LineCount()
		{
			int count = blocks.Count;
			if(count == 0)
			{
				return 1;
			}

			int result = 1;
			for(int n = 0; n < count; n++)
			{
				if(blocks[n].IsLineBreak())
				{
					result++;
				}
			}
			return result;
		}

		/// <summary>
		/// Returns line count of code blocks without counting lines that
		/// consist only of whitespace or comments.
		/// </summary>
		/// <returns> Line count. 0 if there are no code blocks. </returns>
		public int LineCountWithoutCommentsOrEmptyLines()
		{
			int count = blocks.Count;
			if(count == 0)
			{
				return 0;
			}

			int result = 0;
			bool countLine = false;
			for(int n = 0; n < count; n++)
			{
				var block = blocks[n];

				if(block.IsLineBreak())
				{
					// lines had content other than comments or whitespace
					if(countLine)
					{
						result++;
						countLine = false;
					}
					continue;
				}

				// skip blocks constisting only of whitespace
				if(block.IsEmptyOrWhitespace())
				{
					continue;
				}

				// skip comment blocks...
				if(block.IsComment())
				{
					continue;
				}

				// text that was not white space or comments was discovered -> count this line
				countLine = true;
			}

			//count the last line
			if(countLine)
			{
				result++;
			}
			
			return result;
		}

		private float Width([NotNull] FontCharSizes fontCharSizes)
		{
			float largestWidth = 100f;

			float lineWidth = 0f;
			for(int n = blocks.Count - 1; n >= 0; n--)
			{
				var block = blocks[n];
				if(block.IsLineBreak())
				{
					if(lineWidth > largestWidth)
					{
						largestWidth = lineWidth;
					}
					lineWidth = 0f;
				}
				else
				{
					lineWidth += fontCharSizes.GetTextWidth(block.ContentUnformatted);
				}
			}
			return largestWidth;
		}

		public void ToCode([NotNull] ref Code result, [NotNull] ITextSyntaxFormatter builder, [NotNull] FontCharSizes fontCharSizes)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(fontCharSizes != null);
			#endif

			ToCodeLines(ref result.lines, builder);
			result.width = Width(fontCharSizes);
		}
		
		private void ToCodeLines(ref CodeLine[] result, ITextSyntaxFormatter builder)
		{
			int lineCount = LineCount();
			if(result == null)
			{
				result = new CodeLine[0];
			}
			else if(result.Length != lineCount)
			{
				Array.Resize(ref result, lineCount);
			}

			if(lineCount == 0)
			{
				return;
			}

			var sbUnformatted = new StringBuilder(25);
			var sb = new StringBuilder(25);
			
			int lineIndex = 0;
			int count = blocks.Count;
			for(int n = 0; n < count; n++)
			{
				var block = blocks[n];
				if(block.IsLineBreak())
				{
					result[lineIndex] = new CodeLine(sbUnformatted.ToString(), sb.ToString());
					sbUnformatted.Length = 0;
					sb.Length = 0;
					lineIndex++;
				}
				else
				{
					sbUnformatted.Append(block.ContentUnformatted);
					block.ToString(ref sb, builder);
				}
			}
			result[lineIndex] = new CodeLine(sbUnformatted.ToString(), sb.ToString());
		}

		public void ToCodeLine(ref CodeLine result, ITextSyntaxFormatter builder)
		{
			int lineCount = LineCount();
			
			if(lineCount == 0)
			{
				result.formatted = "";
				result.unformatted = "";
				return;
			}

			var sbUnformatted = new StringBuilder(25);
			var sb = new StringBuilder(25);
			
			int count = blocks.Count;
			for(int n = 0; n < count; n++)
			{
				var block = blocks[n];
				if(block.IsLineBreak())
				{
					break;
				}
				
				sbUnformatted.Append(block.ContentUnformatted);
				block.ToString(ref sb, builder);
			}
			result.unformatted = sbUnformatted.ToString();
			result.formatted = sb.ToString();
		}
		
		public void Clear()
		{
			blocks.Clear();
		}
	}
}