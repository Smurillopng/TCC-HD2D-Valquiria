#define DEBUG_ENABLED

using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

using Color = UnityEngine.Color;

namespace Sisus
{
	public delegate void ColorPickerUpdatedCallback(Color initialColor, Color currentColor);
	public delegate void ColorPickerClosedCallback(Color initialColor, Color currentColor, bool wasCancelled);

	[InitializeOnLoad]
	public static class ColorPicker
	{
		public static Action OnOpened;
		public static ColorPickerUpdatedCallback OnUpdated;
		public static ColorPickerClosedCallback OnClosed;
		public static ColorPickerClosedCallback OnStoppedUsingEyeDropper;

		public static Color InitialColor;
		public static Color CurrentColor;
		public static bool wasCancelled;
		
		private static bool initialized;

		private static bool isOpen;
		private static bool usingEyeDropper;

		private static Type colorSelectorType;
		private static FieldInfo getColorField;
		private static PropertyInfo colorMutatorColorProperty;
		private static EditorWindow colorSelectorWindow;
		private static FieldInfo lastPickedColorField;

		public static bool UsingEyeDropper
		{
			get
			{
				return usingEyeDropper;
			}
		}

		public static bool IsOpen
		{
			get
			{
				return isOpen;
			}
		}

		private static Type ColorPickerType
		{
			get
			{
				if(colorSelectorType == null)
				{
					colorSelectorType = Types.GetInternalEditorType("UnityEditor.ColorPicker");
				}
				return colorSelectorType;
			}
		}

		private static FieldInfo LastPickedColorField
		{
			get
			{
				if(lastPickedColorField == null)
				{
					lastPickedColorField = Types.GetInternalEditorType("UnityEditor.EyeDropper").GetField("s_LastPickedColor", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				}
				return lastPickedColorField;
			}
		}

		private static FieldInfo GetColorField
		{
			get
			{
				if(getColorField == null)
				{
					getColorField = ColorPickerType.GetField("m_Color", BindingFlags.Instance | BindingFlags.NonPublic);
				}
				return getColorField;
			}
		}

		/// <summary>
		/// this is initialized in the editor on load due to the usage of the InitializeOnLoad attribute
		/// </summary>
		static ColorPicker()
		{
			EditorApplication.delayCall += SubscribeForOnBeginOnGUIEvent;
		}
		
		private static void SubscribeForOnBeginOnGUIEvent()
		{
			if(!initialized)
			{
				initialized = true;
				DrawGUI.OnEveryBeginOnGUI(OnEveryBeginOnGUI, false);
				InspectorUtility.OnExecuteCommand += OnExecuteCommand;
			}
		}
		
		private static void OnEveryBeginOnGUI()
		{
			if(Event.current.type != EventType.Layout)
			{
				return;
			}

			if(!isOpen)
			{
				if(EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType() == ColorPickerType)
				{
					#if DEV_MODE
					Debug.Log("Detected Color Picker opened with event="+StringUtils.ToString(Event.current)+" with InspectorUtility.ActiveManager.MouseDownInfo.IsClick="+StringUtils.ToColorizedString(InspectorUtility.ActiveManager.MouseDownInfo.IsClick));
					#endif
					HandleOnOpened();
				}
			}
			else if(EditorWindow.focusedWindow != GetColorPickerWindow())
			{
				HandleOnClosed();
			}
		}
		
		private static EditorWindow GetColorPickerWindow()
		{
			if(colorSelectorWindow == null)
			{
				colorSelectorWindow = EditorWindow.focusedWindow;
				if(colorSelectorWindow != null)
				{
					if(colorSelectorWindow.GetType() != ColorPickerType)
					{
						colorSelectorWindow = null;
					}
				}
			}
			return colorSelectorWindow;
		}

		private static void OnExecuteCommand(IInspector commandRecipient, string commandName)
		{
			switch(commandName)
			{
				case "ColorPickerOpened":
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(!isOpen, "Command ColorPickerOpened detected with isOpen="+StringUtils.True);
					#endif
					HandleOnOpened();
					return;
				case "ColorPickerUpdated":
					if(!isOpen)
					{
						#if DEV_MODE
						Debug.LogWarning("Command ColorPickerUpdated detected with isOpen="+StringUtils.False);
						#endif
						HandleOnOpened();
					}

					UpdateCurrentColor();

					#if DEV_MODE && DEBUG_ENABLED
					Debug.Log("ColorPicker - ColorPickerUpdated with InitialColor="+StringUtils.ToColorizedString(InitialColor)+", CurrentColor="+StringUtils.ToColorizedString(CurrentColor));
					#endif

					if(OnUpdated != null)
					{
						OnUpdated(InitialColor, CurrentColor);
					}
					return;
				case "ColorPickerClosed":
					#if DEV_MODE && DEBUG_ENABLED
					Debug.Log("Command ColorPickerClosed detected with isOpen="+StringUtils.False);
					#endif
					if(isOpen)
					{
						HandleOnClosed();
					}
					return;
				case "EyeDropperUpdate":

					CurrentColor = (Color)LastPickedColorField.GetValue(null);

					if(!usingEyeDropper)
					{
						usingEyeDropper = true;
						InitialColor = CurrentColor;
					}

					if(OnUpdated != null)
					{
						OnUpdated(InitialColor, CurrentColor);
					}
					return;
				case "EyeDropperClicked":
					#if DEV_MODE
					Debug.Log("EyeDropperClicked with OnStoppedUsingEyeDropper=" + StringUtils.ToString(OnStoppedUsingEyeDropper)+", InitialColor="+InitialColor+", CurrentColor="+CurrentColor);
					#endif

					HandleStoppedUsingEyeDropper(false);
					return;
				case "EyeDropperCancelled":
					#if DEV_MODE
					Debug.Log("EyeDropperCancelled with OnStoppedUsingEyeDropper="+StringUtils.ToString(OnStoppedUsingEyeDropper)+", InitialColor="+InitialColor+", CurrentColor="+CurrentColor);
					#endif

					HandleStoppedUsingEyeDropper(true);
					return;
			}
		}

		private static void HandleStoppedUsingEyeDropper(bool wasCancelled)
		{
			usingEyeDropper = false;

			if(OnStoppedUsingEyeDropper != null)
			{
				OnStoppedUsingEyeDropper(InitialColor, CurrentColor, wasCancelled);
			}

			// Fix for issue where EditorGUI.ColorField can get stuck in color picker mode.
			// The field will only exit eye dropper mode when it receives the "NewKeyboardFocus" event.
			var inspectorManager = InspectorUtility.ActiveManager;
			if(inspectorManager != null)
			{
				inspectorManager.OnNextLayout(()=>
				{
					var sendEvent = new Event();
					sendEvent.type = EventType.ExecuteCommand;
					sendEvent.commandName = "NewKeyboardFocus";
					var eventWas = Event.current;
					Event.current = sendEvent;
					EditorGUI.ColorField(new Rect(10000f, 10000f, 0f, 0f), Color.white);						
					Event.current = eventWas;
				});
			}
		}

		private static void UpdateCurrentColor()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(isOpen, "ColorPicker.UpdateCurrentColor called with isOpen="+StringUtils.False);
			#endif

			if(wasCancelled)
			{
				CurrentColor = InitialColor;
				return;
			}
			
			object colorMutator;
			if(TryGetColorMutator(out colorMutator))
			{
				if(colorMutatorColorProperty == null)
				{
					colorMutatorColorProperty = colorMutator.GetType().GetProperty("color", BindingFlags.Instance | BindingFlags.Public);

					if(colorMutatorColorProperty == null)
					{
						#if DEV_MODE
						Debug.LogWarning("UpdateCurrentColor called but could not find field \"color\" in type " + colorMutator.GetType().FullName);
						#endif
						return;
					}
				}

				#if DEV_MODE && DEBUG_ENABLED
				var newColor = (Color32)colorMutatorColorProperty.GetValue(colorMutator, null);
				if(CurrentColor != newColor)
                {
					Debug.Log("CurrentColor = "+StringUtils.ToColorizedString(newColor));
                }
				#endif

				CurrentColor = (Color32)colorMutatorColorProperty.GetValue(colorMutator, null);
			}
			#if DEV_MODE
			else { Debug.LogWarning("UpdateCurrentColor called but GetColorPickerWindow() returned "+StringUtils.Null); }
			#endif
		}
		
		private static bool TryGetColorMutator(out object colorMutator)
		{
			var pickerWindow = GetColorPickerWindow();
			if(pickerWindow != null)
			{
				colorMutator = GetColorField.GetValue(pickerWindow);
				return colorMutator != null;
			}
			colorMutator = null;
			return false;
		}

		private static void HandleOnOpened()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!isOpen, "ColorPicker.HandleOnOpened called with isOpen="+StringUtils.True);
			#endif

			isOpen = true;
			wasCancelled = false;

			object colorMutator;
			if(TryGetColorMutator(out colorMutator))
			{
				var originalColorField = colorMutator.GetType().GetField("m_OriginalColor", BindingFlags.Instance | BindingFlags.NonPublic);
				if(originalColorField != null)
				{
					InitialColor = (Color)originalColorField.GetValue(colorMutator);
				}
				#if DEV_MODE
				else { Debug.LogWarning("ColorPicker.HandleOnOpened called but could not find field \"m_OriginalColor\" in type "+colorMutator.GetType().FullName); }
				#endif
				UpdateCurrentColor();
			}
			#if DEV_MODE
			else { Debug.LogWarning("ColorPicker.HandleOnOpened called but GetColorPickerWindow() returned "+StringUtils.Null); }
			#endif
			
			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("ColorPicker - "+StringUtils.Green("Opened")+" with InitialColor="+StringUtils.ToColorizedString(InitialColor)+", CurrentColor="+StringUtils.ToColorizedString(CurrentColor));
			#endif

			if(OnOpened != null)
			{
				OnOpened();
			}
		}

		private static void HandleOnClosed()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(isOpen, "ColorPicker.HandleOnClosed called with isOpen="+StringUtils.False);
			#endif

			var lastSelectedWas = CurrentColor;
			UpdateCurrentColor();

			#if !UNITY_2020_2_OR_NEWER // How the color picker works seems to have changed at some point? Reverting the color when esc is pressed is now handled internally it seems.
			if(CurrentColor != lastSelectedWas)
			{
				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log("ColorPicker - "+StringUtils.Red("Cancelled")+" with InitialColor="+StringUtils.ToColorizedString(InitialColor)+", lastSelected="+StringUtils.ToColorizedString(lastSelectedWas)+", Event="+StringUtils.ToString(Event.current));
				#endif
				CurrentColor = Color.white;
				wasCancelled = true;
			}
			#endif

			isOpen = false;

			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("ColorPicker - "+StringUtils.Red("Closed")+" with InitialColor="+StringUtils.ToColorizedString(InitialColor)+", CurrentColor="+StringUtils.ToColorizedString(CurrentColor)+", Event="+StringUtils.ToString(Event.current)+", keyCode="+Event.current.keyCode);
			#endif

			if(OnClosed != null)
			{
				OnClosed(InitialColor, CurrentColor, wasCancelled);
			}
			
			InitialColor = Color.white;
			CurrentColor = Color.white;
		}
	}
}