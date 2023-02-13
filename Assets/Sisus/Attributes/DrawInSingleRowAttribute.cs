using System;

namespace Sisus.Attributes
{
	/// <summary>
	/// Causes members of target dataset to be drawn in a single row in the inspector.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public class DrawInSingleRowAttribute : TargetableAttribute
	{
		/// <summary>
		/// Causes members of target dataset to be drawn in a single row in the inspector.
		/// </summary>
		public DrawInSingleRowAttribute() : base(Target.Members) { }

		/// <summary>
		/// Causes members of target dataset to be drawn in a single row in the inspector.
		/// </summary>
		/// <param name="attributeTarget">
		/// Determines whether the attribute should target that element that follows it or the members of said element.
		/// </param>
		public DrawInSingleRowAttribute(Target attributeTarget) : base(attributeTarget) { }
	}
}