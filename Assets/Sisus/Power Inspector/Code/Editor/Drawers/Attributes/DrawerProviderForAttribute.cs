using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute that can be used to specify which DrawerProvider class is the default provider for a specific inspector class type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true), MeansImplicitUse(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature, ImplicitUseTargetFlags.Itself)]
	public class DrawerProviderForAttribute : Attribute
	{
		public readonly Type inspectorType;

		/// <summary>
		/// If true the DrawerProvider class which has this attribute has lower priority than any DrawerProvider classes with the attribute where this is false.
		/// </summary>
		public readonly bool isFallback;

		public DrawerProviderForAttribute(Type setTnspectorType, bool setIsFallback = false)
		{
			inspectorType = setTnspectorType;
			isFallback = setIsFallback;
		}
	}
}