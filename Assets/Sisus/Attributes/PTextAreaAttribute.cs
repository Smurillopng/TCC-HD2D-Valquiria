using System;
using UnityEngine;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute to make a string be edited with a height-flexible and scrollable text area.
	/// 
	/// This is just like Unity's built-in TextAreaAttribute but supports targeting of properties and parameters in addition to fields.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
	public class PTextAreaAttribute : PropertyAttribute, ITargetableAttribute
	{
		/// <summary> The minimum amount of lines the text area will use. </summary>
		public readonly int minLines;

		/// <summary> The maximum amount of lines the text area can show before it starts using a scrollbar. </summary>
		public readonly int maxLines;

		/// <summary>
		/// Attribute to make a string be edited with a multi-line textfield.
		/// 
		/// This is just like Unity's built-in MultilineAttribute but supports targeting of properties and parameters in addition to fields.
		/// </summary>
		/// <param name="lineCount"> How many lines of text to make room for. Default is 3. </param>
		public PTextAreaAttribute(int minLineCount, int maxLineCount)
		{
			minLines = minLineCount;
			maxLines = maxLineCount;
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