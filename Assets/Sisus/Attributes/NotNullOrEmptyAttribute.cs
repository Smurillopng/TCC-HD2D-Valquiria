using System;
using System.Collections;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute that specifies that its target should never be null or empty.
	/// 
	/// Works on any class members where value implements ICollection or IEnumerable.
	/// This includes things like arrays, lists and strings.
	/// 
	/// If value can't be cast to ICollection or IEnumerable, then validation will only return false if value value is null.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
	public class NotNullOrEmptyAttribute : ValueValidatorAttribute
	{
		public NotNullOrEmptyAttribute() : base() { }

		public NotNullOrEmptyAttribute(Target attributeTarget) : base(attributeTarget) { }

		/// <inheritdoc/>
		public override bool Validate(object value)
		{
			if(value == null)
			{
				return false;
			}

			var collection = value as ICollection;
			if(collection != null)
			{
				return collection.Count > 0;
			}

			var text = value as string;
			if(text != null)
			{
				return text.Length > 0;
			}

			var ienumerable = value as IEnumerable;
			if(ienumerable != null)
			{
				return ienumerable.GetEnumerator().MoveNext();
			}
			
			return true;
		}
	}
}