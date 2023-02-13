#define DEBUG_SETUP

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Drawer for representing abstract classes including interfaces.
	/// Allows selecting value from popup menu dynamically popuplated with
	/// all implementing classes. If any UnityEngine.Object classes implement
	/// the interface, then an object reference field will be displayed for assigning
	/// those.
	/// </summary>
	[Serializable]
	public class AbstractDrawer : PolymorphicDrawer
	{
		private bool canBeUnityObject;
		private IEnumerable<Type> implementingNonUnityObjectTypes;
		private IEnumerable<Type> implementingUnityObjectTypes;

		/// <inheritdoc />
		protected override bool CanBeUnityObject
		{
			get
			{
				return canBeUnityObject;
			}
		}

		/// <inheritdoc />
		protected override IEnumerable<Type> NonUnityObjectTypes
		{
			get
			{
				return implementingNonUnityObjectTypes;
			}
		}

		public static AbstractDrawer Create(object value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			AbstractDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new AbstractDrawer();
			}
			result.Setup(value, memberInfo != null ? memberInfo.Type : value != null ? value.GetType() : Types.SystemObject, memberInfo, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}
		
		/// <inheritdoc />
		protected override void Setup(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			if(setValueType == null)
			{
				#if DEV_MODE
				Debug.LogError(GetType().Name+".Setup called with setValueType null");
				#endif
				setValueType = setMemberInfo != null ? setMemberInfo.Type : setValue != null ? setValue.GetType() : Types.SystemObject;
			}

			if(setValueType.IsInterface)
			{
				// todo: include invisible types from setValueType's assembly?
				setValueType.GetImplementingConcreteTypes(out implementingUnityObjectTypes, out implementingNonUnityObjectTypes);
				canBeUnityObject = implementingUnityObjectTypes.Any();
			}
			else if(Types.UnityObject.IsAssignableFrom(setValueType))
			{
				implementingUnityObjectTypes = setValueType.GetExtendingTypesNotThreadSafe(false, false);
				implementingNonUnityObjectTypes = ArrayPool<Type>.ZeroSizeArray;
				canBeUnityObject = true;
			}
			else
			{
				implementingNonUnityObjectTypes = setValueType.GetExtendingTypesNotThreadSafe(false, false);
				implementingUnityObjectTypes = ArrayPool<Type>.ZeroSizeArray;
				canBeUnityObject = false;
			}
			
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);

			#if DEV_MODE && DEBUG_SETUP
			Debug.Log(Msg("CanBeUnityObject=", CanBeUnityObject," implementingUnityObjectTypes=", StringUtils.ToString(implementingUnityObjectTypes), ", NonUnityObjectTypes=", StringUtils.ToString(NonUnityObjectTypes), ", DrawInSingleRow=", DrawInSingleRow));
			#endif
		}

		/// <inheritdoc />
		protected override bool IsValidUnityObjectValue(Object test)
		{
			if(test == null)
            {
				return CanBeNull;
            }
			return implementingNonUnityObjectTypes.Contains(test.GetType());
		}

		/// <inheritdoc/>
		protected override bool AllowSceneObjects()
		{
			if(implementingNonUnityObjectTypes == null)
			{
				return false;
			}
			foreach(var type in implementingNonUnityObjectTypes)
            {
				if(type.IsGameObject() || type.IsComponent())
				{
					return true;
				}
            }
			return false;
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			base.Dispose();
		}
	}
}