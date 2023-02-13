using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer representing fields of type ulong aka UInt64.
	/// </summary>
	[Serializable, DrawerForField(typeof(ulong), false, true)]
	public sealed class ULongDrawer : NumericDrawer<ulong>
	{
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ULongDrawer Create(ulong value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			ULongDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ULongDrawer();
			}
			result.Setup(value, typeof(ulong), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((ulong)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref ulong inputValue, ulong inputMouseDownValue, float mouseDelta)
		{
			if(mouseDelta > 0f)
			{
				try
				{
					checked
					{
						inputValue = inputMouseDownValue + (ulong)Mathf.RoundToInt(mouseDelta * IntDrawer.DragSensitivity);	
					}
				}
				catch(OverflowException)
				{
					inputValue = ulong.MaxValue;
				}
			}
			else
			{
				long modify = Mathf.RoundToInt(mouseDelta * IntDrawer.DragSensitivity);
				long setValueIfPositive = (long)inputMouseDownValue + modify;
				if(setValueIfPositive < 0u)
				{
					inputValue = 0u;
				}
				else
				{
					inputValue = (ulong)setValueIfPositive;
				}
			}
		}

		/// <inheritdoc />
		public override ulong DrawControlVisuals(Rect position, ulong value)
		{
			int asInt = value >= int.MaxValue ? int.MaxValue : (int)value;
			return (ulong)DrawGUI.Active.IntField(controlLastDrawPosition, asInt);
		}

		/// <inheritdoc />
		protected override ulong GetRandomValue()
		{
			return RandomUtils.ULong(ulong.MinValue, ulong.MaxValue);
		}
	}
}