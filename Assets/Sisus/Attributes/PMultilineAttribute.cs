using System;
using UnityEngine;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute to make a string be edited with a multi-line textfield.
	/// 
	/// This is just like Unity's built-in MultilineAttribute but supports targeting of properties and parameters in addition to fields.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
	public class PMultilineAttribute : PropertyAttribute, ITargetableAttribute
	{
		/// <summary> How many lines of text to make room for. Default is 3. </summary>
		public readonly int lines;

		/// <summary>
		/// Attribute to make a string be edited with a multi-line textfield.
		/// 
		/// This is just like Unity's built-in MultilineAttribute but supports targeting of properties and parameters in addition to fields.
		/// </summary>
		/// <param name="lineCount"> How many lines of text to make room for. Default is 3. </param>
		public PMultilineAttribute(int lineCount = 3)
		{
			lines = lineCount;
		}

		/// <inheritdoc/>
		public Target Target
		{
			get
			{
				return Target.Members;
			}
		}
	}
}