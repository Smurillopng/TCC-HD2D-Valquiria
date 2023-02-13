using System;

namespace Sisus.Attributes
{
	/// <summary>
	/// Base class for attributes that can be used to override the default data validation logic in Power Inspector for a given target.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public abstract class ValueValidatorAttribute : Attribute, IValueValidator, ITargetableAttribute
	{
		/// <summary>
		/// Allows specifying in the case of collections, whether the attribute targets
		/// the collection itself or its members.
		/// 
		/// By default attributes on collections target the collection.
		/// </summary>
		public readonly Target target = Target.This;

		/// <inheritdoc/>
		public Target Target
		{
			get
			{
				return target;
			}
		}

		public ValueValidatorAttribute() { }

		public ValueValidatorAttribute(Target attributeTarget)
		{
			target = attributeTarget;
		}

		/// <inheritdoc/>
		public abstract bool Validate(object value);

		/// <inheritdoc/>
		public virtual bool Validate(object[] values)
		{
			for(int n = values.Length - 1; n >= 0; n--)
			{
				if(!Validate(values[n]))
				{
					return false;
				}
			}
			return true;
		}
	}
}