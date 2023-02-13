using System;
using System.Collections.Generic;
using Sisus.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary> Drawer for field of the exact type Enum. </summary>
	[Serializable, DrawerForField(typeof(Enum), false, false)]
	public class AnyEnumDrawer : PolymorphicDrawer
	{
		private static IEnumerable<Type> visibleEnumTypes;

		/// <inheritdoc />
		protected override bool CanBeUnityObject
		{
			get
			{
				return false;
			}
		}
		
		/// <inheritdoc />
		protected override IEnumerable<Type> NonUnityObjectTypes
		{
			get
			{
				if(visibleEnumTypes == null)
				{
					visibleEnumTypes = TypeExtensions.VisibleEnumTypes;
				}
				return visibleEnumTypes;
			}
		}

		/// <inheritdoc />
		protected override bool IsValidUnityObjectValue(Object test)
		{
			return false;
		}

		/// <inheritdoc />
		protected override bool AllowSceneObjects()
		{
			return false;
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the class member that the created drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static AnyEnumDrawer Create(object value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			AnyEnumDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new AnyEnumDrawer();
			}
			result.Setup(value, DrawerUtility.GetType(memberInfo, value), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		protected override object GetRandomValue()
		{
			var enumType = TypeExtensions.GetRandomEnumType();
			var enumValues = Enum.GetValues(enumType);
			return enumValues.GetValue(UnityEngine.Random.Range(0, enumValues.Length));
		}
	}
}