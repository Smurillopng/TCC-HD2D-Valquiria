using System;

namespace Sisus.Attributes
{
	/// <summary>
	/// Like Unity's built-in TooltipAttribute but supports various targets besides just fields.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = true)]
	public class PTooltipAttribute : UnityEngine.TooltipAttribute
	{
		/// <summary> Add a tooltip to a field, property or a method in the inspector. </summary>
		/// <param name="tooltip"> The tooltip text. </param>
		public PTooltipAttribute(string tooltip) : base(tooltip) { }

		/// <summary> Add a tooltip to a field, property or a method in the Inspector. </summary>
		/// <param name="tooltipLines" >The lines of text for the tooltip. </param>
		public PTooltipAttribute(params string[] tooltipLines) : base(string.Join("\n\n", tooltipLines)) { }
	}
}