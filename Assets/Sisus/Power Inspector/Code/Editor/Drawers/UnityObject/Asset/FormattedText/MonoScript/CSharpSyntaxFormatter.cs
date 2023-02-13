namespace Sisus
{
	/// <summary>
	/// Modifies SyntaxFormatterBase syntax formatting logic
	/// for use with C# files (.cs). Used by MonoScriptDrawer.
	/// </summary>
	public sealed class CSharpSyntaxFormatter : SyntaxFormatterBase<CSharpSyntaxFormatter>
	{
		protected override bool IsCommentBlockStart()
		{
			if(nextIndex >= length)
			{
				return false;
			}
			return currentChar == '/' && textUnformatted[nextIndex] == '*';
		}

		protected override bool IsCommentBlockEnd()
		{
			if(nextIndex >= length)
			{
				return false;
			}
			return currentChar == '*' && textUnformatted[nextIndex] == '/';
		}

		protected override bool IsCommentLineStart()
		{
			if(nextIndex >= length)
			{
				return false;
			}
			return currentChar == '/' && textUnformatted[nextIndex] == '/';
		}

		protected override bool IsPreprocessorDirectiveStart()
		{
			return currentChar == '#';
		}

		protected override int IsType()
		{
			//List, MyClass etc.
			//a dirty but very performant shortcut :D
			if(char.IsUpper(currentChar))
			{
				int curr;
				if(index != 0)
				{
					curr = index - 1;
					var c = textUnformatted[curr];
					if(!c.IsWhiteSpace() && !c.IsSpecialCharacter())
					{
						return -1;
					}
				}

				curr = index + 1;
				while(curr < length)
				{
					var c = textUnformatted[curr];
					if(c.IsWhiteSpace() || c.IsSpecialCharacter())
					{
						return curr - index;
					}
					curr++;
				}
				return curr - index;
			}
			return -1;
		}

		protected override int IsKeyword()
		{
			//ref, class, string...

			//the @ character allows using reserved
			//keywords for normal identifiers
			if(index > 0 && textUnformatted[index-1] == '@')
			{
				return -1;
			}

			switch(currentChar)
			{
				case 'a':
					if(IsKeyword("abstract"))
					{
						return 8;
					}
					return -1;
				case 'b':
					if(IsKeyword("base") || IsKeyword("bool"))
					{
						return 4;
					}
					if(IsKeyword("break"))
					{
						return 5;
					}
					return -1;
				case 'c':
					if(IsKeyword("case") || IsKeyword("char"))
					{
						return 4;
					}
					if(IsKeyword("const") || IsKeyword("class") || IsKeyword("catch"))
					{
						return 5;
					}
					if(IsKeyword("continue"))
					{
						return 8;
					}
					return -1;
				case 'd':
					if(IsKeyword("double"))
					{
						return 6;
					}
					if(IsKeyword("default") || IsKeyword("dynamic"))
					{
						return 7;
					}
					if(IsKeyword("delegate"))
					{
						return 8;
					}
					return -1;
				case 'e':
					if(IsKeyword("enum") || IsKeyword("else"))
					{
						return 4;
					}
					return -1;
				case 'f':
					if(IsKeyword("for"))
					{
						return 3;
					}
					if(IsKeyword("float") || IsKeyword("false"))
					{
						return 5;
					}
					if(IsKeyword("finally") || IsKeyword("foreach"))
					{
						return 7;
					}
					return -1;
				case 'g':
					if(IsKeyword("get"))
					{
						return 3;
					}
					return -1;
				case 'i':
					if(IsKeyword("if"))
					{
						return 2;
					}
					if(IsKeyword("int"))
					{
						return 3;
					}
					if(IsKeyword("internal"))
					{
						return 8;
					}
					if(IsKeyword("interface"))
					{
						return 9;
					}
					return -1;
				case 'l':
					if(IsKeyword("long"))
					{
						return 4;
					}
					return -1;
				case 'h':
					if(IsKeyword("half"))
					{
						return 4;
					}
					return -1;
				case 'n':
					if(IsKeyword("new"))
					{
						return 3;
					}
					if(IsKeyword("null"))
					{
						return 4;
					}
					if(IsKeyword("namespace"))
					{
						return 9;
					}
					return -1;
				case 'o':
					if(IsKeyword("out"))
					{
						return 3;
					}
					if(IsKeyword("object"))
					{
						return 6;
					}
					if(IsKeyword("override"))
					{
						return 8;
					}
					return -1;
				case 'p':
					if(IsKeyword("public"))
					{
						return 6;
					}
					if(IsKeyword("private"))
					{
						return 7;
					}
					if(IsKeyword("protected"))
					{
						return 9;
					}
					return -1;
				case 'r':
					if(IsKeyword("ref"))
					{
						return 3;
					}
					if(IsKeyword("return"))
					{
						return 6;
					}
					if(IsKeyword("readonly"))
					{
						return 8;
					}
					return -1;
				case 's':
					if(IsKeyword("set"))
					{
						return 3;
					}
					if(IsKeyword("static") || IsKeyword("sealed") || IsKeyword("struct") || IsKeyword("switch") || IsKeyword("string"))
					{
						return 6;
					}
					return -1;
				case 't':
					if(IsKeyword("try"))
					{
						return 3;
					}
					if(IsKeyword("this") || IsKeyword("true"))
					{
						return 4;
					}
					if(IsKeyword("throw"))
					{
						return 5;
					}
					return -1;
				case 'u':
					if(IsKeyword("using"))
					{
						return 5;
					}
					return -1;
				case 'v':
					if(IsKeyword("var"))
					{
						return 3;
					}
					if(IsKeyword("void"))
					{
						return 4;
					}
					if(IsKeyword("virtual"))
					{
						return 7;
					}
					if(IsKeyword("volatile"))
					{
						return 8;
					}
					return -1;
				case 'w':
					if(IsKeyword("where") || IsKeyword("while"))
					{
						return 5;
					}
					return -1;
				default:
					return -1;
			}
		}

		private bool IsKeyword(string keyword)
		{
			return StringUtils.SubstringEqualsWholeWord(textUnformatted, index, keyword);
		}

		public override void Dispose()
		{
			var disposing = this;
			disposing.Clear();
			CSharpSyntaxFormatterPool.Dispose(ref disposing);
		}
	}
}