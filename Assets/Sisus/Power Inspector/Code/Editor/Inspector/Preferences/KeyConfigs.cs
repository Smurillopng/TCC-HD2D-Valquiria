using System;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class KeyConfigs
	{
		[Header("Basic Field Operations")]
		[Tooltip("Resets Selected Field Value To Default")]
		public KeyConfig reset = new KeyConfig(KeyCode.Backspace);
		[Tooltip("Sets Field To A Random Value")]
		public KeyConfig randomize = new KeyConfig(KeyCode.R, EventModifiers.Control);
		public KeyConfig duplicate = new KeyConfig(KeyCode.D, EventModifiers.Control);

		[Tooltip("Selects Next Field Even If Editing Text Field Is True")]
		private KeyConfig2 nextField = new KeyConfig2(new KeyConfig(KeyCode.I, EventModifiers.Control), new KeyConfig(KeyCode.Tab));
		[Tooltip("Selects Previous Field Even If Editing Text Field Is True")]
		private KeyConfig2 prevField = new KeyConfig2(new KeyConfig(KeyCode.I, EventModifiers.Shift | EventModifiers.Control), new KeyConfig(KeyCode.Tab, EventModifiers.Shift));

		[Header("Change Selection")]
		[Tooltip("Selects Next Field Even If Editing Text Field Is True")]
		public KeyConfig nextFieldUp = new KeyConfig(KeyCode.UpArrow, EventModifiers.Alt);
		[Tooltip("Selects Next Field Even If Editing Text Field Is True")]
		public KeyConfig nextFieldLeft = new KeyConfig(KeyCode.LeftArrow, EventModifiers.Alt);
		[Tooltip("Selects Next Field Even If Editing Text Field Is True")]
		public KeyConfig nextFieldDown = new KeyConfig(KeyCode.DownArrow, EventModifiers.Alt);
		[Tooltip("Selects Next Field Even If Editing Text Field Is True")]
		public KeyConfig nextFieldRight = new KeyConfig(KeyCode.RightArrow, EventModifiers.Alt);		

		[Tooltip("Select Previous Component Shown In The Inspector")]
		public KeyConfig prevComponent = new KeyConfig(KeyCode.UpArrow, EventModifiers.Control);
		[Tooltip("Select Next Component Shown In The Inspector")]
		public KeyConfig nextComponent = new KeyConfig(KeyCode.DownArrow, EventModifiers.Control);

		[Tooltip("Select Previous Component Of Matching Type In The Scene")]
		public KeyConfig prevOfType = new KeyConfig(KeyCode.LeftArrow, EventModifiers.Control);
		[Tooltip("Select Next Component Of Matching Type In The Scene")]
		public KeyConfig nextOfType = new KeyConfig(KeyCode.RightArrow, EventModifiers.Control);

		[Header("Misc")]
		[Tooltip("Collapses selected parent and also all its collapsible children")]
		public KeyConfig collapseRecursively = new KeyConfig(KeyCode.LeftArrow, EventModifiers.Alt);
		[Tooltip("Uncollapses selected parent and also all its uncollapsable children")]
		public KeyConfig uncollapseRecursively = new KeyConfig(KeyCode.RightArrow, EventModifiers.Alt);

		public KeyConfig addComponent = new KeyConfig(KeyCode.T, EventModifiers.Control);

		public KeyConfig2 stepBackInSelectionHistory = new KeyConfig2(new KeyConfig(KeyCode.LeftArrow, EventModifiers.Control | EventModifiers.Alt), new KeyConfig(KeyCode.Mouse4));
		public KeyConfig2 stepForwardInSelectionHistory = new KeyConfig2(new KeyConfig(KeyCode.RightArrow, EventModifiers.Control | EventModifiers.Alt), new KeyConfig(KeyCode.Mouse3));

		public KeyConfig openNavigateBackMenu = new KeyConfig(KeyCode.LeftArrow, EventModifiers.Shift | EventModifiers.Alt);
		public KeyConfig openNavigateForwardMenu = new KeyConfig(KeyCode.RightArrow, EventModifiers.Shift | EventModifiers.Alt);

		public KeyConfig toggleSplitView = new KeyConfig(KeyCode.Space, EventModifiers.Alt);

		public KeyConfig toggleLockView = new KeyConfig(KeyCode.Q, EventModifiers.Control);

		public KeyConfig closeSelectedView = new KeyConfig(KeyCode.W, EventModifiers.Control);

		public KeyConfig refresh = new KeyConfig(KeyCode.F5);

		public KeyConfig2 activate = new KeyConfig2(new KeyConfig(KeyCode.Return, EventModifiers.None), new KeyConfig(KeyCode.KeypadEnter, EventModifiers.None));

		[HideInInspector]
		public KeyConfig scrollToTop = new KeyConfig(KeyCode.Home, EventModifiers.FunctionKey);
		[HideInInspector]
		public KeyConfig scrollToBottom = new KeyConfig(KeyCode.End, EventModifiers.FunctionKey);
		[HideInInspector]
		public KeyConfig scrollPageUp = new KeyConfig(KeyCode.PageUp, EventModifiers.FunctionKey);
		[HideInInspector]
		public KeyConfig scrollPageDown = new KeyConfig(KeyCode.PageDown, EventModifiers.FunctionKey);
		
		public bool DetectPreviousField(Event inputEvent, bool useEvent)
		{
			if(prevField.DetectInput(inputEvent))
			{
				if(useEvent)
				{
					DrawGUI.Use(inputEvent);
				}
				return true;
			}
			return false;
		}

		public bool DetectNextField(Event inputEvent, bool useEvent)
		{
			if(nextField.DetectInput(inputEvent))
			{
				if(useEvent)
				{
					DrawGUI.Use(inputEvent);
				}
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// textFieldType determines whether certain inputs like Tab, Return, Up Arrow and Down Arrow
		/// are treaded as reserved input
		/// </summary>
		public bool DetectTextFieldReservedInput(Event inputEvent, TextFieldType textFieldType)
		{
			switch(inputEvent.keyCode)
			{
				case KeyCode.Escape:
					return false;
				case KeyCode.LeftArrow:
				case KeyCode.RightArrow:
					//no modifers: move cursor to next/previous letter
					//ctrl: move cursor to next/previous word
					//shift: select next/previous letter
					//ctrl+shift: select next/previous word
					return (inputEvent.modifiers & EventModifiers.Alt) == EventModifiers.None;
				case KeyCode.UpArrow:
				case KeyCode.DownArrow:
					//no modifers: move cursor to below/above row
					//ctrl: move cursor to very top or bottom
					//shift: select upto letter in below/above row
					//ctrl+shift: select upto letter in top or bottom
					return (inputEvent.modifiers & EventModifiers.Alt) == EventModifiers.None && textFieldType != TextFieldType.Numeric;
				case KeyCode.Home:
				case KeyCode.End:
				case KeyCode.PageUp:
				case KeyCode.PageDown:
					return inputEvent.modifiers == EventModifiers.FunctionKey;
				//UPDATE: Tab, return and enter are only treated as text input in Unity's text areas,
				//not in normal text fields
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
				case KeyCode.Tab:
					return inputEvent.modifiers == EventModifiers.None && textFieldType == TextFieldType.TextArea;
				case KeyCode.CapsLock:
					return inputEvent.modifiers == EventModifiers.CapsLock;
				case KeyCode.Delete:
				case KeyCode.Backspace:
					//backspace and delete are used for clearing input one letter at at time
					//ctrl + backspace or delete are used for clearing input one word at a time
					return inputEvent.modifiers == EventModifiers.FunctionKey || (inputEvent.modifiers | (EventModifiers.FunctionKey | EventModifiers.Control)) == (EventModifiers.FunctionKey | EventModifiers.Control);
				case KeyCode.None:
					return false;
				default:
					if(inputEvent.modifiers == EventModifiers.None)
					{
						return true;
					}
					//shift is used for changing uppercasing
					if(inputEvent.modifiers == EventModifiers.Shift)
					{
						return true;
					}
					return false;
			}
		}
	}
}