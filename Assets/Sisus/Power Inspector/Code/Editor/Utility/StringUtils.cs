//#define SAFE_MODE

#define DEBUG_TO_STRING_TOO_LONG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;
using JetBrains.Annotations;

using System.Runtime.CompilerServices;

namespace Sisus
{
	/// <summary>
	/// Static utility class with methods for converting objects to human-readable string format
	/// with minimal garbage generation.
	/// </summary>
	[UnityEditor.InitializeOnLoad]
	public static class StringUtils
	{
		public const string DoubleFormat = "0.###################################################################################################################################################################################################################################################################################################################################################";
		public const string Null = "<color=red>null</color>";
		public const string False = "<color=red>False</color>";
		public const string True = "<color=green>True</color>";

		/// <summary>
		/// Number of int to string results to cache.
		/// </summary>
		private const int IntToStringResultCacheCount = 2500;

		private const int MaxToStringResultLength = 175;

		private static int recursiveCallCount = 0;
		private const int MaxRecursiveCallCount = 25;

		/// <summary>
		/// The numbers as string.
		/// </summary>
		private static readonly string[] numbersAsString = new string[IntToStringResultCacheCount];

		/// <summary>
		/// Dictionary where results of ToPascalCase method calls get cached
		/// </summary>
		private static readonly Dictionary<string, string> cachedToPascalCaseResults = new Dictionary<string, string>(10);

		/// <summary>
		/// Initializes static members of the StringUtils class.
		/// Called in the editor because of the InitializeOnLoad attribute
		/// </summary>
		[UsedImplicitly]
		static StringUtils()
		{
			Setup();
		}

		/// <summary>
		/// Initializes static members of the StringUtils class.
		/// Called in the editor because of the InitializeOnLoad attribute
		/// and at runtime because of the RuntimeInitializeOnLoadMethod attribute.
		/// </summary>
		private static void Setup()
		{
			for(int i = IntToStringResultCacheCount - 1; i >= 0; i--)
			{
				numbersAsString[i] = i.ToString();
			}
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string ToString(char character)
		{
			if('\0'.Equals(character))
			{
				return "\\0";
			}

			return new string(character, 1);
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string ToString(Event inputEvent)
		{
			return ToString(inputEvent, false);
		}

		private static string ToString(Event inputEvent, bool usingRawType)
		{
			if(inputEvent == null)
			{
				return "null";
			}
			
			var type = usingRawType ? inputEvent.rawType : inputEvent.type;

			var sb = StringBuilderPool.Create();

			switch(type)
			{
				case EventType.Used:
					if(usingRawType)
					{
						sb.Append("Used");
						break;
					}
					sb.Append(ToString(inputEvent, true));
					sb.Insert(0, "(Used)");
					break;
				case EventType.Ignore:
					if(usingRawType)
					{
						sb.Append("Ignore");
						break;
					}
					sb.Append(ToString(inputEvent, true));
					sb.Insert(0, "(Ignore)");
					break;
				case EventType.KeyDown:
				case EventType.KeyUp:
					sb.Append(type);
					sb.Append("(");
					sb.Append(inputEvent.keyCode);
					sb.Append(")");
					break;
				case EventType.MouseMove:
				case EventType.MouseDrag:
				case EventType.ScrollWheel:
					sb.Append(type);
					sb.Append("(");
					sb.Append(ToString(inputEvent.delta));
					sb.Append(")");
					break;
				case EventType.MouseDown:
				case EventType.MouseUp:
					sb.Append(type);
					sb.Append("(");
					sb.Append(ToString(inputEvent.button));
					sb.Append(")");
					break;
				case EventType.ExecuteCommand:
					sb.Append(type);
					sb.Append("(");
					sb.Append(inputEvent.commandName);
					sb.Append(")");
					break;
				default:
					if(inputEvent.character != 0)
					{
						sb.Append(type);
						sb.Append("(");
						sb.Append(ToString(inputEvent.character));
						sb.Append(")");
						break;
					}
					sb.Append(type);
					break;
			}

			if(inputEvent.modifiers != EventModifiers.None)
			{
				sb.Append("+");
				sb.Append(ToString(inputEvent.modifiers));
			}

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string ToString(EventModifiers eventModifiers)
		{
			if(eventModifiers == EventModifiers.None)
			{
				return "Modifiers:None";
			}

			var sb = StringBuilderPool.Create();
			if((eventModifiers & EventModifiers.Shift) == EventModifiers.Shift)
			{
				sb.Append("+Shift");
			}
			if((eventModifiers & EventModifiers.Control) == EventModifiers.Control)
			{
				sb.Append("+Ctrl");
			}
			if((eventModifiers & EventModifiers.Alt) == EventModifiers.Alt)
			{
				sb.Append("+Alt");
			}
			if((eventModifiers & EventModifiers.FunctionKey) == EventModifiers.FunctionKey)
			{
				sb.Append("+FunctionKey");
			}
			if((eventModifiers & EventModifiers.CapsLock) == EventModifiers.CapsLock)
			{
				sb.Append("+CapsLock");
			}
			if((eventModifiers & EventModifiers.Command) == EventModifiers.Command)
			{
				sb.Append("+Command");
			}
			if((eventModifiers & EventModifiers.Numeric) == EventModifiers.Numeric)
			{
				sb.Append("+Numeric");
			}

			sb.Remove(0,1);
			sb.Insert(0, "Modifiers:");
			return StringBuilderPool.ToStringAndDispose(ref sb);
		}
		
		public static string ToStringCompact(object target)
		{
			recursiveCallCount = 0;

			if(target == null)
			{
				return "null";
			}
			var type = target.GetType();
			if(type != Types.Type)
			{
				string result = ToStringInternal(target);
				if(result.Length < 50)
				{
					return result;
				}
			}
			return ToStringSansNamespace(type);
		}

		public static string ToString(int number)
		{
			if(number < 0)
			{
				try
				{
					number = Mathf.Abs(number);
				}
				catch(OverflowException) //this happens for int.MinValue
				{
					return number.ToString(CultureInfo.InvariantCulture);
				}
				return string.Concat("-", number);
			}
			if(number >= IntToStringResultCacheCount)
			{
				return number.ToString(CultureInfo.InvariantCulture);
			}
			return numbersAsString[number];
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string ToString(int number, int minLengthUsingLeadingZeroes)
		{
			return number.ToString(Concat("D", minLengthUsingLeadingZeroes));
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string ToString(decimal number)
		{
			return number.ToString(CultureInfo.InvariantCulture);
		}

		public static string ToString(short number)
		{
			if(number < 0)
			{
				return string.Concat("-", ToString(-number));
			}
			if(number >= IntToStringResultCacheCount)
			{
				return number.ToString(CultureInfo.InvariantCulture);
			}
			return numbersAsString[number];
		}

		public static string ToString(ushort number)
		{
			if(number >= IntToStringResultCacheCount)
			{
				return number.ToString(CultureInfo.InvariantCulture);
			}
			return numbersAsString[number];
		}

		public static string ToString(uint number)
		{
			if(number >= IntToStringResultCacheCount)
			{
				return number.ToString(CultureInfo.InvariantCulture);
			}
			return numbersAsString[(int)number];
		}
		
		public static string ToString(ulong number)
		{
			if(number >= IntToStringResultCacheCount)
			{
				return number.ToString(CultureInfo.InvariantCulture);
			}
			return numbersAsString[((int)number)];
		}

		public static string ToString(long number)
		{
			if(number >= int.MinValue && number <= int.MaxValue)
			{
				return ToString((int)number);
			}
			return number.ToString(CultureInfo.InvariantCulture);
		}

		public static string ToString(float value)
		{
			int i = Mathf.RoundToInt(value);
			if(Mathf.Abs(value - i) <= 0.005f)
			{
				return ToString(i);
			}
			return value.ToString("0.##", CultureInfo.InvariantCulture);
		}

		public static string ToString(double value)
		{
			var rounded = Math.Round(value);
			if(Math.Abs(value - rounded) <= 0.005d && (rounded < IntToStringResultCacheCount && rounded > -IntToStringResultCacheCount))
			{
				return ToString((int)rounded);
			}
			return value.ToString("0.##", CultureInfo.InvariantCulture);
		}
		
		public static string MakeFieldNameHumanReadable([NotNull]string input)
		{
			#if DEV_MODE
			Profiler.BeginSample("SplitPascalCaseToWords");
			#endif

			string result;
			if(cachedToPascalCaseResults.TryGetValue(input, out result))
			{
				return result;
			}

			int length = input.Length;
			switch(length)
			{
				case 0:
					result = "";
					break;
				case 1:
					result = input.ToUpper();
					break;
				default:
					int i = 0;
					int stop = length;

					//skip past prefixes like "m_"
					if(input[1] == '_' && length >= 3)
					{
						i = 2;
					}
					//handle property backing field
					else if(input[0] == '<')
					{
						i = 1;
						stop = length - 16;
					}
					//skip past "_" prefix
					else if(input[0] == '_')
					{
						i = 1;
					}

					var sb = StringBuilderPool.Create();

					//first letter should always be upper case
					sb.Append(char.ToUpper(input[i]));

					#if DEV_MODE && SAFE_MODE
					for(int s = 0; s <= 9; s++)
					{
						char c = s.ToString()[0];
						Debug.Assert(!char.IsUpper(c));
						Debug.Assert(!char.IsLower(c));
					}
					#endif

					// skipping first letter which was already capitalized
					for(i = i + 1; i < stop; i++)
					{
						char c = input[i];
						
						//If this character is a number...
						if(char.IsNumber(c))
						{
							//...and previous character is a letter...
							if(char.IsLetter(input[i - 1]))
							{
								//...add a space before this character.
								sb.Append(' ');
								//e.g. "Id1" => "Id 1", "FBI123" => "FBI 123", "Array2D" => "Array 2D"
							}
						}
						//If this chararacter is an upper case letter...
						else if(char.IsUpper(c))
						{
							//...and previous character is a lower case letter...
							if(char.IsLower(input[i - 1])) //IsLower returns false for numbers, so no need to check && !IsNumber separately
							{
								//...add a space before it.
								sb.Append(' ');
								//e.g. "TestID" => "Test ID", "Test3D => "Test 3D"
							}
							//...or if the next character is a lower case letter
							// and previous character is not a "split point" character (space, slash, underscore etc.)
							else if(length > i + 1 && char.IsLower(input[i + 1])) //IsLower returns false for numbers, so no need to check && !IsNumber separately
							{
								switch(input[i - 1])
								{
									case ' ':
									case '/':
									case '\\':
									case '_':
									case '-':
										break;
									default:
										//...add a space before it.
										sb.Append(' ');
										//e.g. "FBIDatabase" => "FBI Database", "FBI123" => "FBI 123", "My3DFx" => "My 3D Fx"
										break;
								}
								
							}
						}
						// replace underscores with the space character...
						else if(c == '_')
						{
							// ...unless previous character is a split point
							switch(input[i - 1])
							{
								case ' ':
								case '/':
								case '\\':
								case '_':
								case '-':
									break;
								default:
								sb.Append(' ');
									break;
							}
							continue;
						}
						
						sb.Append(c);
					}

					result = StringBuilderPool.ToStringAndDispose(ref sb);
					break;
			}
			
			cachedToPascalCaseResults.Add(input, result);

			#if DEV_MODE
			Profiler.EndSample();
			#endif

			return result;
		}

		public static string SplitPascalCaseToWords([NotNull]string input)
		{
			#if DEV_MODE
			Profiler.BeginSample("SplitPascalCaseToWords");
			#endif

			string result;
			if(cachedToPascalCaseResults.TryGetValue(input, out result))
			{
				return result;
			}

			int length = input.Length;
			switch(length)
			{
				case 0:
					result = "";
					break;
				case 1:
					result = input.ToUpper();
					break;
				default:
					int i = 0;

					//skip past prefixes like "m_"
					if(input[1] == '_' && length >= 3)
					{
						i = 2;
					}
					//skip past "_" prefix
					else if(input[0] == '_')
					{
						i = 1;
					}
					
					var sb = StringBuilderPool.Create();

					//first letter should always be upper case
					sb.Append(char.ToUpper(input[i]));

					#if DEV_MODE && SAFE_MODE
					for(int s = 0; s <= 9; s++)
					{
						char c = s.ToString()[0];
						Debug.Assert(!char.IsUpper(c));
						Debug.Assert(!char.IsLower(c));
					}
					#endif

					// skipping first letter which was already capitalized
					for(i = i + 1; i < length; i++)
					{
						char c = input[i];
						
						//If this character is a number...
						if(char.IsNumber(c))
						{
							//...and previous character is a letter...
							if(char.IsLetter(input[i - 1]))
							{
								//...add a space before this character.
								sb.Append(' ');
								//e.g. "Id1" => "Id 1", "FBI123" => "FBI 123", "Array2D" => "Array 2D"
							}
						}
						//If this chararacter is an upper case letter...
						else if(char.IsUpper(c))
						{
							//...and previous character is a lower case letter...
							if(char.IsLower(input[i - 1])) //IsLower returns false for numbers, so no need to check && !IsNumber separately
							{
								//...add a space before it.
								sb.Append(' ');
								//e.g. "TestID" => "Test ID", "Test3D => "Test 3D"
							}
							//...or if the next character is a lower case letter
							// and previous character is not a "split point" character (space, slash, underscore etc.)
							else if(length > i + 1 && char.IsLower(input[i + 1])) //IsLower returns false for numbers, so no need to check && !IsNumber separately
							{
								switch(input[i - 1])
								{
									case ' ':
									case '/':
									case '\\':
									case '_':
									case '-':
										break;
									default:
										//...add a space before it.
										sb.Append(' ');
										//e.g. "FBIDatabase" => "FBI Database", "FBI123" => "FBI 123", "My3DFx" => "My 3D Fx"
										break;
								}
								
							}
						}
						// replace underscores with the space character...
						else if(c == '_')
						{
							// ...unless previous character is a split point
							switch(input[i - 1])
							{
								case ' ':
								case '/':
								case '\\':
								case '_':
								case '-':
									break;
								default:
								sb.Append(' ');
									break;
							}
							continue;
						}
						
						sb.Append(c);
					}

					result = StringBuilderPool.ToStringAndDispose(ref sb);
					break;
			}
			
			cachedToPascalCaseResults.Add(input, result);

			#if DEV_MODE
			Profiler.EndSample();
			#endif

			return result;
		}

		public static string ToString(DateTime time)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(ToString(time.Year));
			sb.Append(" ");
			sb.Append(ToString(time.Month));
			sb.Append("/");
			sb.Append(ToString(time.Day));
			sb.Append(" ");
			sb.Append(ToString(time.Hour));
			sb.Append(":");
			sb.Append(ToString(time.Minute));
			sb.Append(":");
			sb.Append(ToString(time.Second));
			int ms = time.Millisecond;
			if(ms != 0)
			{
				sb.Append(".");
				sb.Append(ToString(ms));
			}

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string TimeToString(DateTime time)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(ToString(time.Hour));
			sb.Append(":");
			sb.Append(ToString(time.Minute));
			sb.Append(":");
			sb.Append(ToString(time.Second));
			int ms = time.Millisecond;
			if(ms != 0)
			{
				sb.Append(".");
				sb.Append(ToString(time.Millisecond));
			}

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string ToString(TimeSpan time)
		{
			var sb = StringBuilderPool.Create();

			if(time.TotalMilliseconds < 0)
			{
				time = time.Negate();
				sb.Append("-");
			}

			int d = time.Days;
			if(d != 0)
			{
				sb.Append(ToString(d));
				sb.Append("d ");
			}

			int h = time.Hours;
			if(h != 0)
			{
				sb.Append(ToString(h));
				sb.Append("h ");
			}

			int m = time.Minutes;
			if(m != 0)
			{
				sb.Append(ToString(m));
				sb.Append("m ");
			}

			int s = time.Seconds;
			int ms = Mathf.Abs(time.Milliseconds);

			if(s != 0 || ms != 0 || (d == 0 && h == 0 && m == 0))
			{
				sb.Append(ToString(s));
				
				if(ms != 0)
				{
					//remove trailing zeroes
					while(ms % 10 == 0)
					{
						ms = ms / 10;
					}

					sb.Append(".");
					sb.Append(ToString(ms));
				}
				sb.Append("s");
			}

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string ToString(UnityEngine.SceneManagement.Scene scene)
		{
			if(!scene.IsValid())
			{
				return Concat("\"", scene.name, "\"(Invalid)");
			}
			if(scene.isLoaded)
			{
				return Concat("\"", scene.name, "\"(Loaded)");
			}
			return Concat("\"", scene.name, "\"");
		}

		public static string ToString(IDrawer target)
		{
			return target == null ? "null" : target.ToString();
		}

		public static string ToString(UnityEditor.SerializedProperty target)
		{
			return target == null ? "null" : target.propertyPath;
		}
		
		/// <summary>
		/// Converts any given object supported by Unity serialization into a simple and readable string form.
		/// For more complex objects it can use Json serialization.
		/// If length of resulting string would be really long, will just return type name instead.
		/// </summary>
		/// <param name="target">target to convert to string form</param>
		/// <returns>Target represented in human readable string format.</returns>
		public static string ToString(object target)
		{
			recursiveCallCount = 0;
			return ToStringInternal(target);
		}

		/// <summary>
		/// Converts any given object supported by Unity serialization into a simple and readable string form.
		/// For more complex objects it can use Json serialization.
		/// If length of resulting string would be really long, will just return type name instead.
		/// </summary>
		/// <param name="target">target to convert to string form</param>
		/// <returns>Target represented in human readable string format.</returns>
		private static string ToStringInternal(object target)
		{
			if(target == null)
			{
				return "null";
			}

			if(target is Object && target as Object == null)
			{
				return "null";
			}

			var type = target.GetType();
			
			if(type.IsPrimitive)
			{
				if(type == Types.Int)
				{
					return ToString((int)target);
				}

				return target.ToString();
			}

			var asString = target as string;
			if(asString != null)
			{
				return string.Concat("\"", asString, "\"");
			}

			if(type.IsEnum)
			{
				return target.ToString();
			}

			recursiveCallCount++;
			if(recursiveCallCount > MaxRecursiveCallCount)
			{
				recursiveCallCount = 0;
				#if DEV_MODE
				Debug.LogError("StringUtils.ToString max recursive call count ("+MaxRecursiveCallCount+") exceeded with target of type "+TypeToString(target));
				#endif
				return TypeToString(target);
			}

			var ienumerable = target as IEnumerable;
			if(ienumerable != null)
			{
				var asArray = target as Array;
				if(asArray != null)
				{
					return ToString(asArray);
				}

				var asDictionary = target as IDictionary;
				if(asDictionary != null)
				{
					return ToString(asDictionary);
				}

				var asCollection = target as ICollection;
				if(asCollection != null)
				{
					return ToString(asCollection);
				}

				// Only use IEnumerable based ToString if DeclaringType is IEnumerable. Don's use for other classes
				// because they might override ToString() or have many properties besides those accessible via the enumerator.
				if(target.GetType() == typeof(IEnumerable) || target.GetType().DeclaringType == typeof(IEnumerable<>))
				{
					return ToString(ienumerable);
				}
			}
			
			var asEvent = target as Event;
			if(asEvent != null)
			{
				return ToString(asEvent);
			}

			var asGUIContent = target as GUIContent;
			if(asGUIContent != null)
			{
				return ToString(asGUIContent);
			}

			if(type == Types.MonoScript)
			{
				return ToString(target as Object);
			}

			if(type == Types.TextAsset)
			{
				return ToString(target as Object);
			}
			
			try
			{
				string toStringResult = target.ToString();
				if(!string.Equals(toStringResult, type.ToString()))
				{
					if(toStringResult.Length <= MaxToStringResultLength)
					{
						return toStringResult;
					}
					//UPDATE: No longer ignoring, just getting a Substring
					#if DEV_MODE && DEBUG_TO_STRING_TOO_LONG
					//if(!string.Equals(toStringResult, type.ToString())) { Debug.LogWarning("Ignoring "+ type.Name+".ToString() result because length "+ toStringResult.Length+" > "+ MaxToStringResultLength+":\n"+toStringResult); }
					#endif

					return toStringResult.Substring(MaxToStringResultLength);
				}
			}
			catch(Exception e)
			{
				Debug.LogError(e);
			}

			var asObject = target as Object;
			if(asObject != null)
			{
				return ToString(asObject);
			}
			
			var asDelegate = target as MulticastDelegate;
			if(asDelegate != null)
			{
				return ToString(asDelegate);
			}

			return ToStringSansNamespace(type);
		}

		public static string ToString(Object target)
		{
			if(target == null)
			{
				return "null";
			}

			var trans = target.Transform();
			if(trans != null)
			{
				return Concat(ToString(trans.GetHierarchyPath()), "(", target.GetType(), ")");
			}

			return Concat(ToString(target.name), "(", target.GetType(), ")");
		}

		public static string ToString([CanBeNull]IList<Object> list, string delimiter = ",")
		{
			if(list == null)
			{
				return "null";
			}

			int lastIndex = list.Count - 1;
			if(lastIndex == -1)
			{
				return "{}";
			}

			var builder = StringBuilderPool.Create();
			builder.Append('{');
			for(int n = 0; n < lastIndex; n++)
			{
				Append(list[n], builder);
				builder.Append(delimiter);
			}
			Append(list[lastIndex], builder);
			builder.Append('}');
			
			return StringBuilderPool.ToStringAndDispose(ref builder);
		}

		public static void Append(Object target, StringBuilder toBuilder)
		{
			if(target == null)
			{
				toBuilder.Append("null");
				return;
			}

			toBuilder.Append("\"");
			var trans = target.Transform();
			if(trans != null)
			{
				toBuilder.Append(trans.GetHierarchyPath());
			}
			else
			{
				toBuilder.Append(target.name);
			}
			toBuilder.Append("\"(");
			toBuilder.Append(target.GetType().Name);
			toBuilder.Append(")");
		}

		public static string ToString(ConstructorInfo constructor)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(constructor.Name);

			var parameters = constructor.GetParameters();
			int parameterCount = parameters.Length;
			if(parameterCount > 0)
			{
				sb.Append('(');

				sb.Append(ToString(parameters[0].ParameterType));

				for(int n = 1; n < parameterCount; n++)
				{
					var parameter = parameters[n];
					sb.Append(',');
					sb.Append(ToString(parameter.ParameterType));
				}
				sb.Append(')');
			}

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}
		
		public static string ToString(Array array, string delimiter = ",")
		{
			if(array == null)
			{
				return "null";
			}

			switch(array.Rank)
			{
				case 1:
					return Array1DToString(array, delimiter);
				case 2:
					return Array2DToString(array, delimiter);
				default:
					return TypeToString(array);
			}
		}

		public static string Array1DToString(Array array, string delimiter = ",")
		{
			if(array == null)
			{
				return "null";
			}
		
			int lastIndex = array.Length - 1;
			if(lastIndex == -1)
			{
				return "{}";
			}

			var builder = StringBuilderPool.Create();
			builder.Append('{');
			for(int n = 0; n < lastIndex; n++)
			{
				builder.Append(ToString(array.GetValue(n)));
				builder.Append(delimiter);
			}
			builder.Append(ToString(array.GetValue(lastIndex)));
			builder.Append('}');

			return StringBuilderPool.ToStringAndDispose(ref builder);
		}

		public static string Array2DToString(Array array, string delimiter = ",")
		{
			if(array == null)
			{
				return "null";
			}

			int lastIndexY = array.GetLength(1) - 1;
			if(lastIndexY == -1)
			{
				return "{}";
			}

			int lastIndexX = array.GetLength(0) - 1;

			#if DEV_MODE
			Debug.Log("Array2DToString called for array with GetLength(0)="+(lastIndexX+1)+", GetLength(1)="+(lastIndexY+1));
			#endif

			var builder = StringBuilderPool.Create();
			builder.Append('{');
			for(int y = 0; y < lastIndexY; y++)
			{
				builder.Append('{');
				if(lastIndexX > 0)
				{
					for(int x = 0; x < lastIndexX; x++)
					{
						builder.Append(ToString(array.GetValue(x, y)));
						builder.Append(delimiter);
					}
					builder.Append(ToString(array.GetValue(lastIndexX, y)));
				}
				builder.Append('}');
				builder.Append(delimiter);
			}
			if(lastIndexX > 0)
			{
				builder.Append('{');
				if(lastIndexY > 0)
				{
					for(int x = 0; x < lastIndexX; x++)
					{
						builder.Append(ToString(array.GetValue(x, lastIndexY)));
						builder.Append(delimiter);
					}
					builder.Append(ToString(array.GetValue(lastIndexX, lastIndexY)));
				}
				builder.Append('}');
			}
			builder.Append('}');

			return StringBuilderPool.ToStringAndDispose(ref builder);
		}

		public static string TypeToString([CanBeNull]object target)
		{
			if(target is Object)
			{
				return TypeToString(target as Object);
			}

			if(target == null)
			{
				return "null";
			}

			return ToString(target.GetType());
		}

		public static string TypeToString([CanBeNull]Object target)
		{
			if(target == null)
			{
				return "null";
			}

			return ToString(target.GetType());
		}

		public static string TypesToString([CanBeNull]IList list, string delimiter = ",")
		{
			if(list == null)
			{
				return "null";
			}
		
			int lastIndex = list.Count - 1;
			if(lastIndex == -1)
			{
				return "{}";
			}

			var builder = StringBuilderPool.Create();
			builder.Append('{');
			for(int n = 0; n < lastIndex; n++)
			{
				builder.Append(TypeToString(list[n]));
				builder.Append(delimiter);
			}
			builder.Append(TypeToString(list[lastIndex]));
			builder.Append('}');

			return StringBuilderPool.ToStringAndDispose(ref builder);
		}

		public static string ToString(byte[] bytes)
		{
			return Encoding.UTF8.GetString(bytes);
		}

		public static string ToString([CanBeNull]IList list, string delimiter = ", ")
		{
			if(list == null)
			{
				return "null";
			}

			int lastIndex = list.Count - 1;
			if(lastIndex == -1)
			{
				return "{}";
			}

			//don's use the cached StringBuilder because it could get cleared
			//when calling ToString for collection contents
			var builder = StringBuilderPool.Create();

			builder.Append('{');
			for(int n = 0; n < lastIndex; n++)
			{
				recursiveCallCount = 0;

				builder.Append(ToString(list[n]));
				builder.Append(delimiter);
			}

			builder.Append(ToString(list[lastIndex]));
			builder.Append('}');

			return StringBuilderPool.ToStringAndDispose(ref builder);
		}

		public static string ToString([CanBeNull]IDictionary collection, string delimiter = ", ")
		{
			if(collection == null)
			{
				return "null";
			}

			int lastIndex = collection.Count - 1;
			if(lastIndex == -1)
			{
				return "{}";
			}

			recursiveCallCount = 0;

			//don's use the cached StringBuilder because it could get cleared
			//when calling ToString for collection contents
			var builder = StringBuilderPool.Create();
			builder.Append('{');
			foreach(DictionaryEntry entry in collection)
			{
				builder.Append(ToString(entry));
				builder.Append(delimiter);
			}
			builder[builder.Length-1] = '}';
			return StringBuilderPool.ToStringAndDispose(ref builder);
		}

		public static string ToString(ICollection collection, string delimiter = ", ")
		{
			if(collection == null)
			{
				return "null";
			}
		
			int lastIndex = collection.Count - 1;
			if(lastIndex == -1)
			{
				return "{}";
			}

			recursiveCallCount = 0;

			//don's use the cached StringBuilder because it could get cleared
			//when calling ToString for collection contents
			var builder = StringBuilderPool.Create();
			builder.Append('{');
			foreach(var entry in collection)
			{
				builder.Append(ToString(entry));
				builder.Append(delimiter);
			}
			builder[builder.Length-1] = '}';

			return StringBuilderPool.ToStringAndDispose(ref builder);
		}

		public static string LengthToString([CanBeNull]ICollection collection)
		{
			return collection == null ? "null" : ToString(collection.Count);
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string ToString([CanBeNull]string text)
		{
			return text == null ? "null" : Concat("\"", text, "\"");
		}

		public static string ToString([CanBeNull]IEnumerable collection, string delimiter = ", ")
		{
			if(collection == null)
			{
				return "null";
			}

			//don's use the cached StringBuilder because it could get cleared
			//when calling ToString for collection contents
			var builder = StringBuilderPool.Create();

			builder.Append('{');
			
			foreach(var entry in collection)
			{
				builder.Append(ToString(entry));
				builder.Append(delimiter);
			}

			if(builder.Length == 1)
			{
				builder.Append('}');
			}
			else
			{
				// replace last delimiter character with collection end character
				// NOTE: Currently only works with delimiters that are one character long!
				builder[builder.Length - 1] = '}';
			}
			
			return StringBuilderPool.ToStringAndDispose(ref builder);
		}

		public static string ToString([CanBeNull]IEnumerable<Object> collection, string delimiter = ", ")
		{
			if(collection == null)
			{
				return "null";
			}

			//don's use the cached StringBuilder because it could get cleared
			//when calling ToString for collection contents
			var builder = StringBuilderPool.Create();

			builder.Append('{');
			
			foreach(var unityObject in collection)
			{
				Append(unityObject, builder);
				builder.Append(delimiter);
			}

			if(builder.Length == 1)
			{
				builder.Append('}');
			}
			else
			{
				// replace last delimiter character with collection end character
				// NOTE: Currently only works with delimiters that are one character long!
				builder[builder.Length - 1] = '}';
			}
			
			return StringBuilderPool.ToStringAndDispose(ref builder);
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string Concat(string a, string b)
		{
			return string.Concat(a,b);
		}

		public static string Concat(string s1, string s2, string s3, string s4, string s5, string s6, string s7, string s8, string s9, string s10)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(s1);
			sb.Append(s2);
			sb.Append(s3);
			sb.Append(s4);
			sb.Append(s5);
			sb.Append(s6);
			sb.Append(s7);
			sb.Append(s8);
			sb.Append(s9);
			sb.Append(s10);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string s1, string s2, string s3, string s4, string s5, string s6, string s7, string s8)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(s1);
			sb.Append(s2);
			sb.Append(s3);
			sb.Append(s4);
			sb.Append(s5);
			sb.Append(s6);
			sb.Append(s7);
			sb.Append(s8);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string Concat(string s, int number)
		{
			return string.Concat(s, ToString(number));
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string Concat(int number, string s)
		{
			return string.Concat(ToString(number), s);
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string Concat(string a, string b, string c)
		{
			return string.Concat(a,b,c);
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string Concat(string a, string b, string c, string d)
		{
			return string.Concat(a,b,c,d);
		}

		public static string Concat(string a, string b, string c, string d, string e)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			sb.Append(d);
			sb.Append(e);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string a, string b, string c, string d, string e, string f)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			sb.Append(d);
			sb.Append(e);
			sb.Append(f);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string s, string s2, Type type, string s3, string s4, string s5)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(s);
			sb.Append(s2);
			sb.Append(type);
			sb.Append(s3);
			sb.Append(s4);
			sb.Append(s5);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string s1, Type type, string s2)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(s1);
			sb.Append(type);
			sb.Append(s2);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string s1, string s2, Type type, string s3)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(s1);
			sb.Append(s2);
			sb.Append(ToStringSansNamespace(type));
			sb.Append(s3);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string s1, Type type1, string s2, Type type2)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(s1);
			sb.Append(ToStringSansNamespace(type1));
			sb.Append(s2);
			sb.Append(ToStringSansNamespace(type2));

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string s1, Type type1, string s2, Type type2, string s3)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(s1);
			sb.Append(type1);
			sb.Append(s2);
			sb.Append(type2);
			sb.Append(s3);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string s1, Type type1, string s2, Type type2, string s3, Type type3, string s4)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(s1);
			sb.Append(type1);
			sb.Append(s2);
			sb.Append(type2);
			sb.Append(s3);
			sb.Append(type3);
			sb.Append(s4);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string s, string s2, string s3, Type type)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(s);
			sb.Append(s2);
			sb.Append(s3);
			sb.Append(ToStringSansNamespace(type));

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string a, int b, string c)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(a);
			sb.Append(ToString(b));
			sb.Append(c);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(float f1, string s, float f2)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(ToString(f1));
			sb.Append(s);
			sb.Append(ToString(f2));

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(int i1, string s, int i2)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(ToString(i1));
			sb.Append(s);
			sb.Append(ToString(i2));

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string a, string b, int c, string d)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(a);
			sb.Append(b);
			sb.Append(ToString(c));
			sb.Append(d);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string a, int b, string c, object d)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(a);
			sb.Append(ToString(b));
			sb.Append(c);
			
			var ds = d as string;
			if(ds != null)
			{
				sb.Append(ds);
			}
			else
			{
				sb.Append(ToString(d));
			}

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(int num, string s1, Type type, string s2)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(ToString(num));
			sb.Append(s1);
			sb.Append(ToStringSansNamespace(type));
			sb.Append(s2);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string a, int b, string c, int d, string e)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(a);
			sb.Append(ToString(b));
			sb.Append(c);
			sb.Append(ToString(d));
			sb.Append(e);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}
		
		public static string Concat(string s1, string s2, int n, string s3, string s4)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(s1);
			sb.Append(ToString(s2));
			sb.Append(n);
			sb.Append(ToString(s3));
			sb.Append(s4);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string a, int b, string c, int d, string e, int f, string g)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(a);
			sb.Append(ToString(b));
			sb.Append(c);
			sb.Append(ToString(d));
			sb.Append(e);
			sb.Append(ToString(f));
			sb.Append(g);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(float f1, string s1, float f2, string s2, float f3)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(ToString(f1));
			sb.Append(s1);
			sb.Append(ToString(f2));
			sb.Append(s2);
			sb.Append(ToString(f3));

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(float f1, string s1, float f2, string s2, float f3, string s3, float f4)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(ToString(f1));
			sb.Append(s1);
			sb.Append(ToString(f2));
			sb.Append(s2);
			sb.Append(ToString(f3));
			sb.Append(s3);
			sb.Append(ToString(f4));

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(int i1, string s1, int i2, string s2, int i3)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(ToString(i1));
			sb.Append(s1);
			sb.Append(ToString(i2));
			sb.Append(s2);
			sb.Append(ToString(i3));

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(int i1, string s1, int i2, string s2, int i3, string s3, int i4)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(ToString(i1));
			sb.Append(s1);
			sb.Append(ToString(i2));
			sb.Append(s2);
			sb.Append(ToString(i3));
			sb.Append(s3);
			sb.Append(ToString(i4));

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string s, string s2, string s3, string s4, string s5, int n, string s8)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(s);
			sb.Append(s2);
			sb.Append(s3);
			sb.Append(s4);
			sb.Append(s5);
			sb.Append(n);
			sb.Append(s8);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string a, string b, string c, string d, string e, string f, string g)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			sb.Append(d);
			sb.Append(e);
			sb.Append(f);
			sb.Append(g);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string s, string s2, string s3, string s4, string s5, string s6, string s7, int n, string s8)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(s);
			sb.Append(s2);
			sb.Append(s3);
			sb.Append(s4);
			sb.Append(s5);
			sb.Append(s6);
			sb.Append(s7);
			sb.Append(n);
			sb.Append(s8);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string Concat(string s, string s2, Type type, string s3, string s4, string s5, int number, string s6)
		{
			var sb = StringBuilderPool.Create();

			sb.Append(s);
			sb.Append(s2);
			sb.Append(ToStringSansNamespace(type));
			sb.Append(s3);
			sb.Append(s4);
			sb.Append(s5);
			sb.Append(ToString(number));
			sb.Append(s6);

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}
		
		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static bool IsSpecialCharacter(this char character)
		{
			switch(character)
			{
				case '.':
				case '=':
				case '+':
				case '-':
				case '*':
				case '/':
				case '\\':
				case '%':
				case '<':
				case '>':
				case '!':
				case ';':
				case '?':
				case ':':
				case '&':
				case '|':
				case '(':
				case ')':
				case '{':
				case '}':
				case '[':
				case ']':
				case '~':
				case '"':
				case '\'':
					return true;
				default:
					return false;
			}
		}

		[MethodImpl(256)] //MethodImplOptions.AggressiveInlining
		public static bool IsSpace(this char character)
		{
			return character.Equals(' ');
		}


		[MethodImpl(256)] //MethodImplOptions.AggressiveInlining
		public static bool IsWhiteSpace(this char character)
		{
			switch(character)
			{
				case ' ':
				case '\t':
				case '\n':
				case '\r':
					return true;
				default:
					return false;
			}
		}

		[MethodImpl(256)] //MethodImplOptions.AggressiveInlining
		public static bool IsLineEnd(this char character)
		{
			switch(character)
			{
				case '\r':
				case '\n':
					return true;
				default:
					return false;
			}
		}

		public static bool SubstringEqualsWholeWord(string text, int startIndex, string substring)
		{
			int count = substring.Length;
			int lastIndex = startIndex + count - 1;
			int textLength = text.Length;

			if(textLength <= lastIndex)
			{
				return false;
			}
			
			for(int n = count - 1; n >= 0; n--)
			{
				if(text[startIndex + n] != substring[n])
				{
					return false;
				}
			}

			lastIndex++;
			if(textLength > lastIndex && !text[lastIndex].IsSpecialCharacter() && !text[lastIndex].IsWhiteSpace())
			{
				return false;
			}

			startIndex--;
			if(startIndex >= 0 && !text[startIndex].IsSpecialCharacter() && !text[startIndex].IsWhiteSpace())
			{
				return false;
			}

			return true;
		}

		public static string ToString([CanBeNull]Object[] array, string delimiter = ", ")
		{
			if(array == null)
			{
				return "null";
			}

			int lastIndex = array.Length - 1;
			if(lastIndex == -1)
			{
				return "{}";
			}

			var builder = StringBuilderPool.Create();
			builder.Append('{');
			for(int n = 0; n < lastIndex; n++)
			{
				builder.Append(ToString(array[n]));
				builder.Append(delimiter);
			}
			builder.Append(ToString(array[lastIndex]));
			builder.Append('}');
			
			return StringBuilderPool.ToStringAndDispose(ref builder);
		}

		public static string NamesToString([CanBeNull]Object[] array, string delimiter = ", ", bool brackets = false)
		{
			if(array == null)
			{
				return "null";
			}

			int lastIndex = array.Length - 1;
			if(lastIndex == -1)
			{
				return "{}";
			}

			var builder = StringBuilderPool.Create();
			if(brackets)
			{
				builder.Append('{');
			}
			for(int n = 0; n < lastIndex; n++)
			{
				builder.Append(array[n] == null ? "null" : array[n].name);
				builder.Append(delimiter);
			}
			builder.Append(array[lastIndex] == null ? "null" : array[lastIndex].name);
			if(brackets)
			{
				builder.Append('}');
			}
			
			return StringBuilderPool.ToStringAndDispose(ref builder);
		}

		public static string ToString(GUIContent target)
		{
			#if DEV_MODE
			return target == null ? "null" : target == GUIContent.none ? "none" : string.Concat('\"', target.text, '\"');
			#else
			return target == null ? "null" : string.Concat('\"', target.text, '\"');
			#endif
		}
		
		public static string ToString(MulticastDelegate target)
		{
			if(target == null)
			{
				return "null";
			}

			var invocationList = target.GetInvocationList();
			
			int lastIndex = invocationList.Length - 1;
			if(lastIndex == -1)
			{
				return "{}";
			}

			if(lastIndex == 0)
			{
				return ToString(invocationList[0]);
			}

			var builder = StringBuilderPool.Create();
			builder.Append('{');
			for(int n = 0; n < lastIndex; n++)
			{
				ToString(invocationList[n], ref builder);
				builder.Append(',');
			}
			ToString(invocationList[lastIndex], ref builder);
			builder.Append('}');

			return StringBuilderPool.ToStringAndDispose(ref builder);
		}

		public static string ToString([CanBeNull]Delegate del)
		{
			if(del == null)
			{
				return "null";
			}

			var target = del.Target;
			if(target == null)
			{
				return del.Method.Name;
			}

			var builder = StringBuilderPool.Create();
			builder.Append(TypeToString(target));
			builder.Append('.');
			builder.Append(del.Method.Name);
			return StringBuilderPool.ToStringAndDispose(ref builder);
		}

		public static void ToString([CanBeNull]Delegate del, [NotNull]ref StringBuilder builder)
		{
			if(del == null)
			{
				builder.Append("null");
			}

			var target = del.Target;
			if(target != null)
			{
				builder.Append(TypeToString(target));
				builder.Append('.');
			}
			builder.Append(del.Method.Name);
		}

		public static string ToString(Vector2 v)
		{
			var sb = StringBuilderPool.Create();

			sb.Append("(");
			sb.Append(ToString(v.x));
			sb.Append(",");
			sb.Append(ToString(v.y));
			sb.Append(")");

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string ToString(Vector3 v)
		{
			var sb = StringBuilderPool.Create();

			sb.Append("(");
			sb.Append(ToString(v.x));
			sb.Append(",");
			sb.Append(ToString(v.y));
			sb.Append(",");
			sb.Append(ToString(v.z));
			sb.Append(")");

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string ToString(Rect rect)
		{
			var sb = StringBuilderPool.Create();

			sb.Append("(");
			sb.Append(ToString(rect.x));
			sb.Append(",");
			sb.Append(ToString(rect.y));
			sb.Append(",");
			sb.Append(ToString(rect.width));
			sb.Append(",");
			sb.Append(ToString(rect.height));
			sb.Append(")");

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static string ToString(Bounds bounds)
		{
			var sb = StringBuilderPool.Create();

			sb.Append("(x=");
			sb.Append(ToString(bounds.min.x));
			sb.Append("...");
			sb.Append(ToString(bounds.max.x));
			sb.Append(", y=");
			sb.Append(ToString(bounds.min.y));
			sb.Append("...");
			sb.Append(ToString(bounds.max.y));
			sb.Append(", z=");
			sb.Append(ToString(bounds.min.z));
			sb.Append("...");
			sb.Append(ToString(bounds.max.z));
			sb.Append(")");

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string ToString<TKey,TValue>(KeyValuePair<TKey,TValue> keyValuePair)
		{
			return Concat("KeyValuePair(", ToString(keyValuePair.Key), ",", ToString(keyValuePair.Value), ")");
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string ToString(DictionaryEntry dictionaryEntry)
		{
			return Concat("DictionaryEntry(", ToString(dictionaryEntry.Key), ",", ToString(dictionaryEntry.Value), ")");
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		private static string ToStringInternal(DictionaryEntry dictionaryEntry)
		{
			return Concat("DictionaryEntry(", ToStringInternal(dictionaryEntry.Key), ",", ToStringInternal(dictionaryEntry.Value), ")");
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string ToStringSansNamespace([CanBeNull]Type type)
		{
			if(type == null)
			{
				return "null";
			}
			return TypeExtensions.GetShortName(type);
		}

		public static string ToString([CanBeNull]Type type)
		{
			if(type == null)
			{
				return "null";
			}

			// important to check for IsEnum before GetTypeCode, because GetTypeCode returns the underlying type for enums!
			if(type.IsEnum)
			{
				return type.FullName;
			}
			switch(Type.GetTypeCode(type))
			{
				case TypeCode.Boolean:
					return "bool";
				case TypeCode.Char:
					return "char";
				case TypeCode.SByte:
					return "sbyte";
				case TypeCode.Byte:
					return "byte";
				case TypeCode.Int16:
					return "short";
				case TypeCode.UInt16:
					return "ushort";
				case TypeCode.Int32:
					return "int";
				case TypeCode.UInt32:
					return "uint";
				case TypeCode.Int64:
					return "long";
				case TypeCode.UInt64:
					return "ulong";
				case TypeCode.Single:
					return "float";
				case TypeCode.Double:
					return "double";
				case TypeCode.Decimal:
					return "decimal";
				case TypeCode.String:
					return "string";
			}

			return TypeExtensions.GetFullName(type);
		}

		internal static void ToString([NotNull]Type type, char namespaceDelimiter, [NotNull]StringBuilder builder, Type[] genericTypeArguments = null)
		{
			if(type.IsGenericParameter)
			{
				builder.Append(type.Name);
				return;
			}

			if(type.IsArray)
			{
				ToString(type.GetElementType(), namespaceDelimiter, builder);
				int rank = type.GetArrayRank();
				switch(rank)
				{
					case 1:
						builder.Append("[]");
						break;
					case 2:
						builder.Append("[,]");
						break;
					case 3:
						builder.Append("[,,]");
						break;
					default:
						builder.Append('[');
						for(int n = 1; n < rank; n++)
						{
							builder.Append(',');
						}
						builder.Append(']');
						break;
				}
				return;
			}

			if(namespaceDelimiter != '\0' && type.Namespace != null)
			{
				var namespacePart = type.Namespace;
				if(namespaceDelimiter != '.')
				{
					namespacePart = namespacePart.Replace('.', namespaceDelimiter);
				}
				builder.Append(namespacePart);
				builder.Append(namespaceDelimiter);
			}
			else
			{
				// important to check for IsEnum before GetTypeCode, because GetTypeCode it returns the underlying type for enums!
				if(type.IsEnum)
				{
					builder.Append(type.Name);
					return;
				}

				switch(Type.GetTypeCode(type))
				{
					case TypeCode.Boolean:
						builder.Append("bool");
						return;
					case TypeCode.Char:
						builder.Append("char");
						return;
					case TypeCode.SByte:
						builder.Append("sbyte");
						return;
					case TypeCode.Byte:
						builder.Append("byte");
						return;
					case TypeCode.Int16:
						builder.Append("short");
						return;
					case TypeCode.UInt16:
						builder.Append("ushort");
						return;
					case TypeCode.Int32:
						builder.Append("int");
						return;
					case TypeCode.UInt32:
						builder.Append("uint");
						return;
					case TypeCode.Int64:
						builder.Append("long");
						return;
					case TypeCode.UInt64:
						builder.Append("ulong");
						return;
					case TypeCode.Single:
						builder.Append("float");
						return;
					case TypeCode.Double:
						builder.Append("double");
						return;
					case TypeCode.Decimal:
						builder.Append("decimal");
						return;
					case TypeCode.String:
						builder.Append("string");
						return;
				}

				if(type == Types.SystemObject)
				{
					builder.Append("object");
					return;
				}
			}

			#if CSHARP_7_3_OR_NEWER
			// You can create instances of a constructed generic type.
			// E.g. Dictionary<int, string> instead of Dictionary<TKey, TValue>.
			if(type.IsConstructedGenericType)
			{
				genericTypeArguments = type.GenericTypeArguments;
			}
			#endif

			// If this is a nested class type then append containing type(s) before continuing.
			var containingClassType = type.DeclaringType;
			if(containingClassType != null && type != containingClassType)
			{
				// GenericTypeArguments can't be fetched from the containing class type
				// so need to pass them along to the ToString method and use them instead of
				// the results of GetGenericArguments.
				ToString(containingClassType, '\0', builder, genericTypeArguments);
				builder.Append('.');
			}
			
			if(!type.IsGenericType)
			{
				builder.Append(type.Name);
				return;
			}

			var nullableUnderlyingType = Nullable.GetUnderlyingType(type);
			if(nullableUnderlyingType != null)
			{
				// "Int?" instead of "Nullable<Int>"
				ToString(nullableUnderlyingType, '\0', builder);
				builder.Append("?");
				return;
			}
			
			var name = type.Name;

			#if DEV_MODE
			//Debug.Log(name + " IsGenericTypeDefinition=" + type.IsGenericTypeDefinition+ ", IsConstructedGenericType="+ type.IsConstructedGenericType+ ", GetGenericArguments()="+ToString(type.GetGenericArguments())+", GenericTypeArguments=" + ToString(type.GenericTypeArguments));
			#endif

			// If type name doesn't end with `1, `2 etc. then it's not a generic class type
			// but type of a non-generic class nested inside a generic class.
			if(name[name.Length - 2] == '`')
			{
				// E.g. TKey, TValue
				var genericTypes = type.GetGenericArguments();

				builder.Append(name.Substring(0, name.Length - 2));
				builder.Append('<');

				// Prefer using results of GenericTypeArguments over results of GetGenericArguments if available.
				int genericTypeArgumentsLength = genericTypeArguments != null ? genericTypeArguments.Length : 0;
				if(genericTypeArgumentsLength > 0)
				{
					ToString(genericTypeArguments[0], '\0', builder);
				}
				else
				{
					ToString(genericTypes[0], '\0', builder);
				}
				int count = genericTypes.Length;
				for(int n = 1; n < count; n++)
				{
					builder.Append(", ");
					if(genericTypeArgumentsLength > n)
					{
						builder.Append(TypeExtensions.GetShortName(genericTypeArguments[n]));
					}
					else
					{
						builder.Append(TypeExtensions.GetShortName(genericTypes[n]));
					}
				}
				builder.Append('>');
			}
			else
			{
				builder.Append(name);
			}

			return;
		}

		public static string ToString([CanBeNull]PropertyInfo property)
		{
			if(property == null)
			{
				return "null";
			}

			var sb = StringBuilderPool.Create();
			ToString(property, sb);
			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static void ToString([NotNull]PropertyInfo property, [NotNull]StringBuilder sb)
		{
			sb.Append(property.Name);
			var parameters = property.GetIndexParameters();
			int count = parameters.Length;
			if(count > 0)
			{
				sb.Append('[');
				sb.Append(TypeExtensions.GetShortName(parameters[0].ParameterType));
				for(int param = 1; param < count; param++)
				{
					sb.Append(", ");
					sb.Append(TypeExtensions.GetShortName(parameters[param].ParameterType));
				}
				sb.Append(']');
			}
		}

		public static string ToString([CanBeNull]MemberInfo member)
		{
			if(member == null)
			{
				return "null";
			}

			var sb = StringBuilderPool.Create();
			ToString(member, sb);
			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static void ToString([NotNull]MemberInfo member, [NotNull]StringBuilder sb)
		{
			var field = member as FieldInfo;
			if(field != null)
			{
				sb.Append(field.Name);
				return;
			}

			var property = member as PropertyInfo;
			if(property != null)
			{
				ToString(property, sb);
				return;
			}

			var method = member as MethodInfo;
			if(method != null)
			{
				ToString(method, sb);
				return;
			}

			sb.Append(member.ToString());
		}

		public static string ToString([CanBeNull]FieldInfo field)
		{
			return field == null ? "null" : field.Name;
		}

		public static string ToString([CanBeNull]MethodInfo methodInfo)
		{
			if(methodInfo == null)
			{
				return "null";
			}

			var sb = StringBuilderPool.Create();
			ToString(methodInfo, sb);
			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		public static void ToString([NotNull]MethodInfo method, [NotNull]StringBuilder sb)
		{
			string methodName = method.Name;
			if(methodName.StartsWith("op_", StringComparison.Ordinal))
			{
				switch(methodName)
				{
					// Return type is needed for implicit ("op_Implicit") and explicit ("op_Explicit") operators to guarantee unique menu paths.
					case "op_Implicit":
						sb.Append("implicit operator ");
						sb.Append(TypeExtensions.GetShortName(method.ReturnType));
						break;
					case "op_Explicit":
						sb.Append("explicit operator ");
						sb.Append(TypeExtensions.GetShortName(method.ReturnType));
						break;
					case "op_Equality":
						sb.Append("operator ==");
						break;
					case "op_Inequality":
						sb.Append("operator !=");
						break;
					case "op_GreaterThanOrEqual":
						sb.Append("operator >=");
						break;
					case "op_GreaterThan":
						sb.Append("operator >");
						break;
					case "op_LessThanOrEqual":
						sb.Append("operator <=");
						break;
					case "op_LessThan":
						sb.Append("operator <");
						break;
					case "op_Subtraction":
					case "op_UnaryNegation":
						sb.Append("operator -");
						break;
					case "op_Addition":
					case "op_UnaryPlus":
						sb.Append("operator +");
						break;
					case "op_LogicalNot":
						sb.Append("operator !");
						break;
					case "op_Multiply":
						sb.Append("operator *");
						break;
					case "op_Division":
						sb.Append("operator /");
						break;
					case "op_Modulus":
						sb.Append("operator %");
						break;
					case "op_Increment":
						sb.Append("operator ++");
						break;
					case "op_Decrement":
						sb.Append("operator --");
						break;
					case "op_OnesComplement":
						sb.Append("operator ~");
						break;
					case "op_True":
						sb.Append("operator true");
						break;
					case "op_False":
						sb.Append("operator false");
						break;
					case "op_BitwiseAnd":
						sb.Append("operator &");
						break;
					case "op_BitwiseOr":
						sb.Append("operator |");
						break;
					case "op_ExclusiveOr":
						sb.Append("operator ^");
						break;
					case "op_LeftShift":
						sb.Append("operator <<");
						break;
					case "op_RightShift":
						sb.Append("operator >>");
						break;
				}
			}
			else
			{
				sb.Append(method.Name);

				if(method.IsGenericMethod)
				{
					var genericTypes = method.GetGenericArguments();
					sb.Append('<');
					sb.Append(TypeExtensions.GetShortName(genericTypes[0]));
					int args = genericTypes.Length;
					for(int a = 1; a < args; a++)
					{
						sb.Append(", ");
						sb.Append(TypeExtensions.GetShortName(genericTypes[a]));
					}
				}
			}

			sb.Append('(');
			var parameters = method.GetParameters();
			int count = parameters.Length;
			if(count > 0)
			{
				sb.Append(TypeExtensions.GetShortName(parameters[0].ParameterType));
				for(int p = 1; p < count; p++)
				{
					sb.Append(", ");
					sb.Append(TypeExtensions.GetShortName(parameters[p].ParameterType));
				}
			}
			sb.Append(")");
		}

		public static string RemoveWhitespace(string str)
		{
			var chars = str.ToCharArray();
			int resultIndex = 0;
			int length = str.Length;
			for(int i = 0; i < length; i++)
			{
				var c = chars[i];
				if(!IsWhiteSpace(c))
				{
					chars[resultIndex] = c;
					resultIndex++;
				}
			}
			return new string(chars, 0, resultIndex);
		}

		public static string RemoveSpaces(string str)
		{
			var chars = str.ToCharArray();
			int resultIndex = 0;
			int length = str.Length;
			for(int i = 0; i < length; i++)
			{
				var c = chars[i];
				if(!IsSpace(c))
				{
					chars[resultIndex] = c;
					resultIndex++;
				}
			}
			return new string(chars, 0, resultIndex);
		}

		public static bool ContainsWhitespace(string test)
		{
			if(test.IndexOf(' ') != -1)
			{
				return true;
			}
			if(test.IndexOf('\t') != -1)
			{
				return true;
			}
			if(test.IndexOf('\r') != -1)
			{
				return true;
			}
			if(test.IndexOf('\n') != -1)
			{
				return true;
			}
			return false;
		}

		public static bool IsPascalCasing(string test)
		{
			if(test.Length == 0)
			{
				return true;
			}

			var firstLetter = test[0];
			if(firstLetter == char.ToLower(firstLetter))
			{
				return false;
			}

			if(test.IndexOf('_') != -1)
			{
				return false;
			}

			return true;
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string CountToString([CanBeNull]ICollection collection)
		{
			return collection == null ? "null" : ToString(collection.Count);
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string ToColorizedString(bool value)
		{
			return value ? "<color=green>True</color>" : "<color=red>False</color>";
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string ToColorizedString(Object value)
		{
			if(value == null)
			{
				return "<color=red>null</color>";
			}
			return Green(ToString(value));
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string ToColorizedString(object value)
		{
			if(value == null)
			{
				return "<color=red>null</color>";
			}

			if(value is Color)
			{
				return ToColorizedString((Color)value);
			}

			if(value is ICollection)
			{
				var collection = (ICollection)value;
				if(value is Array)
                {
					return ToStringSansNamespace(value.GetType().GetElementType()) + "[" + collection.Count + "]";
                }
				return ToStringSansNamespace(value.GetType()) + "[" + collection.Count + "]";
			}

			return ToColorizedString(ToStringCompact(value));
		}

		public static string ToColorizedString(string value)
		{
			if(value == null)
			{
				return "<color=red>null</color>";
			}

			switch(value)
			{
				case "":
				case "0":
				case "-1":
				case "False":
				case "None":
				case "\0":
				case "[]":
				case "{}":
					return Concat("<color=red>", value, "</color>");
				default:
					return Concat("<color=green>", value, "</color>");
			}
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string ToColorizedString<T>(T value) where T : IConvertible
		{
			return Convert.ToInt32(value) > 0 ? Concat("<color=green>", ToString(value), "</color>") : Concat("<color=red>", ToString(value), "</color>");
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string ToColorizedString(Color color)
		{
			var solidColor = color;
			solidColor.a = 1f;
			return Concat("<color=#", ColorUtility.ToHtmlStringRGBA(solidColor), ">", ToString(color), "</color>");
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string ToString(Color color)
		{
			return "#" + (color.a <= 1f ? ColorUtility.ToHtmlStringRGB(color) : ColorUtility.ToHtmlStringRGBA(color));
		}

		/// <summary>
		/// Converts objects into their string representations and concats them together to form a single string.
		/// </summary>
		/// <returns>
		/// String representations of objects combined together.
		/// </returns>
		public static string ToColorizedString([NotNull]params object[] objects)
		{
			var sb = StringBuilderPool.Create();
			for(int n = 0, count = objects.Length; n < count; n++)
			{
				var add = objects[n];
				var text = add as string;
				if(text != null)
				{
					sb.Append(text);
				}
				else
				{
					sb.Append(ToColorizedString(add));
				}
			}
			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string Red(string input)
		{
			return Concat("<color=red>", input, "</color>");
		}

		[MethodImpl(256)] //256 = MethodImplOptions.AggressiveInlining in .NET 4.5. and later
		public static string Green(string input)
		{
			return Concat("<color=green>", input, "</color>");
		}

		/// <summary> Finds index of substring inside text, ignoring spaces inside text and ignoring casing. </summary>
		/// <param name="text"> The text. </param>
		/// <param name="substring"> The substring to find. </param>
		/// <param name="textIgnoredSpaceCount"> [out] Number of space characters that were skipped in text when matching substring. </param>
		/// <returns> The zero-based index of the found ignoring spaces, or -1 if no match was found. </returns>
		public static int IndexOfIgnoringSpaces(string text, string substring, out int textIgnoredSpaceCount)
		{
			int textLength = text.Length;
			int substringLength = substring.Length;
			int stop = textLength - substringLength;
			
			textIgnoredSpaceCount = 0;
			int substringIgnoredSpaceCount = 0;

			// test each index in text
			for(int i = 0; i <= stop; i++)
			{
				if(text[i] == ' ')
				{
					continue;
				}

				bool failed = false;
				for(int nth = 0; nth < substringLength; nth++)
				{
					var a = text[i + nth - substringIgnoredSpaceCount];
					var b = substring[nth - textIgnoredSpaceCount];

					if(a == b)
					{
						continue;
					}

					if(a == ' ')
					{
						textIgnoredSpaceCount++;
						continue;
					}

					if(b == ' ')
					{
						substringIgnoredSpaceCount++;
						continue;
					}

					if(char.IsUpper(a))
					{
						a = char.ToLowerInvariant(a);
						if(char.IsUpper(b))
						{
							b = char.ToLowerInvariant(b);
						}

						if(a == b)
						{
							continue;
						}
					}
					else if(char.IsUpper(b))
					{
						b = char.ToLowerInvariant(b);

						if(a == b)
						{
							continue;
						}
					}

					failed = true;
					break;
				}

				if(!failed)
				{
					return i;
				}
				textIgnoredSpaceCount = 0;
				substringIgnoredSpaceCount = 0;
			}

			return -1;
		}
		
		/// <summary> If makePluralCondition is true converts word to plural form. </summary>
		/// <param name="input"> The original singular word. </param>
		/// <param name="makePluralCondition"> True to convert word to plural form. </param>
		/// <returns> A string. </returns>
		public static string Plural(this string singular, bool makePluralCondition)
		{
			return makePluralCondition ? Plural(singular) : singular;
		}

		public static string Plural(this string singular)
		{
			int letterCount = singular.Length;
			if(letterCount == 0)
			{
				return singular;
			}
			if(letterCount == 1)
			{
				return singular + "'s"; //E.g. x's
			}
			if(singular[letterCount - 1] == 's')
			{
				return singular + "es"; //E.g. Thomases
			}
			return singular + "s";
		}
	}
}