using System;
using System.Reflection;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Allows adding a help box above a class member, which can be dynamically shown under only certain conditions.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class HelpBoxAttribute : PropertyAttribute, ITargetableAttribute
	{
		public readonly string text;
		public readonly HelpBoxMessageType messageType;

		public readonly bool alwaysShow;

		private readonly string classMemberName;
		private readonly Is comparisonType;
		private readonly object requiredValue;

		public readonly float minHeight = 35f;

		[CanBeNull]
		private readonly string evaluatorExpression;

		[CanBeNull]
		private IShowIfEvaluator evaluator;

		/// <summary>
		/// Show info help box with text.
		/// </summary>
		/// <param name="setText"> Text to show. </param>
		public HelpBoxAttribute(string setText)
		{
			text = setText;
			messageType = HelpBoxMessageType.Info;

			alwaysShow = true;
			classMemberName = "";
			requiredValue = true;
			comparisonType = Is.Equal;
			evaluatorExpression = null;
		}

		/// <summary>
		/// Show help box of given type with text.
		/// </summary>
		/// <param name="setText"> Text to show. </param>
		/// <param name="setMessageType"> Type of the message; info, warning or error. </param>
		public HelpBoxAttribute(string setText, HelpBoxMessageType setMessageType)
		{
			text = setText;
			messageType = setMessageType;

			alwaysShow = true;
			classMemberName = "";
			requiredValue = true;
			comparisonType = Is.Equal;
			evaluatorExpression = null;
		}

		/// <summary>
		/// Show help box of given type with text if attribute target has given value.
		/// </summary>
		/// <param name="setText"> Text to show. </param>
		/// <param name="setMessageType"> Type of the message; info, warning or error. </param>
		/// <param name="setRequiredValue"> The value the target must have in order for the help box to be shown. </param>
		public HelpBoxAttribute(string setText, HelpBoxMessageType setMessageType, object setRequiredValue)
		{
			text = setText;
			messageType = setMessageType;

			alwaysShow = false;
			classMemberName = "";
			comparisonType = Is.Equal;
			requiredValue = setRequiredValue;
			evaluatorExpression = null;
		}

		/// <summary>
		/// Show help box of given type with text if another class member has given value.
		/// </summary>
		/// <param name="setText"> Text to show. </param>
		/// <param name="setMessageType"> Type of the message; info, warning or error. </param>
		/// <param name="setClassMemberName"> Name of class member whose value to check. </param>
		/// <param name="setRequiredValue"> The value the class member must have in order for the help box to be shown. </param>
		public HelpBoxAttribute(string setText, HelpBoxMessageType setMessageType, string setClassMemberName, object setRequiredValue)
		{
			text = setText;
			messageType = setMessageType;

			alwaysShow = false;
			classMemberName = setClassMemberName;
			comparisonType = Is.Equal;
			requiredValue = setRequiredValue;
			evaluatorExpression = null;
		}

		/// <summary>
		/// Show help box of given type with text if another class member value passes comparison.
		/// </summary>
		/// <param name="setText"> Text to show. </param>
		/// <param name="setMessageType"> Type of the message; info, warning or error. </param>
		/// <param name="setClassMemberName"> Name of class member whose value to check. </param>
		/// <param name="comparison"> Type of comparison check to perform on value. </param>
		/// <param name="setRequiredValue"> The value the class member must have in order for the help box to be shown. </param>
		public HelpBoxAttribute(string setText, HelpBoxMessageType setMessageType, Is comparison, object setRequiredValue)
		{
			text = setText;
			messageType = setMessageType;

			alwaysShow = false;
			classMemberName = "";
			comparisonType = comparison;
			requiredValue = setRequiredValue;
			evaluatorExpression = null;
		}

		/// <summary>
		/// Show help box of given type with text if another class member value passes comparison.
		/// </summary>
		/// <param name="setText"> Text to show. </param>
		/// <param name="setMessageType"> Type of the message; info, warning or error. </param>
		/// <param name="setClassMemberName"> Name of class member whose value to check. </param>
		/// <param name="comparison"> Type of comparison check to perform on value. </param>
		/// <param name="setRequiredValue"> The value the class member must have in order for the help box to be shown. </param>
		public HelpBoxAttribute(string setText, HelpBoxMessageType setMessageType, string setClassMemberName, Is comparison, object setRequiredValue)
		{
			text = setText;
			messageType = setMessageType;

			alwaysShow = false;
			classMemberName = setClassMemberName;
			comparisonType = comparison;
			requiredValue = setRequiredValue;
			evaluatorExpression = null;
		}

		/// <summary>
		/// Show help box of given type with text if another class member value passes comparison.
		/// </summary>
		/// <param name="setText"> Text to show. </param>
		/// <param name="setMessageType"> Type of the message; info, warning or error. </param>
		/// <param name="setClassMemberName"> Name of class member whose value to check. </param>
		/// <param name="comparison"> Type of comparison check to perform on value. </param>
		/// <param name="setRequiredValue"> The value the class member must have in order for the help box to be shown. </param>
		/// <param name="setRequiredValue"> Minimum height for the text box. </param>
		public HelpBoxAttribute(string setText, HelpBoxMessageType setMessageType, string setClassMemberName, Is comparison, object setRequiredValue, float setMinHeight)
		{
			text = setText;
			messageType = setMessageType;

			alwaysShow = false;
			classMemberName = setClassMemberName;
			comparisonType = comparison;
			requiredValue = setRequiredValue;
			evaluatorExpression = null;

			minHeight = setMinHeight;
		}

		/// <summary>
		/// Show help box of given type with text if another class member value passes comparison.
		/// </summary>
		/// <param name="setText"> Text to show. </param>
		/// <param name="setMessageType"> Type of the message; info, warning or error. </param>
		/// <param name="comparison"> Type of comparison check to perform on value. </param>
		/// <param name="setRequiredValue"> The value the class member must have in order for the help box to be shown. </param>
		/// <param name="setRequiredValue"> Minimum height for the text box. </param>
		public HelpBoxAttribute(string setText, HelpBoxMessageType setMessageType, Is comparison, object setRequiredValue, float setMinHeight)
		{
			text = setText;
			messageType = setMessageType;

			alwaysShow = false;
			classMemberName = "";
			comparisonType = comparison;
			requiredValue = setRequiredValue;
			evaluatorExpression = null;

			minHeight = setMinHeight;
		}

		/// <summary>
		/// Show help box of given type with text if conditional expression is true.
		/// </summary>
		/// <param name="setText"> Text to show. </param>
		/// <param name="booleanExpression">
		/// The conditional expression which must be true for the help box to be shown.
		/// This can contain multiple comparisons between class members and target values.
		/// Example: intField == 3 && (floatField > 8 || EditorApplication.isPlaying)
		/// </param>
		/// <param name="setMessageType"> Type of the message; info, warning or error. </param>
		public HelpBoxAttribute(string setText, string booleanExpression, HelpBoxMessageType setMessageType)
		{
			text = setText;
			messageType = setMessageType;

			alwaysShow = false;
			classMemberName = "";
			comparisonType = Is.Equal;
			requiredValue = "";
			evaluatorExpression = booleanExpression;
		}


		/// <summary>
		/// Show info help box with text if attribute target has given value.
		/// </summary>
		/// <param name="setText"> Text to show. </param>
		/// <param name="setRequiredValue"> The value the target must have in order for the help box to be shown. </param>
		public HelpBoxAttribute(string setText, object setRequiredValue)
		{
			text = setText;
			messageType = HelpBoxMessageType.Info;

			alwaysShow = false;
			classMemberName = "";
			comparisonType = Is.Equal;
			requiredValue = setRequiredValue;
			evaluatorExpression = null;
		}

		/// <summary>
		/// Show info help box with text if attribute target value passes comparison.
		/// </summary>
		/// <param name="setText"> Text to show. </param>
		/// <param name="comparison"> Type of comparison check to perform on value. </param>
		/// <param name="setRequiredValue"> The value the target must have in order for the help box to be shown. </param>
		public HelpBoxAttribute(string setText, Is comparison, object setRequiredValue)
		{
			text = setText;
			messageType = HelpBoxMessageType.Info;

			alwaysShow = false;
			classMemberName = "";
			comparisonType = comparison;
			requiredValue = setRequiredValue;
			evaluatorExpression = null;
		}		

		/// <summary>
		/// Show info help box with text if another class member has given value.
		/// </summary>
		/// <param name="setText"> Text to show. </param>
		/// <param name="setClassMemberName"> Name of class member whose value to check. </param>
		/// <param name="setRequiredValue"> The value the class member must have in order for the help box to be shown. </param>
		public HelpBoxAttribute(string setText, string setClassMemberName, object setRequiredValue)
		{
			text = setText;
			messageType = HelpBoxMessageType.Info;

			alwaysShow = false;
			classMemberName = setClassMemberName;
			comparisonType = Is.Equal;
			requiredValue = setRequiredValue;
			evaluatorExpression = null;
		}

		/// <summary>
		/// Show info help box with text if another class member value passes comparison.
		/// </summary>
		/// <param name="setText"> Text to show. </param>
		/// <param name="setClassMemberName"> Name of class member whose value to check. </param>
		/// <param name="comparison"> Type of comparison check to perform on value. </param>
		/// <param name="setRequiredValue"> The value the class member must have in order for the help box to be shown. </param>
		public HelpBoxAttribute(string setText, string setClassMemberName, Is comparison, object setRequiredValue)
		{
			text = setText;
			messageType = HelpBoxMessageType.Info;

			alwaysShow = false;
			classMemberName = setClassMemberName;
			comparisonType = comparison;
			requiredValue = setRequiredValue;
			evaluatorExpression = null;
		}

		/// <inheritdoc/>
		public Target Target
		{
			get
			{
				return Target.This;
			}
		}

		public IShowInInspectorIf GetEvaluator()
		{
			return new ShouldShowHelpBoxEvaluator(this);
		}

		public bool ShowInInspector([NotNull]Type containingClassType, [CanBeNull]object containingClassInstance, [CanBeNull]MemberInfo classMember)
		{
			if(alwaysShow)
			{
				return true;
			}

			if(evaluatorExpression != null)
			{
				if(evaluator == null)
				{
					evaluator = EvaluationUtility.GenerateEvaluatorForBooleanExpression(evaluatorExpression);
				}
				return evaluator.Evaluate(containingClassType, containingClassInstance, classMember);
			}

			if(!string.IsNullOrEmpty(classMemberName))
			{
				var classMembers = containingClassType.GetMember(classMemberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

				int classMembersFound = classMembers.Length;

				if(classMembersFound == 0)
				{
					Debug.LogError("HelpBoxAttribute class member by name \"" + classMemberName+"\" not found.");
					return true;
				}

				classMember = classMembers[0];
			}
			
			var field = classMember as FieldInfo;
			object currentValue;
			Type classMemberType;
			if(field is object)
			{
				classMemberType = field.FieldType;
				currentValue = field.GetValue(field.IsStatic ? null : containingClassInstance);
			}
			else
			{
				var property = classMember as PropertyInfo;
				if(property is object)
				{
					classMemberType = property.PropertyType;
					var getAccessor = property.GetGetMethod();
					currentValue = getAccessor.Invoke(getAccessor.IsStatic ? null : containingClassInstance, null);
				}
				else
				{
					Debug.LogError("ShowInInspectorIfAttribute class member by name \"" + classMemberName + "\" was not a field or a property.");
					return true;
				}
			}

			#if DEV_MODE
			Debug.Log("Show if \"" + classMemberName + "\" value (" + (currentValue == null ? "null" : currentValue.ToString()) + ") "+ comparisonType + " " + (requiredValue == null ? "null" : requiredValue.ToString()));
			#endif

			return EvaluationUtility.Test(currentValue, comparisonType, requiredValue);
		}
	}
}