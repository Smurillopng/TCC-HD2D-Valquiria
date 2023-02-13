//#define DEBUG_SETUP
//#define DEBUG_BUILD_MEMBERS

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Sisus.Attributes;

namespace Sisus
{
	/// <summary>
	/// Drawer for representing a KeyValuePair with any type definitions.
	/// </summary>
	[Serializable, DrawerForField(typeof(KeyValuePair<,>), false, true)]
	public class KeyValuePairDrawer : ParentFieldDrawer<object>
	{
		private static readonly Dictionary<Type, KeyValuePair<PropertyInfo, PropertyInfo>> keyValuePairPropertyInfos = new Dictionary<Type, KeyValuePair<PropertyInfo, PropertyInfo>>();

		private bool drawInSingleRow;

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get
			{
				return drawInSingleRow;
			}
		}
		
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static KeyValuePairDrawer Create(object value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			KeyValuePairDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new KeyValuePairDrawer();
			}
			result.Setup(value, DrawerUtility.GetType(memberInfo, value), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void Setup(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			if(setValueType == null)
			{
				#if DEV_MODE
				Debug.LogWarning(GetType().Name+".Setup called with setValueType=null");
				#endif
				setValueType = DrawerUtility.GetType(setMemberInfo, setValue);
			}
			var types = setValueType.GetGenericArguments();
			var keyType = types[0];
			var valueType = types[1];
			drawInSingleRow = DrawerUtility.CanDrawMultipleControlsOfTypeInSingleRow(keyType) && DrawerUtility.CanDrawMultipleControlsOfTypeInSingleRow(valueType);

			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);

			#if DEV_MODE && DEBUG_SETUP
			Debug.Log(StringUtils.ToColorizedString("ReadOnly=", ReadOnly));
			#endif
		}
		
		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList()
		{
			var type = Type;

			PropertyInfo propertyKey;
			PropertyInfo propertyValue;
			KeyValuePair<PropertyInfo, PropertyInfo> propertyInfos;
			if(!keyValuePairPropertyInfos.TryGetValue(type, out propertyInfos))
			{
				propertyKey = type.GetProperty("Key", BindingFlags.Public | BindingFlags.Instance);
				propertyValue = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
				propertyInfos = new KeyValuePair<PropertyInfo, PropertyInfo>(propertyKey, propertyValue);
				keyValuePairPropertyInfos.Add(type, propertyInfos);
			}
			else
			{
				propertyKey = propertyInfos.Key;
				propertyValue = propertyInfos.Value;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(propertyKey != null);
			Debug.Assert(propertyValue != null);
			#endif

			var hierarchy = MemberHierarchy;
			memberBuildList.Add(hierarchy.Get(memberInfo, propertyKey));
			memberBuildList.Add(hierarchy.Get(memberInfo, propertyValue));
		}

		/// <inheritdoc />
		protected override void DoBuildMembers()
		{
			bool readOnly = ReadOnly;

			DrawerArrayPool.Resize(ref members, 2);
			
			var keyInfo = memberBuildList[0];
			members[0] = DrawerProvider.GetForField(keyInfo.GetValue(0), keyInfo.Type, keyInfo, this, GUIContentPool.Create("K"), readOnly);
			var valueInfo = memberBuildList[1];
			members[1] = DrawerProvider.GetForField(valueInfo.GetValue(0), valueInfo.Type, valueInfo, this, GUIContentPool.Create("V"), readOnly);
			
			#if DEV_MODE && DEBUG_BUILD_MEMBERS
			Debug.Log(StringUtils.ToColorizedString(this+".DoBuildMembers with readOnly=", readOnly, ", members[0].ReadOnly=", members[0].ReadOnly, ", members[1].ReadOnly=", members[1].ReadOnly));
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(members[0].ReadOnly == readOnly, StringUtils.ToColorizedString(this+" readOnly (", readOnly, ") != members[0].ReadOnly (", members[0].ReadOnly, ")"));
			Debug.Assert(members[1].ReadOnly == readOnly, StringUtils.ToColorizedString(this+" readOnly (", readOnly, ") != members[1].ReadOnly (", members[1].ReadOnly, ")"));
			#endif
		}
		
		/// <inheritdoc/>
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			if(ReadOnly)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+".TryToManuallyUpdateCachedValueFromMember was called for parent which was ReadOnly. This should not be possible - unless external scripts caused the value change.");
				#endif
				return false;
			}
			
			var parameterTypes = ArrayExtensions.TempTypeArray(memberBuildList[0].Type, memberBuildList[1].Type);
			var parameters = ArrayExtensions.TempObjectArray(null, null);
			var type = Type;
			var constructor = type.GetConstructor(parameterTypes);
			
			var setValues = GetValues();
			for(int n = setValues.Length - 1; n >= 0; n--)
			{
				if(memberIndex == 0)
				{
					parameters[0] = memberValue;
					parameters[1] = members[1].GetValue(n);
				}
				else
				{
					parameters[0] = members[0].GetValue(n);
					parameters[1] = memberValue;
				}
				setValues[n] = constructor.Invoke(parameters);
			}

			SetValues(setValues, true, false);
			return true;
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
			
			menu.AddSeparatorIfNotRedundant();
			menu.Add("Toggle DrawInSingleRow", ()=>{drawInSingleRow = !drawInSingleRow; UpdatePrefixDrawer();});

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}
	}
}