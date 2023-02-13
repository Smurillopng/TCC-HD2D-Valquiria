using System;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Class for representing two different keyboard shortcuts for triggering the same action.
	/// </summary>
	[Serializable]
	public struct KeyConfig2
	{
		[SerializeField]
		private KeyConfig main;
		[SerializeField]
		private KeyConfig secondary;

		public KeyConfig2(KeyConfig setMain, KeyConfig setSecondary)
		{
			main = setMain;
			secondary = setSecondary;
		}

		/// <summary>
		/// Detects whether event KeyCode and Modifiers match the KeyConfig.
		/// 
		/// Will NOT detect if EventType matches EventType.KeyDown etc.
		/// </summary>
		public bool DetectInput(Event e)
		{
			return main.DetectInput(e) || secondary.DetectInput(e);
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
	}
}