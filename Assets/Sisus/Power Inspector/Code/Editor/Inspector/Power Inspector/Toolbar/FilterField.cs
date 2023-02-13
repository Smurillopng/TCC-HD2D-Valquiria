//#define DEBUG_ENABLED
#define DEBUG_SET_TEXT
#define DEBUG_SAVE_CURSOR_POSITIONS

using JetBrains.Annotations;
using System;
using UnityEngine;
using UnityEditor;

namespace Sisus
{
	[Serializable]
	public sealed class FilterField
	{
		private static bool nowEditingAFilterField;
		private static bool selectAllOnMouseUp;
		private static bool dragged;
		private static bool dragToPosition;
		private static bool postponeMove;

		[NonSerialized]
		private readonly TextEditor editor = new TextEditor();

		[NonSerialized]
		private bool isSelected;

		[NonSerialized]
		private int savedCursorPosition;
		#if UNITY_2017_2_OR_NEWER
		[NonSerialized]
		private int savedAltCursorPosition;
		#endif
		[NonSerialized]
		private int savedSelectIndex;

		[NonSerialized]
		private Action<Rect> onDropdownClicked;

		[NonSerialized]
		private GUIStyle style;
		
		public FilterField()
		{
			#if UNITY_2023_1_OR_NEWER
			editor.isMultiline = false;
			#else
			editor.multiline = false;
			#endif
			editor.isPasswordField = false;
			style = InspectorPreferences.Styles.FilterField;
			editor.style = style;
		}

		public FilterField([NotNull]Action<Rect> onDropdownMenuClicked)
		{
			#if UNITY_2023_1_OR_NEWER
			editor.isMultiline = false;
			#else
			editor.multiline = false;
			#endif
			editor.isPasswordField = false;
			onDropdownClicked = onDropdownMenuClicked;
			style = InspectorPreferences.Styles.FilterFieldWithDropdown;
			editor.style = style;
		}

		public void MoveCursorToEnd()
		{
			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("MoveCursorToEnd");
			#endif
			int length = editor.text.Length;

			//this causes the cursor indexes to get cleared as a side effect,
			//but it doesn't matter, since we're going to restore them right away below
			editor.OnFocus();

			editor.cursorIndex = length;
			#if UNITY_2017_2_OR_NEWER
			editor.altCursorPosition = length;
			#endif
			editor.selectIndex = length;
			#if UNITY_2023_1_OR_NEWER
			editor.hasHorizontalCursor = false;
			#else
			editor.hasHorizontalCursorPos = false;
			#endif

			SaveCursorPositions();
		}

		public void Clear()
		{
			#if DEV_MODE && DEBUG_SET_TEXT
			if(editor.text.Length > 0) { Debug.Log("SearchBox - Clearing editor.text: \""+editor.text+"\""); }
			#endif

			editor.text = "";
		}

		public string Text
		{
			get
			{
				return editor.text;
			}

			set
			{
				#if DEV_MODE && DEBUG_SET_TEXT
				if(!string.Equals(editor.text, value)) { Debug.Log("SearchBox - editor.text = \""+value+"\" (was \""+editor.text+"\")"); }
				#endif
				editor.text = value;
			}
		}
		
		public void SaveCursorPositions()
		{
			savedCursorPosition = editor.cursorIndex;
			#if UNITY_2017_2_OR_NEWER
			savedAltCursorPosition = editor.altCursorPosition;
			#endif
			savedSelectIndex = editor.selectIndex;

			#if DEV_MODE && DEBUG_SAVE_CURSOR_POSITIONS && UNITY_2017_2_OR_NEWER
			Debug.Log("SearchBox - saved CursorPosition=" + savedCursorPosition+ ", AltCursorPosition="+ savedAltCursorPosition+", SelectIndex="+ savedSelectIndex);
			#endif
		}

		public void RestoreCursorPositions()
		{
			#if DEV_MODE && DEBUG_SAVE_CURSOR_POSITIONS && UNITY_2017_2_OR_NEWER
			if(editor.cursorIndex != savedCursorPosition || editor.altCursorPosition != savedAltCursorPosition || editor.selectIndex != savedSelectIndex)
			{
				Debug.Log("SearchBox - restored CursorPosition=" + savedCursorPosition + " (was: "+ editor.cursorIndex + "), AltCursorPosition=" + savedAltCursorPosition+ " (was: " + editor.altCursorPosition + "), selectIndex=" + savedSelectIndex+ " (was: " + editor.selectIndex + ")");
			}
			#endif
			
			//this causes the cursor indexes to get cleared as a side effect,
			//but it doesn't matter, since we're going to restore them right away below
			editor.OnFocus();

			editor.cursorIndex = savedCursorPosition;
			#if UNITY_2017_2_OR_NEWER
			editor.altCursorPosition = savedAltCursorPosition;
			#endif
			editor.selectIndex = savedSelectIndex;
			#if UNITY_2023_1_OR_NEWER
			editor.hasHorizontalCursor = false;
			#else
			editor.hasHorizontalCursorPos = false;
			#endif
		}
		
		public string Draw(IInspector inspector, Rect position, string filterString, bool setIsSelected, out bool textChanged)
		{
			return DrawFilterField(inspector, GUIUtility.GetControlID(FocusType.Passive), position, filterString, setIsSelected, out textChanged);
		}

		public string DrawFilterField(IInspector inspector, int id, Rect position, string text, bool setIsSelected, out bool textChanged)
		{
			var e = Event.current;
			var eventType = e.GetTypeForControl(id);
			editor.style = style;

			bool wasSelected = isSelected;
			if(setIsSelected != isSelected)
			{
				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log(StringUtils.ToColorizedString("SearchBox.isSelected = ", setIsSelected));
				#endif

				isSelected = setIsSelected;

				if(isSelected)
				{
					KeyboardControlUtility.KeyboardControl = id;
					BeginEditing(id, position, style);

					if(eventType == EventType.MouseDown || eventType == EventType.MouseUp)
					{
						editor.MoveCursorToPosition(e.mousePosition);
						if(GUI.skin.settings.cursorColor.a > 0f)
						{
							#if DEV_MODE && DEBUG_ENABLED
							Debug.Log("SearchBox.SelectAllOnMouseUp");
							#endif
							selectAllOnMouseUp = true;
						}
					}
					else
					{
						#if DEV_MODE && DEBUG_ENABLED
						Debug.Log("SearchBox.SelectAll");
						#endif
						editor.SelectAll();
					}
				}
			}
			
			string textInput = text;
			
			if(text == null)
			{
				text = "";
			}

			#if DEV_MODE && DEBUG_ENABLED
			if(e.rawType == EventType.MouseDown || e.rawType == EventType.MouseUp || GUIUtility.hotControl == id) { Debug.Log(StringUtils.ToColorizedString("SearchBox Event=", e, ", typeForControl=", eventType, ", button=", e.button, ", pos=", position, ", mousePos=", e.mousePosition, ", pos.Contains(mousePos)=", position.Contains(e.mousePosition), ", HasKeyboardFocus=", HasKeyboardFocus(), ", isSelected=", isSelected, ", GUIUtility.hotControl=", GUIUtility.hotControl, ", id=", id)); }
			#endif

			if(isSelected && inspector.InspectorDrawer.HasFocus && KeyboardControlUtility.JustClickedControl == 0)
			{
				// UPDATE: When in play mode the keyboard control would have a different ID during every layout / Repaint event!
				if(Event.current.type == EventType.Layout)
				{
					KeyboardControlUtility.KeyboardControl = id;
				}
				else
				{
					inspector.RefreshView();
				}
			}

			if(HasKeyboardFocus())
			{
				if(IsEditingControl())
				{
					if(Event.current.type == EventType.Layout)
					{
						#if DEV_MODE && DEBUG_ENABLED
						if(editor.controlID != id) { Debug.Log("editor.controlID = " + id + " (was: " + editor.controlID + ") with Event=" + StringUtils.ToString(Event.current)); }
						#endif
						editor.position = position;
						editor.controlID = id;
						editor.DetectFocusChange();
					}
				}
				else if(DrawGUI.EditingTextField || eventType == EventType.ExecuteCommand && string.Equals(e.commandName, "NewKeyboardFocus"))
				{
					BeginEditing(id, position, style);
					if(GUI.skin.settings.cursorColor.a > 0f)
					{
						#if DEV_MODE && DEBUG_ENABLED
						Debug.Log("SearchBox.SelectAll");
						#endif
						editor.SelectAll();
					}
					if(e.GetTypeForControl(id) == EventType.ExecuteCommand)
					{
						DrawGUI.Use(e);
					}
				}
			}

			if(editor.controlID == id && !isSelected)
			{
				EndEditing();
			}
			
			bool flag1 = false;
			string text1 = editor.text;
			switch(eventType)
			{
				case EventType.MouseDown:
					if(position.Contains(e.mousePosition) && e.button == 0)
					{
						var dropdownButtonRect = position;
						dropdownButtonRect.width = 15f;
						if(onDropdownClicked != null && dropdownButtonRect.Contains(e.mousePosition))
						{
							onDropdownClicked(position);

							if(IsEditingControl())
							{
								Clear();
								EndEditing();
								flag1 = true;
							}
						}
						else if(wasSelected)
						{
							if(e.clickCount == 2 && GUI.skin.settings.doubleClickSelectsWord)
							{
								editor.MoveCursorToPosition(Event.current.mousePosition);
								editor.SelectCurrentWord();
								editor.MouseDragSelectsWholeWords(true);
								editor.DblClickSnap(TextEditor.DblClickSnapping.WORDS);
								dragToPosition = false;
							}
							else if(e.clickCount == 3 && GUI.skin.settings.tripleClickSelectsLine)
							{
								editor.MoveCursorToPosition(e.mousePosition);
								editor.SelectCurrentParagraph();
								editor.MouseDragSelectsWholeWords(true);
								editor.DblClickSnap(TextEditor.DblClickSnapping.PARAGRAPHS);
								dragToPosition = false;
							}
							else
							{
								editor.MoveCursorToPosition(e.mousePosition);
								selectAllOnMouseUp = false;
							}
						}
						else
						{
							if(KeyboardControlUtility.JustClickedControl == 0)
							{
								KeyboardControlUtility.KeyboardControl = id;
							}
							BeginEditing(id, position, style);
							editor.MoveCursorToPosition(e.mousePosition);
							if(GUI.skin.settings.cursorColor.a > 0f)
							{
								#if DEV_MODE && DEBUG_ENABLED
								Debug.Log("SearchBox.SelectAllOnMouseUp");
								#endif
								selectAllOnMouseUp = true;
							}
						}
						KeyboardControlUtility.JustClickedControl = id;
						DrawGUI.Use(e);
					}
					break;
				case EventType.MouseUp:
					if(GUIUtility.hotControl == id)
					{
						if(dragged && dragToPosition)
						{
							#if DEV_MODE && DEBUG_ENABLED
							Debug.Log("SearchBox.MoveSelectionToAltCursor");
							#endif
							editor.MoveSelectionToAltCursor();
							flag1 = true;
						}
						else if(postponeMove)
						{
							editor.MoveCursorToPosition(e.mousePosition);
						}
						else if(selectAllOnMouseUp)
						{
							if(GUI.skin.settings.cursorColor.a > 0f)
							{
								#if DEV_MODE && DEBUG_ENABLED
								Debug.Log("SearchBox.SelectAll");
								#endif
								editor.SelectAll();
							}
							selectAllOnMouseUp = false;
						}
						editor.MouseDragSelectsWholeWords(false);
						dragToPosition = true;
						dragged = false;
						postponeMove = false;
						if(e.button == 0)
						{
							KeyboardControlUtility.JustClickedControl = 0;
							DrawGUI.Use(e);
						}
					}
					break;
				case EventType.MouseDrag:
					if(GUIUtility.hotControl == id)
					{
						if(!e.shift && editor.hasSelection && dragToPosition)
						{
							#if DEV_MODE && DEBUG_ENABLED
							Debug.Log("SearchBox.MoveAltCursorToPosition");
							#endif
							editor.MoveAltCursorToPosition(e.mousePosition);
						}
						else
						{
							if(e.shift)
							{
								#if DEV_MODE && DEBUG_ENABLED
								Debug.Log("SearchBox.MoveCursorToPosition");
								#endif
								editor.MoveCursorToPosition(e.mousePosition);
							}
							else
							{
								#if DEV_MODE && DEBUG_ENABLED
								Debug.Log("SearchBox.SelectToPosition");
								#endif
								editor.SelectToPosition(e.mousePosition);
							}
							dragToPosition = false;
							selectAllOnMouseUp = !editor.hasSelection;
							#if DEV_MODE && DEBUG_ENABLED
							Debug.Log("SearchBox.SelectAllOnMouseUp = " + selectAllOnMouseUp);
							#endif
						}
						dragged = true;
						DrawGUI.Use(e);
					}
					break;
				case EventType.KeyDown:
					if(!isSelected)
					{
						break;
					}
					bool flag2 = false;
					char character = e.character;
					if(IsEditingControl() && editor.HandleKeyEvent(e))
					{
						DrawGUI.Use(e);
						flag1 = true;
						break;
					}
					if(e.keyCode == KeyCode.Escape)
					{
						if(IsEditingControl())
						{
							Clear();
							EndEditing();
							flag1 = true;
						}
					}
					else if(character == 10 || character == 3)
					{
						if(!IsEditingControl())
						{
							#if DEV_MODE && DEBUG_ENABLED
							Debug.Log("SearchBox.SelectAll");
							#endif
							BeginEditing(id, position, style);
							editor.SelectAll();
						}
						else
						{
							EndEditing();
						}
						DrawGUI.Use(e);
					}
					else if(character == 9 || e.keyCode == KeyCode.Tab)
					{
						flag2 = true;
					}
					else if(character != 25 && character != 27 && IsEditingControl())
					{
						if(character != 0)
						{
							editor.Insert(character);
							flag1 = true;
						}
						else if(Input.compositionString.Length > 0)
						{
							editor.ReplaceSelection("");
							flag1 = true;
						}
					}
					if(IsEditingControl() && MightBePrintableKey(e) && !flag2)
					{
						DrawGUI.Use(e);
					}
					break;
				case EventType.Repaint:
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(string.Equals(text, editor.text), "text ("+text+") != editor.text ("+editor.text+")");
					#endif

					if(GUIUtility.hotControl == 0)
					{
						// new test
						if(inspector.MouseoveredPart != InspectorPart.None)
						{
							DrawGUI.Active.AddCursorRect(position, MouseCursor.Text);
						}
					}

					string renderedText = !IsEditingControl() ? text : editor.text;
					if(!IsEditingControl())
					{
						style.Draw(position, GUIContentPool.Temp(renderedText), id, false);
						break;
					}
					editor.DrawCursor(renderedText);
					break;
				default:
					switch(eventType - 13)
					{
						case EventType.MouseDown:
							if(KeyboardControlUtility.KeyboardControl == id)
							{
								switch(e.commandName)
								{
									case "Cut":
									case "Copy":
										if(editor.hasSelection)
										{
											DrawGUI.Use(e);
										}
										break;
									case "Paste":
										if(editor.CanPaste())
										{
											DrawGUI.Use(e);
										}
										break;
									case "SelectAll":
									case "Delete":
										DrawGUI.Use(e);
										break;
									case "UndoRedoPerformed":
										Text = text;
										DrawGUI.Use(e);
										break;
								}
							}
							break;
						case EventType.MouseUp:
							if(KeyboardControlUtility.KeyboardControl == id)
							{
								switch(e.commandName)
								{
									case "OnLostFocus":
										#if DEV_MODE && DEBUG_ENABLED
										Debug.Log("SearchBox - MouseUp / OnLostFocus: end editing");
										#endif
										if(nowEditingAFilterField)
										{
											EndEditing();
										}
										DrawGUI.Use(e);
										break;
									case "Cut":
										BeginEditing(id, position, style);
										editor.Cut();
										flag1 = true;
										break;
									case "Copy":
										editor.Copy();
										DrawGUI.Use(e);
										break;
									case "Paste":
										BeginEditing(id, position, style);
										editor.Paste();
										flag1 = true;
										break;
									case "SelectAll":
										#if DEV_MODE && DEBUG_ENABLED
										Debug.Log("SearchBox.SelectAll");
										#endif
										editor.SelectAll();
										DrawGUI.Use(e);
										break;
									case "Delete":
										BeginEditing(id, position, style);
										if(SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
										{
											editor.Delete();
										}
										else
										{
											editor.Cut();
										}
										flag1 = true;
										DrawGUI.Use(e);
										break;
								}
							}
							break;
						case EventType.MouseDrag:
							if(position.Contains(e.mousePosition))
							{
								if(!IsEditingControl())
								{
									KeyboardControlUtility.KeyboardControl = id;
									BeginEditing(id, position, style);
									editor.MoveCursorToPosition(e.mousePosition);
								}
								ShowTextEditorPopupMenu(inspector);
								DrawGUI.Use(e);
							}
							break;
					}
					break;
			}
			
			textChanged = false;
			if(flag1)
			{
				textChanged = !string.Equals(text1, editor.text);
				if(Event.current.type != EventType.Used)
				{
					DrawGUI.Use(e);
				}
			}
			if(textChanged)
			{
				SaveCursorPositions();
				GUI.changed = true;
				return editor.text;
			}
			return textInput;
		}

		private void BeginEditing(int id, Rect position, GUIStyle style)
		{
			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("Begin Editing");
			#endif

			#if DEV_MODE && DEBUG_ENABLED
			if(editor.controlID != id) { Debug.Log("editor.controlID = " + id + " (was: " + editor.controlID + ") with Event=" + StringUtils.ToString(Event.current)); }
			#endif

			editor.controlID = id;
			#if UNITY_2023_1_OR_NEWER
			editor.isMultiline = false;
			#else
			editor.multiline = false;
			#endif			
			editor.style = style;
			editor.position = position;
			editor.isPasswordField = false;
			nowEditingAFilterField = true;
			editor.scrollOffset = Vector2.zero;
			#if UNITY_EDITOR
			UnityEditor.Undo.IncrementCurrentGroup();
			#endif
			Input.imeCompositionMode = IMECompositionMode.On;
		}

		private void EndEditing()
		{
			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("End Editing");
			#endif

			editor.controlID = 0;
			nowEditingAFilterField = false;
			#if UNITY_EDITOR
			UnityEditor.Undo.IncrementCurrentGroup();
			#endif
		}

		private bool IsEditingControl()
		{
			return isSelected && InspectorUtility.ActiveInspector.InspectorDrawer.HasFocus;
		}

		private bool HasKeyboardFocus()
		{
			return isSelected && InspectorUtility.ActiveInspector.InspectorDrawer.HasFocus;
		}

		private void ShowTextEditorPopupMenu(IInspector inspector)
		{
			var menu = Menu.Create();
			if(editor.hasSelection && !editor.isPasswordField)
			{
				menu.Add("Cut", ()=>
				{
					Clipboard.Copy(editor.SelectedText);
					editor.DeleteSelection();
				});
				menu.Add("Copy", () => Clipboard.Copy(editor.SelectedText));
			}
			if(editor.CanPaste())
			{
				menu.Add("Paste", ()=>editor.Paste());
			}

			var searchBox = inspector.Toolbar.GetItemByType(typeof(ISearchBoxToolbarItem));
			if(searchBox != null && inspector.Manager.MouseoveredInspectorPart == InspectorPart.Toolbar || inspector.Manager.SelectedInspectorPart == InspectorPart.Toolbar)
			{
				ContextMenuUtility.Open(menu, true, inspector, InspectorPart.Toolbar, null, searchBox);
			}
			else
			{
				#if DEV_MODE
				Debug.LogWarning("Filter.ShowTextEditorPopupMenu called but subject was not ISearchBoxToolbarItem on inspector toolbar. Calling ContextMenuUtility with a null subject.");
				#endif

				ContextMenuUtility.Open(menu, null);
			}
		}

		private static bool MightBePrintableKey(Event e)
		{
			var key = e.keyCode;
			if(e.command || e.control || key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6 || (key >= KeyCode.JoystickButton0 && key <= KeyCode.Joystick8Button19 || key >= KeyCode.F1 && key <= KeyCode.F15))
			{
				return false;
			}

			switch(key)
			{
				case KeyCode.Numlock:
				case KeyCode.CapsLock:
				case KeyCode.ScrollLock:
				case KeyCode.RightShift:
				case KeyCode.LeftShift:
				case KeyCode.RightControl:
				case KeyCode.LeftControl:
				case KeyCode.RightAlt:
				case KeyCode.LeftAlt:
				case KeyCode.RightCommand:
				case KeyCode.LeftCommand:
				case KeyCode.LeftWindows:
				case KeyCode.RightWindows:
				case KeyCode.AltGr:
				case KeyCode.Help:
				case KeyCode.Print:
				case KeyCode.SysReq:
				case KeyCode.Menu:
					label_6:
					return false;
				default:
					switch(key - 273)
					{
						case KeyCode.None:
						case (KeyCode)1:
						case (KeyCode)2:
						case (KeyCode)3:
						case (KeyCode)4:
						case (KeyCode)5:
						case (KeyCode)6:
						case (KeyCode)7:
						case KeyCode.Backspace:
							goto label_6;
						default:
							if(key == KeyCode.None)
								return e.character != 0;
							if(key != KeyCode.Backspace && key != KeyCode.Clear && (key != KeyCode.Pause && key != KeyCode.Escape) && key != KeyCode.Delete)
								return true;
							goto label_6;
					}
			}
		}
	}
}