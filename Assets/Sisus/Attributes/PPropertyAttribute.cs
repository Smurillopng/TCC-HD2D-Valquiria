using System;
using UnityEngine;

namespace Sisus.Attributes
{
	/// <summary>
	/// Like Unity's built-in PropertyAttribute but supports targeting of properties and methods in addition to fields.
	/// 
	/// Also allows specifying whether the attribute should apply to the element that follows the attribute, or the members of that element.
	/// 
	/// Note however that targeting is not supported by the default inspector, so you should always handle the situation where your
	/// PropertyDrawer is used for the members of a collection instead of a collection itself.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public abstract class PPropertyAttribute : PropertyAttribute, ITargetableAttribute
	{
		/// <summary>
		/// Allows specifying in the case of collections, whether the attribute targets
		/// the members of the collection or its members.
		/// 
		/// By default attributes on collections target the members of that collection.
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

		protected PPropertyAttribute() : base() { }

		protected PPropertyAttribute(Target attributeTarget) : base()
		{
			target = attributeTarget;
		}
	}
}