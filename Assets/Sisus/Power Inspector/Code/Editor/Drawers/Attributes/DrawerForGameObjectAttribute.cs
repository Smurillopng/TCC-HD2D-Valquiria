using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus.Attributes
{
	/// <summary>
	/// Place this attribute on a class which implements IComponentDrawer to inform DrawerProvider
	/// that the drawers are used to represent Components of the given type - and optionally types inheriting from said type.
	/// </summary>
	public sealed class DrawerForGameObjectAttribute : DrawerForBaseAttribute
	{
		[CanBeNull]
		public readonly Type requireComponentOnGameObject;

		public bool isCategorizedGameObjectDrawer;

		/// <inheritdoc/>
		[NotNull]
		public override Type Target
		{
			get
			{
				return typeof(GameObject);
			}
		}

		/// <inheritdoc/>
		public override bool TargetExtendingTypes
		{
			get
			{
				return false;
			}
		}

		public DrawerForGameObjectAttribute(bool categorizedGameObjectDrawer) : base(false)
		{
			isCategorizedGameObjectDrawer = categorizedGameObjectDrawer;
		}

		public DrawerForGameObjectAttribute([CanBeNull]Type requireComponent) : base(false)
		{
			isCategorizedGameObjectDrawer = false;
			requireComponentOnGameObject = requireComponent;
		}

		internal DrawerForGameObjectAttribute(bool categorizedGameObjectDrawer, bool setIsFallback) : base(setIsFallback)
		{
			isCategorizedGameObjectDrawer = categorizedGameObjectDrawer;
		}

		internal DrawerForGameObjectAttribute(bool categorizedGameObjectDrawer, [CanBeNull]Type requireComponent, bool setIsFallback) : base(setIsFallback)
		{
			isCategorizedGameObjectDrawer = categorizedGameObjectDrawer;
			requireComponentOnGameObject = requireComponent;
		}

		#if DEV_MODE && PI_ASSERTATIONS
		/// <inheritdoc/>
		public override void AssertDataIsValid(Type drawerType)
		{
			typeof(IGameObjectDrawer).IsAssignableFrom(drawerType);
		}
		#endif
	}
}