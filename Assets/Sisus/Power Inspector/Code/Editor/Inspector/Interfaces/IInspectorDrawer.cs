using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_2019_1_OR_NEWER // UI Toolkit doesn't exist in older versions
using System;
using UnityEngine.UIElements;
#endif

namespace Sisus
{
	/// <summary> Updates the event described by deltaTime. </summary>
	/// <param name="deltaTime"> The delta time. </param>
	public delegate void UpdateEvent(float deltaTime);

	/// <summary>
	/// Interface for a class that handles listening for Unity's events like OnGUI and Update
	/// and calling the appropriate methods in an IInspector.
	/// This is usually an EditorWindow, or - for runtime inspectors - a MonoBehaviour.
	/// </summary>
	public interface IInspectorDrawer
	{
		/// <summary>
		/// Returns instance of IdProvider which can be used to generate unique ID's for controls.
		/// </summary>
		IdProvider IdProvider
		{
			get;
		}

		/// <summary>
		/// True if the inspector drawer ands the inspectors have been setup and are ready for drawing targets.
		/// </summary>
		bool SetupDone { get; }

		/// <summary>
		/// Gets a value indicating whether we can split the drawer into two views.
		/// This should always be false for drawers that don't implment ISplittableInspectorDrawer.
		/// </summary>
		/// <value> True if we can split the view, false if not. </value>
		bool CanSplitView { get; }

		/// <summary> Gets a value indicating whether the inspetor drawer is currently being closed. </summary>
		/// <value> True if is closing, false if not. </value>
		bool NowClosing { get; }
		
		/// <summary> Gets or sets the on update. </summary>
		/// <value> The on update. </value>
		UpdateEvent OnUpdate { get; set; }

		/// <summary> Gets the manager that handles tasks related to selection. </summary>
		/// <value> The manager for selection related tasks. </value>
		ISelectionManager SelectionManager { get; }

		/// <summary>
		/// Gets database containing cached prefix column widths.
		/// </summary>
		PrefixColumnWidths PrefixColumnWidths { get; }

		/// <summary>
		/// Gets a value indicating whether or not to update states of animated objects
		/// during this frame / OnGUI call. E.g. if you want to update animations
		/// only during Repaint or Layout events, this can be used to do it.
		/// </summary>
		/// <value> True if should update animations during  this frame, false if not. </value>
		bool UpdateAnimationsNow { get; }

		/// <summary>
		/// Gets the amoutn of time that has passed since the last frame that UpdateAnimationsNow was true.
		/// Systems using tweening during OnGUI can use this to know when to tween their positions next.
		/// </summary>
		/// <value> The time since the last OnGUI call where UpdateAnimationsNow was true. </value>
		float AnimationDeltaTime { get; }

		/// <summary> Gets the manager. </summary>
		/// <value> The manager. </value>
		IInspectorManager Manager { get; }

		/// <summary> Gets the main view. </summary>
		/// <value> The main view. </value>
		IInspector MainView { get; }

		/// <summary> Gets the position. </summary>
		/// <value> The position. </value>
		Rect position { get; }

		/// <summary>
		/// Returns true if cursor is currently over the window's viewport
		/// </summary>
		/// <value>
		/// True if mouse is over, false if not.
		/// </value>
		bool MouseIsOver { get; }

		/// <summary> Gets a value indicating whether this object has focus. </summary>
		/// <value> True if this object has focus, false if not. </value>
		bool HasFocus { get; }

		/// <summary> Gets the UnityEngine.Object that holds the InspectorDrawer's state.
		/// E.g. an EditorWindow, a ScriptableObject or a Component. </summary>
		/// <value> The unity object. </value>
		Object UnityObject { get; }

		/// <summary>
		/// Returns value indicating what windows the inspector drawer is targeting
		/// when it comes to reacting to selection changes and things like that.
		/// </summary>
		InspectorTargetingMode InspectorTargetingMode { get; }

		/// <summary>
		/// Handles creating and caching of all editors used inside the inspector drawer.
		/// </summary>
		Editors Editors { get; }

		/// <summary> Refreshes the view, rebuilding the layout and repainting it. </summary>
		void RefreshView();

		/// <summary> Repaints this GUI after changes were made to it. </summary>
		void Repaint();

		/// <summary>
		/// If IInspectorDrawer is an EditorWindow, then sends an Event to its GUIView.
		/// E.g. SendEvent(EditorGUIUtility.CommandEvent("Paste").
		/// </summary>
		/// <param name="e"> The Event to send. E.g. EditorGUIUtility.CommandEvent("Paste")</param>
		bool SendEvent(Event e);

		/// <summary> Give this window focus / bring it to front. </summary>
		void FocusWindow();

		/// <summary>
		/// React to "Closes Tab" command by closing active inspector view.
		/// </summary>
		void CloseTab();

		/// <summary>
		/// Sends a message to the user of the Inspector.
		/// In the editor the Console can be used.
		/// </summary>
		/// <param name="message"> The message to show. </param>
		/// <param name="context"> (Optional) The UnityEngine.Object context for the message. </param>
		/// <param name="messageType"> (Optional) Type of the message. </param>
		///  <param name="alsoLogToConsole"> (Optional) If true message will also be logged to console, if false it will only be shown as a popup message. </param>
		void Message(GUIContent message, Object context = null, MessageType messageType = MessageType.Info, bool alsoLogToConsole = true);

		/// <summary>
		/// Called when the inspected targets of an inspector of the drawer are changed.
		/// </summary>
		/// <param name="inspected"> New inspected targets. </param>
		/// <param name="drawerGroup"> Drawer for inspected targets. </param>
		void OnInspectedChanged(Object[] inspected, DrawerGroup drawerGroup);

		/// <summary>
		/// Called when a keyboard key is pressed down for all <see cref="IInspectorDrawer"/> instances.
		/// This is called first for the mouseovered instance (if any), then for the selected instance (if any)
		/// and then for other instances (if any).
		/// </summary>
		/// <param name="e"> Event information including <see cref="Event.keyCode"/>. </param>
		void OnKeyDown(Event e);

		/// <summary>
		/// Execute a special command (eg. copy & paste).
		/// "Copy", "Cut", "Paste", "Delete", "FrameSelected", "Duplicate", "SelectAll" and so on.
		/// </summary>
		/// <param name="e"> Event information including <see cref="Event.commandName"/>. </param>
		void OnExecuteCommand(Event e);

		/// <summary>
		/// Validates a special command (e.g. copy & paste).
		/// 
		/// "Copy", "Cut", "Paste", "Delete", "FrameSelected", "Duplicate", "SelectAll" and so on.
		/// </summary>
		/// <param name="e"> Event information including <see cref="Event.commandName"/>. </param>
		void OnValidateCommand(Event e);

		#if UNITY_2019_1_OR_NEWER
		void AddElement(VisualElement element, IDrawer drawer);
		void RemoveElement(VisualElement element, IDrawer drawer);
		#endif
	}
}