using System;
using UnityEngine;

namespace Sisus.Attributes
{
	/// <summary>
	/// Makes target numeric value always be less than or equal to given value.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
	public class PMaxAttribute : PropertyAttribute, ITargetableAttribute
	{
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
		/// Makes target field or property value always be less than or equal to given value.
		/// </summary>
		/// <param name="maximum"> The maximum allowed value. </param>
		public PMaxAttribute(float maxValue)
		{
			max = maxValue;
		}
	}
}