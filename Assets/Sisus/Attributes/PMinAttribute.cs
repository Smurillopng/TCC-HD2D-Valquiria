using System;
using UnityEngine;

namespace Sisus.Attributes
{
	/// <summary>
	/// Makes target numeric value always be more than or equal to given value.
	/// 
	/// This is just like Unity's built-in MinAttribute but supports targeting of properties and parameters in addition to fields.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
	public class PMinAttribute : PropertyAttribute, ITargetableAttribute
	{
		/// <param name="minimum"> The minimum allowed value. </param>
		public readonly float min;

		/// <inheritdoc/>
		public Target Target
		{
			get
			{
				return Target.Members;
			}
		}

		/// <summary>
		/// Makes target field or property value always be more than or equal to given value.
		/// </summary>
		/// <param name="minimum"> The minimum allowed value. </param>
		public PMinAttribute(float minValue)
		{
			min = minValue;
		}
	}
}