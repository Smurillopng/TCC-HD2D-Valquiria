using System;
using UnityEngine;

namespace Sisus.Attributes
{
	/// <summary>
	/// Makes target field or property be shown as a slider in the inspector.
	/// 
	/// This is just like Unity's built-in RangeAttribute but supports targeting of properties and methods in addition to fields.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
	public class PRangeAttribute : PropertyAttribute, ITargetableAttribute, IDrawerSetupDataProvider
	{
		/// <param name="minimum"> The minimum allowed value. </param>
		public readonly float min;

		/// <param name="maximum"> The maximum allowed value. </param>
		public readonly float max;

		/// <inheritdoc/>
		public Target Target
		{
			get
			{
				return Target.Members;
			}
		}

		/// <summary>
		/// Make a float or int be shown as a slider in the Inspector instead of the default number field.
		/// </summary>
		/// <param name="minimum"> The minimum allowed value. </param>
		/// <param name="maximum"> The maximum allowed value. </param>
		public PRangeAttribute(float minValue, float maxValue)
		{
			min = minValue;
			max = maxValue;
		}

		public object[] GetSetupParameters()
		{
			return new object[] { min, max };
		}
	}
}