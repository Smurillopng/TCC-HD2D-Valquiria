using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Place this attribute on a class which implements IPropertyDrawerDrawer to inform DrawerProvider
	/// that the drawers are used to represent PropertyAttributes of a certain attributeType.
	/// </summary>
	public sealed class DrawerForAttributeAttribute : DrawerForBaseAttribute
	{
		[CanBeNull]
		public readonly Type attributeType;

		[NotNull]
		public readonly Type valueType;

		/// <inheritdoc/>
		[NotNull]
		public override Type Target
		{
			get
			{
				return attributeType;
			}
		}

		/// <inheritdoc/>
		public override bool TargetExtendingTypes
		{
			get
			{
				return true; // For now at least DrawerForAttribute will always target extending types
			}
		}

		public DrawerForAttributeAttribute([NotNull]Type setAttributeType) : base(false)
		{
			attributeType = setAttributeType;
			valueType = typeof(object);

			#if DEV_MODE && PI_ASSERTATIONS
			AssertDataIsValid(null);
			#endif
		}

		public DrawerForAttributeAttribute([NotNull]Type setAttributeType, [CanBeNull]Type setValueType) : base(false)
		{
			attributeType = setAttributeType;
			valueType = setValueType == null ? typeof(object) : setValueType;

			#if DEV_MODE && PI_ASSERTATIONS
			AssertDataIsValid(null);
			#endif
		}

		internal DrawerForAttributeAttribute(bool setIsFallback, [NotNull]Type setAttributeType, [CanBeNull]Type setValueType) : base(setIsFallback)
		{
			attributeType = setAttributeType;
			valueType = setValueType == null ? typeof(object) : setValueType;

			#if DEV_MODE && PI_ASSERTATIONS
			AssertDataIsValid(null);
			#endif
		}

		#if DEV_MODE && PI_ASSERTATIONS
		/// <inheritdoc/>
		public override void AssertDataIsValid(Type drawerType)
		{
			string messageBase = drawerType == null ?
				string.Concat("DrawerForAttribute(attributeType=", attributeType == null ? "null" : attributeType.Name, ", valueType=", valueType == null ? "null" : valueType.Name, ")")
				: string.Concat("DrawerForAttribute(attributeType=", attributeType == null ? "null" : attributeType.Name, ", valueType=", valueType == null ? "null" : valueType.Name, ")=>", drawerType.Name, ")");

			if(drawerType != null)
			{
				UnityEngine.Debug.Assert(typeof(IFieldDrawer).IsAssignableFrom(drawerType), messageBase + " - class with attribute does not implement IFieldDrawer.\nDid you mean to use DrawerForComponent or DrawerForAsset?");
			}

			if(attributeType == null)
			{
				UnityEngine.Debug.LogError(messageBase + " - attributeType null.");
			}
			else if(!attributeType.IsSubclassOf(typeof(Attribute)))
			{
				UnityEngine.Debug.LogError(messageBase + " - attributeType " + StringUtils.ToString(attributeType)+ " was not an Attribute.\nDid you mean to use DrawerForField?");
			}

			if(valueType == null)
			{
				UnityEngine.Debug.LogError(messageBase + " - valueType was null. Should use System.Object instead.");
			}
		}
		#endif
	}
}