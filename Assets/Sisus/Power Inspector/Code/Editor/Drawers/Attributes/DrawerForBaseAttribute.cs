using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Base class for attributes used for providing information about which targets a Drawer class is used to represent.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true), MeansImplicitUse(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature, ImplicitUseTargetFlags.Itself)]
	public abstract class DrawerForBaseAttribute : Attribute
	{
		/// <summary>
		/// If true the Drawer class which has this attribute has lower priority than Drawer
		/// with an attribute where this is false. If there are more than one GUIInstructructions with the same Target type,
		/// then ones where this is false will be prioritized.
		/// </summary>
		public readonly bool isFallback;

		/// <summary>
		/// The type of the target which the Drawer represent.
		/// </summary>
		public abstract Type Target
		{
			get;
		}

		/// <summary>
		/// If true the Drawer class which has this attribute will also be used to represent classes that inherit
		/// from Target type.
		/// If there are more than one GUIInstructructions which could be used to represent a target, then ones where Target
		/// matches the type of the target exactly will be prioritized over ones where type of target only inherits from Target
		/// and TargetExtendingTypes is true.
		/// </summary>
		/// <value>
		/// False if drawers are only used for targets whose type matches Target exactly, true if drawers are also
		/// used to represent classes that inherit from Target.
		/// </value>
		public abstract bool TargetExtendingTypes
		{
			get;
		}

		protected DrawerForBaseAttribute(bool setIsFallback)
		{
			isFallback = setIsFallback;
		}

		#if DEV_MODE && PI_ASSERTATIONS
		/// <summary> If attribute contains invalid data log an error to the console. </summary>
		public abstract void AssertDataIsValid([CanBeNull]Type drawerType);
		#endif
	}
}