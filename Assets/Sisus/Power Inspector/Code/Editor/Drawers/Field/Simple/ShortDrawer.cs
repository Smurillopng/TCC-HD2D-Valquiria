using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer representing fields of type int.
	/// </summary>
	[Serializable, DrawerForField(typeof(short), false, true)]
	public sealed class ShortDrawer : NumericDrawer<short>
	{
		public const float DragSensitivity = 0.04f;
		
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ShortDrawer Create(short value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			ShortDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ShortDrawer();
			}
			result.Setup(value, typeof(short), memberInfo, parent, label, readOnly);
			result.LateSetup();
			if(readOnly)
			{
				result.ReadOnly = true;
			}
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((short)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}
		
		/// <inheritdoc />
		public override void OnPrefixDragged(ref short inputValue, short inputMouseDownValue, float mouseDelta)
		{
			inputValue = (short)Mathf.Clamp(inputMouseDownValue + Mathf.RoundToInt(mouseDelta * DragSensitivity), short.MinValue, short.MaxValue);
		}

		/// <inheritdoc />
		public override short DrawControlVisuals(Rect position, short value)
		{
			return DrawGUI.Active.ShortField(controlLastDrawPosition, value);
		}

		/// <inheritdoc />
		protected override short GetRandomValue()
		{
			return RandomUtils.Short();
		}
	}
}