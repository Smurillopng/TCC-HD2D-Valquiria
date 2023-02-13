#define DEBUG_USE_COMPATIBILITY_MODE
//#define DEBUG_DONT_USE_COMPATIBILITY_MODE

#if UNITY_2023_1_OR_NEWER
using System;
using JetBrains.Annotations;
using UnityEngine;
using System.Reflection;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
#endif

namespace Sisus.Compatibility
{
	public static class PluginCompatibilityUtility
	{
		private static bool otherToolsHaveInjectedTheirEditors;

		#if ODIN_INSPECTOR
		private static Type odinEditorType;

		[NotNull]
		private static Type OdinEditorType
		{
			get
			{
				if(odinEditorType == null)
				{
					odinEditorType = TypeExtensions.GetType("Sirenix.OdinInspector.Editor.OdinEditor");
				}

				return odinEditorType;
			}
		}
		#endif

		public static bool OtherToolsHaveInjectedTheirEditors()
		{
			if(otherToolsHaveInjectedTheirEditors)
			{
				return true;
			}

			if(!HasInitArgsInjectedItsEditors())
			{
				return false;
			}

			#if ODIN_INSPECTOR
			if(!HasOdinInjectedItsEditors())
			{
				return false;
			}
			#endif

			otherToolsHaveInjectedTheirEditors = true;
			return true;
		}

		private static bool HasInitArgsInjectedItsEditors()
		{
			var initArgsEditorInjectorType = Type.GetType("Sisus.Init.EditorOnly.InitializableEditorInjector, InitArgs.Editor", false);
			if(initArgsEditorInjectorType != null)
			{
				var isDoneProperty = initArgsEditorInjectorType.GetProperty("IsDone", BindingFlags.Static | BindingFlags.Public);
				if(isDoneProperty != null)
				{
					return (bool)isDoneProperty.GetValue(null, null);
				}
				#if DEV_MODE
				else Debug.LogWarning("InitializableEditorInjector.IsDone property not found!");
				#endif
			}

			return true;
		}

		#if ODIN_INSPECTOR
		private static int timesToWaitForOdin = 50;
		private static bool HasOdinInjectedItsEditors()
		{
			if(timesToWaitForOdin <= 0)
			{
				#if DEV_MODE
				Debug.Log("CustomEditorUtility - Waited long enough for Odin.");
				#endif
				return true;
			}

			timesToWaitForOdin--;

			if(OdinEditorType == null)
			{
				return true;
			}

			if(!CustomEditorUtility.IsReady)
			{
				return false;
			}

			foreach(var customEditorInfos in CustomEditorUtility.CustomEditorsByType.Values)
			{
				foreach(var customEditorInfo in customEditorInfos.customEditors)
				{
					if(customEditorInfo.inspectorType == odinEditorType)
					{
						#if DEV_MODE
						Debug.Log($"CustomEditorUtility - Odin has injected its own Editor.");
						#endif

						return true;
					}
				}
			}

			return false;
		}
		#endif

		/// <summary>
		/// Given a type inheriting from UnityEngine.Object, determines whether or not an Editor should be used for drawing a target of this type, based on current compatibility preferences and installed plug-ins. </summary>
		/// <param name="type"> The type of a UnityEngine.Object target. This cannot be null. </param>
		/// <returns> True if should always use Editors for targets of type, false if not. </returns>
		public static bool UseCompatibilityModeForDisplayingTarget([NotNull]Type type)
		{
			#if ODIN_INSPECTOR && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(type != null, "UseCompatibilityModeForDisplayingTarget was called with null type");
			UnityEngine.Debug.Assert(type.IsUnityObject(), "UseCompatibilityModeForDisplayingTarget was called for type that was not UnityEngine.Object: "+type.Name);
			#endif

			switch(InspectorUtility.Preferences.UseEditorsOverDrawers)
			{
				case UseEditorsOverDrawers.OnlyIfHasCustomEditor:
					return false;
				case UseEditorsOverDrawers.Always:
					return true;
				case UseEditorsOverDrawers.BasedOnPlugins:
					#if ODIN_INSPECTOR
					return true;
					#else
					return false;
					#endif
				case UseEditorsOverDrawers.BasedOnPluginPreferences:

					#if ODIN_INSPECTOR
					var odinConfig = GlobalConfig<InspectorConfig>.Instance;
					if(!odinConfig.EnableOdinInInspector)
					{
						#if DEV_MODE && DEBUG_DONT_USE_COMPATIBILITY_MODE
						UnityEngine.Debug.Log("Use Compatibility Mode For "+type.Name+": "+StringUtils.False);
						#endif
						return false;
					}

					var odinInspectorConfiguredEditorType = odinConfig.DrawingConfig.GetEditorType(type);
					if(odinInspectorConfiguredEditorType == OdinEditorType)
					{
						#if DEV_MODE && DEBUG_USE_COMPATIBILITY_MODE
						UnityEngine.Debug.Log("Use Compatibility Mode For "+type.Name+": "+StringUtils.True);
						#endif
						return true;
					}

					#if DEV_MODE && DEBUG_DONT_USE_COMPATIBILITY_MODE
					UnityEngine.Debug.Log("Use Compatibility Mode For "+type.Name+": "+StringUtils.False);
					#endif

					#endif
					return false;
				default:
					throw new IndexOutOfRangeException();
			}
		}
	}
}
#endif