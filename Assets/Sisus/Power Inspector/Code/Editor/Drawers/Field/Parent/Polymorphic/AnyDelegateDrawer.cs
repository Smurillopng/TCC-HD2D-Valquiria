using System;
using System.Collections.Generic;
using System.Linq;
using Sisus.Attributes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary> Drawer for field of the exact type MulticastDelegate or Delegate. </summary>
	[Serializable, DrawerForField(typeof(MulticastDelegate), false, false), DrawerForField(typeof(Delegate), false, false)]
	public class AnyDelegateDrawer : PolymorphicDrawer
	{
		private static Type[] allDelegateTypes;

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
				return allDelegateTypes;
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
		public static AnyDelegateDrawer Create(object value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			AnyDelegateDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new AnyDelegateDrawer();
			}
			result.Setup(value, DrawerUtility.GetType(memberInfo, value), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		protected override void Setup(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			if(allDelegateTypes == null)
			{
				#if UNITY_2019_3_OR_NEWER
				allDelegateTypes = TypeCache.GetTypesDerivedFrom<MulticastDelegate>().ToArray();
				#else
				allDelegateTypes = TypeExtensions.GetExtendingTypes<MulticastDelegate>(true).ToArray();
				#endif
			}

			if(setValueType == null)
			{
				#if DEV_MODE
				Debug.LogError(GetType().Name+ ".Setup called with setValueType null");
				#endif

				setValueType = DrawerUtility.GetType(setMemberInfo, setValue);
				if(!typeof(Delegate).IsAssignableFrom(setValueType))
				{
					setValueType = typeof(MulticastDelegate);
				}
			}

			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override IDrawer BuildDrawerForValue(Type setType, object setValue, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			return DelegateDrawer.Create((MulticastDelegate)setValue, setMemberInfo, setType, setParent, setLabel, setReadOnly);
		}
	}
}