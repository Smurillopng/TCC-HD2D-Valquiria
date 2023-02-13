using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Place this attribute on a class which implements IFieldDrawer to inform DrawerProvider
	/// that the drawers are used to represent fields or properties of a certain type - and optionally ones
	/// inheriting from said type.
	/// </summary>
	public sealed class DrawerForFieldAttribute : DrawerForBaseAttribute
	{
		private readonly Type type;
		private readonly bool targetExtendingClasses;

		/// <inheritdoc/>
		[NotNull]
		public override Type Target
		{
			get
			{
				return type;
			}
		}

		/// <inheritdoc/>
		public override bool TargetExtendingTypes
		{
			get
			{
				return targetExtendingClasses;
			}
		}

		public DrawerForFieldAttribute(Type setType, bool setUseForExtendingClasses = true) : base(false)
		{
			type = setType;
			targetExtendingClasses = setUseForExtendingClasses && !setType.IsValueType;

			#if DEV_MODE && PI_ASSERTATIONS
			AssertDataIsValid(null);
			#endif
		}

		internal DrawerForFieldAttribute(Type setType, bool setUseForExtendingClasses, bool setIsFallback) : base(setIsFallback)
		{
			type = setType;
			targetExtendingClasses = setUseForExtendingClasses && !setType.IsValueType;

			#if DEV_MODE && PI_ASSERTATIONS
			AssertDataIsValid(null);
			#endif
		}

		#if DEV_MODE && PI_ASSERTATIONS
		/// <inheritdoc/>
		public override void AssertDataIsValid(Type drawerType)
		{
			if(drawerType != null)
			{
				UnityEngine.Debug.Assert(typeof(IFieldDrawer).IsAssignableFrom(drawerType), "DrawerForFieldAttribute found on class which does not implement IFieldDrawer.");
			}
			if(TargetExtendingTypes)
			{
				UnityEngine.Debug.Assert(!type.IsValueType, "DrawerForFieldAttribute targetExtendingClasses was true but Target type "+type.FullName+" was value type.");
			}
		}
		#endif
	}
}