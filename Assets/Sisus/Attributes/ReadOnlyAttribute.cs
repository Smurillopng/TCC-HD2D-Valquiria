using System;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute that specifies that its target should be drawn as a non-editable read-only field.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class ReadOnlyAttribute : TargetableAttribute
	{
		public ReadOnlyAttribute() : base() { }

		public ReadOnlyAttribute(Target attributeTarget) : base(attributeTarget) { }
	}
}