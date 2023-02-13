using System;

namespace Sisus.Attributes
{
	/// <summary>
	/// Causes members of target dataset to be drawn without the parent foldout in the inspector.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public class InlineAttribute : TargetableAttribute
	{
		/// <summary>
		/// Causes members of target dataset to be drawn without the parent foldout in the inspector.
		/// </summary>
		public InlineAttribute() : base(Target.Members) { }

		/// <summary>
		/// Causes members of target dataset to be drawn without the parent foldout in the inspector.
		/// </summary>
		/// <param name="attributeTarget">
		/// Determines whether the attribute should target that element that follows it or the members of said element.
		/// </param>
		public InlineAttribute(Target attributeTarget) : base(attributeTarget) { }
	}
}