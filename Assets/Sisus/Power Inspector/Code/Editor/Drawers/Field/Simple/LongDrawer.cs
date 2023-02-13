using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer representing fields of type long aka Int64.
	/// </summary>
	[Serializable, DrawerForField(typeof(long), false, true)]
	public sealed class LongDrawer : NumericDrawer<long>
	{
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static LongDrawer Create(long value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			LongDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new LongDrawer();
			}
			result.Setup(value, typeof(long), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((long)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref long inputValue, long inputMouseDownValue, float mouseDelta)
		{
			inputValue = inputMouseDownValue + Mathf.RoundToInt(mouseDelta * IntDrawer.DragSensitivity);
		}

		/// <inheritdoc />
		public override long DrawControlVisuals(Rect position, long value)
		{
			int asInt = value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;
			return DrawGUI.Active.IntField(controlLastDrawPosition, asInt);
		}

		/// <inheritdoc />
		protected override long GetRandomValue()
		{
			return RandomUtils.Long(long.MinValue, long.MaxValue);
		}
	}
}