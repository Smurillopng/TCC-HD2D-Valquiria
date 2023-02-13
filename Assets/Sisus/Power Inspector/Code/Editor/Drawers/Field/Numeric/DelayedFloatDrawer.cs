using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer representing fields of type float.
	/// </summary>
	[Serializable, DrawerForAttribute(true, typeof(DelayedAttribute), typeof(float))]
	public sealed class DelayedFloatDrawer : NumericDrawer<float>
	{
		public const float DragSensitivity = 0.04f;

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static DelayedFloatDrawer Create(float value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			DelayedFloatDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new DelayedFloatDrawer();
			}
			result.Setup(value, typeof(float), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((float)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		public void SetupInterface(object attribute, object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((float)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref float inputValue, float inputMouseDownValue, float mouseDelta)
		{
			try
			{
				inputValue = inputMouseDownValue + mouseDelta * DragSensitivity;
			}
			catch(Exception e)
			{
				Debug.LogWarning(e + " with inputValue="+ inputValue+ ", inputMouseDownValue="+ inputMouseDownValue+ ", mouseDelta="+ mouseDelta);
			}
		}

		/// <inheritdoc />
		public override float DrawControlVisuals(Rect position, float value)
		{
			return DrawGUI.Active.FloatField(position, value, true);
		}

		/// <inheritdoc />
		protected override bool ValuesAreEqual(float a, float b)
		{
			return a.Approximately(b);
		}

		/// <inheritdoc />
		protected override bool GetDataIsValidUpdated()
		{
			return !float.IsNaN(Value);
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(!ReadOnly)
			{
				menu.AddSeparatorIfNotRedundant();

				var val = Value;

				var rounded = Mathf.Round(val);
				if(!val.Equals(rounded))
				{
					menu.Add("Round", () => Value = rounded, val.Equals(rounded));
				}
				if(!val.Equals(0f))
				{
					menu.Add("Invert", () => Value = 0f - val);
				}

				menu.Add("Set To.../Zero", () => Value = 0f, val.Equals(0f));
				menu.Add("Set To.../One", () => Value = 1f, val.Equals(1f));
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		/// <inheritdoc />
		protected override float GetRandomValue()
		{
			return RandomUtils.Float(-float.MaxValue, float.MaxValue);
		}
	}
}