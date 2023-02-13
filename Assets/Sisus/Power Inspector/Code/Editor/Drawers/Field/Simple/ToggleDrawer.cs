using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Draws a toggle control which can be backed by a boolean field or property.
	/// </summary>
	[Serializable, DrawerForField(typeof(bool), false, true)]
	public sealed class ToggleDrawer : PrefixControlComboDrawer<bool>
	{
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ToggleDrawer Create(bool value, [CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, [CanBeNull]GUIContent label, bool readOnly)
		{
			ToggleDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ToggleDrawer();
			}
			result.Setup(value, typeof(bool), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			if(setValue == null)
			{
				if(setMemberInfo != null && setMemberInfo.CanRead)
				{
					Setup((bool)setMemberInfo.GetValue(0), setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
				}
				else
				{
					#if DEV_MODE
					Debug.LogWarning(ToString(setLabel, setMemberInfo) + " SetupInterface called with null value and null or ReadOnly memberInfo: "+StringUtils.ToString(setMemberInfo));
					#endif
					Setup(false, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
				}
			}
			else
			{
				Setup((bool)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
			}
		}

		/// <inheritdoc />
		public override bool DrawControlVisuals(Rect position, bool value)
		{
			return DrawGUI.Active.Toggle(position, value);
		}

		/// <inheritdoc />
		protected override bool GetRandomValue()
		{
			return RandomUtils.Bool();
		}
	}
}