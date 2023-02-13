namespace Sisus
{
	/// <summary>
	/// Modifies SyntaxFormatterBase syntax formatting logic
	/// for use with INI files (.ini). Used by IniDrawer.
	/// </summary>
	public sealed class IniSyntaxFormatter : SyntaxFormatterBase<IniSyntaxFormatter>
	{
		protected override bool IsCommentBlockStart()
		{
			return false;
		}

		protected override bool IsCommentBlockEnd()
		{
			return false;
		}

		/// <summary>
		/// Comments
		/// </summary>
		/// <returns></returns>
		protected override bool IsCommentLineStart()
		{
			if(nextIndex >= length)
			{
				return false;
			}

			if(currentChar == ';' || currentChar == '#')
			{
				for(int n = index - 1; n >= 0; n--)
				{
					var character = textUnformatted[n];
					if(character.IsLineEnd())
					{
						break;
					}
					if(!character.IsWhiteSpace())
					{
						return false;
					}
				}
				return true;
			}

			return false;
		}

		protected override bool IsCharStart()
		{
			return false;
		}
		
		protected override bool IsPreprocessorDirectiveStart()
		{
			return false;
		}

		/// <summary>
		/// Sections
		/// </summary>
		/// <returns></returns>
		protected override int IsType()
		{
			if(currentChar == '[')
			{
				int curr = index + 1;
				while(curr < length)
				{
					var c = textUnformatted[curr];
					if(c == ']')
					{
						return curr - index + 1;
					}
					if(c.IsLineEnd())
					{
						return -1;
					}
					curr++;
				}
			}
			return -1;
		}
		
		/// <summary>
		/// Keys
		/// </summary>
		/// <returns></returns>
		protected override int IsKeyword()
		{
			switch(currentChar)
			{
				case '[':
				case ';':
				case '=':
				case ':':
					return -1;
			}

			if(currentChar.IsWhiteSpace())
			{
				return -1;
			}
				
			if(index != 0)
			{
				for(int n = index - 1; n >= 0; n--)
				{
					var character = textUnformatted[n];
					if(character.IsLineEnd())
					{
						break;
					}
					if(!character.IsWhiteSpace())
					{
						return -1;
					}
				}
			}

			for(int n = index + 1; n < length; n++)
			{
				var character = textUnformatted[n];
				switch(character)
				{
					case '=':
					case ':':
					case '\r':
					case '\n':
						return n - index;
				}
			}

			//support last row having a key without a value
			return length - index;
		}

		/// <summary>
		/// Delimiter
		/// </summary>
		/// <returns></returns>
		protected override bool IsNumber()
		{
			return currentChar == '=' || currentChar == ':';
		}

		public override void Dispose()
		{
			var disposing = this;
			disposing.Clear();
			IniSyntaxFormatterPool.Dispose(ref disposing);
		}
	}
}