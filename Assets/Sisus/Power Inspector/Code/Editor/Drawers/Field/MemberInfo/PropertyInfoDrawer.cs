﻿using System;
using System.Reflection;
using System.Collections.Generic;
using Sisus.Attributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(PropertyInfo), true, true)] //use for extending classes must be true, so that PropertyInfoDrawer is used for RuntimeType correctly
	public class PropertyInfoDrawer : MemberInfoBaseDrawer<PropertyInfo>
	{
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedPropertyInfo for the class member that the created drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static PropertyInfoDrawer Create(PropertyInfo value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			PropertyInfoDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new PropertyInfoDrawer();
			}
			result.Setup(value, typeof(PropertyInfo), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}


		/// <inheritdoc />
		protected override void GenerateMenuItems(ref List<PopupMenuItem> rootItems, ref Dictionary<string, PopupMenuItem> groupsByLabel, ref Dictionary<string, PopupMenuItem> itemsByLabel)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(MemberInfoDrawerUtility.IsReady);
			#endif

			rootItems = MemberInfoDrawerUtility.propertyRootItems;
			groupsByLabel = MemberInfoDrawerUtility.propertyGroupsByLabel;
			itemsByLabel = MemberInfoDrawerUtility.propertyItemsByLabel;
		}

		/// <inheritdoc />
		protected override string GetLabelText(PropertyInfo value)
		{
			var sb = StringBuilderPool.Create();
			sb.Append(TypeExtensions.GetShortName(value.ReflectedType));
			sb.Append('.');
			StringUtils.ToString(value, sb);
			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		/// <inheritdoc />
		protected override GUIContent MenuLabel()
		{
			return GUIContentPool.Create("Property");
		}

		/// <inheritdoc />
		protected override PropertyInfo GetRandomValue()
		{
			if(!GenerateMenuItemsIfNotGenerated())
			{
				Debug.LogWarning("Can generate random value yet. Setup still in progress...");
				return Value;
			}

			int count = MemberInfoDrawerUtility.propertyItemsByLabel.Count;
			int random = Random.Range(0, count);
			var ienumerator = MemberInfoDrawerUtility.propertyItemsByLabel.Values.GetEnumerator();
			for(int n = random - 1; n >= 0; n--)
			{
				ienumerator.MoveNext();
			}
			return (PropertyInfo)ienumerator.Current.IdentifyingObject;
		}
	}
}