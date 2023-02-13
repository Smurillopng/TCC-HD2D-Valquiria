using System;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute that supports specifying whether it applies to the element that follows it, or the members of that element.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
	public abstract class TargetableAttribute : Attribute, ITargetableAttribute
	{
		/// <summary>
		/// Determines the attribute applies to the element that follows it, or the members of that element.
		/// 
		/// By default an attribute applies to the element that follows it.
		/// </summary>
		public readonly Target target = Target.Default;

		/// <inheritdoc/>
		public Target Target
		{
			get
			{
				return target;
			}
		}

		protected TargetableAttribute() { }

		/// <summary>
		/// Attribute that supports specifying whether it applies to the element that follows it, or the members of that element.
		/// </summary>
		/// <param name="attributeTarget">
		/// Determines whether the attribute should target that element that follows it or the members of said element.
		/// </param>
		protected TargetableAttribute(Target attributeTarget)
		{
			target = attributeTarget;
		}
	}
}