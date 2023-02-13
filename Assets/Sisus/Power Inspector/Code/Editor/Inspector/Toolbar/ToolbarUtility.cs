using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sisus.Attributes;

namespace Sisus
{
	/// <summary>
	/// Class that handles creating, caching and returning default drawer providers for inspectors.
	/// </summary>
	public static class ToolbarUtility
	{
		private static Dictionary<Type, Type[]> toolbarItemsByToolbarType;

		/// <summary> Gets all toolbar items for toolbar and sets them up. </summary>
		/// <param name="inspector"> The inspector that contains the toolbar. This cannot be null. </param>
		/// <param name="toolbar"> The toolbar that contains the tiems. This cannot be null. </param>
		/// <returns> An array of toolbar items. This will never be null. </returns>
		[NotNull]
		public static IInspectorToolbarItem[] GetItemsForToolbar([NotNull]IInspector inspector, [NotNull]IInspectorToolbar toolbar)
		{
			if(toolbarItemsByToolbarType == null)
			{
				BuildDictionaries();
			}
			
			Type[] itemTypes;
			if(!toolbarItemsByToolbarType.TryGetValue(toolbar.GetType(), out itemTypes))
			{
				return ArrayPool<IInspectorToolbarItem>.ZeroSizeArray;
			}

			int count = itemTypes.Length;
			IInspectorToolbarItem[] items = new IInspectorToolbarItem[count];
			for(int n = count - 1; n >= 0; n--)
			{
				var itemType = itemTypes[n];
				var item = (IInspectorToolbarItem)itemType.CreateInstance();
				var alignment = ((ToolbarItemForAttribute)itemType.GetCustomAttributes(typeof(ToolbarItemForAttribute), false)[0]).alignment;
				item.Setup(inspector, toolbar, alignment);
				items[n] = item;
			}
			
			return items;
		}

		private static void BuildDictionaries()
		{
			#if DEV_MODE
			var timer = new ExecutionTimeLogger();
			timer.Start("ToolbarUtility.BuildDictionaries");
			#endif

			toolbarItemsByToolbarType = new Dictionary<Type, Type[]>(2);

			var itemsForToolbars = new Dictionary<Type, List<ToolbarItemInfo>>(6);

			foreach(var type in TypeExtensions.GetTypesWithAttributeNotThreadSafe<ToolbarItemForAttribute>())
			{
				foreach(var attribute in type.GetCustomAttributes(typeof(ToolbarItemForAttribute), false))
				{
					var toolbarItemFor = attribute as ToolbarItemForAttribute;
					if(toolbarItemFor == null)
					{
						continue;
					}

					var toolbarType = toolbarItemFor.inspectorToolbarType;
					if(toolbarType == null)
					{
						UnityEngine.Debug.LogError(toolbarItemFor.GetType().Name + " on class "+type.Name+" NullReferenceException - toolbarType was null!");
						continue;
					}

					List<ToolbarItemInfo> itemsForToolbar;
					if(!itemsForToolbars.TryGetValue(toolbarType, out itemsForToolbar))
					{
						itemsForToolbar = new List<ToolbarItemInfo>(1);
						itemsForToolbars.Add(toolbarType, itemsForToolbar);
					}
					InsertItem(itemsForToolbar, type, toolbarItemFor);

					#if DEV_MODE && PI_ASSERTATIONS
					UnityEngine.Debug.Assert(itemsForToolbars.ContainsKey(toolbarType));
					UnityEngine.Debug.Assert(itemsForToolbars[toolbarType] != null);
					UnityEngine.Debug.Assert(itemsForToolbars[toolbarType] == itemsForToolbar);
					UnityEngine.Debug.Assert(typeof(IInspectorToolbar).IsAssignableFrom(toolbarType), "ToolbarItemForAttribute on class "+type.Name+" is targeting class "+toolbarType.Name + " that does not implement IInspectorToolbar.");
					UnityEngine.Debug.Assert(typeof(IInspectorToolbarItem).IsAssignableFrom(type), "ToolbarItemForAttribute found on class "+type.Name + " that does not implement IInspectorToolbarItem.");
					UnityEngine.Debug.Assert(!type.IsAbstract, "ToolbarItemForAttribute cannot be inherited but was found on abstract type "+type+".");
					UnityEngine.Debug.Assert(!toolbarType.IsAbstract, "ToolbarItemForAttribute only supports targeting by direct type, but was provided an abstract toolbar type "+toolbarType.Name+".");
					#endif
				}
			}

			// note: derived types currently not supported, ToolbarItemFor and ToolbarFor must target exact toolbar / inspector types.

			foreach(var itemsForToolbar in itemsForToolbars)
			{
				var itemsInfoList = itemsForToolbar.Value;
				var count = itemsInfoList.Count;
				var itemsTypes = new Type[count];
				for(int n = 0; n < count; n++)
				{
					itemsTypes[n] = itemsInfoList[n].classType;
				}
				toolbarItemsByToolbarType[itemsForToolbar.Key] = itemsTypes;
			}

			#if DEV_MODE
			timer.FinishAndLogResults();
			#endif
		}

		private static void InsertItem(List<ToolbarItemInfo> items, Type itemType, ToolbarItemForAttribute itemInfo)
		{
			var alignment = itemInfo.alignment;
			int index = itemInfo.indexInToolbar;
			int count = items.Count;

			if(alignment == ToolbarItemAlignment.Left)
			{
				for(int n = 0; n < count; n++)
				{
					var existingItem = items[n];
					if(existingItem.alignment == ToolbarItemAlignment.Right)
					{
						items.Insert(n, new ToolbarItemInfo(alignment, index, itemType));
						return;
					}

					var existingItemIndex = existingItem.indexInToolbar;
					if(index < existingItemIndex)
					{
						items.Insert(n, new ToolbarItemInfo(alignment, index, itemType));
						return;
					}
					
					// Conflict: two items with same alignment and index.
					// Discard this item if it's marked as fallback,
					// Otherwise discard the other item.
					if(index == existingItemIndex)
					{
						HandleItemConflict(items, n, itemType, itemInfo);
						return;
					}
				}
				items.Add(new ToolbarItemInfo(alignment, index, itemType));
			}
			else
			{
				for(int n = count - 1; n >= 0; n--)
				{
					var existingItem = items[n];
					if(existingItem.alignment == ToolbarItemAlignment.Left)
					{
						items.Insert(n + 1, new ToolbarItemInfo(alignment, index, itemType));
						return;
					}

					var existingItemIndex = existingItem.indexInToolbar;
					if(index < existingItemIndex)
					{
						items.Insert(n + 1, new ToolbarItemInfo(alignment, index, itemType));
						return;
					}

					// Conflict: two items with same alignment and index.
					// Discard this item if it's marked as fallback,
					// Otherwise discard the other item.
					if(index == existingItemIndex)
					{
						HandleItemConflict(items, n, itemType, itemInfo);
						return;
					}
				}
				items.Insert(0, new ToolbarItemInfo(alignment, index, itemType));
			}
		}

		private static void HandleItemConflict(List<ToolbarItemInfo> items, int indexInList, Type itemType, ToolbarItemForAttribute itemInfo)
		{
			#if DEV_MODE
			UnityEngine.Debug.Assert(items[indexInList].alignment == itemInfo.alignment);
			UnityEngine.Debug.Assert(items[indexInList].indexInToolbar == itemInfo.indexInToolbar);
			UnityEngine.Debug.Assert(typeof(IInspectorToolbarItem).IsAssignableFrom(items[indexInList].classType));
			UnityEngine.Debug.Assert(typeof(IInspectorToolbarItem).IsAssignableFrom(itemType));
			#endif

			if(!itemInfo.isFallback)
			{
				#if DEV_MODE
				UnityEngine.Debug.LogWarning("Discarding toolbar item "+items[indexInList].classType.Name+" because it's alignment and index conflicted with existing item "+itemType.Name+".");
				#endif
				items.RemoveAt(indexInList);
				items.Insert(indexInList, new ToolbarItemInfo(itemInfo.alignment, itemInfo.indexInToolbar, itemType));
			}
			#if DEV_MODE
			else { UnityEngine.Debug.LogWarning("Discarding toolbar item "+itemType.Name+" because it's alignment ("+itemInfo.alignment+") and index ("+itemInfo.indexInToolbar+") conflicted with existing item "+items[indexInList].classType.Name+"."); }
			#endif
		}

		public class ToolbarItemInfo
		{
			public readonly ToolbarItemAlignment alignment;
			public readonly int indexInToolbar;
			public readonly Type classType;

			public ToolbarItemInfo(ToolbarItemAlignment setAlignment, int setIndexInToolbar, Type setClassType)
			{
				alignment = setAlignment;
				indexInToolbar = setIndexInToolbar;
				classType = setClassType;
			}
		}
	}
}