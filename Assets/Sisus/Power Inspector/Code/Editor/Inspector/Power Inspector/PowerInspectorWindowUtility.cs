using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public delegate IInspectorDrawer CreateNewInspectorDrawer(Object[] inspect, bool lockView = false, bool addAsTab = true, Vector2 minSize = default(Vector2));

	/// <summary>
	/// Utility class for performing actions related to the Power Inspector window,
	/// accessible even for scripts outside of Editor script folders.
	/// </summary>
	public static class PowerInspectorWindowUtility
	{
		#if UNITY_EDITOR
		[NotNull]
		private static CreateNewInspectorDrawer createNewWindow;
		#endif

		#if UNITY_EDITOR
		public static void RegisterCreateNewWindowDelegate([NotNull]CreateNewInspectorDrawer createNewWindowDelegate)
		{
			createNewWindow = createNewWindowDelegate;
		}
		#endif

		#if UNITY_EDITOR
		[NotNull]
		public static void OpenNewWindow()
		{
			DrawGUI.ExecuteMenuItem(PowerInspectorMenuItemPaths.NewWindow);
		}
		#endif

		#if UNITY_EDITOR
		[NotNull]
		public static IInspectorDrawer OpenNewWindow(Object inspect, bool lockView = false, bool addAsTab = true, Vector2 minSize = default(Vector2))
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Event.current != null, "Shouldn't call PowerInspectorWindow.OpenNewWindow with Event.current null, because can't perform full Setup immediately. Use OpenWindowNowOrNextLayout instead.");
			#endif

			return createNewWindow(ArrayPool<Object>.CreateWithContent(inspect), lockView, addAsTab, minSize);
		}
		#endif

		#if UNITY_EDITOR
		public static void OpenWindowNowOrNextLayout(Object inspect, bool lockView = false, bool addAsTab = true, Vector2 minSize = default(Vector2))
		{
			var targets = ArrayPool<Object>.CreateWithContent(inspect);

			if(Event.current == null)
			{
				#if DEV_MODE
				Debug.LogWarning("Delaying opening of Power Inspector window"+ (inspect != null ? " for target " + StringUtils.ToString(inspect) : "") +" until next layout event.");
				#endif
				InspectorManager.Instance().OnNextLayout(()=> createNewWindow(targets, lockView, addAsTab, minSize));
				return;
			}

			createNewWindow(targets, lockView, addAsTab, minSize);
		}
		#endif

		#if UNITY_EDITOR
		[NotNull]
		public static IInspectorDrawer OpenNewWindow(Object[] inspect, bool lockView = false, bool addAsTab = true, Vector2 minSize = default(Vector2))
		{
			return createNewWindow(inspect, lockView, addAsTab, minSize);
		}
		#endif

		#if UNITY_EDITOR
		public static void OpenWindowIfNotAlreadyOpen()
		{
			if(GetExistingWindow() == null)
			{
				OpenNewWindow();
			}
		}
		#endif

		[CanBeNull]
		public static PowerInspector GetExistingWindow()
		{
			#if UNITY_EDITOR
			if(InspectorManager.InstanceExists())
			{
				var inspector = InspectorManager.Instance().LastSelectedActiveOrDefaultInspector(InspectorSplittability.IsSplittable) as PowerInspector;
				if(inspector != null)
				{
					return inspector;
				}

				var inspectors = InspectorManager.Instance().ActiveInstances;
				for(int n = inspectors.Count - 1; n >= 0; n--)
				{
					inspector = inspectors[n] as PowerInspector;
					if(inspector != null)
					{
						return inspector;
					}
				}
			}
			#endif
			return null;
		}

		public static bool WindowIsOpen()
		{
			#if UNITY_EDITOR
			if(!InspectorManager.InstanceExists())
			{
				return false;
			}
			
			var inspectors = InspectorManager.Instance().ActiveInstances;
			for(int n = inspectors.Count - 1; n >= 0; n--)
			{
				if(inspectors[n] is PowerInspector && IsPowerInspectorWindow(inspectors[n].InspectorDrawer))
				{
					return true;
				}
			}
			#endif
			return false;
		}

		public static bool IsPowerInspectorWindow(IInspectorDrawer test)
		{
			return string.Equals(test.GetType().Name, "PowerInspectorWindow");
		}
	}
}