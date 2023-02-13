#if UNITY_EDITOR
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Contains extensions methods for EditorWindows, utilizing reflection to call some internal fields, properties and methods.
	/// While this is an Editor only class, it is not placed inside an Editor folder, because some still that are not Editor-only
	/// still use it, just wrapped inside EDITOR preprocessor directives.
	/// </summary>
	public static class EditorWindowExtensions
	{
		/// <summary> Checks if EditorWindow is docked. </summary>
		/// <param name="window"> The window to test. </param>
		/// <returns> True if docked, false if not. </returns>
		public static bool IsDocked([NotNull]this EditorWindow window)
		{
			#if UNITY_2020_1_OR_NEWER
			return window.docked;
			#else
			var docked = typeof(EditorWindow).GetProperty("docked", BindingFlags.Instance | BindingFlags.NonPublic);
			return (bool)docked.GetValue(window, null);
			#endif
		}

		public static void AddTab([NotNull]this EditorWindow window, Type editorWindowType)
		{
			var dockArea = window.ParentHostView();
			var dockAreaType = Types.GetInternalEditorType("UnityEditor.DockArea");
			var addTabMethod = dockAreaType.GetMethod("AddTabToHere", BindingFlags.NonPublic | BindingFlags.Instance);
			addTabMethod.Invoke(dockArea, ArrayExtensions.TempObjectArray(editorWindowType));
		}

		public static bool IsVisible([NotNull]this EditorWindow window)
		{
			if(!window.IsDocked())
			{
				return true;
			}

			var hostView = window.ParentHostView();
			if(hostView == null)
			{
				return true;
			}
			var hostViewType = Types.GetInternalEditorType("UnityEditor.HostView");
			var actualViewProperty = hostViewType.GetProperty("actualView", BindingFlags.NonPublic | BindingFlags.Instance);
			if(actualViewProperty == null)
			{
				#if DEV_MODE
				Debug.LogError("Failed to find actualView property of HostView");
				#endif
				return true;
			}
			var actualView = actualViewProperty.GetValue(hostView, null) as EditorWindow;
			return actualView == window;
		}

		/// <summary>
		/// Sets EditorWindow as the active tab in its docked area.
		/// If EditorWindow is not docked or it already is the active
		/// tab, then this method will have no effect.
		/// </summary>
		/// <param name="window"> The window to act on. </param>
		public static void SetAsSelectedTab(this EditorWindow window)
		{
			if(!window.IsDocked())
			{
				return;
			}

			var hostView = window.ParentHostView();
			if(hostView == null)
			{
				return;
			}
			var hostViewType = Types.GetInternalEditorType("UnityEditor.HostView");
			var actualViewProperty = hostViewType.GetProperty("actualView", BindingFlags.NonPublic | BindingFlags.Instance);
			if(actualViewProperty == null)
			{
				#if DEV_MODE
				Debug.LogError("Failed to find actualView property of HostView");
				#endif
				return;
			}

			var actualView = actualViewProperty.GetValue(hostView, null) as EditorWindow;
			if(actualView == window)
			{
				return;
			}
			
			var dockAreaType = Types.GetInternalEditorType("UnityEditor.DockArea");
			var panesField = dockAreaType.GetField("m_Panes", BindingFlags.NonPublic | BindingFlags.Instance);
			var panes = panesField.GetValue(hostView) as List<EditorWindow>;
			int selectedIndex = panes.IndexOf(window);
			var selectedProperty = dockAreaType.GetProperty("selected", BindingFlags.Public | BindingFlags.Instance);
			selectedProperty.SetValue(hostView, selectedIndex, null);
		}

		/// <summary> Gets the parent host view of the EditorWindow from the internal "m_Parent" field. </summary>
		/// <param name="window"> The EditorWindow whose host view to get. </param>
		/// <returns> Parent of type HostView. </returns>
		[CanBeNull]
		public static object ParentHostView([NotNull]this EditorWindow window)
		{
			var parentField = window.GetType().GetField("m_Parent", BindingFlags.NonPublic | BindingFlags.Instance);
			return parentField.GetValue(window);
		}

		/// <summary> Dock EditorWindow as tab on the parent host view of the existing EditorWindow. </summary>
		/// <param name="window"> The window whose host view to dock on. </param>
		/// <param name="tab"> The EditorWindow to dock. </param>
		public static void AddTab([NotNull]this EditorWindow window, EditorWindow tab)
		{
			var dockArea = window.ParentHostView();

			if(dockArea == null)
			{
				#if DEV_MODE
				Debug.LogWarning("m_Parent of target window "+ window.GetType().Name+" (\""+window.name+"\") was null; can't add "+tab.GetType().Name+ " (\"" + tab.name + "\") as tab. You might want to revert Layout to factory settings.");
				#endif
				return;
			}
			
			var dockAreaType = Types.GetInternalEditorType("UnityEditor.DockArea");
			
			var parentHostViewType = dockArea.GetType();
			if(parentHostViewType != dockAreaType && !parentHostViewType.IsSubclassOf(dockAreaType))
			{
				#if DEV_MODE
				Debug.LogWarning("m_Parent of target window "+ window.GetType().Name+" (\""+window.name+"\") was not a DockArea (but "+parentHostViewType.Name+"); can't add "+tab.GetType().Name+ " (\"" + tab.name + "\") as tab.");
				#endif
				return;
			}

			#if UNITY_2018_3_OR_NEWER
			var types = ArrayPool<Type>.Create(2);
			types[1] = Types.Bool;
			#else
			var types = ArrayPool<Type>.Create(1);
			#endif
			types[0] = typeof(EditorWindow);

			var addTabMethod = dockAreaType.GetMethod("AddTab", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, types, null);
			ArrayPool<Type>.Dispose(ref types);
			#if UNITY_2018_3_OR_NEWER
			var parameters = ArrayPool<object>.Create(2);
			parameters[1] = true;
			#else
			var parameters = ArrayPool<object>.Create(1);
			#endif
			parameters[0] = tab;

			addTabMethod.Invoke(dockArea, parameters);
		}

		public static EditorWindow SelectNextTab([NotNull]this EditorWindow window)
		{
			var dockAreaType = Types.GetInternalEditorType("UnityEditor.DockArea");
			var m_ParentField = window.GetType().GetField("m_Parent", BindingFlags.NonPublic | BindingFlags.Instance);
			var dockArea = m_ParentField.GetValue(window);

			var panesField = dockAreaType.GetField("m_Panes", BindingFlags.NonPublic | BindingFlags.Instance);
			var panes = panesField.GetValue(dockArea) as List<EditorWindow>;
			int count = panes.Count;
			if(count <= 1)
			{
				return window;
			}
			int index = panes.IndexOf(window) + 1;
			if(index >= count)
			{
				index = 0;
			}
			var select = panes[index];
			select.Focus();
			return select;
		}

		public static EditorWindow SelectPreviousTab([NotNull]this EditorWindow window)
		{
			var dockAreaType = Types.GetInternalEditorType("UnityEditor.DockArea");
			var m_ParentField = window.GetType().GetField("m_Parent", BindingFlags.NonPublic | BindingFlags.Instance);
			var dockArea = m_ParentField.GetValue(window);

			var panesField = dockAreaType.GetField("m_Panes", BindingFlags.NonPublic | BindingFlags.Instance);
			var panes = panesField.GetValue(dockArea) as List<EditorWindow>;
			int count = panes.Count;
			if(count <= 1)
			{
				return window;
			}
			int index = panes.IndexOf(window) - 1;
			if(index < 0)
			{
				index = count - 1;
			}
			var select = panes[index];
			select.Focus();
			return select;
		}

		/// <summary>
		/// Finds all EditorWindows by given type, and if none of the found instances are
		/// visible (undocked or the active tab in their dock area), then sets on of the
		/// instances as the selected tab in their dock area.
		/// </summary>
		/// <param name="editorWindowType"> Type of the EditorWindow that should be set as selected tab. </param>
		/// <param name="disregardIfHasSameHostView">
		/// (Optional) Instances of EditorWindow will not be set as selected tab if they are contained within the same dock area with this EditorWindow.
		/// </param>
		/// <returns>
		/// True if an EditorWindow instance of type already was visible or was set visible by this method.
		/// False if no instances of EditorWindow were found or could not set any of found instances visible.
		/// </returns>
		public static bool MakeAtLeastOneInstanceSelectedTab(Type editorWindowType, EditorWindow disregardIfHasSameHostView = null)
		{
			var windows = GetExistingWindows(editorWindowType);
			if(windows.Length == 0)
			{
				return false;
			}

			foreach(var window in windows)
			{
				if(window.IsVisible())
				{
					return true;
				}
			}

			var skipIfHostView = disregardIfHasSameHostView != null ? disregardIfHasSameHostView.ParentHostView() as ScriptableObject : null;
			foreach(var window in windows)
			{
				var hostView = window.ParentHostView() as ScriptableObject;
				if(hostView != skipIfHostView)
				{
					window.SetAsSelectedTab();
					return true;
				}
			}

			return false;
		}

		public static EditorWindow GetExistingWindow(Type editorWindowType)
		{
			var instances = UnityEngine.Resources.FindObjectsOfTypeAll(editorWindowType);
			return instances.Length > 0 ? instances[0] as EditorWindow : null;
		}

		public static EditorWindow[] GetExistingWindows(Type editorWindowType)
		{
			return UnityEngine.Resources.FindObjectsOfTypeAll(editorWindowType) as EditorWindow[];
		}
	}
}
#endif