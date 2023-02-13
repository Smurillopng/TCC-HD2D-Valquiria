#define USE_IL

//#define DEBUG_GENERATE_ITEMS
#define DEBUG_FAIL_GENERATE_ITEMS
//#define DEBUG_SETUP_TIME

using System;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using Sisus.Attributes;
using JetBrains.Annotations;

#if CSHARP_7_3_OR_NEWER && USE_IL
using Sisus.Vexe.FastReflection;
#endif

namespace Sisus
{
	[InitializeOnLoad]
	public static class HeaderToolbarItemAttributeUtility
	{
		private static readonly Dictionary<Type, List<Func<HeaderPartDrawer>>> contextMenuMethodsByType = new Dictionary<Type, List<Func<HeaderPartDrawer>>>();

		private static volatile bool IsReady;

		static HeaderToolbarItemAttributeUtility()
		{
			EditorApplication.delayCall += SetupWhenReady;
		}

		private static void SetupWhenReady()
		{
			ThreadPool.QueueUserWorkItem(SetupThreaded);
		}

		private static void SetupThreaded(object threadTaskId)
		{
			#if DEV_MODE && DEBUG_SETUP_TIME
			var timer = new ExecutionTimeLogger();
			timer.Start("HeaderToolbarItemAttributeUtility.SetupThreaded");
			#endif

			foreach(var type in TypeExtensions.GetAllTypesThreadSafe(typeof(HeaderToolbarItemAttribute).Assembly, true, true, true))
			{
				var staticMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				for(int m = staticMethods.Length - 1; m >= 0; m--)
				{
					var method = staticMethods[m];
					var toolbarItemAttributes = method.GetCustomAttributes(typeof(HeaderToolbarItemAttribute), false);
					foreach(var toolbarItemAttribute in toolbarItemAttributes)
					{
						var toolbarItem = (HeaderToolbarItemAttribute)toolbarItemAttribute;
						var targetType = toolbarItem.targetType;
						if(targetType == null)
						{
							#if DEV_MODE && DEBUG_FAIL_GENERATE_ITEMS
							UnityEngine.Debug.LogWarning("HeaderToolbarItemAttribute on class "+type.FullName+" targetType was null.");
							#endif
						}

						#if DEV_MODE && PI_ASSERTATIONS
						UnityEngine.Debug.Assert(Types.UnityObject.IsAssignableFrom(targetType));
						#endif

						#if CSHARP_7_3_OR_NEWER && USE_IL
						Func<HeaderPartDrawer> getItemDrawer = ()=>method.DelegateForCall<object, HeaderPartDrawer>().Invoke(null, null);
						#else
						Func<HeaderPartDrawer> getItemDrawer = ()=>method.Invoke(null, null) as HeaderPartDrawer;
						#endif

						RegisterHeaderToolbarItemForExactType(targetType, getItemDrawer);
						foreach(var extendingType in targetType.GetExtendingUnityObjectTypes(true))
						{
							RegisterHeaderToolbarItemForExactType(extendingType, getItemDrawer);
						}
					}
				}
			}

			#if DEV_MODE && DEBUG_SETUP_TIME
			timer.FinishAndLogResults();
			#endif

			IsReady = true;
		}

		public static void GetAdditionalHeaderToolbarItems([NotNull]Type targetType, [NotNull]HeaderParts addTo)
		{
			if(!IsReady)
			{
				return;
			}

			List<Func<HeaderPartDrawer>> getItemDrawers;
			if(!contextMenuMethodsByType.TryGetValue(targetType, out getItemDrawers))
			{
				return;
			}
			
			for(int n = 0, count = getItemDrawers.Count; n < count; n++)
			{
				addTo.Add(getItemDrawers[n]());
			}
		}

		private static void RegisterHeaderToolbarItemForExactType(Type targetType, Func<HeaderPartDrawer> getItemDrawer)
		{
			#if DEV_MODE && DEBUG_GENERATE_ITEMS
			UnityEngine.Debug.Log("RegisterHeaderToolbarItemForExactType(" + targetType.Name + "): " + StringUtils.ToString(getItemDrawer));
			#endif

			List<Func<HeaderPartDrawer>> getItemDrawers;
			if(!contextMenuMethodsByType.TryGetValue(targetType, out getItemDrawers))
			{
				getItemDrawers = new List<Func<HeaderPartDrawer>>(1);
				contextMenuMethodsByType.Add(targetType, getItemDrawers);
			}
			getItemDrawers.Add(getItemDrawer);
		}
	}
}