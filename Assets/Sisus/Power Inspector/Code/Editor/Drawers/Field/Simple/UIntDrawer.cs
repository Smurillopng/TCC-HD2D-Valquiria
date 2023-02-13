using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer representing fields of type uint.
	/// </summary>
	[Serializable, DrawerForField(typeof(uint), false, true)]
	public sealed class UIntDrawer : NumericDrawer<uint>
	{
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static UIntDrawer Create(uint value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			UIntDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new UIntDrawer();
			}
			result.Setup(value, typeof(uint), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((uint)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref uint inputValue, uint inputMouseDownValue, float mouseDelta)
		{
			int setValue = (int)inputMouseDownValue + Mathf.RoundToInt(mouseDelta * IntDrawer.DragSensitivity);
			if(setValue < 0)
			{
				inputValue = 0u;
			}
			else
			{
				inputValue = (uint)setValue;
			}
		}

		/// <inheritdoc />
		public override uint DrawControlVisuals(Rect position, uint value)
		{
			int setValue = DrawGUI.Active.IntField(controlLastDrawPosition, (int)value);
			if(setValue < 0)
			{
				return 0u;
			}
			return (uint)setValue;
		}

		/// <inheritdoc />
		protected override uint GetRandomValue()
		{
			return RandomUtils.UInt();
		}
	}
}