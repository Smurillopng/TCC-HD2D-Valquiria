//#define DEBUG_GENERATE_CONTEXT_MENU_ITEMS
//#define DEBUG_FAIL_GENERATE_CONTEXT_MENU_ITEMS
//#define DEBUG_ADD
#define DEBUG_SETUP_TIME

using JetBrains.Annotations;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	[InitializeOnLoad]
	public static class MenuItemAttributeUtility
	{
		private static readonly Dictionary<Type, List<ContextMenuItemInfo>> contextMenuMethodsByType = new Dictionary<Type, List<ContextMenuItemInfo>>();

		static MenuItemAttributeUtility()
		{
			EditorApplication.delayCall += SetupWhenReady;
		}

		private static void SetupWhenReady()
		{
			// Calling TypeExtensions.GetType requires TypeExtensions Setup to be completed first.
			if(!TypeExtensions.IsReady)
			{
				EditorApplication.delayCall += SetupWhenReady;
				return;
			}

			ThreadPool.QueueUserWorkItem(SetupThreaded);
		}

		private static void SetupThreaded(object threadTaskId)
		{
			#if DEV_MODE && DEBUG_SETUP_TIME
			//var timer = new ExecutionTimeLogger();
			//timer.Start("MenuItemAttributeUtility.SetupThreaded");
			#endif

			foreach(var type in TypeExtensions.GetAllTypesThreadSafe(typeof(MenuItem).Assembly, true, true, true))
			{
				var staticMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				for(int m = staticMethods.Length - 1; m >= 0; m--)
				{
					var method = staticMethods[m];
					var menuItems = method.GetCustomAttributes(typeof(MenuItem), false);
					foreach(var menuItemAttribute in menuItems)
					{
						var menuItem = (MenuItem)menuItemAttribute;
						var itemPath = menuItem.menuItem;
						if(!itemPath.StartsWith("CONTEXT/", StringComparison.OrdinalIgnoreCase) && !IsBlackListed(itemPath))
						{
							continue;
						}

						int targetNameEnd = itemPath.IndexOf('/', 8);
						if(targetNameEnd == -1)
						{
							continue;
						}

						var typeName = itemPath.Substring(8, targetNameEnd - 8);
						var targetType = TypeExtensions.GetType(typeName, Types.UnityObject);
						if(targetType == null)
						{
							#if DEV_MODE && DEBUG_FAIL_GENERATE_CONTEXT_MENU_ITEMS
							UnityEngine.Debug.LogWarning("Context menu item \""+itemPath+"\" in class "+type.FullName+": UnityObject of type " + typeName + " was not found.");
							#endif
							continue;
						}

						#if DEV_MODE && PI_ASSERTATIONS
						UnityEngine.Debug.Assert(Types.UnityObject.IsAssignableFrom(targetType));
						#endif

						#if DEV_MODE && DEBUG_GENERATE_CONTEXT_MENU_ITEMS
						UnityEngine.Debug.Log("Found context menu item \""+itemPath+"\" for type "+typeName+" in class "+type.FullName);
						#endif
										
						var label = itemPath.Substring(targetNameEnd + 1);

						AddContextMenuMethodForType(targetType, method, label, menuItem.priority, menuItem.validate);
						foreach(var extendingType in TypeExtensions.GetExtendingTypes(targetType, true))
						{
							AddContextMenuMethodForType(extendingType, method, label, menuItem.priority, menuItem.validate);
						}						
					}
				}
			}

			#if DEV_MODE && DEBUG_SETUP_TIME
			//timer.FinishAndLogResults();
			#endif
		}

		private static bool IsBlackListed(string menuItem)
		{
			switch(menuItem)
			{
				case PowerInspectorMenuItemPaths.ViewInPowerInspector:
				case PowerInspectorMenuItemPaths.PeekInPowerInspector:
					return true;
				default:
					return false;
			}
		}

		private static void AddContextMenuMethodForType(Type targetType, MethodInfo method, string label, int priority, bool isValidateMethod)
		{
			List<ContextMenuItemInfo> itemInfos;
			if(!contextMenuMethodsByType.TryGetValue(targetType, out itemInfos))
			{
				itemInfos = new List<ContextMenuItemInfo>(1);
				contextMenuMethodsByType.Add(targetType, itemInfos);
			}
			else
			{
				for(int n = itemInfos.Count - 1; n >= 0; n--)
				{
					var itemInfo = itemInfos[n];
					if(string.Equals(itemInfo.label, label))
					{
						if(isValidateMethod)
						{
							itemInfo.validateMethod = method;
							return;
						}
						else if(itemInfo.method == null)
						{
							itemInfo.method = method;
							return;
						}
						#if DEV_MODE
						UnityEngine.Debug.LogWarning("Context menu item conflict \""+itemInfo.label+"\" for type "+targetType.Name +" via method "+itemInfo.method.Name+".");
						#endif
					}
				}
				
			}

			itemInfos.Add(new ContextMenuItemInfo(label, isValidateMethod ? null : method, isValidateMethod ? method : null, priority));
		}


		public static void AddItemsFromMenuItemAttributesToContextMenu([NotNull]Menu menu, [NotNull]Object[] targets)
		{
			int targetCount = targets.Length;
			if(targetCount == 0)
			{
				return;
			}

			var firstTarget = targets[0];
			if(firstTarget == null)
			{
				return;
			}

			var targetType = firstTarget.GetType();
			List<ContextMenuItemInfo> itemInfos;
			if(contextMenuMethodsByType.TryGetValue(targetType, out itemInfos))
			{
				bool separatorAdded = false;
				for(int n = 0, count = itemInfos.Count; n < count; n++)
				{
					var itemInfo = itemInfos[n];

					var validateMethod = itemInfo.validateMethod;

					// Skip methods that have a validate method and don't pass validation at this time
					// In the default inspector these are still drawn in the menu, just greyed out,
					// but in Power Inspector they are skipped completely to avoid cluttering up the menus.
					if(validateMethod != null)
					{
						var parameters = validateMethod.GetParameters();
						if(parameters.Length == 0)
						{
							if(!(bool)validateMethod.Invoke())
							{
								continue;
							}
						}
						else if(parameters.Length == 1)
						{
							bool failedValidate = false;

							for(int t = targetCount - 1; t >= 0; t--)
							{
								if(!(bool)validateMethod.Invoke(null, new object[] { new MenuCommand(targets[t]) }))
								{
									failedValidate = true;
									break;
								}
							}

							if(failedValidate)
							{
								continue;
							}
						}
					}

					#if DEV_MODE && DEBUG_ADD
					UnityEngine.Debug.Log("Adding context menu item \""+itemInfo.label+"\" for type "+targetType.Name +" via method "+itemInfo.method.Name);
					#endif

					if(!separatorAdded)
					{
						separatorAdded = true;
						menu.AddSeparatorIfNotRedundant();
					}

					string label = itemInfo.label;
					var item = Menu.Item(label, ()=>
					{
						for(int t = targets.Length - 1; t >= 0; t--)
						{
							itemInfo.method.InvokeWithParameter(null, new MenuCommand(targets[t], 0));
						}
					});

					if(menu.Contains(label))
					{
						#if DEV_MODE
						UnityEngine.Debug.LogWarning("Context menu item conflict \""+itemInfo.label+"\" for type "+targetType.Name +" via method "+itemInfo.method.Name+". Adding with MenuItemAttribute suffix.");
						#endif
						menu.AddEvenIfDuplicate(label + "\tMenuItemAttribute", item.Effect);
					}
					else
					{
						menu.Add(item);
					}
				}
			}
		}

		private class ContextMenuItemInfo
		{
			public readonly string label;
			public MethodInfo method;
			[CanBeNull]
			public MethodInfo validateMethod;
			public readonly int priority;

			public ContextMenuItemInfo(string setLabel, [CanBeNull]MethodInfo setMethod, [CanBeNull]MethodInfo setValidateMethod, int setPriority)
			{
				label = setLabel;
				method = setMethod;
				validateMethod = setValidateMethod;
				priority = setPriority;
			}
		}
	}
}