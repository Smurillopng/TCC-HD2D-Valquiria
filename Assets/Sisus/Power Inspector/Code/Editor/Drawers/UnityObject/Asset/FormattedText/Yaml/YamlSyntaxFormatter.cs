namespace Sisus
{
	/// <summary>
	/// Modifies SyntaxFormatterBase syntax formatting logic
	/// for use with YAML files (.yaml, .yml). Used by MonoScriptDrawer.
	/// </summary>
	public sealed class YamlSyntaxFormatter : SyntaxFormatterBase<YamlSyntaxFormatter>
	{
		protected override bool IsCommentBlockStart()
		{
			return false;
		}

		protected override bool IsCommentBlockEnd()
		{
			return false;
		}

		protected override bool IsCommentLineStart()
		{
			if(nextIndex >= length)
			{
				return false;
			}
			return currentChar == '#';
		}

		protected override bool IsCharStart()
		{
			return false;
		}

		protected override bool IsPreprocessorDirectiveStart()
		{
			return false;
		}

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

		//protected override int IsKeyword()
		//{
		//	switch(currentChar)
		//	{
		//		case 'f':
		//			if(IsKeyword("false"))
		//			{
		//				return 5;
		//			}
		//			return -1;
		//		case 't':
		//			if(IsKeyword("true"))
		//			{
		//				return 4;
		//			}
		//			return -1;
		//		default:
		//			return -1;
		//	}
		//}

		//private bool IsKeyword(string keyword)
		//{
		//	return StringUtils.SubstringEqualsWholeWord(codeUnformatted, index, keyword);
		//}

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
				var c = textUnformatted[n];
				switch(c)
				{
					//case '=':
					case ':':
					case '\r':
					case '\n':
						return n - index;
				}
			}

			return -1;
		}

		protected override bool IsNumber()
		{
			return currentChar == ':';
		}

		public override void Dispose()
		{
			var disposing = this;
			disposing.Clear();
			YamlSyntaxFormatterPool.Dispose(ref disposing);
		}
	}
}