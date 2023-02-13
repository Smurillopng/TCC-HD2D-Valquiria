#define DEBUG_SET_KEYBOARD_CONTROL
#define DEBUG_SET_HOT_CONTROL

using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Class that allows getting and setting data related keyboard-focused inspector control
	/// </summary>
	public static class KeyboardControlUtility
	{
		#if UNITY_EDITOR
		public static readonly KeyboardControlInfo Info = new KeyboardControlInfo();
		#endif

		private static int setKeyboardControl;
		private static int setKeyboardControlTimes;

		public static int KeyboardControl
		{
			get
			{
				return GUIUtility.keyboardControl;
			}

			set
			{
				#if DEV_MODE
				Debug.Assert(!ObjectPicker.IsOpen);
				Debug.Assert(!ColorPicker.UsingEyeDropper);
				#endif

				if(GUIUtility.keyboardControl != value)
				{
					if(GUIUtility.hotControl != 0)
					{
						#if DEV_MODE
						Debug.LogError("KeyboardControl = " + value + " called with GUIUtility.hotControl="+ GUIUtility.hotControl);
						#endif
						return;
					}

					#if DEV_MODE && DEBUG_SET_KEYBOARD_CONTROL
					Debug.Log("KeyboardControl = "+value + " (was " + KeyboardControl + ") with Event="+(Event.current == null ? "null" : Event.current.rawType.ToString()));
					#endif
				
					GUIUtility.keyboardControl = value;
					setKeyboardControl = value;

					if(InspectorUtility.ActiveInspector != null)
					{
						InspectorUtility.ActiveInspector.RefreshView();
					}
				}
			}
		}

		public static int JustClickedControl
		{
			get
			{
				return GUIUtility.hotControl;
			}

			set
			{
				#if DEV_MODE
				Debug.Assert(!ObjectPicker.IsOpen);
				#endif

				if(GUIUtility.hotControl != value)
				{
					#if DEV_MODE && DEBUG_SET_HOT_CONTROL
					Debug.Log("JustClickedControl = "+value + " (was "+JustClickedControl+")");
					#endif
				
					GUIUtility.hotControl = value;
				}
			}
		}

		/// <summary>
		/// Sets KeyboardControl to controlId and repeats it the set number
		/// of times each layout. This is useful when overriding Unity's
		/// built-in control focus system, since it can get applied after
		/// a significant delay
		/// </summary>
		public static void SetKeyboardControl(int controlId, int repeatTimes = 3)
		{
			#if DEV_MODE && DEBUG_SET_KEYBOARD_CONTROL
			Debug.Log("SetKeyboardControl(" + controlId + ", repeat="+ repeatTimes+")");
			#endif

			setKeyboardControl = controlId;
			setKeyboardControlTimes = repeatTimes;
			ApplySetKeyboardControlStep();
		}

		private static void ApplySetKeyboardControlStep()
		{
			if(GUIUtility.hotControl == 0)
			{
				KeyboardControl = setKeyboardControl;
			}

			setKeyboardControlTimes--;
			if(setKeyboardControlTimes > 0)
			{
				DrawGUI.OnNextBeginOnGUI(ApplySetKeyboardControlStep, true);
			}
			else if(InspectorUtility.ActiveInspector != null)
			{
				InspectorUtility.ActiveInspector.RefreshView();
			}
		}
	}
}