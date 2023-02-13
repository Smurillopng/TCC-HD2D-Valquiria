#define USE_THREADING

using System;
using System.Text;

namespace Sisus
{
	/// <summary>
	/// A container of CodeLines, optimized for line-by-line renderering of Code.
	/// Each CodeLine contains a syntax formatted and plain text representation of its code,
	/// for easy switching between the two representations when displaying the code to the end-user. 
	/// </summary>
	[Serializable]
	public class Code
	{
		public CodeLine[] lines;
		public float width;
		private ITextSyntaxFormatter builder;

		public int LineCount
		{
			get
			{
				return lines.Length;
			}
		}

		public string this[int index]
		{
			get
			{
				return lines[index].formatted;
			}
		}

		public Code(ITextSyntaxFormatter codeBuilder)
		{
			lines = new CodeLine[0];
			builder = codeBuilder;
		}
	
		public string GetLineUnformatted(int index)
		{
			return lines[index].unformatted;
		}

		public void SetLine(int index, string value, bool rebuild)
		{
			lines[index].Set(value, builder);

			if(rebuild)
			{
				string newTextUnformatted = ToString();
				builder.SetCode(newTextUnformatted);
				builder.BuildAllBlocks();
			}
		}

		public void RemoveAt(int index, bool rebuild)
		{
			lines = lines.RemoveAt(index);

			if(rebuild)
			{
				string newTextUnformatted = ToString();
				builder.SetCode(newTextUnformatted);
				builder.BuildAllBlocks();
			}
		}

		public void InsertAt(int index, string content, bool rebuild)
		{
			lines = lines.InsertAt(index, CodeLinePool.Create(content, builder));

			if(rebuild)
			{
				string newTextUnformatted = ToString();
				builder.SetCode(newTextUnformatted);
				builder.BuildAllBlocks();
			}
		}

		public override string ToString()
		{
			int count = lines.Length;
			if(count > 0)
			{
				var sb = new StringBuilder(count*15);
				sb.Append(lines[0].unformatted);

				for(int n = 1; n < count; n++)
				{
					sb.Append(Environment.NewLine);
					sb.Append(lines[n].unformatted);
				}

				return sb.ToString();
			}
			return "";
		}

		public void Dispose()
		{
			if(builder != null)
			{
				builder.Dispose();
				builder = null;
			}

			for(int n = LineCount - 1; n >= 0; n--)
			{
				CodeLinePool.Dispose(ref lines[n]);
			}
		}

		public string TextUnformatted
		{
			get
			{
				return builder.TextUnformatted;
			}
		}
	}
}