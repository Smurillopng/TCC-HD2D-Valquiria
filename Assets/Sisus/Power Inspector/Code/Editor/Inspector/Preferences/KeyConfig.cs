#define DEBUG_INPUT_DETECTED

using System;
using UnityEngine;
using Sisus.Attributes;
using UnityEngine.Serialization;

namespace Sisus
{
	/// <summary>
	/// Class for representing a keyboard shortcut configuration in the form of a single key and modifiers.
	/// </summary>
	[Serializable]
	public struct KeyConfig
	{
		// Unity treats Alt Gr the same as if Control + Alt were both pressed down on the Windows platorm.
		// So if Alt Gr is currently down, we need to handle ignoring the control modifier.
		private static bool altGrIsDown;

		[SerializeField]
		private KeyCode keyCode;

		[SerializeField, HideInInspector]
		private bool shift;

		[SerializeField, HideInInspector, FormerlySerializedAs("control")]
		private bool controlOrCommand;

		[SerializeField, HideInInspector]
		private bool alt;

		[ShowInInspector]
		private bool Shift
		{
			get
			{
				return shift;
			}

			set
			{
				shift = value;
			}
		}

		[ShowInInspector]
		#if UNITY_EDITOR_OSX
		private bool Command
		#else
		private bool Control
		#endif
		{
			get
			{
				return controlOrCommand;
			}

			set
			{
				controlOrCommand = value;
			}
		}

		[ShowInInspector]
		private bool Alt
		{
			get
			{
				return alt;
			}

			set
			{
				alt = value;
			}
		}

		public KeyConfig(KeyCode keyCode)
		{
			this.keyCode = keyCode;
			shift = false;
			controlOrCommand = false;
			alt = false;
		}

		public KeyConfig(KeyCode keyCode, EventModifiers modifiers)
		{
			this.keyCode = keyCode;

			shift = (modifiers & EventModifiers.Shift) != EventModifiers.None;
			controlOrCommand = (modifiers & EventModifiers.Control) != EventModifiers.None || (modifiers & EventModifiers.Command) != EventModifiers.None;
			alt = (modifiers & EventModifiers.Alt) != EventModifiers.None;
		}

		public KeyConfig(KeyCode keyCode, bool shift, bool controlOrCommand, bool alt)
		{
			this.keyCode = keyCode;

			this.shift = shift;
			this.controlOrCommand = controlOrCommand;
			this.alt = alt;
		}

		/// <summary>
		/// Call this during key down events to support AltGr in shortcuts
		/// </summary>
		public static void OnAltGrDown()
		{
			#if DEV_MODE
			Debug.Log("AltGrIsDown = true");
			#endif
			altGrIsDown = true;
		}

		/// <summary>
		/// Call this during key down events to support AltGr in shortcuts
		/// </summary>
		public static void OnAltGrUp()
		{
			#if DEV_MODE
			Debug.Log("AltGrIsDown = false");
			#endif
			altGrIsDown = false;
		}

		/// <summary>
		/// Detects whether event KeyCode and Modifiers match the KeyConfig.
		/// 
		/// Will NOT detect if EventType matches EventType.KeyDown etc.
		/// </summary>
		public bool DetectInput(Event e)
		{
			if(e.keyCode == keyCode && keyCode != KeyCode.None)
			{
				if(alt != e.alt)
				{
					#if DEV_MODE
					Debug.Assert(alt != altGrIsDown);
					#endif
					return false;
				}

				#if UNITY_EDITOR_OSX
				if(controlOrCommand != e.command)
				#else
				if(controlOrCommand != e.control)
				#endif
				{
					// Unity treats Alt Gr the same as if Control + Alt were both pressed down on the Windows platorm.
					// So if Alt Gr is currently down, ignore the control modifier.
					if(!altGrIsDown || controlOrCommand || !alt)
					{
						return false;
					}
				}
				else if(altGrIsDown && alt && controlOrCommand)
				{
					#if DEV_MODE
					Debug.Log(ToString()+".DetectInput - returning False because Alt Gr is down.");
					#endif
					return false;
				}

				if(shift != e.shift)
				{
					return false;
				}

				#if DEV_MODE && DEBUG_INPUT_DETECTED
				Debug.Log("Shortcut Detected: " + ToString()+" with DrawGUI.EditingTextField="+DrawGUI.EditingTextField+ ", EditorGUIUtility.editingTextField=" + UnityEditor.EditorGUIUtility.editingTextField+", modifiers="+e.modifiers);
				#endif
				return true;
			}
			return false;
		}

		public bool DetectAndUseInput(Event e)
		{
			if(DetectInput(e))
			{
				DrawGUI.Use(e);
				return true;
			}
			return false;
		}

		public bool DetectInput(Event e, bool useEvent)
		{
			if(useEvent)
			{
				return DetectAndUseInput(e);
			}
			return DetectInput(e);
		}

		public bool DetectKeyDown(Event e, bool useRawType = false)
		{
			if(useRawType)
			{
				return e.rawType == EventType.KeyDown;
			}
			return e.type == EventType.KeyDown;
		}

		public bool DetectInputAndKeyDown(Event e, bool useEvent, bool useRawType = false)
		{
			return DetectKeyDown(e, useRawType) && DetectInput(e, useEvent);
		}

		public override string ToString()
		{
			var s = keyCode.ToString();
			if(alt)
			{
				s = StringUtils.Concat("alt+", s);
			}
			if(controlOrCommand)
			{
				s = StringUtils.Concat("ctrl+", s);
			}
			if(shift)
			{
				s = StringUtils.Concat("shift+", s);
			}
			return s;
		}
	}
}