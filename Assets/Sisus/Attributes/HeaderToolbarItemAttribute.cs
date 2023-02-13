using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute that can be used to mark a method that defines a header toolbar item for components and/or assets of specific type(s).
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false), MeansImplicitUse(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature, ImplicitUseTargetFlags.Itself)]
	public class HeaderToolbarItemAttribute : Attribute
	{
		/// <summary>
		/// UnityEngine.Object derived type or interface type.
		/// </summary>
		[NotNull]
		public readonly Type targetType;

		public HeaderToolbarItemAttribute(Type setTargetType)
		{
			targetType = setTargetType;
		}
	}
}