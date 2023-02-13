using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Place this attribute on a class which implements IDecoratorDrawerDrawer to inform DrawerProvider
	/// that the drawers are used to represent PropertyAttributes of a certain type.
	/// </summary>
	public sealed class DrawerForDecoratorAttribute : DrawerForBaseAttribute
	{
		private readonly Type type;

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
				return true;
			}
		}

		public DrawerForDecoratorAttribute([NotNull]Type setType) : base(false)
		{
			type = setType;

			#if DEV_MODE && PI_ASSERTATIONS
			AssertDataIsValid(null);
			#endif
		}
		
		internal DrawerForDecoratorAttribute([NotNull]Type setType, bool setIsFallback) : base(setIsFallback)
		{
			type = setType;

			#if DEV_MODE && PI_ASSERTATIONS
			AssertDataIsValid(null);
			#endif
		}

		#if DEV_MODE && PI_ASSERTATIONS
		/// <inheritdoc/>
		public override void AssertDataIsValid(Type drawerType)
		{
			string messageBase = drawerType == null ? string.Concat("DrawerForAttribute(type=", type == null ? "null" : type.Name, ")") : string.Concat("DrawerForAttribute(type=", type == null ? "null" : type.Name, ")=>", drawerType.Name, ")");

			if(type == null)
			{
				UnityEngine.Debug.LogError(messageBase + " - Target attribute type was null.");
			}
			else
			{
				UnityEngine.Debug.Assert(type.IsSubclassOf(typeof(Attribute)), messageBase + " - Target type " + type.Name + " was not an Attribute.\nDid you mean to use DrawerForField?");
			}
		}
		#endif
	}
}