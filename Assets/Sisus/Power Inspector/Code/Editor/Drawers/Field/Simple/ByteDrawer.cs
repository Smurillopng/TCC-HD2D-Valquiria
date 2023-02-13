using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(byte), false, true)]
	public sealed class ByteDrawer : NumericDrawer<byte>
	{
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns>
		/// The instance, ready to be used. If value is null and can't generate an instance based on memberInfo (e.g. because member type is
		/// a generic type definition), then returns null.  </returns>
		public static ByteDrawer Create(byte value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			ByteDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ByteDrawer();
			}
			result.Setup(value, typeof(byte), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((byte)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref byte inputValue, byte inputMouseDownValue, float mouseDelta)
		{
			inputValue = (byte)Mathf.Clamp(inputMouseDownValue + Mathf.RoundToInt(mouseDelta * IntDrawer.DragSensitivity), 0, 255);
		}

		/// <inheritdoc />
		public override byte DrawControlVisuals(Rect position, byte value)
		{
			int valueInt = DrawGUI.Active.ClampedIntField(controlLastDrawPosition, value, 0, 255);
			return (byte)valueInt;
		}

		/// <inheritdoc />
		protected override byte GetRandomValue()
		{
			return RandomUtils.Byte();
		}
	}
}