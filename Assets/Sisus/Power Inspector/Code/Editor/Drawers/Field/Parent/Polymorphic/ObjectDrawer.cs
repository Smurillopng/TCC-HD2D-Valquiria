using System;
using System.Collections.Generic;
using System.Linq;
using Sisus.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(object), false, true)]
	public class ObjectDrawer : PolymorphicDrawer
	{
		private static IEnumerable<Type> nonUnityObjectTypes;

		/// <inheritdoc />
		protected override bool CanBeUnityObject
		{
			get
			{
				return true;
			}
		}
		
		/// <inheritdoc />
		protected override IEnumerable<Type> NonUnityObjectTypes
		{
			get
			{
				if(nonUnityObjectTypes == null)
				{
					nonUnityObjectTypes = TypeExtensions.AllNonUnityObjectTypes;
				}
				return nonUnityObjectTypes;
			}
		}

		/// <inheritdoc/>
		protected override bool IsValidUnityObjectValue(Object test)
		{
			return true;
		}

		/// <inheritdoc/>
		protected override bool AllowSceneObjects()
		{
			return true;
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ObjectDrawer Create(object value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			ObjectDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ObjectDrawer();
			}
			result.Setup(value, DrawerUtility.GetType(memberInfo, value), memberInfo, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}
	}
}