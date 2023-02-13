using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	public class ValueTest : IShowIfEvaluator
	{
		#if UNITY_EDITOR
		private const int CheckAssembliesOfTypesEmptySlot = 2;
		#else
		private const int CheckAssembliesOfTypesEmptySlot = 1;
		#endif
		private static readonly Type[] checkAssembliesOfTypes = new Type[]
		{
				typeof(GameObject),
				#if UNITY_EDITOR
				typeof(UnityEditor.Editor),
				#endif
				null //empty slot
		};

		private static readonly Dictionary<string, MemberInfo> StaticClassMembersByString = new Dictionary<string, MemberInfo>();

		[CanBeNull]
		public readonly string left;

		public readonly Is comparison;

		[CanBeNull]
		public readonly string right;

		public ValueTest(string setLeft, Is setComparison, string setRight)
		{
			left = setLeft;
			comparison = setComparison;
			right = setRight;

			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("ValueTest(" + ToString() + ")");
			#endif
		}

		public ValueTest(string setLeft, Is setComparison, [CanBeNull]object setRight)
		{
			left = setLeft;
			comparison = setComparison;
			right = setRight == null ? null : setRight.ToString();

			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("ValueTest(" + ToString() + ")");
			#endif
		}

		public bool Evaluate([NotNull]Type containingClassType, [CanBeNull]object containingClassInstance, [CanBeNull]MemberInfo classMember)
		{
			object leftValue = ParseValue(left, containingClassType, containingClassInstance, classMember);
			object rightValue = ParseValue(right, containingClassType, containingClassInstance, classMember);

			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log((classMember == null ? "" : classMember.Name)+".Evalute "+ (leftValue == null ? "null" : leftValue.ToString()) + " "+ comparison + " " + (rightValue == null ? "null" : rightValue.ToString()));
			#endif

			return EvaluationUtility.Test(leftValue, comparison, rightValue);
		}

		public override string ToString()
		{
			return left + " " + comparison + " " +right;
		}

		private static object ParseValue(string valueString, [NotNull]Type containingClassType, [CanBeNull]object containingClassInstance, [CanBeNull]MemberInfo classMember)
		{
			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("ParseValue("+(valueString == null ? "null" : "\"" +  valueString + "\"")+")");
			#endif

			if(string.IsNullOrEmpty(valueString))
			{
				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log(valueString == null ? "null" : "empty");
				#endif

				return valueString;
			}

			if(string.Equals(valueString, "true", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if(string.Equals(valueString, "false", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if(string.Equals(valueString, "null", StringComparison.OrdinalIgnoreCase))
			{
				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log("null");
				#endif

				return null;
			}

			if(valueString[0] == '"')
			{
				return valueString.Substring(1, valueString.Length - 2);
			}

			if(char.IsDigit(valueString[0]))
			{
				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log(valueString+"[0] is digit");
				#endif

				double numeric;
				if(double.TryParse(valueString, out numeric))
				{
					return numeric;
				}

				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log("failed to parse double");
				#endif
			}

			if(!string.Equals(valueString, "this"))
			{
				// Handle static class members. E.g. Application.isPlaying.
				int dot = valueString.IndexOf('.');
				if(dot != -1)
				{
					MemberInfo staticClassMember;
					if(StaticClassMembersByString.TryGetValue(valueString, out staticClassMember))
					{
						if(staticClassMember != null)
						{
							#if DEV_MODE && DEBUG_ENABLED
							Debug.Log("StaticClassMembersByString(" + valueString+"): "+staticClassMember.Name);
							#endif

							classMember = staticClassMember;
						}
						else
						{
							return valueString;
						}
					}
					else
					{
						checkAssembliesOfTypes[CheckAssembliesOfTypesEmptySlot] = containingClassType;
						staticClassMember = ParseStaticClassMember(valueString);
							
						//cache even if null to avoid trying to fetch again
						StaticClassMembersByString.Add(valueString, staticClassMember);

						if(staticClassMember != null)
						{
							#if DEV_MODE && DEBUG_ENABLED
							Debug.Log("ParseStaticClassMember(" + valueString+"): "+staticClassMember.Name);
							#endif

							classMember = staticClassMember;
						}
						else
						{
							return valueString;
						}
					}
				}
				else
				{
					// Handle class members in same class
					var classMembers = containingClassType.GetMember(valueString, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
					int classMembersFound = classMembers.Length;
					if(classMembersFound == 0)
					{
						return valueString;
					}
					classMember = classMembers[0];
				}
			}

			var field = classMember as FieldInfo;
			if(field is object)
			{
				#if DEV_MODE && DEBUG_ENABLED
				var val = field.GetValue(field.IsStatic ? null : containingClassInstance);
				Debug.Log("Field! value: "+ (val == null ? "null" : "\""+val.ToString()+"\""));
				#endif

				return field.GetValue(field.IsStatic ? null : containingClassInstance);
			}

			var property = classMember as PropertyInfo;
			if(property is object)
			{
				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log("Property!");
				#endif

				var getAccessor = property.GetGetMethod();
				return getAccessor.Invoke(getAccessor.IsStatic ? null : containingClassInstance, null);
			}

			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("Not field or property");
			#endif

			return valueString;
		}

		[CanBeNull]
		private static MemberInfo ParseStaticClassMember(string valueString)
		{
			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("ParseStaticClassMember(" + valueString + ")");
			#endif

			var dot = valueString.IndexOf('.');
			if(dot == -1)
			{
				return null;
			}

			string className = valueString.Substring(0, dot);
			var type = ParseType(className);

			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("ParseType(" + className + "): "+(type == null ? "null" : type.FullName));
			#endif

			if(type == null)
			{
				return null;
			}

			string memberName = valueString.Substring(dot + 1);
			var members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			for(int n = members.Length - 1; n >= 0; n--)
			{
				var classMember = members[n];
				if(string.Equals(classMember.Name, memberName))
				{
					#if DEV_MODE && DEBUG_ENABLED
					Debug.Log("ParseStaticClassMember " + memberName + " found");
					#endif
					return classMember;
				}
			}

			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("ParseStaticClassMember " + memberName + " not found");
			#endif

			return null;
		}

		[CanBeNull]
		private static Type ParseType(string className)
		{
			Type resultIfNoBetterFound = null;

			for(int t = checkAssembliesOfTypes.Length - 1; t >= 0; t--)
			{
				var checkAssemblyOfType = checkAssembliesOfTypes[t];
				if(checkAssemblyOfType == null)
				{
					continue;
				}

				string preferredNamespace = checkAssemblyOfType.Namespace;

				var typesInAssembly = checkAssemblyOfType.Assembly.GetTypes();
				for(int n = typesInAssembly.Length - 1; n >= 0; n--)
				{
					var type = typesInAssembly[n];
					if(string.Equals(type.Name, className))
					{
						if(string.Equals(type.Namespace, preferredNamespace))
						{
							return type;
						}
						resultIfNoBetterFound = type;
					}
				}
			}
			return resultIfNoBetterFound;
		}
	}
}