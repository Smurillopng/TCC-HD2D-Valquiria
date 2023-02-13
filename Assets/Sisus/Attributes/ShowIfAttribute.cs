using System;
using System.Reflection;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute that can be used to make class member that follows the attribute only be shown in the inspector when a predicate statement is true.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = false), MeansImplicitUse(ImplicitUseKindFlags.Access | ImplicitUseKindFlags.Assign)]
	public class ShowIfAttribute : Attribute, IShowInInspectorIf
	{
		[CanBeNull]
		private readonly string expression;

		[CanBeNull]
		private IShowIfEvaluator evaluator;

		/// <summary>
		/// Only show the class member that follows this attribute if the predicate statement is true.
		/// </summary>
		/// <param name="booleanExpression">
		/// The conditional expression which must be true for the help box to be shown.
		/// This can contain multiple comparisons between class members and target values.
		/// Example: intField == 3 && (floatField > 8 || EditorApplication.isPlaying)
		/// </param>
		/// <param name="setMessageType"> Type of the message; info, warning or error. </param>
		public ShowIfAttribute(string booleanExpression) : base()
		{
			expression = booleanExpression;
		}

		/// <summary>
		/// Only show the class member that follows this attribute if another class member
		/// by the name <paramref name="classMemberName"/> has the value of <paramref name="requiredValue"/>.
		/// </summary>
		/// <param name="classMemberName"> Name of another class member inside the same class as the class member with this attribute. </param>
		/// <param name="requiredValue"></param>
		public ShowIfAttribute(string classMemberName, object requiredValue) : base()
		{
			evaluator = new ValueTest(classMemberName, Is.Equal, requiredValue);
			expression = null;
		}

		/// <summary>
		/// Only show the class member that follows this attribute if another class member
		/// by the name <paramref name="classMemberName"/> has the value of <paramref name="requiredValue"/>.
		/// </summary>
		/// <param name="classMemberName"> Name of another class member inside the same class as the class member with this attribute. </param>
		/// <param name="requiredValue"></param>
		public ShowIfAttribute(string classMemberName, Is comparison, object requiredValue) : base()
		{
			evaluator = new ValueTest(classMemberName, comparison, requiredValue);
			expression = null;
		}

		/// <summary>
		/// Only show the class member that follows this attribute if another class member
		/// by the name <paramref name="classMemberName"/> has the value of <paramref name="requiredValue"/>.
		/// </summary>
		/// <param name="classMemberName"> Name of another class member inside the same class as the class member with this attribute. </param>
		/// <param name="requiredValue"></param>
		public ShowIfAttribute(string classMemberName, string comparison, object requiredValue) : base()
		{
			expression = null;

			Is comparisonType;
			switch(comparison)
			{
				case "==":
					comparisonType = Is.Equal;
					break;
				case "!=":
					comparisonType = Is.Not;
					break;
				case "<":
					comparisonType = Is.Smaller;
					break;
				case "<=":
					comparisonType = Is.SmallerOrEqual;
					break;
				case ">":
					comparisonType = Is.Larger;
					break;
				case ">=":
					comparisonType = Is.LargerOrEqual;
					break;
				default:
					throw new Exception("Unrecognized comparison: \""+comparison+ "\". Accepted comparison values are: ==, !=, <, >, <= and >=.");
			}

			evaluator = new ValueTest(classMemberName, comparisonType, requiredValue);
		}

		/// <inheritdoc/>
		public bool ShowInInspector([NotNull]Type containingClassType, [CanBeNull]object containingClassInstance, [CanBeNull]MemberInfo classMember)
		{
			if(evaluator == null)
			{
				evaluator = EvaluationUtility.GenerateEvaluatorForBooleanExpression(expression);
			}
			return evaluator.Evaluate(containingClassType, containingClassInstance, classMember);
		}
	}
}