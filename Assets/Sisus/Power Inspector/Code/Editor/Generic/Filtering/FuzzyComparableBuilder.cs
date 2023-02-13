//#define SAFE_MODE

using System.Collections.Generic;

namespace Sisus
{
	public static class FuzzyComparableBuilder
	{
		private const int Lower = 0;
		private const int Upper = 1;
		private const int NonLetter = 2;

		private const int Space = ' ';

		private static readonly List<int> SplitStringBuilder = new List<int>();
		private static readonly List<int> SplitPointList = new List<int>(3);

		/// <summary>
		/// Generates splitPoints and mainPartIndex from textInput.
		/// </summary>
		/// <param name="textInput"> Text input. </param>
		/// <param name="splitPoints"> Indexes in text containing split points like a space, period, forward slash, backslash or an underline.</param>
		/// <param name="mainPartIndex">
		/// If textInput was split into substrings by splitPoints then mainPartIndex would indicate which part is the main part of the text in terms of text matching priority.
		/// If the text contains any backslash, forward slahes or periods, then the main part of the word will be the section that follows the last occurrence of these characters.
		/// Otherwise the main part is the first part (0 index).
		/// </param>
		/// <returns></returns>
		public static int[] GenerateFuzzyComparableData(string textInput, out int[] splitPoints, out int mainPartIndex)
		{
			#if DEV_MODE
			Profiler.BeginSample("GenerateFuzzyComparableData");
			#endif

			int[] result;
			int length = textInput.Length;
			switch(length)
			{
				case 0:
					result = ArrayPool<int>.ZeroSizeArray;
					splitPoints = ArrayPool<int>.ZeroSizeArray;
					mainPartIndex = -1;
					break;
				case 1:
					result = new int[]{ char.ToLower(textInput[0]) };
					splitPoints = ArrayPool<int>.ZeroSizeArray;
					mainPartIndex = 0;
					break;
				default:
					mainPartIndex = 0;

					int i = 0;
					char c = textInput[0];
					
					if(length >= 2)
					{
						//skip past single-letter + underscore prefixes like "m_"
						if(textInput[1] == '_' && length >= 3)
						{
							i = 2;
							c = textInput[2];
						}
						//skip past underscore prefixes
						else if(c == '_')
						{
							i = 1;
							c = textInput[1];
						}
					}
					char lc = char.ToLower(c);
					int num = lc;
					int prevType = char.IsNumber(lc) ? NonLetter : Upper;

					SplitStringBuilder.Add(num);

					#if DEV_MODE && SAFE_MODE
					for(int t = 0; t <= 9; t++)
					{
						char test = t.ToString()[0];
						UnityEngine.Debug.Assert(!char.IsUpper(test));
						UnityEngine.Debug.Assert(!char.IsLower(test));
					}
					UnityEngine.Debug.Assert(!char.IsLower(' '));
					UnityEngine.Debug.Assert(!char.IsLower('_'));
					UnityEngine.Debug.Assert(!char.IsLower('.'));
					UnityEngine.Debug.Assert(!char.IsLower('/'));
					UnityEngine.Debug.Assert(!char.IsLower('\\'));
					#endif

					// skipping first letter which was already converted to lower case
					for(i++; i < length; i++)
					{
						c = textInput[i];
						num = c;

						switch(num)
						{
							case '0':
							case '1':
							case '2':
							case '3':
							case '4':
							case '5':
							case '6':
							case '7':
							case '8':
							case '9':
								//If this character is a number but previous is not...
								//if(!char.IsNumber(prev))
								if(prevType != NonLetter)
								{
									//SplitPointList.Add(i);
									SplitPointList.Add(SplitStringBuilder.Count);

									// ...add a space before this character.
									// E.g. "Id1" => "Id 1", "FBI123" => "FBI 123", "Array2D" => "Array 2D"
									SplitStringBuilder.Add(Space);
								}
								SplitStringBuilder.Add(num);
								prevType = NonLetter;
								break;
							case ' ':
							case '_':
								SplitPointList.Add(SplitStringBuilder.Count);
								SplitStringBuilder.Add(Space);
								prevType = NonLetter;
								break;
							case '.':
							case '/':
							case '\\':
								SplitPointList.Add(SplitStringBuilder.Count);
								SplitStringBuilder.Add(Space);

								mainPartIndex = SplitPointList.Count;
								prevType = NonLetter;
								break;
							default:
								lc = char.ToLower(c);
								// If this chararacter is an upper case letter...
								if(lc != c)
								{
									// If this character is not already preceded by a split point...
									int splitPointCount = SplitPointList.Count;
									if(splitPointCount == 0 || SplitPointList[splitPointCount - 1] != SplitStringBuilder.Count - 1)
									{
										// ...and previous character is a lower case letter...
										// if(char.IsLower(input[i - 1])) //IsLower returns false for numbers, so no need to check && !IsNumber separately
										if(prevType == Lower)
										{
											// ...add a space before it.
											// E.g. "TestID" => "Test ID", "Test3D => "Test 3D".
											SplitPointList.Add(SplitStringBuilder.Count);

											SplitStringBuilder.Add(Space);
										}
										// ...or if the next character is a lower case letter
										else if(length > i + 1 && char.IsLower(textInput[i + 1])) //IsLower returns false for numbers, so no need to check && !IsNumber separately
										{
											// ...add a space before it.
											// E.g. "FBIDatabase" => "FBI Database", "FBI123" => "FBI 123", "My3DFx" => "My 3D Fx"
											SplitPointList.Add(SplitStringBuilder.Count);

											SplitStringBuilder.Add(Space);
										}
									}

									num = lc;
									SplitStringBuilder.Add(num);
									prevType = Upper;
									break;
								}

								// add lower case character as is to both lists
								SplitStringBuilder.Add(num);
								prevType = Lower;
								break;
						}
					}

					result = SplitStringBuilder.ToArray();
					SplitStringBuilder.Clear();

					splitPoints = SplitPointList.ToArray();
					SplitPointList.Clear();
					break;
			}
			
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(result.Length > 0 || textInput.Length == 0, "GenerateFuzzyComparableData(\"" + textInput+"\") was empty.");
			#endif
			
			#if DEV_MODE
			Profiler.EndSample();
			#endif

			return result;
		}
	}
}