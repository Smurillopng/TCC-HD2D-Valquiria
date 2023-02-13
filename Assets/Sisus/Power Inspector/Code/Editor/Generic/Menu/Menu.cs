//#define DEBUG_OPEN
//#define DEBUG_ADD
//#define DEBUG_ADD_SEPARATOR

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using OnActivateItem = UnityEditor.GenericMenu.MenuFunction;
using OnActivateItemWithParameter = UnityEditor.GenericMenu.MenuFunction2;
using System.Linq;

namespace Sisus
{
	/// <summary>
	/// A class representing a context menu.
	/// 
	/// Offers a lot of flexibility in detecting, removing, rearranging and modifying items that have been added to the menu.
	/// 
	/// In the editor can be converted into a buit-in GenericMenu using the method "ToGenericMenu".
	/// </summary>
	[Serializable]
	public class Menu
	{
		private const int DefaultCapacity = 20;

		private static Menu ReusableInstance;
		private static readonly Pool<ItemInfo> ItemPool = new Pool<ItemInfo>(30);

		private static readonly Stack<ItemInfo> ReusableItemsStack = new Stack<ItemInfo>();		

		private static bool genericMenuFieldsGenerated;
		private static FieldInfo genericMenuItemsField;
		private static FieldInfo genericMenuContentField;
		private static FieldInfo genericMenuSeparatorField;
		private static FieldInfo genericMenuOnField;
		private static FieldInfo genericMenuFuncField;
		private static FieldInfo genericMenuFunc2Field;
		private static FieldInfo genericMenuUserDataField;

		private List<ItemInfo> items;
		
		public int Count
		{
			get
			{
				return items.Count;
			}
		}

		[System.Runtime.CompilerServices.IndexerName("MenuItem")]
		public ItemInfo this[int index]
		{
			get
			{
				return items[index];
			}

			set
			{
				items[index] = value;
			}
		}

		/// <summary>
		/// Generates FieldInfos for various fields related to GenericMenu.MenuItem,
		/// which can't be directly accessed due to it being a private class.
		/// </summary>
		private static void SetupGenericMenuFields()
		{
			if(genericMenuFieldsGenerated)
			{
				return;
			}
			genericMenuFieldsGenerated = true;

			genericMenuItemsField = typeof(GenericMenu).GetField("menuItems");

			var menuItemType = Types.GetInternalEditorType("UnityEditor.GenericMenu.MenuItem");
			genericMenuContentField = menuItemType.GetField("content", BindingFlags.Instance);
			genericMenuSeparatorField = menuItemType.GetField("separator", BindingFlags.Instance);
			genericMenuOnField = menuItemType.GetField("on", BindingFlags.Instance);
			genericMenuFuncField = menuItemType.GetField("func", BindingFlags.Instance);
			genericMenuFunc2Field = menuItemType.GetField("func2", BindingFlags.Instance);
			genericMenuUserDataField = menuItemType.GetField("userData", BindingFlags.Instance);
		}

		public static ItemInfo Item(string label, OnActivateItem effect)
		{
			ItemInfo item;
			if(!ItemPool.TryGet(out item))
			{
				item = new ItemInfo();
			}
			item.Setup(label, effect);
			return item;
		}

		public static ItemInfo DisabledItem(string label)
		{
			ItemInfo item;
			if(!ItemPool.TryGet(out item))
			{
				item = new ItemInfo();
			}
			item.Setup(label);
			return item;
		}

		public static ItemInfo DisabledItem(string label, string tooltip)
		{
			ItemInfo item;
			if(!ItemPool.TryGet(out item))
			{
				item = new ItemInfo();
			}
			item.Setup(label, tooltip);
			return item;
		}

		public static ItemInfo DisabledItem(string label, bool on)
		{
			ItemInfo item;
			if(!ItemPool.TryGet(out item))
			{
				item = new ItemInfo();
			}
			item.Setup(label, on);
			return item;
		}

		public static ItemInfo Item(string label, OnActivateItem effect, bool on)
		{
			ItemInfo item;
			if(!ItemPool.TryGet(out item))
			{
				item = new ItemInfo();
			}
			item.Setup(label, effect, on);
			return item;
		}

		public static ItemInfo Item(string label, string tooltip, OnActivateItem effect, bool on)
		{
			ItemInfo item;
			if(!ItemPool.TryGet(out item))
			{
				item = new ItemInfo();
			}
			item.Setup(label, tooltip, effect, on);
			return item;
		}

		public static ItemInfo Item(string label, string tooltip, OnActivateItem effect)
		{
			ItemInfo item;
			if(!ItemPool.TryGet(out item))
			{
				item = new ItemInfo();
			}
			item.Setup(label, tooltip, effect);
			return item;
		}

		public static ItemInfo Item(string label, OnActivateItemWithParameter effect, object onActiveItemParameterValue)
		{
			ItemInfo item;
			if(!ItemPool.TryGet(out item))
			{
				item = new ItemInfo();
			}
			item.Setup(label, effect, onActiveItemParameterValue);
			return item;
		}

		public static ItemInfo Item(string label, string tooltip, OnActivateItemWithParameter effect, object onActiveItemParameterValue)
		{
			ItemInfo item;
			if(!ItemPool.TryGet(out item))
			{
				item = new ItemInfo();
			}
			item.Setup(label, tooltip, effect, onActiveItemParameterValue);
			return item;
		}

		public static ItemInfo Item(string label, string tooltip, OnActivateItemWithParameter effect, object onActiveItemParameterValue, bool on)
		{
			ItemInfo item;
			if(!ItemPool.TryGet(out item))
			{
				item = new ItemInfo();
			}
			item.Setup(label, tooltip, effect, onActiveItemParameterValue, on);
			return item;
		}

		public static ItemInfo SeparatorLine()
		{
			ItemInfo item;
			if(!ItemPool.TryGet(out item))
			{
				item = new ItemInfo();
			}
			item.SetupAsSeparator();
			return item;
		}

		public static ItemInfo SeparatorLine(string submenuPath)
		{
			ItemInfo item;
			if(!ItemPool.TryGet(out item))
			{
				item = new ItemInfo();
			}
			item.SetupAsSeparator(submenuPath);
			return item;
		}

		public static Menu Create()
		{
			if(ReusableInstance != null)
			{
				var result = ReusableInstance;
				ReusableInstance = null;
				return result;
			}
			return new Menu();
		}
		
		private Menu()
		{
			items = new List<ItemInfo>(DefaultCapacity);
		}
		
		public void Add(ItemInfo menuItem)
		{
			if(!menuItem.IsSeparator)
			{
				for(int n = items.Count - 1; n >= 0; n--)
				{
					if(string.Equals(items[n].Text, menuItem.Text))
					{
						Debug.LogWarning("Same item \""+menuItem.Text + "\" was added to Menu multiple times. Overriding previous entry.");

						items[n] = menuItem;
						return;
					}
				}
			}

			#if DEV_MODE && DEBUG_ADD
			Debug.Log("Menu.Add(" + menuItem + ")");
			#elif DEV_MODE && DEBUG_ADD_SEPARATOR
			Debug.Log("Menu.Add(" + menuItem + ")");
			#endif

			items.Add(menuItem);
		}

		public void Add(string label, OnActivateItem effect)
		{
			Add(Item(label, effect));
		}

		public void Add(string label, OnActivateItem effect, bool on)
		{
			Add(Item(label, effect, on));
		}

		public void Add(string label, string tooltip, OnActivateItem effect)
		{
			Add(Item(label, tooltip, effect));
		}

		public void Add(string label, string tooltip, OnActivateItem effect, bool on)
		{
			Add(Item(label, tooltip, effect, on));
		}
		
		public void Add(string label, OnActivateItemWithParameter effect, object onActiveItemParameterValue)
		{
			Add(Item(label, "", effect, onActiveItemParameterValue));
		}

		public void Add(string label, OnActivateItemWithParameter effect, object onActiveItemParameterValue, bool on)
		{
			Add(Item(label, "", effect, onActiveItemParameterValue, on));
		}

		public void Add(string label, string tooltip, OnActivateItemWithParameter effect, object onActiveItemParameterValue, bool on)
		{
			Add(Item(label, tooltip, effect, onActiveItemParameterValue, on));
		}

		public void AddEvenIfDuplicate(string label, OnActivateItem effect)
		{
			Add(Item(MakeUniqueLabel(label), effect));
		}

		public void AddEvenIfDuplicate(string label, string tooltip, OnActivateItem effect, bool on)
		{
			Add(Item(MakeUniqueLabel(label), tooltip, effect, on));
		}

		public void AddDisabled(string label)
		{
			Add(DisabledItem(label));
		}

		public void AddDisabled(string label, string tooltip)
		{
			Add(DisabledItem(label, tooltip));
		}

		public void AddDisabled(string label, bool on)
		{
			Add(DisabledItem(label, on));
		}

		/// <summary>
		/// If menu already contains the label, adds a suffix after the label
		/// that makes it unique.
		/// </summary>
		/// <param name="label"> The desired label that should be made to be unique. </param>
		/// <returns> Unique label. </returns>
		private string MakeUniqueLabel(string label)
		{
			int suffix = 0;
			string labelWithSuffix = label;
			while(Contains(labelWithSuffix))
			{
				suffix++;
				labelWithSuffix = label;
				for(int n = 0; n < suffix; n++)
				{
					labelWithSuffix += " ";
				}

			}
			return labelWithSuffix;
		}
		
		public bool NotEmptyAndLastItemIsNotSeparator()
		{
			int count = Count;
			return count > 0 && !(items[count - 1].IsSeparator);
		}

		public bool NotFirstSubMenuIndexAndSeparatorDoesntExistAtIndexAlready(int index)
		{
			int count = Count;
			if(count == 0)
			{
				return false;
			}

			if(index == 0)
			{
				return false;
			}

			int i;
			string nextItemSubMenu = "";
			if(index < count)
			{
				var nextItem = items[index];
				if(nextItem.IsSeparator)
				{
					return false;
				}
				i = nextItem.Text.LastIndexOf('/');
				if(i != -1)
				{
					nextItemSubMenu = nextItem.Text.Substring(0, i + 1);
				}
			}

			var prevItem = items[index - 1];
			if(prevItem.IsSeparator)
			{
				return false;
			}

			i = prevItem.Text.LastIndexOf('/');
			if(i != -1)
			{
				string prevItemSubMenu = prevItem.Text.Substring(0, i + 1);
				if(!string.Equals(prevItemSubMenu, nextItemSubMenu))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// If menu contains no items or the last item in the menu is already a separator, do nothing,
		/// otherwise add a separator as the last item in the menu.
		/// </summary>
		public void AddSeparatorIfNotRedundant()
		{
			if(NotEmptyAndLastItemIsNotSeparator())
			{
				AddSeparator();
			}
		}

		/// <summary>
		/// Add a separator as the last item in the menu.
		/// 
		/// NOTE: This adds a separator even if the menu contains no prior items,
		/// or if the last item already is a separator. If you don't want this behaviour,
		/// use the AddSeparatorIfNotRedundant method instead.
		/// </summary>
		public void AddSeparator()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(!NotEmptyAndLastItemIsNotSeparator())
			{
				Debug.LogWarning("AddSeparator called for Menu which was empty or already had separator as last item : "+ToString());
			}
			#endif

			Add(SeparatorLine());
		}

		public void AddSeparator(string subMenuPath)
		{
			Add(SeparatorLine(subMenuPath));
		}

		public void RemoveAt(int index)
		{
			items.RemoveAt(index);
		}

		public bool Remove(string label)
		{
			int index = IndexOf(label);
			if(index != -1)
			{
				items.RemoveAt(index);
				return true;
			}
			return false;
		}

		public void MoveItemToBottom(string itemLabel)
		{
			for(int n = Count - 1; n >= 0; n--)
			{
				var item = items[n];
				if(string.Equals(item.Text, itemLabel))
				{
					RemoveAt(n);
					Add(item);
					return;
				}
			}
		}
		
		public void MoveCategoryToBottom(string categoryPath)
		{
			if(categoryPath[categoryPath.Length - 1] != '/')
			{
				categoryPath += '/';
			}
			
			#if DEV_MODE && PI_ASSERTATIONS
			int countWas = Count;
			Debug.Assert(ReusableItemsStack.Count == 0);
			#endif

			int removedCount = 0;
			for(int n = Count - 1; n >= 0; n--)
			{
				var item = items[n];
				if(item.Text.StartsWith(categoryPath, StringComparison.Ordinal))
				{
					RemoveAt(n);
					ReusableItemsStack.Push(item);
					removedCount++;
				}
			}

			for(int n = removedCount - 1; n >= 0; n--)
			{
				Add(ReusableItemsStack.Pop());
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(ReusableItemsStack.Count == 0);
			Debug.Assert(Count == countWas);
			#endif
		}

		public bool ContainsCategory(string categoryName)
		{
			if(!categoryName.EndsWith("/", StringComparison.Ordinal))
			{
				categoryName += "/";
			}

			for(int n = Count - 1; n >= 0; n--)
			{
				if(items[n].Text.StartsWith(categoryName, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		public bool Contains(string label)
		{
			return IndexOf(label) != -1;
		}

		public int IndexOf(string label)
		{
			for(int n = Count - 1; n >= 0; n--)
			{
				if(string.Equals(items[n].Text, label))
				{
					return n;
				}
			}
			return -1;
		}

		public void Insert(int index, string label, OnActivateItem effect)
		{
			Insert(index, Item(label, effect));
		}

		public void Insert(int index, string label, OnActivateItem effect, bool on)
		{
			Insert(index, Item(label, effect, on));
		}

		public void Insert(int index, ItemInfo menuItem)
		{
			items.Insert(index, menuItem);
		}

		public void InsertSeparator(int index)
		{
			#if DEV_MODE && DEBUG_ADD_SEPARATOR
			Debug.Log("InsertSeparator(" + index + ")");
			#endif

			if(Count > index)
			{
				var itemAt = items[index];
				int menuPathEnd = itemAt.Text.LastIndexOf('/');
				if(menuPathEnd != -1)
				{
					Insert(index, SeparatorLine(itemAt.Text.Substring(0, menuPathEnd + 1)));
					return;
				}
			}

			Insert(index, SeparatorLine());
		}

		public void InsertSeparator(int index, string subMenuPath)
		{
			Insert(index, SeparatorLine(subMenuPath));
		}

		public void InsertSeparatorIfNotRedundant(int index)
		{
			if(NotFirstSubMenuIndexAndSeparatorDoesntExistAtIndexAlready(index))
			{
				InsertSeparator(index);
			}
		}

		public void AddRange(params ItemInfo[] menuItems)
		{
			items.AddRange(menuItems);
		}

		public void AddRange(GenericMenu itemsFromMenu)
		{
			int count = itemsFromMenu.GetItemCount();
			if(count == 0)
			{
				return;
			}

			
			SetupGenericMenuFields();
			var menuItems = genericMenuItemsField.GetValue(itemsFromMenu) as ArrayList;

			for(int n = 0; n < count; n++)
			{
				//GenericMenu.MenuItem is a private class, so can't cast it directly
				object menuItem = menuItems[n];
				var separator = (bool)genericMenuSeparatorField.GetValue(menuItem);
				
				if(separator)
				{
					AddSeparatorIfNotRedundant();
				}
				else
				{
					var label = genericMenuContentField.GetValue(menuItem) as GUIContent;
					var on = (bool)genericMenuOnField.GetValue(menuItem);
					var func = genericMenuFuncField.GetValue(menuItem) as OnActivateItem;
					if(func != null)
					{
						Add(label.text, label.tooltip, func, on);
					}
					else
					{
						var func2 = genericMenuFunc2Field.GetValue(menuItem) as OnActivateItemWithParameter;
						var userData = genericMenuUserDataField.GetValue(menuItem);
						Add(label.text, label.tooltip, func2, userData, on);
					}
				}
			}
		}

		public GenericMenu ToGenericMenu()
		{
			var menu = new GenericMenu();
			AddToGenericMenu(ref menu);
			return menu;
		}

		public void AddToGenericMenu([NotNull]ref GenericMenu menu)
		{
			int count = Count;
			if(count == 0)
            {
				return;
            }

			if(menu.GetItemCount() > 0)
            {
				var itemsToRemove = items.Select((item) => item.Text);

                for(int i = count - 1; i >= 0; i--)
                {
					// if "Copy" item is found in the menu always also remove the "Paste" item.
					// Otherwise internal "Paste" item can be shown as first item in the context menu and it won't even work.
					if(string.Equals(items[i].Text, "Copy"))
                    {
						#if UNITY_2021_1_OR_NEWER
						itemsToRemove = itemsToRemove.Append("Paste");
						#else
						itemsToRemove = itemsToRemove.Concat(new string[] { "Paste" });
						#endif
						break;
                    }
                }

				// Remove duplicates like "Copy" and "Paste".
				ContextMenuUtility.RemoveItems(menu, itemsToRemove);

				if(menu.GetItemCount() > 0)
				{
					menu.AddSeparator("");
				}
			}

			// todo: use Dictionary<string,bool> to track if last item was separator separately for each subMenuPath
			bool lastItemWasSeparator = true;

			for(int n = 0; n < count; n++)
			{
				var item = items[n];
				if(item.IsSeparator)
				{
					if(lastItemWasSeparator)
					{
						#if DEV_MODE
						Debug.LogWarning("Skipping duplicate separator");
						#endif
						continue;
					}

					menu.AddSeparator(item.Text);

					lastItemWasSeparator = true;
				}
				else
				{
					lastItemWasSeparator = false;

					var effect = item.effect;
					if(effect != null)
					{
						menu.AddItem(item.Label, item.on, effect);
					}
					else
					{
						var effectWithParameter = item.effectWithParameter;
						if(effectWithParameter != null)
						{
							menu.AddItem(item.Label, item.on, effectWithParameter, item.effectParameterValue);
						}
						else
						{
							#if UNITY_2018_3_OR_NEWER
							menu.AddDisabledItem(item.Label, item.on);
							#else
							menu.AddDisabledItem(item.Label);
							#endif
						}
					}
				}
			}
		}

		public void Open()
		{
			#if DEV_MODE && DEBUG_OPEN
			Debug.Log("Opening menu with "+Count+" items: " +StringUtils.ToString(items));
			#endif

			var e = Event.current;
			if(e != null && (e.type == EventType.MouseDown || e.type == EventType.ContextClick || e.type == EventType.KeyDown))
			{
				DrawGUI.Use(e);
			}
			#if DEV_MODE
			//else { Debug.LogWarning("Won't use event of type "+(e == null ? "null" : e.type.ToString())); }
			#endif
			
			ToGenericMenu().ShowAsContext();
		}
		
		public void OpenAt(Rect position)
		{
			#if DEV_MODE && DEBUG_OPEN
			Debug.Log("Opening menu with "+Count+" items @ "+position);
			Debug.Assert(Count > 0);
			#endif

			var e = Event.current;
			if(e != null && (e.type == EventType.MouseDown || e.type == EventType.ContextClick || e.type == EventType.KeyDown))
			{
				DrawGUI.Use(e);
			}
			#if DEV_MODE
			//else { Debug.LogWarning("Won't use event of type "+(e == null ? "null" : e.type.ToString())); }
			#endif

			ToGenericMenu().DropDown(position);
		}

		public void Dispose()
		{
			for(int n = Count - 1; n >= 0; n--)
			{
				var item = items[n];
				item.Dispose();
				ItemPool.Dispose(ref item);
			}
			items.Clear();

			if(ReusableInstance == null)
			{
				ReusableInstance = this;
			}
			#if DEV_MODE
			else { Debug.LogWarning("Menu was disposed but ReusableInstance was not null. Maybe should cache more than one instance?"); }
			#endif
		}

		public override string ToString()
		{
			return StringUtils.Concat("Menu", StringUtils.ToString(items));
		}

		[Serializable]
		public sealed class ItemInfo
		{
			public bool on;
			public OnActivateItem effect;
			public OnActivateItemWithParameter effectWithParameter;
			public object effectParameterValue;
			public bool isSeparator;

			private GUIContent label;
			private object methodOwner;

			public string Text
			{
				get
				{
					return label.text;
				}
			}

			public bool IsSeparator
			{
				get
				{
					return isSeparator;
				}
			}

			public GUIContent Label
			{
				get
				{
					return label;
				}
			}

			public OnActivateItem Effect
			{
				get
				{
					return effect;
				}
			}

			public OnActivateItemWithParameter EffectWithParameter
			{
				get
				{
					return effectWithParameter;
				}
			}

			public ItemInfo() { }

			public void Setup(string setLabel, string methodName, object setMethodOwner)
			{
				label = GUIContentPool.Create(setLabel, methodName);
				methodOwner = setMethodOwner;
				GenerateActionsUsingRefection(methodName);
			}

			public void Setup(string setLabel, string setTooltip, OnActivateItem setEffect)
			{
				label = GUIContentPool.Create(setLabel, setTooltip);
				effect = setEffect;
			}

			public void Setup(string setLabel, OnActivateItem setEffect)
			{
				label = GUIContentPool.Create(setLabel);
				effect = setEffect;
			}

			public void Setup(string setLabel, OnActivateItem setEffect, bool setOn)
			{
				label = GUIContentPool.Create(setLabel);
				
				effect = setEffect;
				on = setOn;
			}

			public void Setup(string setLabel, string setTooltip, OnActivateItem setEffect, bool setOn)
			{
				label = GUIContentPool.Create(setLabel, setTooltip);
				effect = setEffect;
				on = setOn;
			}

			public void Setup(string setLabel, OnActivateItemWithParameter setEffect, object setEffectParameterValue)
			{
				label = GUIContentPool.Create(setLabel);
				effectWithParameter = setEffect;
				effectParameterValue = setEffectParameterValue;
			}

			public void Setup(string setLabel, string setTooltip, OnActivateItemWithParameter setEffect, object setEffectParameterValue)
			{
				label = GUIContentPool.Create(setLabel, setTooltip);
				effectWithParameter = setEffect;
				effectParameterValue = setEffectParameterValue;
			}

			public void Setup(string setLabel, string setTooltip, OnActivateItemWithParameter setEffect, object setEffectParameterValue, bool setOn)
			{
				label = GUIContentPool.Create(setLabel, setTooltip);
				effectWithParameter = setEffect;
				effectParameterValue = setEffectParameterValue;
				on = setOn;
			}

			/// <summary> Setups ItemInfo for an inactive (greyed out) menu item. </summary>
			/// <param name="setLabel"> The label for the menu item. </param>
			public void Setup(string setLabel)
			{
				label = GUIContentPool.Create(setLabel);
			}

			/// <summary> Setups ItemInfo for an inactive (greyed out) menu item. </summary>
			/// <param name="setLabel"> The label for the menu item. </param>
			/// <param name="setTooltip"> The tooltip for the menu item. </param>
			public void Setup(string setLabel, string setTooltip)
			{
				label = GUIContentPool.Create(setLabel, setTooltip);
			}

			/// <summary> Creates ItemInfo for an inactive (greyed out) menu item. </summary>
			/// <param name="setLabel"> The label for the menu item. </param>
			/// <param name="setOn"> If true the enabled checkmark is shown after the menu item? </param>
			public void Setup(string setLabel, bool setOn)
			{
				label = GUIContentPool.Create(setLabel);
				on = setOn;
			}

			/// <summary> Setups ItemInfo for a separator. </summary>
			public void SetupAsSeparator()
			{
				label = GUIContent.none;
				isSeparator = true;
			}

			/// <summary> Setups ItemInfo for a separator at the given sub menu path. </summary>
			/// <param name="setLabel"> The label for the menu item. </param>
			public void SetupAsSeparator(string subMenuPath)
			{
				// Important: if the subMenuPath doesn't end with a forward slash then it won't work properly.
				if(subMenuPath.Length > 0 && !subMenuPath.EndsWith("/"))
				{
					#if DEV_MODE
					Debug.LogWarning("SetupAsSeparator called with argument \""+subMenuPath+"\" which didn't end with a '/'.");
					#endif

					subMenuPath += "/";
				}

				label = GUIContentPool.Create(subMenuPath);
				isSeparator = true;
			}

			private void GenerateActionsUsingRefection(string methodName, FieldInfo field = null)
			{
				if(methodOwner != null)
				{
					var method = methodOwner.GetType().GetMethod(methodName);
					ParameterInfo[] parameters = method.GetParameters();
					if(parameters.Length == 0)
					{
						effect = ()=>method.Invoke(methodOwner, null);
					}
					else
					{
						if(field != null)
						{
							effect = ()=>method.InvokeWithParameter(methodOwner, field.GetValue(methodOwner));
						}
						else
						{
							effect = ()=>method.InvokeWithParameter(methodOwner, parameters[0].ParameterType.DefaultValue());
						}
					}
				}
			}

			public void Invoke()
			{
				if(effectWithParameter != null)
				{
					effectWithParameter(effectParameterValue);
				}
				else
				{
					effect();
				}
			}

			public void Dispose()
			{
				if(label == GUIContent.none)
				{
					label = null;
				}
				else
				{
					GUIContentPool.Dispose(ref label);
				}

				methodOwner = null;
				on = false;
				effect = null;
				effectWithParameter = null;
				effectParameterValue = null;
				isSeparator = false;
			}

			public override string ToString()
			{
				if(isSeparator)
				{
					return Text.Length > 0 ? "Separator(" + StringUtils.ToString(label) + ")" : "Separator";
				}
				return StringUtils.ToString(label);
			}
		}
	}
}