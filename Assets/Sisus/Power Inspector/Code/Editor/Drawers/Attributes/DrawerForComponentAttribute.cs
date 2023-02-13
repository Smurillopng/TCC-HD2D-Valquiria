using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Place this attribute on a class which implements IComponentDrawer to inform DrawerProvider
	/// that the drawers are used to represent Components of the given type - and optionally types inheriting from said type.
	/// </summary>
	public sealed class DrawerForComponentAttribute : DrawerForBaseAttribute
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

		public DrawerForComponentAttribute(Type setType, bool targetsExtendingClasses = true) : base(false)
		{
			type = setType;
			targetExtendingClasses = targetsExtendingClasses;

			#if DEV_MODE && PI_ASSERTATIONS
			AssertDataIsValid(null);
			#endif
		}

		public DrawerForComponentAttribute(Type setType, bool targetsExtendingClasses, bool setIsFallback) : base(setIsFallback)
		{
			type = setType;
			targetExtendingClasses = targetsExtendingClasses;

			#if DEV_MODE && PI_ASSERTATIONS
			AssertDataIsValid(null);
			#endif
		}

		#if DEV_MODE && PI_ASSERTATIONS
		/// <inheritdoc/>
		public override void AssertDataIsValid(Type drawerType)
		{
			string messageBase = drawerType == null ? string.Concat("DrawerForComponent(", type == null ? "null" : type.Name, ")") : string.Concat("DrawerForComponent(", type == null ? "null" : type.Name, ")=>", drawerType.Name);

			if(drawerType != null)
			{
				UnityEngine.Debug.Assert(typeof(IComponentDrawer).IsAssignableFrom(drawerType), messageBase + " - class with attribute does not implement IComponentDrawer.\nDid you mean to use DrawerForAsset?");
			}

			if(type == null)
			{
				UnityEngine.Debug.LogError(messageBase + " - Target type was null.");
			}
			else
			{
				UnityEngine.Debug.Assert(type.IsComponent(), messageBase + " - Target type is not a Component.\nDid you mean to use DrawerForAsset?");
			}
		}
		#endif
	}
}