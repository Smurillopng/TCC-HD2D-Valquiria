using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(Nullable<>), false, true)]
	public class NullableDrawer : PolymorphicDrawer
	{
		private readonly Type[] nullableTypeInArray = new Type[1];
		
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
				return nullableTypeInArray;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the class member that the created drawer represents. Can be null. </param>
		/// <param name="nullableType"> The underlying type of the Nullable type. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static NullableDrawer Create(object value, LinkedMemberInfo memberInfo, Type nullableType, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			NullableDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new NullableDrawer();
			}
			result.Setup(value, memberInfo, nullableType, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		protected sealed override void Setup(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method of NullableDrawer.");
		}

		/// <inheritdoc/>
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setValueType != null);
			Debug.Assert(setValueType.IsGenericType, setValue);
			Debug.Assert(Nullable.GetUnderlyingType(setValueType) != null, setValue);
			#endif

			Setup(setValue, setMemberInfo, Nullable.GetUnderlyingType(setValueType), setParent, setLabel, setReadOnly);
		}

		/// <summary>
		/// Sets up the drawer so that it is ready to be used.
		/// </summary>
		/// <param name="setValue"> The initial cached value of the drawers. </param>
		/// <param name="setMemberInfo"> LinkedMemberInfo for the field or property that these drawers represent. </param>
		/// <param name="setNullableType"> The underlying type of the Nullable type. </param>
		/// <param name="setParent"> Drawer whose member these Drawer are. Can be null. </param>
		/// <param name="setLabel"> The label (name) of the field. Can be null. </param>
		/// <param name="setReadOnly"> True if Drawer should be read only. </param>
		private void Setup([CanBeNull]object setValue, [CanBeNull]LinkedMemberInfo setMemberInfo, [NotNull]Type setNullableType, [CanBeNull]IParentDrawer setParent, [CanBeNull]GUIContent setLabel, bool setReadOnly)
		{
			#if DEV_MODE
			Debug.Assert(setNullableType != null);
			#endif

			nullableTypeInArray[0] = setNullableType;

			var setValueType = typeof(Nullable<>).MakeGenericType(setNullableType);
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

			if(!DrawMembersInlined)
			{
				var randomizer = ValueDrawer;
				if(randomizer == null)
				{
					SetValue(Type.DefaultValue());
					randomizer = ValueDrawer;
				}

				if(randomizer != null)
				{
					randomizer.Randomize(false);
				}
			}
			else
			{
				Value = Type.DefaultValue();
				if(!IsNull)
				{
					base.DoRandomize();
				}
			}
		}

		/// <inheritdoc />
		protected override object GetRandomValue()
		{
			if(RandomUtils.Bool())
			{
				return null;
			}
			else
			{
				return Type.DefaultValue();
			}
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