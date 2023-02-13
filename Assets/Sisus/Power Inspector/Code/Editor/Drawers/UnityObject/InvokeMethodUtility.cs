#define HIDE_INVOKE_BUTTON_BUTTON_IF_NO_METHODS

//#define DEBUG_VALIDATORS
//#define DEBUG_BUILD_VALIDATORS
//#define DEBUG_GET_VALIDATOR
//#define DEBUG_GET_VALIDATOR_FAILED
//#define DEBUG_SKIP_ADD_METHOD
//#define DEBUG_ADD_METHOD

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	public static class InvokeMethodUtility
	{
		private const bool SplitMethodNamesIntoWords = true;
		private static Object currentMenuTarget;
		private static readonly Dictionary<Type, Dictionary<string, MethodInfo>> ContextMenuValidators = new Dictionary<Type, Dictionary<string, MethodInfo>>();
		private static readonly IsInvokingReturner IsInvokingValidator = new IsInvokingReturner();
		private static readonly FalseReturner FalseValidator = new FalseReturner();
		private static readonly IsNotDirtyReturner IsNotDirtyValidator = new IsNotDirtyReturner();
		private static readonly Dictionary<MethodInfo, string> MethodFullNames = new Dictionary<MethodInfo, string>();

		/// <summary>
		/// Opens execute method menu at given position. If DebugMode is true for subject,
		/// then more methods will be included in the opened menu.
		/// </summary>
		/// <param name="subject"> The subject whose menu is being opened. This cannot be null. </param>
		/// <param name="position"> The position. </param>
		public static void OpenExecuteMethodMenu([NotNull]IUnityObjectDrawer subject, Rect position)
		{
			OpenExecuteMethodMenu(subject, position, subject.DebugMode);
		}

		/// <summary> Opens execute method menu. </summary>
		public static void OpenExecuteMethodMenu([NotNull]IUnityObjectDrawer subject, Rect position, bool includeInvisible)
		{
			var menu = GenerateExecuteMethodMenu(subject.UnityObjects, subject.Type, includeInvisible);
			if(menu.Count > 0)
			{
				ContextMenuUtility.OpenAt(menu, position, subject, Part.MethodInvokerButton);
			}
			#if DEV_MODE
			else { Debug.LogWarning(subject+ ".GenerateExecuteMethodMenu with type "+ StringUtils.ToString(subject.Type)+" returned zero menu items"); }
			#endif
		}
		
		/// <summary> Query if this object has execute method menu items. </summary>
		/// <returns> True if execute method menu items, false if not. </returns>
		public static bool HasExecuteMethodMenuItems([NotNull]IUnityObjectDrawer subject)
		{
			#if HIDE_INVOKE_BUTTON_BUTTON_IF_NO_METHODS

			var subjectType = subject.Type;
			// All MonoBehaviours have "StopAllCoroutines"
			if(Types.MonoBehaviour.IsAssignableFrom(subjectType))
			{
				return true;
			}

			// All ScriptableObjects have "SetDirty".
			// UPDATE: The method now has the Obsolete attribute, so it won't be around for long.
			if(subjectType.IsScriptableObject())
			{
				return true;
			}
			
			// For other Components and assets, need to check their methods manually
			var target = subject.UnityObject;
			currentMenuTarget = target;
			return HasExecuteMethodMenuItems(target, subjectType, subject.DebugMode);

			#else
			return true;
			#endif
		}

		private static bool HasExecuteMethodMenuItems([CanBeNull]Object target, [NotNull]Type type, bool includeInvisible)
		{
			var validator = GetContextMenuValidators(type);
			var methods = type.GetMethods(includeInvisible ? BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public : BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
			for(int n = methods.Length - 1; n >= 0; n--)
			{
				bool disabled;
				if(ShowAsMenuItem(target, methods[n], validator, out disabled, false))
				{
					return true;
				}
			}

			var baseType = type.BaseType;
			return baseType != null ? HasExecuteMethodMenuItems(target, baseType, includeInvisible) : false;
		}

		/// <summary> Generates an execute method menu. </summary>
		/// <returns> The execute method menu. </returns>
		public static Menu GenerateExecuteMethodMenu([CanBeNull]Object[] targets, [NotNull]Type type, bool includeInvisible)
		{
			var menu = Menu.Create();

			currentMenuTarget = targets != null && targets.Length > 0 ? targets[0] : null;

			var validators = GetContextMenuValidators(type);

			if(currentMenuTarget != null)
			{
				GetMenuItemsFromMethodInfos(targets, targets, type, includeInvisible ? BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly : BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, ref menu, "", SplitMethodNamesIntoWords, validators, false);
			}

			int countWas = menu.Count;

			GetMenuItemsFromMethodInfos(targets, targets, type, includeInvisible ? BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly : BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly, ref menu, currentMenuTarget == null ? "" : "Static/", SplitMethodNamesIntoWords, validators, false);
			
			if(countWas > 0 && menu.Count > countWas)
			{
				menu.InsertSeparatorIfNotRedundant(countWas);
			}

			return menu;
		}

		/// <summary> Generates an execute method menu. </summary>
		/// <returns> The execute method menu. </returns>
		public static void AddExecuteMethodMenuItems(ref Menu menu, [CanBeNull]Object[] unityObjects, object[] targets, [NotNull]Type type, bool includeInvisible, bool includeStatic, bool canHaveReturnValue, string prefix = "Invoke/")
		{
			currentMenuTarget = unityObjects != null && unityObjects.Length > 0 ? unityObjects[0] : null;

			var validators = GetContextMenuValidators(type);

			int countAtStart = menu.Count;

			if(targets != null && targets.Length > 0 && targets[0] != null)
			{
				GetMenuItemsFromMethodInfos(unityObjects, targets, type, includeInvisible ? BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly : BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, ref menu, prefix, SplitMethodNamesIntoWords, validators, canHaveReturnValue);
			}

			if(includeStatic)
			{
				int countAfterInstanceMethods = menu.Count;

				GetMenuItemsFromMethodInfos(unityObjects, targets, type, includeInvisible ? BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly : BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly, ref menu, currentMenuTarget == null ? prefix : prefix + "/Static/", SplitMethodNamesIntoWords, validators, canHaveReturnValue);
			
				if(countAfterInstanceMethods > countAtStart && menu.Count > countAfterInstanceMethods)
				{
					menu.InsertSeparatorIfNotRedundant(countAfterInstanceMethods);
				}
			}
		}

		public static bool IsDisabled([NotNull]Object[] targets, MethodInfo method)
		{
			int count = targets.Length;
			if(count == 0)
			{
				return false;
			}

			var type = targets[0].GetType();
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				if(IsDisabled(targets[n], type, method))
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsDisabled([NotNull]Object[] targets, Type type, MethodInfo method)
		{
			int count = targets.Length;
			if(count == 0)
			{
				return false;
			}

			for(int n = targets.Length - 1; n >= 0; n--)
			{
				if(IsDisabled(targets[n], type, method))
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsDisabled([CanBeNull]Object target, [NotNull]Type type, MethodInfo method)
		{
			var validators = GetContextMenuValidators(type);
			if(validators == null)
			{
				return false;
			}

			currentMenuTarget = target;

			MethodInfo validator;
			if(validators.TryGetValue(GetContextMenuNameOrFullName(method), out validator))
			{
				bool show;
				try
				{
					show = (bool)validator.Invoke(target, null);
				}
				#if DEV_MODE
				catch(Exception e)
				{
					Debug.LogError(e);
				#else
				catch(Exception)
				{
				#endif
					show = true;
				}

				#if DEV_MODE && DEBUG_GET_VALIDATOR
				Debug.Log("validator "+StringUtils.Green("found")+" for "+GetContextMenuNameOrFullName(method)+"! show = "+StringUtils.ToColorizedString(show));
				#endif

				return !show;
			}
			
			return false;
		}

		[CanBeNull]
		private static Dictionary<string, MethodInfo> GetContextMenuValidators(Type type)
		{
			Dictionary<string, MethodInfo> validators;
			if(ContextMenuValidators.TryGetValue(type, out validators))
			{
				return validators;
			}
			
			var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
			for(int n = methods.Length - 1; n >= 0; n--)
			{
				var method = methods[n];

				// grey out Cancel Invoking if MonoBehaviour not currently invoking any coroutines
				if(string.Equals(method.Name, "CancelInvoke"))
				{
					if(Types.MonoBehaviour.IsAssignableFrom(type))
					{
						if(validators == null)
						{
							validators = new Dictionary<string, MethodInfo>();
						}

						#if DEV_MODE && DEBUG_BUILD_VALIDATORS
						Debug.Log("validators.Add("+GetFullName(method)+", IsInvokingValidator");
						#endif

						validators.Add(GetFullName(method), IsInvokingValidator);
						continue;
					}
				}
				
				// grey out Set Dirty if target is already dirty
				if(string.Equals(method.Name, "SetDirty"))
				{
					if(Types.UnityObject.IsAssignableFrom(type))
					{
						if(validators == null)
						{
							validators = new Dictionary<string, MethodInfo>();
						}

						#if DEV_MODE && DEBUG_BUILD_VALIDATORS
						Debug.Log("validators.Add("+GetFullName(method)+", IsNotDirtyValidator");
						#endif

						validators.Add(GetFullName(method), IsNotDirtyValidator);
						continue;
					}
				}

				var attributes = method.GetCustomAttributes(false);
				for(int c = attributes.Length - 1; c >= 0; c--)
				{
					var attribute = attributes[c];
					if(!method.IsStatic)
					{
						var contextMenu = attribute as ContextMenu;
						if(contextMenu != null)
						{
							if(contextMenu.validate && method.ReturnType == Types.Bool && Types.UnityObject.IsAssignableFrom(type))
							{
								if(validators == null)
								{
									validators = new Dictionary<string, MethodInfo>();
								}

								#if DEV_MODE && DEBUG_BUILD_VALIDATORS
								Debug.Log("validators.Add("+contextMenu.menuItem+", ContextMenuValidator");
								#endif
								
								validators.Add(contextMenu.menuItem, method);
							}
							continue;
						}
					}
					
					var editorBrowsable = attribute as EditorBrowsableAttribute;
					if(editorBrowsable != null)
					{
						if(editorBrowsable.State == EditorBrowsableState.Never)
						{
							if(validators == null)
							{
								validators = new Dictionary<string, MethodInfo>();
							}

							#if DEV_MODE && DEBUG_BUILD_VALIDATORS
							Debug.Log("validators.Add("+GetFullName(method)+", FalseValidator (EditorBrowsable(Never))");
							#endif

							validators.Add(GetFullName(method), FalseValidator);
						}
						continue;
					}

					var browsable = attribute as BrowsableAttribute;
					if(browsable != null)
					{
						if(!browsable.Browsable)
						{
							if(validators == null)
							{
								validators = new Dictionary<string, MethodInfo>();
							}

							#if DEV_MODE && DEBUG_BUILD_VALIDATORS
							Debug.Log("validators.Add("+GetFullName(method)+", FalseValidator (Browsable(false))");
							#endif

							validators.Add(GetFullName(method), FalseValidator);
						}
						continue;
					}

					var hideInInspector = attribute as HideInInspector;
					if(hideInInspector != null)
					{
						if(validators == null)
						{
							validators = new Dictionary<string, MethodInfo>();
						}

						#if DEV_MODE && DEBUG_BUILD_VALIDATORS
						Debug.Log("validators.Add("+GetFullName(method)+", FalseValidator (HideInInspector)");
						#endif

						validators.Add(GetFullName(method), FalseValidator);
					}
				}
			}

			ContextMenuValidators.Add(type, validators);
			
			var baseType = type.BaseType;
			if(baseType != null)
			{
				var nestedValidators = GetContextMenuValidators(baseType);
				if(nestedValidators != null)
				{
					foreach(var validator in nestedValidators)
					{
						if(validators == null)
						{
							validators = new Dictionary<string, MethodInfo>();
						}
						validators.Add(validator.Key, validator.Value);
					}
				}
			}

			return validators;
		}
		
		private static string GetFullName(MethodInfo method)
		{
			string fullName;
			if(!MethodFullNames.TryGetValue(method, out fullName))
			{
				fullName = StringUtils.ToString(method);
				MethodFullNames.Add(method, fullName);
			}
			return fullName;
		}

		/// <summary> Gets menu item from method infos. </summary>
		/// <param name="target"> UnityEngine.Object target that contains the method. </param>
		/// <param name="type"> Type of the class that owns the method. For non-static classes this should equal type of the target. </param>
		/// <param name="bindingFlags"> The binding flags to use when searching for methods. </param>
		/// <param name="menu"> [in,out] The menu into which the item will be added. </param>
		/// <param name="menuItemPrefix"> The menu item prefix. </param>
		/// <param name="splitNamesIntoWords"> Separate menu labels into words instead of using original method names? </param>
		/// <param name="validators">
		///	Contains validator functions for ContextMenu items by their item names.
		/// Generated from methods that have ContextMenu attribute and contructor isValidateFunction parameter is true.
		/// If validator returns false, the menu item will be greyed out and cannot be clicked in the context menu.
		/// </param>
		private static void GetMenuItemsFromMethodInfos([CanBeNull]Object[] unityObjects, [CanBeNull]object[] targets, [NotNull]Type type, BindingFlags bindingFlags, ref Menu menu, string menuItemPrefix, bool splitNamesIntoWords, Dictionary<string, MethodInfo> validators, bool canHaveReturnValue)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(bindingFlags.HasFlag(BindingFlags.DeclaredOnly));
			#endif

			int lastSeparatorIndex = menu.Count;

			var methods = type.GetMethods(bindingFlags);

			for(int n = methods.Length - 1; n >= 0; n--)
			{
				GetMenuItemFromMethodInfo(unityObjects, targets, methods[n], ref menu, menuItemPrefix, splitNamesIntoWords, validators, canHaveReturnValue);
			}
			
			for(var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
			{
				int countWas = menu.Count;
				
				methods = baseType.GetMethods(bindingFlags);

				for(int n = methods.Length - 1; n >= 0; n--)
				{
					GetMenuItemFromMethodInfo(unityObjects, targets, methods[n], ref menu, menuItemPrefix, splitNamesIntoWords, validators, canHaveReturnValue);
				}
				
				if(lastSeparatorIndex < countWas && menu.Count > countWas)
				{
					lastSeparatorIndex = countWas;
					menu.InsertSeparatorIfNotRedundant(countWas);
				}
			}
		}
		
		/// <summary> Generates invoke method menu item from method information. </summary>
		/// <param name="target"> UnityEngine.Object target that contains the method. </param>
		/// <param name="method"> The method for which to generate the item. </param>
		/// <param name="addToMenu"> [in,out] The menu into which the item will be added. </param>
		/// <param name="menuItemPrefix"> Prefix that should be given to menu item text. If an empty string, no prefix will be added. </param>
		/// <param name="splitNamesIntoWords"> Separate menu labels into words instead of using original method names? </param>
		/// <param name="validators">
		///	Contains validator functions for ContextMenu items by their item names.
		/// Generated from methods that have ContextMenu attribute and contructor isValidateFunction parameter is true.
		/// If validator returns false, the menu item will be greyed out and cannot be clicked in the context menu.
		/// </param>
		private static void GetMenuItemFromMethodInfo([CanBeNull]Object[] unityObjects, object[] targets, MethodInfo method, ref Menu addToMenu, [NotNull]string menuItemPrefix, bool splitNamesIntoWords, Dictionary<string, MethodInfo> validators, bool canHaveReturnValue)
		{
			bool disabled;
			if(!ShowAsMenuItem(targets == null || targets.Length == 0 ? null : targets[0], method, validators, out disabled, canHaveReturnValue))
			{
				#if DEV_MODE && DEBUG_SKIP_ADD_METHOD
				Debug.Log("ShowAsMenuItem " + method + ": " + StringUtils.False+"\nParams=" + StringUtils.ToColorizedString(method.GetParameters().Length) + ", HasReturnValue=" + StringUtils.ToColorizedString(method.ReturnType != typeof(void)) + ", IsAbstract=" + StringUtils.ToColorizedString(method.IsAbstract) + ", IsAssembly=" + StringUtils.ToColorizedString(method.IsAssembly) + ", IsGenericMethod=" + StringUtils.ToColorizedString(method.IsGenericMethod));
				#endif
				return;
			}

			if(disabled)
			{
				addToMenu.AddDisabled(string.Concat(menuItemPrefix, method.Name));
				return;
			}

			string name = method.Name;
			if(splitNamesIntoWords)
			{
				name = StringUtils.SplitPascalCaseToWords(name);

				if(name.Length == 0)
				{
					#if DEV_MODE
					Debug.Log(method + " name was empty after SplitPascalCaseToWords");
					#endif
					return;
				}
			}

			string menuPath = string.Concat(menuItemPrefix, name);
			if(addToMenu.Contains(menuPath))
			{
				return;
			}

			#if DEV_MODE && DEBUG_ADD_METHOD
			Debug.Log("<color=green>Adding method</color>: " + name);
			#endif

			addToMenu.Add(menuPath, ()=>
			{
				if(!Application.isPlaying && WarnAboutInvokingInEditMode(method, unityObjects) && !EditorUtility.DisplayDialog("Execute In Edit Mode?", "Execute method \""+ name + "\" in edit mode? This could result in changes that can be undone.", "Execute", "Cancel"))
				{
					return;
				}

				bool runAsCoroutine;
				if(method.ReturnType != typeof(IEnumerator))
				{
					runAsCoroutine = false;
				}
				else if(!InspectorUtility.Preferences.askAboutStartingCoroutines)
				{
					runAsCoroutine = true;
				}
				else
				{
					switch(DrawGUI.Active.DisplayDialog("Invoke as coroutine?", "The method return type is IEnumerable. Would you like to start it as a coroutine?", "Start As Coroutine", "Invoke As Normal Method", "Cancel"))
					{
						case 0:
							runAsCoroutine = true;
							break;
						case 1:
							runAsCoroutine = false;
							break;
						case 2:
							return;
						default:
							#if DEV_MODE
							throw new IndexOutOfRangeException();
							#else
							runAsCoroutine = false;
							break;
							#endif
					}
				}

				int count = targets.Length;

				if(runAsCoroutine)
				{
					for(int n = 0; n < count; n++)
					{
						var target = targets[n];
						var ienumerator = (IEnumerator)method.Invoke(target, null);
						var monoBehaviour = target as MonoBehaviour;

						Undo.RegisterFullObjectHierarchyUndo(method.IsStatic || unityObjects == null || unityObjects.Length == 0 ? null : unityObjects[n], method.Name);

						if(!Application.isPlaying)
						{
							StaticCoroutine.StartCoroutineInEditMode(ienumerator);
						}
						else if(monoBehaviour != null)
						{
							monoBehaviour.StartCoroutine(ienumerator);
						}
						else
						{
							StaticCoroutine.StartCoroutine(ienumerator);
						}
					}

					InspectorUtility.ActiveInspector.Message(string.Concat(name, " started."));
				}
				else if(method.ReturnType != Types.Void && method.ReturnType != Types.IEnumerator)
				{
					for(int n = 0; n < count; n++)
					{
						var target = targets[n];
						Undo.RegisterFullObjectHierarchyUndo(method.IsStatic || unityObjects == null || unityObjects.Length == 0 ? null : unityObjects[n], method.Name);
						object result = method.Invoke(target, null);
						string resultAsJson = Clipboard.Copy(result, method.ReturnType);
						if(method.ReturnType == Types.String && string.IsNullOrEmpty(resultAsJson))
						{
							resultAsJson = "\"\"";
						}
						InspectorUtility.ActiveInspector.Message(string.Concat(name, " result: " + resultAsJson));
					}
				}
				else
				{
					for(int n = 0; n < count; n++)
					{
						var target = targets[n];
						Undo.RegisterFullObjectHierarchyUndo(method.IsStatic || unityObjects == null || unityObjects.Length == 0 ? null : unityObjects[n], method.Name);
						method.Invoke(target, null);
					}
					InspectorUtility.ActiveInspector.Message(string.Concat(name, " invoked."));
				}
			});
		}

		private static bool WarnAboutInvokingInEditMode(MethodInfo method, [CanBeNull]Object[] onUnityObjects)
		{
			if(!InspectorUtility.Preferences.warnAboutInvokingInEditMode)
			{
				return false;
			}

			if(Attribute<PureAttribute>.ExistsOn(method))
			{
				return false;
			}

			#if UNITY_2018_3_OR_NEWER
			if(Attribute<ExecuteAlways>.ExistsOn(method))
			{
				return false;
			}
			#endif

			if(Attribute<ExecuteInEditMode>.ExistsOn(method))
			{
				return false;
			}


			switch(method.Name)
			{
				case "ToString":
				case "GetHashCode":
					return false;
			}

			var monoBehaviour = onUnityObjects == null || onUnityObjects.Length == 0 ? null : onUnityObjects[0] as MonoBehaviour;
			if(monoBehaviour != null && monoBehaviour.runInEditMode)
			{
				return false;
			}
			return true;
		}

		///  <summary> Generates invoke method menu item from method information. </summary>
		///  <param name="target"> UnityEngine.Object target that contains the method. </param>
		///  <param name="method"> The method for which to generate the item. </param>
		///  <param name="validators">
		/// 	Contains validator functions for ContextMenu items by their item names.
		///  Generated from methods that have ContextMenu attribute and contructor isValidateFunction parameter is true.
		///  If validator returns false, the menu item will be greyed out and cannot be clicked in the context menu.
		///  </param>
		/// <param name="disabled"> If true, then the menu item should be shown greyed out and not be clickable in the context menu. </param>
		private static bool ShowAsMenuItem(object target, MethodInfo method, Dictionary<string, MethodInfo> validators, out bool disabled, bool canHaveReturnValue)
		{
			disabled = false;

			if(method.IsAbstract || method.IsAssembly || method.IsGenericMethod)
			{
				return false;
			}

			// Currently only showing methods with return type void
			// NOTE: If bool types were to be returned, then should NOT return ones that are validate functions for ContextMenu items.
			if(method.ReturnType != Types.Void)
			{
				if(method.ReturnType == typeof(IEnumerator))
				{
					if(!Application.isPlaying || (target as MonoBehaviour) == null || string.Equals(method.Name, "GetEnumerator"))
					{
						return false;
					}
				}
				else if(!canHaveReturnValue)
				{
					return false;
				}

				switch(method.Name)
				{
					case "ToArray":
					case "Clone":
					case "GetEnumerator":
					case "AsReadOnly":
						return false;
					default:
						// Skip property getters to avoid cluttering the menu with unnecessary items.
						// In most instances you can see the property values in the inspector already.
						if(method.Name.StartsWith("get_", StringComparison.Ordinal))
						{
							return false;
						}
						break;
				}
			}

			if(method.GetParameters().Length > 0)
			{
				return false;
			}

			if(validators != null)
			{
				MethodInfo validator;
				if(validators.TryGetValue(GetContextMenuNameOrFullName(method), out validator))
				{
					bool show;
					try
					{
						show = (bool)validator.Invoke(target, null);
					}
					#if DEV_MODE
					catch(Exception e)
					{
						Debug.LogError(e);
					#else
					catch(Exception)
					{
					#endif
						show = true;
					}
					disabled = !show;

					#if DEV_MODE && DEBUG_GET_VALIDATOR
					Debug.Log("validator "+StringUtils.Green("found")+" for "+GetContextMenuNameOrFullName(method)+"! show = "+StringUtils.ToColorizedString(show));
					#endif

					return true;
				}
				#if DEV_MODE && DEBUG_GET_VALIDATOR_FAILED
				Debug.Log("validator "+StringUtils.Red("not found")+" for "+GetContextMenuNameOrFullName(method)+" among "+validators.Count+" options");
				#endif
			}
			
			return true;
		}

		private static string GetContextMenuNameOrFullName(MethodInfo method)
		{
			string result;
			if(MethodFullNames.TryGetValue(method, out result))
			{
				return result;
			}

			var contextMenuAttributes = method.GetCustomAttributes(Types.ContextMenu, false);
			if(contextMenuAttributes.Length > 0)
			{
				var contextMenu = contextMenuAttributes[0] as ContextMenu;
				result = contextMenu.menuItem;
			}
			else
			{
				result = StringUtils.ToString(method);
			}
			MethodFullNames.Add(method, result);
			
			return result;
		}
		
		private static bool ShowAsMenuItem(MethodInfo method, bool targetIsMonoBehaviour)
		{
			if(method.IsAbstract || method.IsAssembly || method.IsGenericMethod)
			{
				return false;
			}

			// Should we show methods with return types other than void?
			// bool? int? IEnumerator? All?
			if(method.ReturnType != Types.Void && (!Application.isPlaying || method.ReturnType != typeof(IEnumerator) || !targetIsMonoBehaviour))
			{
				//if(method.GetCustomAttributes(typeof(PureAttribute), true).Length > 0)
				return false;
			}

			if(method.GetParameters().Length > 0)
			{
				return false;
			}

			if(string.Equals(method.Name, "CancelInvoke"))
			// UPDATE: If "StopAllCoroutines" is always shown, it guarantees that there will always be
			// at least one method available in the menu for MonoBehaviours. This has the benefit that we don't need to hide
			// the execute icon when there are no invokable methods, and thus don't need to get the method infos
			// before the execute icon is clicked, which helps with performance.
			//string.Equals(method.Name, "StopAllCoroutines", StringComparison.Ordinal)
			{
				return false;
			}

			return true;
		}
		
		private static void OnMenuItemClicked(PopupMenuItem item)
		{
			var target = currentMenuTarget;
			currentMenuTarget = null;
			
			var method = item.IdentifyingObject as MethodInfo;

			#if DEV_MODE
			Debug.Assert(method.ReturnType != typeof(IEnumerator) || Application.isPlaying);
			#endif
			
			#if UNITY_EDITOR
			if(!Application.isPlaying)
			{
				if(target == null || !target.RunsInEditMode())
				{
					if(WarnAboutInvokingInEditMode(method, ArrayPool<Object>.ZeroSizeArray) && !EditorUtility.DisplayDialog("Execute In Edit Mode?", "Execute method " + method.Name + " in edit mode? This might not work properly for all methods.", "Execute", "Cancel"))
					{
						return;
					}
				}
			}
			#endif
			
			#if UNITY_EDITOR
			Undo.RegisterFullObjectHierarchyUndo(method.IsStatic ? null : target, method.Name);
			#endif

			if(method.ReturnType == typeof(IEnumerator))
			{
				(target as MonoBehaviour).StartCoroutine(method.Name);
				return;
			}
			
			method.Invoke(method.IsStatic ? null : target, null);
		}

		/// <summary>
		/// Helper class for use with ContextMenu item validation.
		/// 
		/// Can be used to create a validator for a ContextMenu item that always returns false,
		/// forcing them to appear as greyed out and not clickable in the context menu.
		/// </summary>
		private class FalseReturner
		{
			private readonly MethodInfo validator;

			public FalseReturner()
			{
				validator = typeof(FalseReturner).GetMethod("False", BindingFlags.Static | BindingFlags.NonPublic);
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(validator != null);
				#endif
			}

			[UsedImplicitly]
			private static bool False()
			{
				#if DEV_MODE && DEBUG_VALIDATORS
				Debug.Log("FalseReturner: returning "+StringUtils.False);
				#endif
				return false;
			}

			public static implicit operator MethodInfo(FalseReturner falseReturner)
			{
				return falseReturner.validator;
			}
		}

		/// <summary>
		/// Helper class for use with "SetDirty" context menu item validation.
		/// 
		/// Can be used to create a validator for the ScriptableObject.SetDirty method context menu item that
		/// return true when the target Unity Object IS NOT dirty, and false when the target Unity Object
		/// IS dirty.
		/// </summary>
		private class IsNotDirtyReturner
		{
			private readonly MethodInfo validator;
			
			public IsNotDirtyReturner()
			{
				validator = typeof(IsNotDirtyReturner).GetMethod("IsNotDirty", BindingFlags.Static | BindingFlags.NonPublic);
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(validator != null);
				#endif
			}

			[UsedImplicitly]
			private static bool IsNotDirty()
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(currentMenuTarget != null);
				#endif
			
				#if !UNITY_EDITOR
				return true;
				#elif UNITY_2019_1_OR_NEWER //not sure if this is the exact version where EditorUtility.IsDirty became public
				return currentMenuTarget != null && !EditorUtility.IsDirty(currentMenuTarget);
				#else
				return currentMenuTarget != null && (bool)typeof(EditorUtility).GetMethod("IsDirty", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).InvokeWithParameter(null, currentMenuTarget);
				#endif
			}

			public static implicit operator MethodInfo(IsNotDirtyReturner isNotDirtyReturner)
			{
				return isNotDirtyReturner.validator;
			}
		}

		/// <summary>
		/// Helper class for use with "CancelInvoke" context menu item validation.
		/// 
		/// Can be used to create a validator for the MonoBehaviour.CancelInvoke method context menu item that
		/// return true when the target MonoBehaviour is invoking a method, and false when not.
		/// </summary>
		private class IsInvokingReturner
		{
			private readonly MethodInfo validator;
			
			public IsInvokingReturner()
			{
				validator = typeof(IsInvokingReturner).GetMethod("IsInvoking", BindingFlags.Static | BindingFlags.NonPublic, null, CallingConventions.Any, ArrayPool<Type>.ZeroSizeArray, null);
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(validator != null);
				#endif
			}

			[UsedImplicitly]
			private static bool IsInvoking()
			{
				var monoBehaviour = currentMenuTarget as MonoBehaviour;
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(monoBehaviour != null);
				#endif
				return monoBehaviour != null && monoBehaviour.IsInvoking();
			}

			public static implicit operator MethodInfo(IsInvokingReturner isInvokingReturner)
			{
				return isInvokingReturner.validator;
			}
		}
	}
}