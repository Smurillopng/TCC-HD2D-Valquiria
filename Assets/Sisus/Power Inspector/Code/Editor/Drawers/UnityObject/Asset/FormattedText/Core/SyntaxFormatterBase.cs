using JetBrains.Annotations;

namespace Sisus
{
	/// <summary>
	/// Takes a block of unformatted text and splits it into distinct chunks optimized
	/// for easily applying syntax formatting to it with Rich Text markup tags for improved readability.
	/// 
	/// The CodeBlockGroup can then easily be expoerted into formatted text for line-by-line rendering.
	/// </summary>
	public abstract class SyntaxFormatterBase<T> : ITextSyntaxFormatter where T : SyntaxFormatterBase<T>, new()
	{
		protected string textUnformatted;

		protected int index;
		protected int nextIndex = 1;
		protected int length;
		protected bool isEscapeSequence;
		protected char currentChar;

		private int currentBlockStart;

		private CodeBlockType currentType = CodeBlockType.Default;

		[NotNull]
		private CodeBlockGroup generatedBlocks;
		private int lineCount;

		public string TextUnformatted
		{
			get
			{
				return textUnformatted;
			}
		}

		/// <summary>
		/// Gets the code blocks that were generated during the last BuildAllBlocks or BuildNextBlocks call.
		/// 
		/// This is cleared and reused during each BuildNextBlocks call. So if you want to cache the data
		/// of this property and make sure that it persists beyond the next call to BuildNextBlocks, you should
		/// convert it to an array first using CodeBlockGroup.ToArray().
		/// </summary>
		/// <value> The generated blocks. </value>
		[NotNull]
		public CodeBlockGroup GeneratedBlocks
		{
			get
			{
				return generatedBlocks;
			}
		}

		/// <summary>
		/// Gets rich text color tag that precedes all comment blocks.
		/// </summary>
		/// <value> Rich text color tag. </value>
		/// <example>
		/// <![CDATA[<color=green>]]>
		/// </example>
		/// <example>
		/// <![CDATA[<color=#rrggbbaa>]]>
		/// </example>
		public virtual string CommentColorTag
		{
			get
			{
				return InspectorUtility.Preferences.theme.SyntaxHighlight.CommentColorTag;
			}
		}

		/// <summary>
		/// Gets rich text color tag that precedes all numbers.
		/// </summary>
		/// <value> Rich text color tag. </value>
		/// <example>
		/// <![CDATA[<color=green>]]>
		/// </example>
		/// <example>
		/// <![CDATA[<color=#rrggbbaa>]]>
		/// </example>
		public virtual string NumberColorTag
		{
			get
			{
				return InspectorUtility.Preferences.theme.SyntaxHighlight.NumberColorTag;
			}
		}

		/// <summary>
		/// Gets rich text color tag that precedes all text strings (i.e. text inside quotation marks).
		/// </summary>
		/// <value> Rich text color tag. </value>
		/// <example>
		/// <![CDATA[<color=green>]]>
		/// </example>
		/// <example>
		/// <![CDATA[<color=#rrggbbaa>]]>
		/// </example>
		public virtual string StringColorTag
		{
			get
			{
				return InspectorUtility.Preferences.theme.SyntaxHighlight.StringColorTag;
			}
		}

		/// <summary>
		/// Gets rich text color tag that precedes all keywords (i.e. ref, class, int...).
		/// </summary>
		/// <value> Rich text color tag. </value>
		/// <example>
		/// <![CDATA[<color=green>]]>
		/// </example>
		/// <example>
		/// <![CDATA[<color=#rrggbbaa>]]>
		/// </example>
		public virtual string KeywordColorTag
		{
			get
			{
				return InspectorUtility.Preferences.theme.SyntaxHighlight.KeywordColorTag;
			}
		}

		public SyntaxFormatterBase()
		{
			generatedBlocks = new CodeBlockGroup();
			textUnformatted = "";
			length = 0;
		}

		/// <summary>
		/// Sets unformatted code value from which CodeBlocks will be generated when BuildAllBlocks is called.
		/// Also clears current progress of generated blocks, if any.
		/// </summary>
		/// <param name="setUnormattedCode"> The unformatted code. </param>
		public void SetCode(string setUnormattedCode)
		{
			Clear();
			textUnformatted = setUnormattedCode;
			length = setUnormattedCode.Length;
		}

		/// <summary>
		/// Sets unformatted code value for given line from which CodeBlocks will be generated when BuildAllBlocks is called.
		/// Also clears current progress of generated blocks, if any.
		/// </summary>
		/// <param name="lineIndex"> The zero-based index of the line to replace. </param>
		/// <param name="value"> The unformatted code for the line. </param>
		public void SetLine(int lineIndex, string value)
		{
			int count = textUnformatted.Length;
			if(count == 0)
			{
				SetCode(value);
				return;
			}

			if(lineIndex == 0)
			{
				int lineEnd = textUnformatted.IndexOfAny(new char[]{'\r', '\n' });
				if(lineEnd == -1)
				{
					SetCode(value);
					return;
				}
				SetCode(value + textUnformatted.Substring(lineEnd));
				return;
			}

			int startOfLine = 0;
			int currentLineIndex = 0;
			for(int n = 0; n < count; n++)
			{
				switch(textUnformatted[n])
				{
					case '\n':
						int endOfLine = n;
						if(currentLineIndex == lineIndex)
						{
							SetCode(textUnformatted.Substring(0, startOfLine) + value + textUnformatted.Substring(endOfLine));
							return;
						}
						startOfLine = n + 1;
						currentLineIndex++;
						break;
					case '\r':
						endOfLine = n;
						if(currentLineIndex == lineIndex)
						{
							SetCode(textUnformatted.Substring(0, startOfLine) + value + textUnformatted.Substring(endOfLine));
							return;
						}
						currentLineIndex++;
						if(nextIndex < length && textUnformatted[nextIndex] == '\n')
						{
							n++;
						}
						startOfLine = n + 1;
						break;
				}
			}

			if(startOfLine == 0)
			{
				SetCode(value);
				return;
			}

			if(startOfLine < count)
			{
				SetCode(textUnformatted.Substring(0, startOfLine) + value);
			}
			#if DEV_MODE
			else { UnityEngine.Debug.LogWarning($"SetLine({lineIndex}, {value}) index out of bounds. textUnformatted:\n{textUnformatted}"); }
			#endif
		}

		/// <summary>
		/// Clears generated blocks and reverts code iteration progress to initial state.
		/// Unformatted code will NOT be cleared (Use SetCode("") for that).
		/// </summary>
		public void Clear()
		{
			generatedBlocks.Clear();

			index = 0;
			nextIndex = 1;
			currentBlockStart = 0;
			isEscapeSequence = false;
			currentType = CodeBlockType.Default;
			lineCount = 0;
		}

		/// <summary> Builds CodeBlocks from unformatted code that was set using SetCode. </summary>
		public virtual void BuildAllBlocks()
		{
			if(length > 0)
			{
				BuildNextBlocks(int.MaxValue);
			}
		}

		/// <summary>
		/// Iterates through codeFormatted letter by letter, generating CodeBlocks, until
		/// end of text is reached or linesToGenerate number of lines have been processed.
		/// </summary>
		public void BuildNextBlocks(int linesToGenerate)
		{
			generatedBlocks.Clear();
			while(ReadNextCharForCodeBlock() && lineCount < linesToGenerate)
			{

			}
		}

		/// <summary>
		/// Moves to inspect the next character in the unformatted code
		/// and detects changes to the current CodeBlock. When a new code
		/// block is discovered, the previous one is added to the codeBlockGroup. </summary>
		/// <returns> False if end of code was reached, true if not. </returns>
		public virtual bool ReadNextCharForCodeBlock()
		{
			if(index >= length)
			{
				EndBlock();
				return false;
			}

			currentChar = textUnformatted[index];

			int charCount;

			switch(currentType)
			{
				case CodeBlockType.CommentBlock:
					if(IsCommentBlockEnd())
					{
						AddToIndex(2);
						EndBlock();
						break;
					}
					charCount = IsLineEnd();
					if(charCount != -1)
					{
						LineBreak(charCount);
						break;
					}
					NextIndex();
					break;
				//both comment line and preprocessor directive
				//are effective until next line break
				case CodeBlockType.CommentLine:
				case CodeBlockType.PreprocessorDirective:
					charCount = IsLineEnd();
					if(charCount != -1)
					{
						EndBlock();
						LineBreak(charCount);
						break;
					}
					NextIndex();
					break;
				case CodeBlockType.String:
					if(IsStringEnd())
					{
						NextIndex();
						EndBlock();
						isEscapeSequence = false;
						break;
					}

					if(IsEscapeSequence())
					{
						isEscapeSequence = true;
						NextIndex();
						break;
					}
					isEscapeSequence = false;

					//if this happens the MonoScript should have a compile error
					//but still it could happen
					charCount = IsLineEnd();
					if(charCount != -1)
					{
						EndBlock(); //reset the type since it's an error, to prevent possibly all the rest of the code being syntax highlighted as a string
						LineBreak(charCount);
						break;
					}

					NextIndex();
					break;
				case CodeBlockType.Char:
					if(IsCharEnd())
					{
						NextIndex();
						EndBlock();
						isEscapeSequence = false;
						break;
					}

					if(IsEscapeSequence())
					{
						isEscapeSequence = true;
						NextIndex();
						break;
					}
					isEscapeSequence = false;

					//if this happens it's an error
					//but still it could happen
					charCount = IsLineEnd();
					if(charCount != -1)
					{
						LineBreak(charCount);
						break;
					}
					NextIndex();
					break;
				case CodeBlockType.Type: //List, MyClass
				case CodeBlockType.Keyword: //ref, class, string...
					charCount = IsWhiteSpace();
					if(charCount != -1)
					{
						EndBlock();
						if(IsLineEnd() != -1)
						{
							LineBreak(charCount);
						}
						break;
					}

					if(IsSpecialCharacter())
					{
						EndBlock();
						NextIndex();
						break;
					}

					NextIndex();
					break;
				case CodeBlockType.Number:
					if(IsNumber())
					{
						NextIndex();
						break;
					}
					EndBlock();
					currentType = CodeBlockType.Default;
					break;
				default:
					if(IsCommentBlockStart())
					{
						EndBlock();
						AddToIndex(2);
						currentType = CodeBlockType.CommentBlock;
						break;
					}
					if(IsCommentLineStart())
					{
						EndBlock();
						AddToIndex(2);
						currentType = CodeBlockType.CommentLine;
						break;
					}
					if(IsStringStart())
					{
						EndBlock();
						NextIndex();
						currentType = CodeBlockType.String;
						break;
					}
					if(IsCharStart())
					{
						EndBlock();
						NextIndex();
						currentType = CodeBlockType.Char;
						break;
					}
					if(IsPreprocessorDirectiveStart())
					{
						EndBlock();
						NextIndex();
						currentType = CodeBlockType.PreprocessorDirective;
						break;
					}
					charCount = IsLineEnd();
					if(charCount != -1)
					{
						LineBreak(charCount);
						break;
					}
					charCount = IsKeyword();
					if(charCount != -1)
					{
						EndBlock();
						AddToIndex(charCount);
						currentType = CodeBlockType.Keyword;
						EndBlock();
						break;
					}
					charCount = IsType();
					if(charCount != -1)
					{
						if(index + charCount > length)
						{
							UnityEngine.Debug.LogError("IsType returned charCount " + charCount + " but index " + index + " + charCount was more than length " + length);
							NextIndex();
							break;
						}
						EndBlock();
						AddToIndex(charCount);
						currentType = CodeBlockType.Type;
						EndBlock();
						break;
					}
					if(IsNumber())
					{
						EndBlock();
						currentType = CodeBlockType.Number;
						NextIndex();
						break;
					}

					NextIndex();
					break;
			}
			return true;
		}

		/// <summary> Increments current index by one. </summary>
		protected void NextIndex()
		{
			index = nextIndex;
			nextIndex++;
		}

		/// <summary> Increments current index by given amount. </summary>
		/// <param name="amount"> The amount by which to increment. </param>
		private void AddToIndex(int amount)
		{
			index = index + amount;
			nextIndex = index + 1;
		}

		/// <summary> Ends current block at current index and adds it to generatedBlocks. </summary>
		protected void EndBlock()
		{
			int blockLength = index - currentBlockStart;

			if(blockLength == 0)
			{
				return;
			}

			if(currentBlockStart < 0 || blockLength < 0 || index > length)
			{
				if(index < 0)
				{
					UnityEngine.Debug.LogError("EndBlock(" + currentType + ") - blockStart=" + currentBlockStart + ", blockLength=" + blockLength + ", index=" + index + ", length=" + length + " with generatedBlocks:\n" + generatedBlocks);
					return;
				}

				UnityEngine.Debug.LogError("EndBlock(" + currentType + ") - blockStart=" + currentBlockStart + ", blockLength=" + blockLength + ", index=" + index + ", length=" + length + " with generatedBlocks:\n" + generatedBlocks);

				while(index >= length)
				{
					index--;
				}
				if(index <= currentBlockStart)
				{
					return;
				}
			}

			#if DEV_MODE && DEBUG_ENABLED
			UnityEngine.Debug.Log("EndBlock("+currentType+") - blockStart="+currentBlockStart+", blockLength="+blockLength+", index="+index+", length="+length+" with block: \""+codeUnformatted.Substring(currentBlockStart, blockLength)+"\"");
			#endif

			generatedBlocks.Add(new CodeBlock(textUnformatted.Substring(currentBlockStart, blockLength), currentType));
			currentBlockStart = index;
			currentType = CodeBlockType.Default;
		}

		/// <summary> Add line break of given character count to generatedBlocks. </summary>
		/// <param name="charCount"> Number of characters in line break. </param>
		protected void LineBreak(int charCount)
		{
			int blockLength = index - currentBlockStart;
			if(blockLength > 0)
			{
				generatedBlocks.Add(new CodeBlock(textUnformatted.Substring(currentBlockStart, blockLength), currentType));
				currentBlockStart = index;
			}

			AddToIndex(charCount);
			blockLength = index - currentBlockStart;

			if(currentBlockStart < 0 || blockLength < 0 || index > length)
			{
				if(index < 0)
				{
					UnityEngine.Debug.LogError("LineBreak() - blockStart=" + currentBlockStart + ", blockLength=" + blockLength + ", index=" + index + ", length=" + length + " with generatedBlocks:\n" + generatedBlocks);
					return;
				}

				UnityEngine.Debug.LogError("LineBreak(" + currentType + ") - blockStart=" + currentBlockStart + ", blockLength=" + blockLength + ", index=" + index + ", length=" + length + " with generatedBlocks:\n" + generatedBlocks);

				while(index >= length)
				{
					index--;
				}
				if(index <= currentBlockStart)
				{
					return;
				}
			}

			#if DEV_MODE && DEBUG_ENABLED
			UnityEngine.Debug.Log("LineBreak("+currentType+") - blockStart="+currentBlockStart+", blockLength="+blockLength+", index="+index+", length="+length+" with block: \""+codeUnformatted.Substring(currentBlockStart, blockLength)+"\"");
			#endif

			generatedBlocks.Add(new CodeBlock(textUnformatted.Substring(currentBlockStart, blockLength), CodeBlockType.LineBreak));
			currentBlockStart = index;
			lineCount++;
		}

		/// <summary> Determines whether or not element at current index is the start of a comment block. </summary>
		/// <returns> True if is the start of a comment block, false if isn't. </returns>
		protected abstract bool IsCommentBlockStart();

		/// <summary> Determines whether or not element at current index is the end of a comment block. </summary>
		/// <returns> True if is the end of a comment block, false if isn't. </returns>
		protected abstract bool IsCommentBlockEnd();

		/// <summary> Determines whether or not element at current index is the start of a comment line. </summary>
		/// <returns> True if is the start of a comment line, false if isn't. </returns>
		protected abstract bool IsCommentLineStart();

		/// <summary> Determines whether or not character at current index is the escape sequence character. </summary>
		/// <returns> True if is escape sequence, false if isn't. </returns>
		private bool IsEscapeSequence()
		{
			if(isEscapeSequence)
			{
				return false;
			}

			return currentChar == '\\';
		}

		/// <summary> Determines whether or not element starting at current index is the start of a preprocessor directive. </summary>
		/// <returns> True if is the start of preprocessor directive, false if isn't. </returns>
		protected abstract bool IsPreprocessorDirectiveStart();

		/// <summary> Determines whether or not character at current index is the starting tag of a string (think quotation mark). </summary>
		/// <returns> True if is the starting tag of a string, false if isn't. </returns>
		protected virtual bool IsStringStart()
		{
			if(currentChar == '"')
			{
				return true;
			}
			if(currentChar == '@' && nextIndex < length && textUnformatted[nextIndex] == '"')
			{
				return true;
			}
			return false;
		}

		/// <summary> Determines whether or not character at current index is the closing tag of a string (think quotation mark). </summary>
		/// <returns> True if is the closing tag of a string, false if isn't. </returns>
		private bool IsStringEnd()
		{
			return currentChar == '"' && !isEscapeSequence;
		}

		/// <summary> Determines whether or not character at current index is the starting tag of a char. </summary>
		/// <returns> True if is the starting tag of a char, false if isn't. </returns>
		protected virtual bool IsCharStart()
		{
			if(currentChar == '\'')
			{
				return true;
			}
			return false;
		}

		/// <summary> Determines whether or not character at current index is the closing tag of a char. </summary>
		/// <returns> True if is the closing tag of a char, false if isn't. </returns>
		private bool IsCharEnd()
		{
			return currentChar == '\'' && !isEscapeSequence;
		}

		/// <summary> Determines whether or not character at current index is a line end character. </summary>
		/// <returns>
		/// -1 if character is not a line end character.
		/// 1 if character is a single-letter line end character (CR or LF).
		/// 2 if character is a two-letter line end character (CR+LF).
		protected int IsLineEnd()
		{
			switch(currentChar)
			{
				case '\n':
					return 1;
				case '\r':
					if(nextIndex < length && textUnformatted[nextIndex] == '\n')
					{
						return 2;
					}
					return 1;
				default:
					return -1;
			}
		}

		/// <summary> Determines whether or not character at current index is a whitespace. </summary>
		/// <returns>
		/// -1 if character is not a whitespace.
		/// 1 if character is a single-letter whitespace character.
		/// 2 if character is a two-letter whitespace character (CR+LF)
		/// </returns>
		private int IsWhiteSpace()
		{
			switch(currentChar)
			{
				case ' ':
				case '\t':
				case '\n':
					return 1;
				case '\r':
					if(nextIndex >= length && textUnformatted[nextIndex] == '\n')
					{
						return 2;
					}
					return 1;
				default:
					return -1;
			}
		}

		/// <summary> Determines whether or not character at current index is a special character. </summary>
		/// <returns> True if is a special character, false if isn't. </returns>
		private bool IsSpecialCharacter()
		{
			return currentChar.IsSpecialCharacter();
		}

		/// <summary> Determines whether or not character at current index is a number. </summary>
		/// <returns> True if is a number, false if isn't. </returns>
		protected virtual bool IsNumber()
		{
			if(currentType == CodeBlockType.Number)
			{
				switch(currentChar)
				{
					case 'f': //float
					case 'F':
					case 'd': //double
					case 'D':
					case 'm': //decimal
					case 'M':
					case 'b': //binary
					case 'B':
					case '_': //digit separator
					case 'x': //0x marks the beginning of a hexadecimal
					case 'X':
					case 'A': //letters A to F are hexadecimal digits
					case 'a':
					//case 'b':
					//case 'B':
					case 'C':
					case 'c':
					//case 'd':
					//case 'D':
					case 'E':
					case 'e':
						//case 'f':
						//case 'F':
						return true;
				}
			}

			switch(currentChar)
			{
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				case '0':
					return true;
				default:
					return false;
			}
		}

		/// <summary> Determines whether or not word at current index is a type. </summary>
		/// <returns> -1 if word is not type, else returns length of word. </returns>
		protected abstract int IsType();

		/// <summary> Determines whether or not word at current index is a keyword. </summary>
		/// <returns> -1 if word is not a keyword, else returns length of word. </returns>
		protected abstract int IsKeyword();

		/// <summary> Reverts this instance to its default state and disposes it to the object pool. </summary>
		public abstract void Dispose();
	}
}