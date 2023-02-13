using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Sisus
{
	/// <summary>
	/// Drawer for a control that when clicked opens up a popup menu containing
	/// a list of items from which the user can select one item. Value is the zero-based
	/// index of the selected item.
	/// One can react to the selected item changing by subscribing to the OnValueChanged event.
	/// </summary>
	[Serializable]
	public sealed class PopupMenuDrawer : PopupMenuSelectableDrawer<int>
	{
		private string[] items = new string[0];

		/// <inheritdoc />
		public override Type Type
		{
			get
			{
				return Types.Int;
			}
		}
		
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="selectedIndex"> The selected index in items. </param>
		/// <param name="items"> The item options to display in the popup menu when the field is clicked. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static PopupMenuDrawer Create(int selectedIndex, string[] items, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			PopupMenuDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new PopupMenuDrawer();
			}
			result.Setup(selectedIndex, items, memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <summary>
		/// Prevents a default instance of the drawer from being created.
		/// The Create method should be used instead of this constructor.
		/// </summary>
		private PopupMenuDrawer() { }

		/// <inheritdoc/>
		protected sealed override void Setup(int setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method of PopupMenuDrawer.");
		}

		private void Setup(int selectedIndex, string[] setItems, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			items = setItems;
			base.Setup(selectedIndex, typeof(int), setMemberInfo, setParent, setLabel, setReadOnly);
		}

		private void SetSelectedItem(int index)
		{
			if(index >= 0 && index < items.Length)
			{
				Value = index;
			}
			else
			{
				Value = -1;
			}
		}
				
		/// <inheritdoc />
		public override object DefaultValue(bool _)
		{
			return -1;
		}

		/// <inheritdoc />
		protected override Type GetTypeContext()
		{
			return null;
		}

		/// <inheritdoc />
		protected override void GenerateMenuItems(ref List<PopupMenuItem> rootItems, ref Dictionary<string, PopupMenuItem> groupsByLabel, ref Dictionary<string, PopupMenuItem> itemsByLabel)
		{
			for(int n = items.Length - 1; n >= 0; n--)
			{
				var item = items[n];
				PopupMenuUtility.BuildPopupMenuItemWithLabel(ref rootItems, ref groupsByLabel, ref itemsByLabel, n, null, item, "", null);
			}
		}

		/// <inheritdoc />
		protected override GUIContent MenuLabel()
		{
			return GUIContentPool.Create(label);
		}
		
		/// <inheritdoc />
		protected override string GetPopupItemLabel(int value)
		{
			return value >= 0 && value < items.Length ? items[value] : "";
		}

		/// <inheritdoc />
		protected override string GetLabelText(int value)
		{
			return value >= 0 && value < items.Length ? items[value] : "";
		}

		/// <inheritdoc />
		public override bool CanPasteFromClipboard()
		{
			return Clipboard.CanPasteAs(Types.String);
		}

		/// <inheritdoc/>
		protected override void DoPasteFromClipboard()
		{
			var valueString = Clipboard.Paste(Types.String) as string;
			if(valueString != null)
			{
				SetValue(Array.IndexOf(items, valueString));
			}
			else
			{
				SetValue(-1);
			}
		}

		/// <inheritdoc />
		protected override void DoRandomize()
		{
			SetSelectedItem(GetRandomValue());
		}

		/// <inheritdoc />
		protected override int GetRandomValue()
		{
			int count = items.Length;
			return count == 0 ? -1 : Random.Range(0, items.Length);
		}
	}
}