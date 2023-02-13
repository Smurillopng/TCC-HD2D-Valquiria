//#define TICK_CLEAR_ALL_ITEM_IN_MENU
//#define TICK_DEFAULT_VALUE_WHEN_HAS_OTHER_FLAGS

using System;
using System.Collections.Generic;
using System.Linq;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary> Drawer for enumeration fields. </summary>
	[Serializable, DrawerForField(typeof(Enum), true, true)]
	public class EnumDrawer : PopupMenuSelectableDrawer<Enum>
	{
		private const string SelectNoneMenuItemLabel = "Clear All";

		private bool hasFlagsAttribute;

		/// <inheritdoc />
		protected override bool CanTickMultipleItems
		{
			get
			{
				return hasFlagsAttribute;
			}
		}

		private Type UnderlyingType
		{
			get
			{
				try
				{
					return Enum.GetUnderlyingType(Type);
				}
				catch(ArgumentException e)
				{
					Debug.LogError(ToString()+" Enum.GetUnderlyingType("+StringUtils.ToString(Type)+ ") "+e);
					return Types.Int;
				}
			}
		}

		/// <inheritdoc/>
		public override string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetDrawerInfoUrl("enum-drawer");
			}
		}

		private bool AddClearAllMenuItem()
		{
			if(!hasFlagsAttribute)
			{
				return false;
			}

			var allFlags = Enum.GetValues(Type);
			int count = allFlags.Length;
			for(int n = 0; n < count; n++)
			{
				var flagValue = Convert.ToUInt64(allFlags.GetValue(n));
				if(flagValue == 0)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the class member that the created drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static EnumDrawer Create(Enum value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			EnumDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new EnumDrawer();
			}
			result.Setup(value, DrawerUtility.GetType(memberInfo, value), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((Enum)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void Setup(Enum setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(DrawerUtility.GetType(setMemberInfo, setValue) != Types.Enum, "Fields of exact type Enum should use AnyEnumDrawer, not EnumDrawer!");
			#endif

			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
			hasFlagsAttribute = Type.IsDefined(Types.FlagsAttribute, false);
		}
		
		/// <inheritdoc />
		protected override void GenerateMenuItems(ref List<PopupMenuItem> rootItems, ref Dictionary<string, PopupMenuItem> groupsByLabel, ref Dictionary<string, PopupMenuItem> itemsByLabel)
		{
			PopupMenuUtility.BuildPopupMenuItemsForEnumType(ref rootItems, ref groupsByLabel, ref itemsByLabel, typeContext);

			if(AddClearAllMenuItem())
			{
				// only add if menu doesn't already contain entry by name
				if(!itemsByLabel.ContainsKey(SelectNoneMenuItemLabel))
				{
					var item = PopupMenuItem.Item(null as Type, SelectNoneMenuItemLabel, "0", null);
					rootItems.Insert(0, item);
					itemsByLabel.Add(SelectNoneMenuItemLabel, item);
				}
			}
		}

		/// <inheritdoc />
		protected override string GetTooltip(Enum value)
		{
			return StringUtils.ToString(Convert.ChangeType(value, UnderlyingType));
		}

		/// <inheritdoc />
		protected override void GetTickedMenuItems(List<PopupMenuItem> rootItems, List<PopupMenuItem> results)
		{
			#if DEV_MODE
			Debug.Assert(results.Count == 0);
			#endif

			if(hasFlagsAttribute)
			{
				var value = Value;
				var defaultValue = (Enum)DefaultValue();

				if(value.Equals(defaultValue))
				{
					if(!AddClearAllMenuItem())
					{
						FindMenuItemsForFlag(rootItems, Value, results, defaultValue);
					}
					#if TICK_CLEAR_ALL_ITEM_IN_MENU
					else
					{
						const int SelectNoneMenuItemIndex = 0;
						results.Add(rootItems[SelectNoneMenuItemIndex]);
					}
					#endif
					return;
				}

				var allFlags = Value.GetFlags();
				for(int n = allFlags.Count - 1; n >= 0; n--)
				{
					int count = results.Count;

					var flag = allFlags[n];

					if(flag.Equals(defaultValue))
					{
						continue;
					}

					FindMenuItemsForFlag(rootItems, allFlags[n], results, defaultValue);
				}
			}
			else
			{
				var find = PopupMenuUtility.FindMenuItemByIdentifyingObject(rootItems, Value);
				if(find != null)
				{
					results.Add(find);
				}
			}
		}
		 
		private static void FindMenuItemsForFlag(List<PopupMenuItem> searchItems, Enum targetFlag, List<PopupMenuItem> results, Enum noneValue)
		{
			for(int n = searchItems.Count - 1; n >= 0; n--)
			{
				var item = searchItems[n];

				if(item.IsGroup)
				{
					FindMenuItemsForFlag(item.children, targetFlag, results, noneValue);
				}
				else
				{
					var itemValue = item.IdentifyingObject as Enum;

					if(itemValue != null && targetFlag.HasFlag(itemValue))
					{
						#if !TICK_DEFAULT_VALUE_WHEN_HAS_OTHER_FLAGS
						if(!itemValue.Equals(noneValue) || targetFlag.Equals(noneValue))
						#endif
						{
							results.Add(item);
						}						
					}
				}
			}
		}
		
		/// <inheritdoc />
		protected override void OnPopupMenuItemClicked(PopupMenuItem item)
		{
			if(string.Equals(item.label, SelectNoneMenuItemLabel))
			{
				Value = Value.ClearFlags();
				return;
			}
			
			var setValue = item.IdentifyingObject as Enum;

			if(setValue.Equals(DefaultValue()))
			{
				Value = setValue;
				return;
			}

			if(!hasFlagsAttribute)
			{
				Value = setValue;
				return;
			}
			
			var valueWas = Value;
			if(valueWas.HasFlag(setValue))
			{
				Value = valueWas.RemoveFlag(setValue);
			}
			else
			{
				Value = valueWas.SetFlag(setValue);
			}
		}

		/// <inheritdoc />
		protected override Enum GetRandomValue()
		{
			var enumValues = Enum.GetValues(Type);
			return (Enum)enumValues.GetValue(UnityEngine.Random.Range(0, enumValues.Length));
		}
		
		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(hasFlagsAttribute && !ReadOnly)
			{
				menu.AddSeparatorIfNotRedundant();

				menu.Add("Clear All Flags", ()=>Value = Value.ClearFlags());
				menu.Add("Set All Flags", () => Value = Value.SetAllFlags());
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(memberInfo == null || !memberInfo.MixedContent)
			{
				int index = menu.IndexOf("Copy");
				if(index != -1)
				{
					menu.Insert(index + 1, Menu.Item("Copy Underlying Value", CopyUnderlyingValueToClipboard));
				}
			}
		}

		/// <inheritdoc />
		protected override GUIContent MenuLabel()
		{
			return GUIContentPool.Create(Type.Name, Type.FullName);
		}
		
		/// <inheritdoc />
		public override object DefaultValue(bool _ = false)
		{
			var enumType = Type;
			var underlyingType = UnderlyingType;
			var defaultValue = underlyingType.DefaultValue();
			try
			{
				return Enum.ToObject(enumType, defaultValue);
			}
			catch(ArgumentException e)
			{
				Debug.LogError(ToString()+" Enum.ToObject(type="+StringUtils.ToString(enumType)+ ", value="+StringUtils.ToString(defaultValue)+") with underlyingType="+StringUtils.ToString(underlyingType)+", defaultValue.GetType()="+StringUtils.TypeToString(defaultValue)+", memberInfo="+StringUtils.ToString(memberInfo)+"\n"+e);
				return 0;
			}
		}

		/// <inheritdoc />
		protected override string GetLabelText(Enum value)
		{
			var label = value.ToString();

			#if UNITY_2019_2_OR_NEWER
			var memberInfo = Type.GetMember(label).FirstOrDefault();
			InspectorNameAttribute inspectorName;
			if(memberInfo != null && Attribute<InspectorNameAttribute>.TryGet(memberInfo, false, out inspectorName))
            {
				return inspectorName.displayName;
			}
			#endif
			
			if(string.Equals(label, "0", StringComparison.OrdinalIgnoreCase))
			{
				// empty string? "0"? None?
				return "";
			}

			return label;
		}
		
		private void CopyUnderlyingValueToClipboard()
		{
			object underlyingValue = GetUnderlyingValue();
			Clipboard.Copy(underlyingValue);

			Clipboard.SendCopyToClipboardMessage("Copied{0} underlying value", GetFieldNameForMessages());
		}
		
		private object GetUnderlyingValue()
		{
			return Convert.ChangeType(Value, UnderlyingType);
		}
	}
}