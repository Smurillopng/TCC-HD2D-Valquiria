using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer representing fields of type int.
	/// </summary>
	[Serializable, DrawerForField(typeof(int), false, true)]
	public sealed class IntDrawer : NumericDrawer<int>
	{
		public const float DragSensitivity = 0.25f;

		/// <inheritdoc />
		protected override int ValueDuringMixedContent
		{
			get
			{
				return 1961271802;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static IntDrawer Create(int value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			IntDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new IntDrawer();
			}
			result.Setup(value, typeof(int), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((int)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}
		
		/// <inheritdoc />
		public override void OnPrefixDragged(ref int inputValue, int inputMouseDownValue, float mouseDelta)
		{
			inputValue = inputMouseDownValue + Mathf.RoundToInt(mouseDelta * DragSensitivity);
		}

		/// <inheritdoc />
		public override int DrawControlVisuals(Rect position, int value)
		{
			return DrawGUI.Active.IntField(controlLastDrawPosition, value);
		}

		/// <inheritdoc />
		protected override int GetRandomValue()
		{
			return RandomUtils.Int();
		}
	}
}