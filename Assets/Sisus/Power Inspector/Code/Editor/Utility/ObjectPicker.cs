//#define DEBUG_ENABLED

using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

namespace Sisus
{
	public delegate void ObjectPickerUpdatedCallback(Object initialObject, Object currentObject);
	public delegate void ObjectPickerClosedCallback(Object initialObject, Object currentObject, bool wasCancelled);

	[InitializeOnLoad]
	public static class ObjectPicker
	{
		public static Action OnOpened;
		public static ObjectPickerUpdatedCallback OnUpdated;
		public static ObjectPickerClosedCallback OnClosed;

		public static Object InitialObject;
		public static Object CurrentObject;
		public static bool wasCancelled;
		
		private static bool initialized;
		private static bool isOpen;
		
		private static Type objectSelectorType;
		private static MethodInfo getInitialObjectMethod;
		private static EditorWindow objectSelectorWindow;

		public static bool IsOpen
		{
			get
			{
				return isOpen;
			}
		}

		private static Type ObjectSelectorType
		{
			get
			{
				if(objectSelectorType == null)
				{
					objectSelectorType = Types.GetInternalEditorType("UnityEditor.ObjectSelector");
				}
				return objectSelectorType;
			}
		}

		private static MethodInfo GetInitialObjectMethod
		{
			get
			{
				if(getInitialObjectMethod == null)
				{
					getInitialObjectMethod = ObjectSelectorType.GetMethod("GetInitialObject", BindingFlags.Static | BindingFlags.Public);
				}
				return getInitialObjectMethod;
			}
		}

		/// <summary>
		/// this is initialized in the editor on load due to the usage of the InitializeOnLoad attribute
		/// </summary>
		static ObjectPicker()
		{
			EditorApplication.delayCall += SubscribeForOnBeginOnGUIEvent;
		}
		
		private static void SubscribeForOnBeginOnGUIEvent()
		{
			if(!ApplicationUtility.IsReady())
			{
				EditorApplication.delayCall += SubscribeForOnBeginOnGUIEvent;
				return;
			}

			if(!initialized)
			{
				initialized = true;
				DrawGUI.OnEveryBeginOnGUI(DetectObjectPickerOpen, false);
				InspectorUtility.OnExecuteCommand += OnExecuteCommand;
			}
		}
		
		private static void DetectObjectPickerOpen()
		{
			if(Event.current.type != EventType.Layout)
			{
				return;
			}

			if(!isOpen)
			{
				if(EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType() == ObjectSelectorType)
				{
					#if DEV_MODE
					Debug.Log("Detected Object Picker opened with event="+StringUtils.ToString(Event.current)+" with InspectorUtility.ActiveManager.MouseDownInfo.IsClick="+StringUtils.ToColorizedString(InspectorUtility.ActiveManager.MouseDownInfo.IsClick));
					#endif
					HandleOnOpened();
				}
			}
			else if(EditorWindow.focusedWindow != GetObjectSelectorWindow())
			{
				HandleOnClosed();
			}
		}
		
		private static EditorWindow GetObjectSelectorWindow()
		{
			if(objectSelectorWindow == null)
			{
				objectSelectorWindow = EditorWindow.focusedWindow;
				if(objectSelectorWindow != null)
				{
					if(objectSelectorWindow.GetType() != ObjectSelectorType)
					{
						objectSelectorWindow = null;
					}
				}
			}
			return objectSelectorWindow;
		}

		private static void OnExecuteCommand(IInspector commandRecipient, string commandName)
		{
			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("ObjectPicker.OnExecuteCommand: "+commandName);
			#endif

			switch(commandName)
			{
				case "ObjectSelectorOpened":
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(!isOpen, "Command ObjectSelectorOpened detected with isOpen="+StringUtils.True);
					#endif
					HandleOnOpened();
					return;
				case "ObjectSelectorUpdated":
					if(!isOpen)
					{
						#if DEV_MODE
						Debug.LogWarning("Command ObjectSelectorUpdated detected with isOpen="+StringUtils.False);
						#endif
						HandleOnOpened();
					}

					UpdateCurrentObject();

					#if DEV_MODE && DEBUG_ENABLED
					Debug.Log("ObjectPicker - ObjectSelectorUpdated with InitialObject="+StringUtils.ToColorizedString(InitialObject)+", CurrentObject="+StringUtils.ToColorizedString(CurrentObject));
					#endif

					if(OnUpdated != null)
					{
						OnUpdated(InitialObject, CurrentObject);
					}
					return;
				case "ObjectSelectorClosed":
					#if DEV_MODE && DEBUG_ENABLED
					Debug.Log("Command ObjectSelectorClosed detected with isOpen="+StringUtils.False);
					#endif
					if(isOpen)
					{
						HandleOnClosed();
					}
					return;
			}
		}

		private static void UpdateCurrentObject()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(isOpen, "ObjectPicker.UpdateCurrentObject called with isOpen="+StringUtils.False);
			#endif

			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("ObjectPicker.UpdateCurrentObject called with CurrentObject="+ StringUtils.ToColorizedString(CurrentObject) + " isOpen =" + StringUtils.False+ ", wasCancelled="+ StringUtils.ToColorizedString(wasCancelled)+", Event="+StringUtils.ToString(Event.current)+", LastInputEvent="+ StringUtils.ToString(DrawGUI.LastInputEventType));
			#endif

			if(wasCancelled)
			{
				CurrentObject = InitialObject;
				return;
			}

			var selectorWindow = GetObjectSelectorWindow();
			if(selectorWindow != null)
			{
				CurrentObject = EditorGUIUtility.GetObjectPickerObject();
			}
			#if DEV_MODE && PI_ASSERTATIONS
			else { Debug.LogWarning("UpdateCurrentObject called but ObjectSelectorWindow was not found. Won't update the value."); }
			#endif
		}
		
		private static void HandleOnOpened()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!isOpen, "ObjectPicker.HandleOnOpened called with isOpen="+StringUtils.True);
			#endif

			isOpen = true;
			wasCancelled = false;

			var selectorWindow = GetObjectSelectorWindow();
			if(selectorWindow != null)
			{
				InitialObject = GetInitialObjectMethod.Invoke(selectorWindow) as Object;
				UpdateCurrentObject();
			}
			#if DEV_MODE && PI_ASSERTATIONS
			else { Debug.LogWarning("ObjectPicker.HandleOnOpened called but GetObjectSelectorWindow() returned "+StringUtils.Null); }
			#endif
			
			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("ObjectPicker - "+StringUtils.Green("Opened")+" with InitialObject="+StringUtils.ToColorizedString(InitialObject)+", CurrentObject="+StringUtils.ToColorizedString(CurrentObject));
			#endif

			if(OnOpened != null)
			{
				OnOpened();
			}
		}

		private static void HandleOnClosed()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(isOpen, "ObjectPicker.HandleOnClosed called with isOpen="+StringUtils.False);
			#endif

			var lastSelectedWas = CurrentObject;
			UpdateCurrentObject();
			if(CurrentObject != lastSelectedWas)
			{
				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log("ObjectPicker - "+StringUtils.Red("Cancelled")+" with InitialObject="+StringUtils.ToColorizedString(InitialObject)+", lastSelected="+StringUtils.ToColorizedString(lastSelectedWas)+ ", CurrentObject=" + StringUtils.ToColorizedString(CurrentObject) + " Event=" + StringUtils.ToString(Event.current));
				#endif
				CurrentObject = null;
				wasCancelled = true;
			}

			isOpen = false;

			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("ObjectPicker - "+StringUtils.Red("Closed")+" with InitialObject="+StringUtils.ToColorizedString(InitialObject)+", CurrentObject="+StringUtils.ToColorizedString(CurrentObject)+", Event="+StringUtils.ToString(Event.current)+", keyCode="+Event.current.keyCode);
			#endif

			if(OnClosed != null)
			{
				OnClosed(InitialObject, CurrentObject, wasCancelled);
			}
			
			InitialObject = null;
			CurrentObject = null;
		}
	}
}