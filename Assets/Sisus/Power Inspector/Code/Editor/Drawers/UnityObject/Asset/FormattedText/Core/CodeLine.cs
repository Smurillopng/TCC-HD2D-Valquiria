using System;

namespace Sisus
{
	/// <summary>
	/// Represents a line of code, optimized for line-by-line rendering of code.
	/// Each CodeLine contains a syntax formatted and plain text representation of its code,
	/// for easy switching between the two representations when displaying the code to the end-user. 
	/// </summary>
	[Serializable]
	public class CodeLine
	{
		public string formatted;
		public string unformatted;

		public CodeLine(string unformatted, ITextSyntaxFormatter builder)
		{
			Set(unformatted, builder);
		}

		public CodeLine(string unformatted, string formatted)
		{
			this.unformatted = unformatted;
			this.formatted = formatted;
		}

		public void Set(string value, ITextSyntaxFormatter builder)
		{
			unformatted = value;

			if(value.Length == 0)
			{
				formatted = value;
			}
			else
			{
				//this isn't perfect, would need to recreate syntax formatting
				//for whole code to handle stuff like /*-blocks
				builder.SetCode(value);
				builder.BuildAllBlocks();
				var codeLine = this;
				builder.GeneratedBlocks.ToCodeLine(ref codeLine, builder);
			}
		}

		public override string ToString()
		{
			return unformatted;
		}
	}
}