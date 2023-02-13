using System;
using System.Collections.Generic;
using UnityEngine;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Sisus.Attributes
{
	public static class EvaluationUtility
	{
		private static readonly Dictionary<string, IShowIfEvaluator> EvaluatorCache = new Dictionary<string, IShowIfEvaluator>();

		public static bool Test([CanBeNull]object value)
		{
			return Test(value, Is.Equal, true);
		}

		public static bool Test([CanBeNull]object value, Is comparison, [CanBeNull]object other)
		{
			#if DEV_MODE
			Debug.Log("Test "+ (value == null ? "null" : "\""+value.ToString()+"\" (" +(value.GetType().Name)+")") + " " + comparison + " " + (other == null ? "null" : "\""+other.ToString() + "\" (" + other.GetType().Name + ")"));
			#endif

			switch(comparison)
			{
				case Is.Equal:
					return CompareTo(value, other) == 0;
				case Is.Not:
					#if DEV_MODE
					var comp = CompareTo(value, other);
					Debug.Log("Is.Not: CompareTo="+comp+". Result="+(comp != 0));
					#endif

					return CompareTo(value, other) != 0;
				case Is.Smaller:
					return CompareTo(value, other) < 0;
				case Is.SmallerOrEqual:
					return CompareTo(value, other) <= 0;
				case Is.Larger:
					return CompareTo(value, other) > 0;
				case Is.LargerOrEqual:
					return CompareTo(value, other) >= 0;
				default:
					throw new IndexOutOfRangeException(comparison.ToString()+" unsupported comparison type.");
			}
		}
		
		public static int CompareTo([CanBeNull]object value, [CanBeNull]object other)
		{
			if(value == null)
			{
				if(other == null)
				{
					#if DEV_MODE
					Debug.Log("both null, returning 0");
					#endif

					return 0;
				}
				else if(other is Object)
				{
					if((other as Object) == null)
					{
						Debug.Log("value null, other null UnityEngine.Object, returning 0");
						return 0;
					}
				}
				else if(string.Equals(other as string, "null", StringComparison.OrdinalIgnoreCase))
				{
					#if DEV_MODE
					Debug.Log("value null, other \"null\", returning 0");
					#endif

					return 0;
				}
				
				#if DEV_MODE
				Debug.Log("value null, returning -1");
				#endif

				return -1;
			}

			if(other == null)
			{
				if(value is Object)
				{
					if((value as Object) == null)
					{
						#if DEV_MODE && DEBUG_ENABLED
						Debug.Log("other null, value null UnityEngine.Object, returning 0");
						#endif
						return 0;
					}
				}
				else if(string.Equals(value as string, "null", StringComparison.OrdinalIgnoreCase))
				{
					#if DEV_MODE && DEBUG_ENABLED
					Debug.Log("other null, value \"null\", returning 0");
					#endif

					return 0;
				}

				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log("other null (with value=\"" + value + "\" ("+ value.GetType().Name + ")), returning 1");
				#endif

				return 1;
			}

			var comparable = value as IComparable;
			var otherComparable = other as IComparable;
			if(comparable == null || otherComparable == null)
			{
				#if DEV_MODE
				Debug.Log("Value (" + value.GetType().Name + ") or other ("+other.GetType().Name+") not IComparable. Converting both to string.");
				#endif

				comparable = value.ToString();
				otherComparable = other.ToString();
			}
			else
			{
				var valueType = value.GetType();
				var otherType = other.GetType();
				if(!valueType.Equals(otherType))
				{
					if(otherType.IsEnum)
					{
						comparable = (IComparable)Enum.Parse(otherType, value.ToString());
					}
					else
					{
						var convertible = value as IConvertible;
						var otherConvertible = other as IConvertible;
						if(convertible == null || otherConvertible == null)
						{
							#if DEV_MODE
							Debug.Log("Value (" + valueType.Name + ") and other ("+ otherType.Name+") are of mismatching types and not IConvertible. Converting both to string.");
							#endif
							comparable = value.ToString();
							otherComparable = other.ToString();
						}
						else
						{
							#if DEV_MODE
							Debug.Log("Value (" + valueType.Name + ") and other (" + otherType.Name + ") are of mismatching types. Converting other to "+valueType.Name+".");
							#endif
							try
							{
								otherComparable = (IComparable)Convert.ChangeType(other, valueType);
							}
							#if DEV_MODE
							catch(Exception e)
							{								
								Debug.Log("Failed to convert "+other+" from "+otherType.Name + " to "+valueType.Name+ ".Converting both to string.\n" + e);
							#else
							catch
							{
							#endif
								comparable = value.ToString();
								otherComparable = other.ToString();
							}
						}
					}					
				}
			}

			return comparable.CompareTo(otherComparable);
		}

		public static IShowIfEvaluator GenerateEvaluatorForBooleanExpression([NotNullOrEmpty]string expression)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!string.IsNullOrEmpty(expression));
			#endif

			IShowIfEvaluator cached;
			if(EvaluatorCache.TryGetValue(expression, out cached))
			{
				return cached;
			}

			string rawInput = expression;
			expression = expression.Replace(" ", "").Replace("&&", "&").Replace("||", "|");

			#if DEV_MODE
			Debug.Log("GenerateEvaluatorFromExpression: " + expression);
			#endif

			IShowIfEvaluator left = null;
			bool any = false;

			int start = expression.LastIndexOf('(');
			if(start == -1)
			{
				left = GenerateEvaluatorForExpressionWithoutParentheses(expression);

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(left != null);
				#endif

				EvaluatorCache.Add(rawInput, left);

				return left;
			}

			for(; start != -1; start = expression.LastIndexOf('(', start - 1))
			{
				int end = expression.IndexOf(')', start + 1);
				string expressionWithoutParentheses = expression.Substring(start + 1, end - start - 1);
				var newEvaluator = GenerateEvaluatorForExpressionWithoutParentheses(expressionWithoutParentheses);
				if(left == null)
				{
					left = newEvaluator;
				}
				else
				{
					left = new Expression(left, newEvaluator, any);
				}

				if(start > 0)
				{
					var c = expression[start - 1];
					if(c == '|')
					{
						any = true;
					}
					else if(c == '&')
					{
						any = false;
					}
					else
					{
						c = expression[end + 1];
						if(c == '|')
						{
							any = true;
						}
						else if(c == '&')
						{
							any = false;
						}
					}
				}
				else
				{
					var c = expression[end + 1];
					if(c == '|')
					{
						any = true;
					}
					else if(c == '&')
					{
						any = false;
					}
				}
			}

			int firstStart = expression.IndexOf('(');
			if(firstStart > 0)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(left != null);
				#endif

				any = expression[firstStart - 1] == '|';
				string expressionWithoutParentheses = expression.Substring(0, firstStart - 1);
				var newEvaluator = GenerateEvaluatorForExpressionWithoutParentheses(expressionWithoutParentheses);				
				left = new Expression(left, newEvaluator, any);
			}

			int lastEnd = expression.LastIndexOf(')');
			if(lastEnd < expression.Length - 1)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(left != null);
				#endif

				any = expression[lastEnd + 1] == '|';
				string expressionWithoutParentheses = expression.Substring(lastEnd + 2);
				var newEvaluator = GenerateEvaluatorForExpressionWithoutParentheses(expressionWithoutParentheses);
				
				left = new Expression(left, newEvaluator, any);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(left != null);
			#endif

			EvaluatorCache.Add(rawInput, left);

			return left;
		}

		private static IShowIfEvaluator GenerateEvaluatorForExpressionWithoutParentheses(string expression)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!string.IsNullOrEmpty(expression));
			#endif

			#if DEV_MODE
			Debug.Log("ExpressionWithoutParentheses: " + expression);
			#endif

			IShowIfEvaluator ifAny;
			if(TryGenerateEvaluatorForExpressionWithoutParentheses(true, expression, out ifAny))
			{
				return ifAny;
			}

			IShowIfEvaluator ifAll;
			if(TryGenerateEvaluatorForExpressionWithoutParentheses(false, expression, out ifAll))
			{
				return ifAll;
			}

			return GenerateEvaluatorForAtomicFormula(expression);
		}

		private static bool TryGenerateEvaluatorForExpressionWithoutParentheses(bool ifAny, string expression, out IShowIfEvaluator left)
		{
			left = null;
			var atomSeparator = ifAny ? '|' : '&';
			int end = expression.IndexOf(atomSeparator);
			if(end == -1)
			{
				return false;
			}

			int start = 0;
			do
			{
				string expressionAtom = expression.Substring(start, end - start);
				var leafEvaluator = GenerateEvaluatorForAtomicFormula(expressionAtom);
				if(left == null)
				{
					left = leafEvaluator;
				}
				else
				{
					left = new Expression(left, leafEvaluator, ifAny);
				}

				start = end + 1;
				end = expression.IndexOf(atomSeparator, start + 1);
			}
			while(end != -1);

			if(start < expression.Length - 1)
			{
				string expressionAtom = expression.Substring(start);
				var leafEvaluator = GenerateEvaluatorForAtomicFormula(expressionAtom);
				if(left == null)
				{
					left = leafEvaluator;
				}
				else
				{
					left = new Expression(left, leafEvaluator, ifAny);
				}
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(left != null);
			#endif

			return true;
		}

		private static IShowIfEvaluator GenerateEvaluatorForAtomicFormula(string atomicFormula)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!string.IsNullOrEmpty(atomicFormula));
			#endif

			#if DEV_MODE
			Debug.Log("FromExpressionAtom input: "+ atomicFormula);
			#endif

			bool negation;
			if(atomicFormula[0] == '!')
			{
				negation = true;
				atomicFormula = atomicFormula.Substring(1);
			}
			else
			{
				negation = false;
			}

			IShowIfEvaluator result;

			int e = atomicFormula.IndexOf('=');
			if(e != -1)
			{
				if(atomicFormula[e + 1] == '=')
				{
					result = new ValueTest(atomicFormula.Substring(0, e), Is.Equal, atomicFormula.Substring(e + 2));
				}
				else if(atomicFormula[e - 1] == '!')
				{
					result = new ValueTest(atomicFormula.Substring(0, e - 1), Is.Not, atomicFormula.Substring(e + 1));
				}
				else if(atomicFormula[e - 1] == '<')
				{
					result = new ValueTest(atomicFormula.Substring(0, e - 1), Is.SmallerOrEqual, atomicFormula.Substring(e + 1));
				}
				else if(atomicFormula[e - 1] == '>')
				{
					result = new ValueTest(atomicFormula.Substring(0, e - 1), Is.LargerOrEqual, atomicFormula.Substring(e + 1));
				}
				else
				{
					result = new ValueTest(atomicFormula.Substring(0, e), Is.Equal, atomicFormula.Substring(e + 1));
				}
			}
			else
			{
				int smaller = atomicFormula.IndexOf('<');
				if(smaller != -1)
				{
					result = new ValueTest(atomicFormula.Substring(0, smaller), Is.Smaller, atomicFormula.Substring(smaller + 1));
				}
				else
				{
					int larger = atomicFormula.IndexOf('>');
					if(larger != -1)
					{
						result = new ValueTest(atomicFormula.Substring(0, larger), Is.Larger, atomicFormula.Substring(larger + 1));
					}
					else
					{
						result = new ValueTest(atomicFormula, Is.Equal, "True");
					}
				}
			}

			if(negation)
			{
				#if DEV_MODE
				Debug.Log("FromExpressionAtom output: !" + result);
				#endif
				return new Negation(result);
			}
			#if DEV_MODE
			Debug.Log("FromExpressionAtom output: "+result);
			#endif
			return result;
		}
	}
}