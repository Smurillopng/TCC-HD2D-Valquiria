using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Place this attribute on a class which implements IAssetDrawer to inform DrawerProvider
	/// that the drawers are used to represent assets of the given type - and optionally asset classes inheriting
	/// from said type.
	/// </summary>
	public sealed class DrawerForAssetAttribute : DrawerForBaseAttribute
	{
		private readonly Type type;
		private readonly bool targetExtendingTypes;

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
				return targetExtendingTypes;
			}
		}

		public DrawerForAssetAttribute(Type setType, bool targetsExtendingTypes = true) : base(false)
		{
			type = setType;
			targetExtendingTypes = targetsExtendingTypes;

			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(type.IsUnityObject());
			UnityEngine.Debug.Assert(!type.IsComponent());
			#endif
		}

		public DrawerForAssetAttribute(Type setType, bool targetsExtendingTypes, bool setIsFallback) : base(setIsFallback)
		{
			type = setType;
			targetExtendingTypes = targetsExtendingTypes;

			#if DEV_MODE && PI_ASSERTATIONS
			AssertDataIsValid(null);
			#endif
		}

		/// <summary> If Type is not visible from assembly of the Drawer, can also fetch it by name. </summary>
		internal DrawerForAssetAttribute(string internalTypeName, bool targetsExtendingTypes, bool setIsFallback) : base(setIsFallback)
		{
			#if UNITY_EDITOR
			if(internalTypeName.StartsWith("UnityEditor.", StringComparison.Ordinal))
			{
				type = Types.GetInternalEditorType(internalTypeName);

				if(type == null)
				{
					type = TypeExtensions.GetType(internalTypeName);
				}
			}
			else if(internalTypeName.StartsWith("UnityEditorInternal.", StringComparison.Ordinal))
			{
				type = Types.GetInternalEditorInternalType(internalTypeName);

				if(type == null)
				{
					type = TypeExtensions.GetType(internalTypeName);
				}
			}
			else
			#endif
			if(internalTypeName.StartsWith("UnityEngine.", StringComparison.Ordinal))
			{
				type = Types.GetInternalType(internalTypeName);

				if(type == null)
				{
					type = TypeExtensions.GetType(internalTypeName);
				}
			}
			else
			{
				type = TypeExtensions.GetType(internalTypeName);
			}

			if(type == null)
			{
				#if DEV_MODE
				UnityEngine.Debug.LogError("DrawerForAssetAttribute: Failed to fetch internal type \""+ internalTypeName + "\"!");
				#endif
				targetsExtendingTypes = false;
				type = Types.Void;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			AssertDataIsValid(null);
			#endif

			targetExtendingTypes = targetsExtendingTypes;
		}

		#if DEV_MODE && PI_ASSERTATIONS
		/// <inheritdoc/>
		public override void AssertDataIsValid(Type drawerType)
		{
			string messageBase = drawerType == null ? string.Concat("DrawerForAsset(", type == null ? "null" : type.Name, ")") : string.Concat("DrawerForAsset(", type == null ? "null" : type.Name, ")=>", drawerType.Name);

			if(drawerType != null)
			{
				UnityEngine.Debug.Assert(typeof(IAssetDrawer).IsAssignableFrom(drawerType), messageBase + " - class with attribute does not implement IAssetDrawer.\nDid you mean to use DrawerForComponent?");
			}

			if(type == null)
			{
				UnityEngine.Debug.LogError(messageBase + " - Target type was null.");
			}
			else
			{
				UnityEngine.Debug.Assert(type.IsUnityObject(), messageBase + " - Target type is not UnityEngine.Object.\nDid you mean to use DrawerForField?");
				UnityEngine.Debug.Assert(!type.IsComponent(), messageBase + " - Target type is a Component.\nDid you mean to use DrawerForComponent?");
			}
		}
		#endif
	}
}