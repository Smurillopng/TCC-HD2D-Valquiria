using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(sbyte), false, true)]
	public sealed class SByteDrawer : NumericDrawer<sbyte>
	{
		public static SByteDrawer Create(sbyte value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			SByteDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new SByteDrawer();
			}
			result.Setup(value, typeof(sbyte), memberInfo, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((sbyte)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref sbyte inputValue, sbyte inputMouseDownValue, float mouseDelta)
		{
			inputValue = (sbyte)Mathf.Clamp(inputMouseDownValue + Mathf.RoundToInt(mouseDelta * IntDrawer.DragSensitivity), 0, 255);
		}

		/// <inheritdoc />
		public override sbyte DrawControlVisuals(Rect position, sbyte value)
		{
			int valueInt = DrawGUI.Active.ClampedIntField(controlLastDrawPosition, value, -128, 127);
			return (sbyte)valueInt;
		}

		/// <inheritdoc />
		protected override sbyte GetRandomValue()
		{
			return RandomUtils.SByte();
		}
	}
}