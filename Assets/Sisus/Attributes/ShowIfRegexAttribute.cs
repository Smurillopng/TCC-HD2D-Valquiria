using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute that specifies that its target should never be null or empty.
	/// 
	/// Works on any class members where value implements ICollection or IEnumerable.
	/// This includes things like arrays, lists and strings.
	/// 
	/// If value can't be cast to ICollection or IEnumerable, then validation will only return false if value value is null.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class ShowIfRegexAttribute : Attribute, IShowInInspectorIf
	{
		public readonly string classMemberName;
		public readonly string pattern;

		/// <summary>
		/// Only show the class member that follows this attribute if its value matches the regular expression pattern.
		/// </summary>
		public ShowIfRegexAttribute(string setPattern) : base()
		{
			classMemberName = "";
			pattern = setPattern;
		}

		/// <summary>
		/// Only show the class member that follows this attribute if the value of another class member
		/// by the name <paramref name="setClassMemberName"/> matches the regular expression pattern.
		/// </summary>
		public ShowIfRegexAttribute(string setClassMemberName, string setPattern) : base()
		{
			classMemberName = setClassMemberName;
			pattern = setPattern;
		}

		/// <inheritdoc/>
		public bool ShowInInspector([NotNull]Type containingClassType, [CanBeNull]object containingClassInstance, [CanBeNull]MemberInfo classMember)
		{
			if(!string.IsNullOrEmpty(classMemberName))
			{
				var classMembers = containingClassType.GetMember(classMemberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

				int classMembersFound = classMembers.Length;

				if(classMembersFound == 0)
				{
					Debug.LogError("ShowIfRegexAttribute class member by name \"" + classMemberName + "\" not found.");
					return true;
				}

				classMember = classMembers[0];
			}
			
			object value;
			var field = classMember as FieldInfo;
			if(field is object)
			{
				value = field.GetValue(field.IsStatic ? null : containingClassInstance);
			}
			else
			{
				var property = classMember as PropertyInfo;
				if(property is object)
				{
					var getAccessor = property.GetGetMethod();
					value = getAccessor.Invoke(getAccessor.IsStatic ? null : containingClassInstance, null);
				}
				else
				{
					Debug.LogError("ShowInInspectorIfRegexAttribute class member by name \"" + classMemberName + "\" was not a field or a property.");
					return true;
				}
			}

			if(value == null)
			{
				return false;
			}

			var valueString = value as string;
			if(valueString == null)
			{
				valueString = value.ToString();
			}

			return Regex.Match(valueString, pattern).Success;
		}
	}
}