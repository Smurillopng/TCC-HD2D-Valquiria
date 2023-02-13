//#define USE_THREADING
//#define DISPLAY_UNFORMATTED_UNTIL_SYNTAX_FORMATTING_DONE

#define DEBUG_NULL_FONT
//#define DEBUG_SYNTAX_FORMAT_TIME
#define DEBUG_CLICK
//#define DEBUG_SET_EDIT_MODE

using System;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;

#if USE_THREADING
using System.Threading;
#endif

namespace Sisus.CreateScriptWizard
{
	[Serializable]
	public class CreateScriptWizardPreviewDrawer
	{
		private const float RowHeight = DrawGUI.SingleLineHeightWithoutPadding;

		private static bool displayUnformatted;
		private static CreateScriptWizardPreviewDrawer pooled;
		private static readonly StringBuilder sb = new StringBuilder(512);

		private string editedLineControlName = "";
		private int firstVisibleLine;
		private int lastVisibleLine = -1;
		protected bool editDefaultReferences;

		#if DEV_MODE && DEBUG_SYNTAX_FORMAT_TIME
		private ExecutionTimeLogger syntaxFormatTimer = new ExecutionTimeLogger();
		#endif
		
		[SerializeField]
		protected string textUnformatted = "";

		[SerializeField]
		private int selectedLine = -1;
		[SerializeField]
		private bool editMode;

		[SerializeField]
		private Vector2 scrollPosition;

		#if USE_THREADING
		private volatile bool setupInProgress;
		private volatile bool setupDoneAndUnapplied;
		#endif

		[SerializeField]
		private bool unsavedChanges;
		private int focusField;

		private Code code;
		
		#if USE_THREADING
		private volatile Code threadGeneratedCode;
		#endif

		/// <summary>
		/// This is incremented by one every time this instance is pooled.
		/// Used before results of threaded tasks are applied to instance
		/// to test that this instance hasn't been pooled and unpooled
		/// while the task was running.
		/// </summary>
		private volatile int instanceId;

		private float heightWhenVisibleLinesLastUpdated;
		private float widthWhenVisibleLinesLastUpdated;

		private float viewportHeight;

		private Rect lastDrawPosition;

		private Action<string> onTextChanged;
		private Func<bool> selectNextControl;
		private Func<bool> selectPreviousControl;
		private Func<bool> selectControlOnRight;
		private Func<bool> selectControlOnLeft;

		public int SelectedLine
		{
			get
			{
				return selectedLine;
			}
		}

		private IInspectorManager Manager
		{
			get
			{
				return InspectorUtility.ActiveManager != null ? InspectorUtility.ActiveManager : InspectorManager.Instance();
			}
		}

		private bool AllowEditing
		{
			get
			{
				return true;
			}
		}

		private InspectorPreferences Preferences
		{
			get
			{
				return InspectorUtility.Preferences;
			}
		}

		private float ContentHeight
		{
			get
			{
				return code.LineCount * RowHeight;
			}
		}

		private float ContentWidth
		{
			get
			{
				float result = LineNumberColumnWidth + code.width;
				var padding = 5f;
				return result + padding;
			}
		}

		private float LineNumberColumnWidth
		{
			get
			{
				if(lastVisibleLine < 10)
				{
					return 12f;
				}
				if(lastVisibleLine < 100)
				{
					return 20f;
				}
				if(lastVisibleLine < 1000)
				{
					return 28f;
				}
				if(lastVisibleLine < 10000)
				{
					return 36f;
				}
				return 44f;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static CreateScriptWizardPreviewDrawer Create(string text, Action<string> onTextChanged, Func<bool> selectNextControl, Func<bool> selectPreviousControl, Func<bool> selectControlOnRight, Func<bool> selectControlOnLeft)
		{
			CreateScriptWizardPreviewDrawer result;
			if(pooled != null)
			{
				result = pooled;
				pooled = null;
			}
			else
			{
				result = new CreateScriptWizardPreviewDrawer();
			}
			result.Setup(text, onTextChanged, selectNextControl, selectPreviousControl, selectControlOnRight, selectControlOnLeft);
			return result;
		}

		private void Setup(string text, Action<string> onTextChanged, Func<bool> selectNextControl, Func<bool> selectPreviousControl, Func<bool> selectControlOnRight, Func<bool> selectControlOnLeft)
		{
			#if DEV_MODE
			Debug.Log(GetType().Name + ".Setup("+text.Length+ ") with instanceId=" + instanceId);
			#endif

			textUnformatted = text;

			this.onTextChanged = onTextChanged;
			this.selectNextControl = selectNextControl;
			this.selectPreviousControl = selectPreviousControl;
			this.selectControlOnRight = selectControlOnRight;
			this.selectControlOnLeft = selectControlOnLeft;

			code = new Code(CreateSyntaxFormatter());
		
			SetEditMode(false, ReasonSelectionChanged.Initialization);
			selectedLine = -1;
			unsavedChanges = false;

			#if USE_THREADING
			setupInProgress = true;
			setupDoneAndUnapplied = false;
			
			// build syntax formatting on another thread to avoid UI slow downs when user
			// selects a large formatted text file
			ThreadPool.QueueUserWorkItem(RebuildMembersWithSyntaxFormattingThreaded, instanceId);

				#if DISPLAY_UNFORMATTED_UNTIL_SYNTAX_FORMATTING_DONE
				int maxLinesToDo = Mathf.CeilToInt(Height / RowHeight) + 1;
				string firstScreenful = codeUnformatted.GetFirstLines(maxLinesToDo);
				Debug.Log("maxLinesToDo="+maxLinesToDo);
				if(!setupDoneAndUnapplied)
				{
					CodeBuilder.Instance.SetCode(firstScreenful);
					CodeBuilder.Instance.BuildAllBlocks();
					CodeBuilder.Instance.GeneratedBlocks.ToCode(ref code);
					CodeBuilder.Instance.Clear();
				}
				#endif
				
			#else
			RebuildAllMembersWithSyntaxFormatting();
			#endif
		}

		/// <summary> Creates instance of syntax formatter appropriate for the drawer. </summary>
		/// <returns> Syntax formatter. </returns>
		private CSharpSyntaxFormatter CreateSyntaxFormatter()
		{
			return CSharpSyntaxFormatterPool.Pop();
		}

		/// <inheritdoc cref="IDrawer.Draw" />
		public void Draw(Rect position)
		{
			Profiler.BeginSample("FormattedTextAssetDrawer.Draw");

			float lineNumberColumnWidth = LineNumberColumnWidth;

			var leftColumnRect = position;
			float leftOffset = Mathf.Max(0f, scrollPosition.x);
			leftColumnRect.x = lineNumberColumnWidth - leftOffset;
			leftColumnRect.width = position.width - leftColumnRect.x;
			leftColumnRect.height = ContentHeight - scrollPosition.y;

			EditorGUI.DrawRect(leftColumnRect, new Color(0f, 0f, 0f, 0.1f));

			#if DEV_MODE && DEBUG_SYNTAX_FORMAT_TIME
			if(syntaxFormatTimer.HasResultsToReport)
			{
				syntaxFormatTimer.LogResults();
			}
			#endif

			#if USE_THREADING
			if(setupDoneAndUnapplied)
			{
				setupDoneAndUnapplied = false;
				var disposeCode = code;
				code = null;
				disposeCode.Dispose();

				var setCode = threadGeneratedCode;
				threadGeneratedCode = null;
				code = setCode;
				UpdateVisibleLines();
				GUI.changed = true;
			}
			#endif

			var e = Event.current;

			if(e.type == EventType.KeyDown && selectedLine != -1)
			{
				OnKeyboardInputGiven(Event.current, Preferences.keyConfigs);
			}
			
			if(selectedLine >= 0)
			{
				if(editMode)
				{
					if(!DrawGUI.EditingTextField)
					{
						DrawGUI.EditingTextField = true;
					}
				}
				else if(DrawGUI.EditingTextField)
				{
					DrawGUI.EditingTextField = false;
				}
			}

			if(e.type == EventType.Layout)
			{
				OnLayoutEvent(position);
			}
			
			var rowRect = position;
			rowRect.height = RowHeight;
			var windowRect = position;
			windowRect.height = viewportHeight;
			var contentRect = windowRect;
			contentRect.y = 0f;
			
			contentRect.height = ContentHeight;
			contentRect.width = ContentWidth;

			var scrollViewBgWas = GUI.skin.scrollView.normal.background;
			GUI.skin.scrollView.normal.background = DrawGUI.DarkTexture;

			var setScrollPosition = GUI.BeginScrollView(windowRect, scrollPosition, contentRect);
			{
				rowRect.y = 0f;
				rowRect.width = Mathf.Max(position.width - 2f, ContentWidth);
				
				#if !DISPLAY_UNFORMATTED_UNTIL_SYNTAX_FORMATTING_DONE && USE_THREADING
				if(setupInProgress)
				{
					GUI.changed = true;
				}
				else
				#endif
				{
					//only draw lines that are on-screen for optimization purposes
					float firstY = rowRect.y + firstVisibleLine * RowHeight;
					rowRect.y = firstY;

					if(e.type == EventType.MouseDown)
					{
						for(int n = firstVisibleLine; n < lastVisibleLine; n++)
						{
							if(rowRect.Contains(e.mousePosition))
							{
								var lineNumberRect = rowRect;
								lineNumberRect.width = LineNumberColumnWidth;
								OnLineClicked(n, lineNumberRect.Contains(e.mousePosition));
								break;
							}
							rowRect.y += RowHeight;
						}
						rowRect.y = firstY;
					}

					for(int n = firstVisibleLine; n < lastVisibleLine; n++)
					{
						DrawLine(rowRect, n);
						rowRect.y += RowHeight;
					}
				}
			}
			GUI.EndScrollView();

			GUI.skin.scrollView.normal.background = scrollViewBgWas;

			if(setScrollPosition != scrollPosition)
			{
				SetScrollPosition(setScrollPosition);
			}

			Profiler.EndSample();
		}

		private void DrawLine(Rect position, int index)
		{
			Profiler.BeginSample("FormattedTextAssetDrawer.DrawLine");

			int lineNumber = index + 1;
			var posLeft = position;
			posLeft.width = LineNumberColumnWidth;
			GUI.Label(posLeft, StringUtils.ToString(lineNumber), InspectorPreferences.Styles.LineNumber);

			position.x += LineNumberColumnWidth;
			position.width -= LineNumberColumnWidth;

			Debug.Assert(position.height == RowHeight);

			bool isSelectedLine = selectedLine == index;

			if(isSelectedLine)
			{
				var selectionRect = position;
				if(ContentHeight > DrawGUI.InspectorHeight)
				{
					selectionRect.width = DrawGUI.InspectorWidth;
					if(HasHorizontalScrollBar())
					{
						selectionRect.width -= DrawGUI.ScrollBarWidth + 1f;
					}

					if(scrollPosition.x.Equals(0f))
					{
						selectionRect.x += 1f;
						selectionRect.width -= LineNumberColumnWidth;
					}
					else if(scrollPosition.x >= LineNumberColumnWidth)
					{
						selectionRect.x = scrollPosition.x + 1f;
						selectionRect.width -= 1f;
					}
					else
					{
						selectionRect.x = 1f + LineNumberColumnWidth;
						selectionRect.width -= LineNumberColumnWidth - scrollPosition.x;
					}
				}
				DrawGUI.DrawRect(selectionRect, Preferences.theme.BackgroundSelected);
			}
			
			if(displayUnformatted || (isSelectedLine && editMode))
			{
				if(code.LineCount <= index)
				{
					#if DEV_MODE
					Debug.LogWarning("code.Length ("+code.LineCount+") <= index ("+index+")");
					#endif

					SetSelectedLine(-1);
					RebuildAllMembersWithSyntaxFormatting();
				}
				else
				{
					EditorGUI.BeginChangeCheck();
					{
						if(isSelectedLine)
						{
							DrawerUtility.HandleFieldFocus(editedLineControlName, ref focusField);
						}

						string lineWas = code.lines[index].unformatted;
						string setLine = lineWas;
						if(isSelectedLine && editMode)
						{
							setLine = EditorGUI.TextField(position, lineWas, InspectorPreferences.Styles.formattedText);
						}
						else
						{
							EditorGUI.SelectableLabel(position, lineWas, InspectorPreferences.Styles.formattedText);
						}

						if(EditorGUI.EndChangeCheck())
						{
							if(!string.Equals(lineWas, setLine))
							{
								//testing fix for number of line breaks changing when saving changes
								//this most probably is not needed!
								setLine = setLine.Replace("\n","").Replace("\r","");

								code.SetLine(index, setLine, true);

								if(onTextChanged != null)
								{
									onTextChanged(code.TextUnformatted);
								}
							}
						}
					}
				}
			}
			else
			{
				EditorGUI.SelectableLabel(position, code[index], InspectorPreferences.Styles.formattedText);
			}

			Profiler.EndSample();
		}

		/// <summary>
		/// Called when user pressed a mouse button down over a line in the text editor.
		/// </summary>
		/// <param name="index"> Zero-based index of the clicked line. </param>
		/// <param name="lineNumberClicked"> True if the line number on the left side of the row was clicked. False if text portion was clicked. </param>
		private void OnLineClicked(int index, bool lineNumberClicked)
		{
			#if DEV_MODE && DEBUG_CLICK
			Debug.Log(StringUtils.ToColorizedString("OnLineClicked(", index, ") with lineNumberClicked=", lineNumberClicked, ", button=", Event.current.button, ", clickCount=", Event.current.clickCount, ", selectedLine=", selectedLine));
			#endif

			switch(Event.current.button)
			{
				case 0:
					if(selectedLine != index)
					{
						SetSelectedLine(index);
					}
					
					if(Event.current.clickCount == 2)
					{
						if(!lineNumberClicked)
						{
							//Not sure if I like how easy it is to start editing lines.
							//Could consider only doing this if F2 is pressed, or edit button is clicked?
							if(!editMode)
							{
								SetEditMode(true, ReasonSelectionChanged.ControlClicked);
							}
						}
					}

					break;
				case 1:
					var menu = Menu.Create();
					menu.Add("Copy Line To Clipboard", ()=>CopyLineToClipboard(index));
					if(AllowEditing)
					{
						menu.Add("Delete Row", ()=>code.RemoveAt(index, true));
						menu.Add("Insert Row", ()=>code.InsertAt(index, "", true));
						menu.Add("Insert Summary", ()=>
						{
							string lineUnformatted = code.GetLineUnformatted(index);
							int lineCharCount = lineUnformatted.Length;
							for(int i = 0; i < lineCharCount; i++)
							{
								char test = lineUnformatted[i];
								if(char.IsWhiteSpace(test))
								{
									sb.Append(test);
								}
								else
								{
									break;
								}
							}
							string leadingWhiteSpace = sb.ToString();
							sb.Length = 0;

							//if the current line is empty, instert the summary directly at that line
							//otherwise insert it above the line
							if(lineUnformatted.Trim().Length == 0)
							{
								index++;
							}
							code.InsertAt(index, string.Concat(leadingWhiteSpace, "/// </summary>"), true);
							code.InsertAt(index, string.Concat(leadingWhiteSpace, "/// "), true);
							code.InsertAt(index, string.Concat(leadingWhiteSpace, "/// <summary>"), true);
							Manager.OnNextLayout(()=> selectedLine = index + 1);
							UpdateVisibleLines();
						});
					}
					
					ContextMenuUtility.Open(menu, null, Part.LineNumber);
					break;
			}
		}

		private void CopyLineToClipboard(int lineIndex)
		{
			string lineText = code.GetLineUnformatted(lineIndex);
			Clipboard.Copy(lineText);
			Clipboard.SendCopyToClipboardMessage("Copied line "+StringUtils.ToString(lineIndex + 1), "", lineText);
		}

		private void SetEditMode(bool setEditMode, ReasonSelectionChanged reason)
		{
			#if USE_THREADING
			if(setupInProgress)
			{
				setEditMode = false;
			}
			#endif

			if(setEditMode == editMode)
			{
				return;
			}

			#if DEV_MODE && DEBUG_SET_EDIT_MODE && USE_THREADING
			Debug.Log(StringUtils.ToColorizedString("SetEditMode ", setEditMode, " because ", reason, " (was=", editMode, "), with setupInProgress=", setupInProgress, ", selectedLine=", selectedLine, ", lineCount=", code.LineCount, ", EditingTextField=", DrawGUI.EditingTextField));
			#endif
			
			if(setEditMode)
			{
				editMode = true;
				if(selectedLine < 0)
				{
					SetSelectedLine(0);
				}
				else
				{
					FocusSelectedLine();
				}

				if(code.LineCount == 0)
				{
					unsavedChanges = true;
					var builder = CreateSyntaxFormatter();
					code.Dispose();
					code = new Code(builder);
				}

				DrawGUI.EditingTextField = true;
			}
			else
			{
				if(unsavedChanges)
				{
					// TO DO: ask to save or discard using a dialog window?
				}
				editMode = false;
				if(selectedLine != -1)
				{
					DrawGUI.EditingTextField = true;
				}
				focusField = 0;
			}
		}

		private void OnToggleDisplayFormattedButtonClicked()
		{
			SetDisplayUnformatted(!displayUnformatted);
			DrawGUI.UseEvent();
		}

		private void SetDisplayUnformatted(bool setDisplayUnformatted)
		{
			if(displayUnformatted != setDisplayUnformatted)
			{
				displayUnformatted = setDisplayUnformatted;
				//DrawGUI.EditingTextField = false;
				DrawGUI.EditingTextField = false;
				focusField = 0;
				GUI.changed = true;
				//RebuildHeaderButtons();
			}
		}
		
		private void FocusSelectedLine()
		{
			if(editMode && selectedLine >= 0)
			{
				KeyboardControlUtility.KeyboardControl = 0;
				editedLineControlName = string.Concat("EditedLine", selectedLine);
				focusField = 3;
			}
			else
			{
				KeyboardControlUtility.SetKeyboardControl(0, 3);
			}
		}

		/// <inheritdoc />
		private void OnLayoutEvent(Rect position)
		{
			lastDrawPosition = position;

			viewportHeight = CalculateViewportHeight();

			if(!heightWhenVisibleLinesLastUpdated.Equals(viewportHeight) || !widthWhenVisibleLinesLastUpdated.Equals(position.width))
			{
				UpdateVisibleLines();
			}
		}

		private float CalculateViewportHeight()
		{
			float height = lastDrawPosition.height;
			return height < 0f ? 0f : height;
		}

		/// <summary>
		/// Rebuild members with syntax formatting.
		/// Work is done on another thread to avoid UX slow downs when user
		/// selects a large formatted text file.
		/// </summary>
		/// <param name="threadTaskId"> Information describing the state. </param>
		private void RebuildMembersWithSyntaxFormattingThreaded(object threadTaskId)
		{
			#if DEV_MODE
			Debug.Log(GetType().Name + ".RebuildMembersWithSyntaxFormattingThreaded(" + threadTaskId + ") with instanceId=" + instanceId);
			#endif

			RebuildAllMembersWithSyntaxFormatting((int)threadTaskId);
		}

		private void RebuildAllMembersWithSyntaxFormatting()
		{
			#if DEV_MODE
			Debug.Log(GetType().Name + ".RebuildAllMembersWithSyntaxFormatting() with instanceId=" + instanceId);
			#endif

			RebuildAllMembersWithSyntaxFormatting(instanceId);
		}

		private void RebuildAllMembersWithSyntaxFormatting(int threadTaskId)
		{
			#if DEV_MODE
			Debug.Log(GetType().Name + ".RebuildAllMembersWithSyntaxFormatting(" + threadTaskId + ") with instanceId=" + instanceId);
			#endif

			#if DEV_MODE && DEBUG_SYNTAX_FORMAT_TIME
			syntaxFormatTimer.Start("SyntaxFormatting");
			#endif

			BuildMembersWithSyntaxFormatting(threadTaskId);

			#if USE_THREADING
			// When using threading, it's possible that the drawer were
			// disposed or reused for another script while the thread was doing its thing.
			if(threadTaskId == instanceId)
			{
				UpdateVisibleLines();
				
				setupDoneAndUnapplied = true;
				setupInProgress = false;
			}
			#else
			UpdateVisibleLines();
			#endif

			#if DEV_MODE && DEBUG_SYNTAX_FORMAT_TIME
			syntaxFormatTimer.Finish();
			#endif

			#if DEV_MODE
			Debug.Log("RebuildAllMembersWithSyntaxFormatting done");
			#endif
		}

		private void BuildMembersWithSyntaxFormatting(int threadTaskId)
		{
			BuildMembersWithSyntaxFormatting(CreateSyntaxFormatter(), threadTaskId);
		}

		private void BuildMembersWithSyntaxFormatting(CSharpSyntaxFormatter builder, int threadTaskId)
		{
			var setCode = new Code(builder);
			builder.SetCode(textUnformatted);
			builder.BuildAllBlocks();

			builder.GeneratedBlocks.ToCode(ref setCode, builder, Fonts.NormalSizes);
			//NOTE: no need to call builder.Dispose here as it's already handled by Code.Dispose

			#if USE_THREADING
			if(threadTaskId == instanceId)
			{
				var dispose = threadGeneratedCode;
				threadGeneratedCode = null;

				if(dispose != null)
				{
					#if DEV_MODE
					Debug.Assert(dispose != setCode);
					#endif

					dispose.Dispose();
				}

				threadGeneratedCode = setCode;
			}
			else
			{
				setCode.Dispose();
			}
			#else
			if(code != null)
			{
				code.Dispose();
			}
			code = setCode;
			#endif
		}
		

		private void SetScrollPosition(Vector2 value)
		{
			scrollPosition = value;
			UpdateVisibleLines();
		}

		private void UpdateVisibleLines()
		{
			firstVisibleLine = Mathf.FloorToInt(scrollPosition.y / RowHeight);

			heightWhenVisibleLinesLastUpdated = viewportHeight;
			widthWhenVisibleLinesLastUpdated = lastDrawPosition.width;

			float visibleHeight = viewportHeight;

			//if has horizontal scrollbar, adjust height
			if(HasHorizontalScrollBar())
			{
				visibleHeight -= DrawGUI.ScrollBarWidth;
			}
			lastVisibleLine = Mathf.Min(firstVisibleLine + Mathf.CeilToInt(visibleHeight / RowHeight) + 1, code.LineCount);
		}

		private bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			switch(inputEvent.keyCode)
			{
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					if(inputEvent.shift || inputEvent.control)
					{
						if(selectedLine >= 0)
						{
							GUI.changed = true;
							DrawGUI.Use(inputEvent);
							code.InsertAt(inputEvent.shift ? selectedLine : selectedLine + 1, "", true); //shift+enter adds above, ctrl+enter adds below
							if(inputEvent.control)
							{
								selectedLine = Mathf.Min(code.LineCount - 1, selectedLine + 1);
							}
							UpdateVisibleLines();
							return true;
						}
						return false;
					}
					// here should apply the currently edited line before changing row!
					// TODO: support shift+enter to add line breaks or something?
					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					return true;
				case KeyCode.UpArrow:
					int selectLine = selectedLine - 1;
					if(selectLine < 0)
					{
						selectLine = 0;
					}
					SetSelectedLine(selectLine);
					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					return true;
				case KeyCode.DownArrow:
					GUI.changed = true;
					DrawGUI.Use(inputEvent);

					selectLine = selectedLine + 1;
					if(selectLine >= code.LineCount)
					{
						selectLine = code.LineCount - 1;
					}
					SetSelectedLine(selectLine);
					return true;
				case KeyCode.RightArrow:
					if(!editMode)
					{
						float max = ContentWidth - lastDrawPosition.width;
						if(max <= 0f)
						{
							if(selectControlOnRight != null && selectControlOnRight())
							{
								return true;
							}
							return false;
						}
						if(HasHorizontalScrollBar())
						{
							max += DrawGUI.ScrollBarWidth;
						}
						float setScrollPos = Mathf.Min(scrollPosition.x + (inputEvent.control ? 1000000f : 30f), max);
						scrollPosition.x = setScrollPos;
						return false;
					}
					return false;
				case KeyCode.LeftArrow:
					if(!editMode)
					{
						if(ContentWidth > lastDrawPosition.width)
						{
							float setScrollPos = Mathf.Max(scrollPosition.x - (inputEvent.control ? 1000000f : 30f), 0f);
							scrollPosition.x = setScrollPos;
						}
						else if(selectControlOnLeft != null && selectControlOnLeft())
						{
							return true;
						}
					}
					return false;
				case KeyCode.Home:
					if(!DrawGUI.EditingTextField || inputEvent.control)
					{
						SetSelectedLine(0);
					}
					return true;
				case KeyCode.End:
					if(!DrawGUI.EditingTextField || inputEvent.control)
					{
						SetSelectedLine(code.LineCount - 1);
					}
					return true;
				case KeyCode.PageUp:
					selectLine = Mathf.Max(selectedLine - Mathf.RoundToInt(lastDrawPosition.height / RowHeight), 0);
					SetSelectedLine(selectLine);
					return true;
				case KeyCode.PageDown:
					selectLine = Mathf.Min(selectedLine + Mathf.RoundToInt(lastDrawPosition.height / RowHeight), code.LineCount - 1);
					SetSelectedLine(selectLine);
					return true;
				case KeyCode.Escape:
					SetEditMode(false, ReasonSelectionChanged.KeyPressShortcut);
					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					return true;
				case KeyCode.F2:
					SetEditMode(true, ReasonSelectionChanged.KeyPressShortcut);
					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					return true;
				case KeyCode.S:
					if(inputEvent.control && unsavedChanges)
					{
						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						return true;
					}
					return false;
				case KeyCode.Tab:
					if(!inputEvent.control && !inputEvent.alt)
					{
						if(inputEvent.shift)
						{
							if(selectedLine <= 0)
							{
								if(selectPreviousControl != null && selectPreviousControl())
								{
									SetSelectedLine(-1);
									return true;
								}
							}
							else
							{
								SetSelectedLine(selectedLine - 1);
								GUI.changed = true;
								DrawGUI.Use(inputEvent);
								return true;
							}
							return false;
						}
						
						GUI.changed = true;
						DrawGUI.Use(inputEvent);

						selectLine = selectedLine + 1;
						if(selectLine < code.LineCount)
						{
							SetSelectedLine(selectedLine + 1);
							return true;
						}

						if(selectNextControl != null && selectNextControl())
						{
							SetSelectedLine(-1);
							return true;
						}
						return false;
					}
					break;
				case KeyCode.Backspace:
				case KeyCode.Delete:
					if((inputEvent.modifiers == EventModifiers.Shift || inputEvent.modifiers == EventModifiers.Control) && selectedLine >= 0)
					{
						GUI.changed = true;
						DrawGUI.UseEvent();
						if(code.LineCount <= 1)
						{
							code.SetLine(selectedLine, "", true);

							if(onTextChanged != null)
							{
								onTextChanged(code.TextUnformatted);
							}
						}
						else
						{
							code.RemoveAt(selectedLine, true);
							UpdateVisibleLines();

							if(onTextChanged != null)
							{
								onTextChanged(code.TextUnformatted);
							}
						}

						selectedLine = Mathf.Min(code.LineCount, selectedLine);
						
						return true;
					}
					return false;
			} 

			//allows supporting various keyboard shortcuts, like SelectNextOfType,
			//but need to be careful to consume all inputs that would result in unwanted behaviour
			//return base.OnKeyboardInputGiven(inputEvent, keys);
			return false;
		}

		private bool HasHorizontalScrollBar()
		{
			return ContentWidth > lastDrawPosition.width;
		}

		protected bool HasVerticalScrollBar()
		{
			return ContentHeight > lastDrawPosition.height;
		}

		public void SetSelectedLine(int index)
		{
			if(code.LineCount < index)
			{
				index = code.LineCount - 1;
			}

			selectedLine = index;

			GUI.changed = true;

			if(selectedLine == -1)
			{
				return;
			}

			FocusSelectedLine();

			float yMin = scrollPosition.y;
			float yTargetMin = index * RowHeight;

			if(yTargetMin < yMin)
			{
				var setScroll = scrollPosition;
				setScroll.y = yTargetMin;
				SetScrollPosition(setScroll);
			}
			else
			{
				float viewHeight = lastDrawPosition.height - DrawGUI.ScrollBarWidth - RowHeight;
				float yMax = scrollPosition.y + viewHeight;
				float yTargetMax = yTargetMin + RowHeight;
				if(yTargetMax >= yMax)
				{
					var setScroll = scrollPosition;
					setScroll.y = yTargetMax - viewHeight;
					SetScrollPosition(setScroll);
				}
			}
		}

		public void Dispose()
		{
			instanceId++;
			pooled = this;

			// NEW: reset state too
			selectedLine = -1;
			editMode = false;
			unsavedChanges = false;
			focusField = 0;
		}
	}
}