using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	#if UNITY_2019_3_OR_NEWER
	//[Sisus.Attributes.DrawerForAttribute(typeof(SerializeReference), false)]
	#endif
	public class SerializeReferenceDrawer : PolymorphicDrawer, IPropertyDrawerDrawer
	{
		/// <inheritdoc/>
		public bool RequiresPropertyDrawerType
		{
			get
			{
				return false;
			}
		}

		private readonly Type[] typeInArray = new Type[1];
		
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
				return typeInArray;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the class member that the created drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static SerializeReferenceDrawer Create(object value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			SerializeReferenceDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new SerializeReferenceDrawer();
			}
			result.Setup(value, DrawerUtility.GetType(memberInfo, value), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc cref="IFieldDrawer.SetupInterface" />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		public void SetupInterface(object attribute, object setValue, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(setValue, DrawerUtility.GetType(setMemberInfo, setValue), setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void Setup(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setValueType != null);
			Debug.Assert(setValue != null || setMemberInfo != null);
			#endif
			typeInArray[0] = setValueType;
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void DoRandomize()
		{
			if(RandomUtils.Bool())
			{
				Value = null;
				return;
			}

			Value = Type.DefaultValue();
			if(!IsNull)
			{
				base.DoRandomize();
			}
		}

		/// <inheritdoc />
		protected override object GetRandomValue()
		{
			return null;
		}

		/// <inheritdoc />
		protected override bool AllowSceneObjects()
		{
			return false;
		}

		/// <inheritdoc />
		protected override bool IsValidUnityObjectValue(Object test)
		{
			return false;
		}
	}
}