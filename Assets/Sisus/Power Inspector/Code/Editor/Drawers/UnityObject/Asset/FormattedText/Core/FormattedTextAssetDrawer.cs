#define USE_THREADING
#define DISPLAY_LINE_COUNT
//#define DISPLAY_UNFORMATTED_UNTIL_SYNTAX_FORMATTING_DONE

#define DEBUG_NULL_FONT
//#define DEBUG_SETUP_TIME
//#define DEBUG_SYNTAX_FORMAT_TIME
#define DEBUG_CLICK
//#define DEBUG_SET_EDIT_MODE

using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Object = UnityEngine.Object;

#if USE_THREADING
using System.Threading;
#endif

namespace Sisus
{
	/// <summary>
	/// Drawer for a text asset file with syntax formatting support.
	/// </summary>
	/// <typeparam name="TFormatter"> Type of the syntax formatter. </typeparam>
	[Serializable]
	public abstract class FormattedTextAssetDrawer<TFormatter> : CustomEditorAssetDrawer, IScrollable where TFormatter : ITextSyntaxFormatter, new()
	{
		private const float RowHeight = DrawGUI.SingleLineHeightWithoutPadding;
		private static bool displayUnformatted;
		private static readonly StringBuilder sb = new StringBuilder(8192);

		private string editedLineControlName = "";
		private int firstVisibleLine;
		private int lastVisibleLine;
		protected bool editDefaultReferences;
		protected bool showAssemblyDefinition = true;
		
		#if DEV_MODE && DEBUG_SETUP_TIME
		private ExecutionTimeLogger setupTimer = new ExecutionTimeLogger();
		#endif

		#if DEV_MODE && DEBUG_SYNTAX_FORMAT_TIME
		private ExecutionTimeLogger syntaxFormatTimer = new ExecutionTimeLogger();
		#endif
		
		protected string textUnformatted = "";

		private int selectedLine = -1;
		private bool editMode;
		
		private int doFullSyntaxFormattingAfterTicks = -1;

		private Vector2 scrollPosition;

		private volatile bool setupInProgress;
		private volatile bool setupDoneAndUnapplied;

		private bool unsavedChanges;
		private int focusField;

		private Code code;
		protected AssemblyDefinitionAsset assemblyDefinitionAsset;
		protected volatile bool assemblyDefinitionAssetReady;

		#if USE_THREADING
		private volatile Code threadGeneratedCode;
		#if DISPLAY_LINE_COUNT
		private volatile int threadGeneratedLineCount;
		#endif
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

		private GUIContent lineCountTooltip = new GUIContent("");

		/// <inheritdoc/>
		public virtual bool HasScrollView
        {
			get
            {
				return !UsesEditorForDrawingBody;
            }
        }

		/// <inheritdoc/>
		public override Part SelectedPart
		{
			get
			{
				return selectedLine != -1 ? Part.Line : base.SelectedPart;
			}
		}

		/// <inheritdoc cref="IDrawer.Height" />
		public override float Height
		{
			get
			{
				return viewportHeight;
			}
		}

		#if UNITY_2018_1_OR_NEWER
		/// <inheritdoc/>
		protected override bool HasPresetIcon
		{
			get
			{
				return false;
			}
		}
		#endif

		/// <inheritdoc/>
		protected override bool UsesEditorForDrawingBody
		{
			get
			{
				return editDefaultReferences || DebugMode;
			}
		}

		/// <inheritdoc/>
		protected override bool CanBeSelectedWithoutHeaderBeingSelected
		{
			get
			{
				return true;
			}
		}

		/// <summary> Gets a value indicating whether entering syntax highlighted mode is allowed. </summary>
		/// <value> True if syntax highlighting allowed, false if not. </value>
		protected virtual bool AllowSyntaxHighlighting
		{
			get
			{
				return true;
			}
		}

		/// <summary> Allow entering edit mode? </summary>
		/// <value> True if editing text asset content is allowed, false if not. </value>
		protected virtual bool AllowEditing
		{
			get
			{
				return !IsPackageAsset;
			}
		}

		private bool DisplayUnformatted
		{
			get
			{
				return displayUnformatted || !AllowSyntaxHighlighting;
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

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		protected static T Create<T>(Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector) where T : FormattedTextAssetDrawer<TFormatter>, new()
		{
			T result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new T();
			}

			#if DEV_MODE && DEBUG_SETUP_TIME
			result.setupTimer.Start("MonoScriptDrawer.Create");
			result.setupTimer.StartInterval("Setup");
			#endif

			result.Setup(targets, targets, null, parent, inspector);

			#if DEV_MODE && DEBUG_SETUP_TIME
			result.setupTimer.FinishInterval();
			result.setupTimer.StartInterval("LateSetup");
			#endif

			result.LateSetup();

			#if DEV_MODE && DEBUG_SETUP_TIME
			result.setupTimer.FinishInterval();
			result.setupTimer.FinishAndLogResults();
			#endif

			return result;
		}

		/// <inheritdoc />
		protected override void Setup(Object[] setTargets, Object[] setEditorTargets, Type setEditorType, IParentDrawer setParent, IInspector setInspector)
		{
			code = new Code(CreateSyntaxFormatter());

			SetEditorTargets(setTargets, ref setEditorTargets);
			
			SetEditMode(false, ReasonSelectionChanged.Initialization);
			SetSelectedLine(-1, ReasonSelectionChanged.Initialization);
			unsavedChanges = false;

			base.Setup(setTargets, setEditorTargets, setEditorType, setParent, setInspector);
			
			if(setTargets.Length == 1)
			{
				UpdateTextFromFile();

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
				int maxVisibleRowsOnScreen = Mathf.CeilToInt(DrawGUI.CurrentAreaHeight / DrawGUI.buttonHeight) + 1;
				BuildMembersWithSyntaxFormatting(maxVisibleRowsOnScreen);
				if(maxVisibleRowsOnScreen < code.Length)
				{
					doFullSyntaxFormattingAfterTicks = 5;
				}
				else
				{
					doFullSyntaxFormattingAfterTicks = -1;
				}
				#endif
			}
		}

		/// <summary> Creates instance of syntax formatter appropriate for the drawer. </summary>
		/// <returns> Syntax formatter. </returns>
		protected abstract TFormatter CreateSyntaxFormatter();

		protected virtual void SetEditorTargets(Object[] setTargets, ref Object[] assetImporters)
		{
			if(assetImporters == null)
			{
				AssetImporters.TryGet(setTargets, ref assetImporters);
			}
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

		protected virtual string GetTextFromFile()
		{
			return File.ReadAllText(FullPath);
		}

		private void UpdateTextFromFile()
		{
			try
			{
				textUnformatted = GetTextFromFile();
			}
			#if DEV_MODE
			catch(UnauthorizedAccessException e)
			{
				Debug.LogWarning(e);
			}
			catch(ArgumentException e) //Empty path
			{
				Debug.LogWarning(e);
			}
			#else
			catch{}
			#endif
		}

		/// <summary>
		/// Rebuild members with syntax formatting.
		/// Work is done on another thread to avoid UX slow downs when user
		/// selects a large formatted text file.
		/// </summary>
		/// <param name="threadTaskId"> Information describing the state. </param>
		private void RebuildMembersWithSyntaxFormattingThreaded(object threadTaskId)
		{
			RebuildAllMembersWithSyntaxFormatting((int)threadTaskId);
		}

		private void RebuildAllMembersWithSyntaxFormatting()
		{
			RebuildAllMembersWithSyntaxFormatting(instanceId);
		}

		private void RebuildAllMembersWithSyntaxFormatting(int threadTaskId)
		{
			#if DEV_MODE && DEBUG_SYNTAX_FORMAT_TIME
			syntaxFormatTimer.Start("SyntaxFormatting");
			#endif

			doFullSyntaxFormattingAfterTicks = -1;

			BuildMembersWithSyntaxFormatting(threadTaskId);

			//when using threading, it's possible that the drawer were
			//disposed or reused for another script while the thread was doing
			//its thing.
			if(threadTaskId == instanceId)
			{
				UpdateVisibleLines();
				setupDoneAndUnapplied = true;
				setupInProgress = false;
			}

			#if DEV_MODE && DEBUG_SYNTAX_FORMAT_TIME
			syntaxFormatTimer.Finish();
			#endif
		}

		private void BuildMembersWithSyntaxFormatting(int threadTaskId)
		{
			BuildMembersWithSyntaxFormatting(CreateSyntaxFormatter(), threadTaskId);
		}

		private void BuildMembersWithSyntaxFormatting(TFormatter builder, int threadTaskId)
		{
			var setCode = new Code(builder);
			builder.SetCode(textUnformatted);
			builder.BuildAllBlocks();

			builder.GeneratedBlocks.ToCode(ref setCode, builder, Fonts.NormalSizes);
			//NOTE: no need to call builder.Dispose here as it's already handled by Code.Dispose

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
				
				#if DISPLAY_LINE_COUNT && USE_THREADING
				threadGeneratedLineCount = builder.GeneratedBlocks.LineCountWithoutCommentsOrEmptyLines();
				#endif
			}
			else
			{
				setCode.Dispose();
			}
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthWhenNotUsingEditor(int indentLevel)
		{
			return LineNumberColumnWidth;
		}

		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList() { }

		/// <inheritdoc />
		protected override void DoBuildMembers() { }


		/// <summary>
		/// Draws everything just like CustomEditorAssetDrawer would, using the Editor, or if Debug Mode is enabled, using member drawers.
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		protected bool DrawUsingEditor(Rect position)
		{
			if(selectedLine != -1)
			{
				SetSelectedLine(-1, ReasonSelectionChanged.BecameInvisible);
			}
			if(editMode)
			{
				SetEditMode(false, ReasonSelectionChanged.Initialization);
			}
			firstVisibleLine = 0;
			lastVisibleLine = 0;

			return base.Draw(position);
		}

		/// <inheritdoc cref="IDrawer.Draw" />
		public override bool Draw(Rect position)
		{
			bool dirty;

			if(editDefaultReferences || DebugMode)
			{
				dirty = DrawUsingEditor(position);
				return dirty;
			}

			Profiler.BeginSample("FormattedTextAssetDrawer.Draw");

			if(ShouldRebuildDrawers())
			{
				#if DEV_MODE
				Debug.LogWarning(this+".Draw() - code was null, rebuilding");
				#endif
				inspector.RebuildDrawersIfTargetsChanged();

				Profiler.EndSample();
				return false;
			}

			float lineNumberColumnWidth = LineNumberColumnWidth;

			var leftColumnRect = position;
			float leftOffset = Mathf.Max(0f, scrollPosition.x);
			leftColumnRect.x = lineNumberColumnWidth - leftOffset;
			leftColumnRect.width = DrawGUI.InspectorWidth - leftColumnRect.x;
			leftColumnRect.height = ContentHeight - scrollPosition.y;

			DrawGUI.Active.ColorRect(leftColumnRect, new Color(0f, 0f, 0f, 0.1f));

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

				#if DISPLAY_LINE_COUNT
				var subtitle = HeaderSubtitle;
				subtitle.tooltip = StringUtils.Concat(subtitle.tooltip, " with ", threadGeneratedLineCount, " lines of code\n(ignoring empty lines and comments).");
				lineCountTooltip.tooltip = subtitle.tooltip;
				#endif

				OnNextLayout(UpdateAssemblyDefinitionAsset);
			}
			#endif

			dirty = false;

			var e = Event.current;

			if(e.type == EventType.KeyDown)
			{
				dirty = OnKeyboardInputGiven(Event.current, inspector.Preferences.keyConfigs);
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
			
			if(doFullSyntaxFormattingAfterTicks >= 0)
			{
				doFullSyntaxFormattingAfterTicks--;
				if(doFullSyntaxFormattingAfterTicks == -1)
				{
					RebuildAllMembersWithSyntaxFormatting();
					GUI.changed = true;
					dirty = true;
				}
			}

			if(e.type == EventType.Layout)
			{
				OnLayoutEvent(position);
			}

			if(!HeadlessMode)
			{
				float headerHeight = HeaderHeight;
				var prefixPosition = position;
				prefixPosition.height = headerHeight;

				if(DrawPrefix(prefixPosition))
				{ 
					dirty = true;
				}

				position.y += headerHeight;
			}

			if(showAssemblyDefinition && Target is MonoScript)
			{
				position.height = DrawGUI.SingleLineHeight;
				EditorGUI.DrawRect(position, Preferences.theme.Background);

				if(!assemblyDefinitionAssetReady)
                {
					position.width += DrawGUI.SingleLineHeight;
					bool guiWasEnabled = GUI.enabled;
					GUI.enabled = false;
					EditorGUI.ObjectField(position, null as Object, typeof(Object), false);
					GUI.enabled = guiWasEnabled;
					position.width -= DrawGUI.SingleLineHeight;
				}
				else if(assemblyDefinitionAsset != null)
				{
					position.width += DrawGUI.SingleLineHeight;
					bool guiWasEnabled = GUI.enabled;
					GUI.enabled = false;
					EditorGUI.ObjectField(position, assemblyDefinitionAssetReady ? assemblyDefinitionAsset : null, typeof(AssemblyDefinitionAsset), false);
					GUI.enabled = guiWasEnabled;
					position.width -= DrawGUI.SingleLineHeight;
				}
				else
				{
					position.x += DrawGUI.RightPadding;
					EditorGUI.LabelField(position, "Assembly-CSharp.dll");
					position.x -= DrawGUI.RightPadding;
				}

				position.y += DrawGUI.SingleLineHeight;
			}

			position.height = RowHeight;
			var windowRect = position;
			windowRect.height = Height;
			var contentRect = windowRect;
			contentRect.y = 0f;
				
			contentRect.height = ContentHeight;
			contentRect.width = ContentWidth;
				
			//if has horizontal scrollbar, adjust height to leave space for it
			if(contentRect.width > windowRect.width)
			{
				windowRect.height -= DrawGUI.ScrollBarWidth;

				//not sure why this is needed...?
				windowRect.height -= DrawGUI.ScrollBarWidth;
			}
				
			var scrollViewBgWas = GUI.skin.scrollView.normal.background;
			GUI.skin.scrollView.normal.background = DrawGUI.DarkTexture;

			var setScrollPosition = GUI.BeginScrollView(windowRect, scrollPosition, contentRect);
			{
				position.y = 0f;
				position.width = Mathf.Max(DrawGUI.InspectorWidth - 2f, ContentWidth);
					
				if(targets.Length > 1)
				{
					position.height = DrawGUI.SingleLineHeightWithPadding;
					GUI.Label(position, string.Concat(StringUtils.ToString(targets.Length), " Text Assets Selected"));
				}
				#if !DISPLAY_UNFORMATTED_UNTIL_SYNTAX_FORMATTING_DONE
				else if(setupInProgress)
				{
					GUI.changed = true;
				}
				#endif
				else
				{
					//only draw lines that are on-screen for optimization purposes
					float firstY = position.y + firstVisibleLine * RowHeight;
					position.y = firstY;

					if(e.type == EventType.MouseDown)
					{
						for(int n = firstVisibleLine; n < lastVisibleLine; n++)
						{
							if(position.Contains(e.mousePosition))
							{
								var lineNumberRect = position;
								lineNumberRect.width = LineNumberColumnWidth;
								OnLineClicked(n, lineNumberRect.Contains(e.mousePosition));
								break;
							}
							position.y += RowHeight;
						}
						position.y = firstY;
					}

					for(int n = firstVisibleLine; n < lastVisibleLine; n++)
					{
						DrawLine(position, n);
						position.y += RowHeight;
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

			return dirty;
		}
		
		#if DISPLAY_LINE_COUNT
		/// <inheritdoc/>
		protected override void DrawHeaderBase(Rect position)
		{
			base.DrawHeaderBase(position);

			position.width = position.width - HeaderButtonsWidth - HeaderButtonsRightSidePadding;
			GUI.Label(position, lineCountTooltip);
		}
		#endif

		private void UpdateAssemblyDefinitionAsset()
		{
			assemblyDefinitionAsset = GetAssemblyDefinitionAsset();
			assemblyDefinitionAssetReady = true;
		}

		private string GetAssemblyDefinitionAssetPath()
        {
			string localFilePath = AssetDatabase.GetAssetPath(Target);
			string directoryPath = Path.GetDirectoryName(localFilePath);

			while(!string.IsNullOrEmpty(directoryPath))
            {
                string[] files = Directory.GetFiles(directoryPath, "*.asmdef", SearchOption.TopDirectoryOnly);

				if(files.Length > 0)
                {
					return files[0];
				}

				directoryPath = Path.GetDirectoryName(directoryPath);
			}

			return null;
		}

		private AssemblyDefinitionAsset GetAssemblyDefinitionAsset()
        {
			if(Target as MonoScript == null)
			{
				return null;
			}
			
			string path = GetAssemblyDefinitionAssetPath();
			return path == null ? null : AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
		}

		/// <inheritdoc />
		protected override bool ShouldRebuildDrawers()
		{
			return code == null;
		}

		/// <inheritdoc />
		protected override void OnLayoutEvent(Rect position)
		{
			viewportHeight = CalculateViewportHeight();

			base.OnLayoutEvent(position);

			if(!heightWhenVisibleLinesLastUpdated.Equals(viewportHeight) || !widthWhenVisibleLinesLastUpdated.Equals(lastDrawPosition.width))
			{
				UpdateVisibleLines();
			}
		}

		private float CalculateViewportHeight()
		{
			float height = inspector.State.WindowRect.height - inspector.ToolbarHeight - inspector.PreviewAreaHeight;

			if(height < 0f)
			{
				return 0f;
			}

			if(HasHorizontalScrollBar())
			{
				height -= DrawGUI.ScrollBarWidth;
			}

			if(!UserSettings.MergedMultiEditMode && inspector.State.inspected.Length > 1)
			{
				height = Mathf.Min(ContentHeight, height);
			}

			if((Target as MonoScript) != null)
            {
				height += DrawGUI.SingleLineHeight;
            }

			return height;
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
				DrawGUI.DrawRect(selectionRect, inspector.Preferences.theme.BackgroundSelected);
			}
			
			if(DisplayUnformatted || (isSelectedLine && editMode))
			{
				if(code.LineCount <= index)
				{
					#if DEV_MODE
					Debug.LogWarning("code.Length ("+code.LineCount+") <= index ("+index+")");
					#endif

					SetSelectedLine(-1, ReasonSelectionChanged.Initialization);
					UpdateTextFromFile();
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

								code.SetLine(index, setLine, false);
								SetHasUnsavedChanges(true);
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

		private void SetHasUnsavedChanges(bool setUnsavedChanges)
		{
			if(unsavedChanges != setUnsavedChanges)
			{
				unsavedChanges = setUnsavedChanges;
				RebuildHeaderButtons();
			}
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

			int lineNumber = index + 1;
			
			switch(Event.current.button)
			{
				case 0:
					if(selectedLine != index)
					{
						SetSelectedLine(index, ReasonSelectionChanged.Initialization);
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
						else
						{
							#if DEV_MODE && DEBUG_CLICK
							Debug.Log("Opening " + Target.name + ".cs at line "+lineNumber+"...");
							#endif

							//when a line number is double clicked
							//open text editor at the line in question
							AssetDatabase.OpenAsset(Target, lineNumber);
						}
					}

					break;
				case 1:
					var menu = Menu.Create();
					menu.Add("Open At Line "+lineNumber, ()=>AssetDatabase.OpenAsset(Target, lineNumber));
					menu.Add("Copy Line To Clipboard", ()=>CopyLineToClipboard(index));
					if(AllowEditing)
					{
						menu.Add("Delete Row", ()=>code.RemoveAt(index, false));
						menu.Add("Insert Row", ()=>code.InsertAt(index, "", false));
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
							code.InsertAt(index, string.Concat(leadingWhiteSpace, "/// </summary>"), false);
							code.InsertAt(index, string.Concat(leadingWhiteSpace, "/// "), false);
							code.InsertAt(index, string.Concat(leadingWhiteSpace, "/// <summary>"), false);
							inspector.OnNextLayout(()=> selectedLine = index + 1);
							UpdateVisibleLines();
						});
					}
					
					ContextMenuUtility.Open(menu, this, Part.LineNumber);
					break;
				//when a row is middle clicked
				//open text editor at the line in question
				case 2:
					AssetDatabase.OpenAsset(Target, lineNumber);
					break;
			}
		}

		private void SaveChanges()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(unsavedChanges);
			#endif

			SetHasUnsavedChanges(false);

			string fullPath = FullPath;
			
			if(inspector != null)
			{
				inspector.Message("Saving changes to file @ "+fullPath);
			}
			
			File.WriteAllText(fullPath, code.ToString());
			EditorUtility.SetDirty(Target);
			AssetDatabase.Refresh();

			doFullSyntaxFormattingAfterTicks = 5;
		}

		private void DiscardChanges(ReasonSelectionChanged reason)
		{
			if(inspector != null)
			{
				inspector.Message("Discarding changes to file @ " + FullPath);
			}

			UpdateTextFromFile();
			RebuildAllMembersWithSyntaxFormatting();

			SetHasUnsavedChanges(false);

			SetEditMode(false, reason);
		}

		/// <inheritdoc/>
		protected override void Open()
		{
			AssetDatabase.OpenAsset(Target);
			SetSelectedLine(-1, ReasonSelectionChanged.PrefixClicked);
		}

		/// <inheritdoc/>
		protected override void ShowInExplorer()
		{
			EditorUtility.RevealInFinder(LocalPath);
		}

		private void OnUnappliedChangesButtonClicked()
		{
			var menu = Menu.Create();
			menu.Add("Save Changes", SaveChanges);
			menu.Add("Discard Changes", OnDiscardChangesButtonClicked);
			ContextMenuUtility.OpenAt(menu, PrefixLabelPosition, this, Part.UnappliedChangesButton);
		}

		private void OnDiscardChangesButtonClicked()
		{
			DiscardChanges(ReasonSelectionChanged.ControlClicked);
		}

		/// <inheritdoc/>
		protected override void DoBuildHeaderButtons()
		{
			base.DoBuildHeaderButtons();

			if(unsavedChanges)
			{
				AddHeaderButton(Button.Create(GUIContentPool.Create("Changes", "Save or discard unapplied changes to the file."), OnUnappliedChangesButtonClicked));
			}

			if(AllowEditing)
			{
				if(editMode)
				{
					AddHeaderButton(Button.Create(InspectorLabels.Current.StopEditing, OnToggleEditModeButtonClicked, new Color(0.75f, 1f, 0.75f, 1f)));
				}
				else
				{
					AddHeaderButton(Button.Create(InspectorLabels.Current.StartEditing, OnToggleEditModeButtonClicked));
				}
			}

			if(AllowSyntaxHighlighting)
			{
				var labels = inspector.Preferences.labels;

				if(!displayUnformatted)
				{
					AddHeaderButton(Button.Create(labels.Formatted, OnToggleDisplayFormattedButtonClicked, new Color(0.75f, 1f, 0.75f, 1f)));
				}
				else
				{
					AddHeaderButton(Button.Create(labels.Unformatted, OnToggleDisplayFormattedButtonClicked));
				}
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
				DrawGUI.EditingTextField = false;
				focusField = 0;
				GUI.changed = true;
				RebuildHeaderButtons();
			}
		}

		private void OnToggleEditModeButtonClicked()
		{
			SetEditMode(!editMode, ReasonSelectionChanged.ControlClicked);
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

		/// <inheritdoc/>
		protected override void SelectHeaderPart(HeaderPartDrawer select, bool setKeyboardControl = true)
		{
			if(select != null && selectedLine != -1)
			{
				SetSelectedLine(-1, ReasonSelectionChanged.PrefixClicked);
			}
			base.SelectHeaderPart(select, setKeyboardControl);
		}

		private void SetSelectedLine(int index, ReasonSelectionChanged reason)
		{
			selectedLine = index;
			FocusSelectedLine();

			float yMin = scrollPosition.y;
			float yTargetMin = index * RowHeight;
			
			if(selectedLine != -1)
			{
				if(SelectedHeaderPart != HeaderPart.None)
				{
					SelectHeaderPart(HeaderPart.None);
				}

				if(yTargetMin < yMin)
				{
					var setScroll = scrollPosition;
					setScroll.y = yTargetMin;
					SetScrollPosition(setScroll);
				}
				else
				{
					float viewHeight = Height - DrawGUI.ScrollBarWidth - RowHeight;
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

			if(index != -1 && !Selected)
			{
				Select(reason);
			}

			GUI.changed = true;
		}

		private void SetEditMode(bool setEditMode, ReasonSelectionChanged reason)
		{
			if(setupInProgress)
			{
				setEditMode = false;
			}

			if(setEditMode == editMode)
			{
				return;
			}

			#if DEV_MODE && DEBUG_SET_EDIT_MODE
			Debug.Log(StringUtils.ToColorizedString("SetEditMode ", setEditMode, " because ", reason, " (was=", editMode, "), with setupInProgress=", setupInProgress, ", selectedLine=", selectedLine, ", lineCount=", code.LineCount, ", EditingTextField=", DrawGUI.EditingTextField));
			#endif
			
			if(setEditMode)
			{
				editMode = true;
				if(selectedLine < 0)
				{
					SetSelectedLine(0, reason);
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
				DrawGUI.EditingTextField = false;
				focusField = 0;
			}

			RebuildHeaderButtons();
		}

		private bool HasHorizontalScrollBar()
		{
			return ContentWidth > Width;
		}

		protected bool HasVerticalScrollBar()
		{
			return ContentHeight > Height;
		}

		/// <inheritdoc/>
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			if(HeaderIsSelected)
			{
				return base.OnKeyboardInputGiven(inputEvent, keys);
			}

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
							code.InsertAt(inputEvent.shift ? selectedLine : selectedLine + 1, "", false); //shift+enter adds above, ctrl+enter adds below
							SetHasUnsavedChanges(true);
							if(inputEvent.control)
							{
								selectedLine = Mathf.Min(code.LineCount - 1, selectedLine + 1);
							}
							UpdateVisibleLines();
							return true;
						}
						return false;
					}
					//here should apply the currently edited line before changing row!
					//DrawGUI.EditingTextField = false;
					//TO DO: support shift+enter to add line breaks or something?
					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					//editingLine = !editingLine;
					SetEditMode(!editMode, ReasonSelectionChanged.KeyPressShortcut);
					return true;
				case KeyCode.UpArrow:
					int selectLine = selectedLine - 1;
					if(selectLine >= 0)
					{
						SetSelectedLine(selectedLine - 1, ReasonSelectionChanged.SelectControlUp);
					}
					else
					{
						SelectHeaderPart(HeaderPart.Base);
					}
					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					return true;
				case KeyCode.DownArrow:

					GUI.changed = true;
					DrawGUI.Use(inputEvent);

					selectLine = selectedLine + 1;
					if(selectLine < code.LineCount)
					{
						SetSelectedLine(selectLine, ReasonSelectionChanged.SelectControlDown);
						return true;
					}
					inspector.Select(GetNextSelectableDrawerDown(0, this), ReasonSelectionChanged.SelectControlDown);
					//return false;
					return true;
				case KeyCode.RightArrow:
					if(!editMode)
					{
						float max = ContentWidth - Width;
						if(max <= 0f)
						{
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
					if(!editMode && ContentWidth > Width)
					{
						float setScrollPos = Mathf.Max(scrollPosition.x - (inputEvent.control ? 1000000f : 30f), 0f);
						scrollPosition.x = setScrollPos;
						return false;
					}
					return false;
				case KeyCode.Home:
					if(!DrawGUI.EditingTextField || inputEvent.control)
					{
						SetSelectedLine(0, ReasonSelectionChanged.KeyPressShortcut);
					}
					return true;
				case KeyCode.End:
					if(!DrawGUI.EditingTextField || inputEvent.control)
					{
						SetSelectedLine(code.LineCount - 1, ReasonSelectionChanged.KeyPressShortcut);
					}
					return true;
				case KeyCode.PageUp:
					selectLine = Mathf.Max(selectedLine - Mathf.RoundToInt(Height / RowHeight), 0);
					SetSelectedLine(selectLine, ReasonSelectionChanged.KeyPressShortcut);
					return true;
				case KeyCode.PageDown:
					selectLine = Mathf.Min(selectedLine + Mathf.RoundToInt(Height / RowHeight), code.LineCount - 1);
					SetSelectedLine(selectLine, ReasonSelectionChanged.KeyPressShortcut);
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
						SaveChanges();
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
							if(selectedLine > -1)
							{
								SetSelectedLine(selectedLine - 1, ReasonSelectionChanged.SelectControlUp);
								if(selectedLine == -1)
								{
									SelectHeaderPart(HeaderPart.ContextMenuIcon);
								}
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
							SetSelectedLine(selectedLine+1, ReasonSelectionChanged.SelectNextControl);
							return true;
						}

						var select = GetNextSelectableDrawerDown(0, this);
						if(select == null || select == this)
						{
							KeyboardControlUtility.SetKeyboardControl(0, 3);
							return false;
						}
						inspector.Select(select, ReasonSelectionChanged.SelectNextControl);
						return true;
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
							code.SetLine(selectedLine, "", false);
						}
						else
						{
							code.RemoveAt(selectedLine, false);
							UpdateVisibleLines();
						}

						selectedLine = Mathf.Min(code.LineCount, selectedLine);
						
						return true;
					}
					return false;
			} 

			//allows supporting various keyboard shortcuts, like SelectNextOfType,
			//but need to be careful to consume all inputs that would result in unwanted behaviour
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc/>
		public override void AddItemsToOpeningViewMenu(ref Menu menu)
		{
			#if DEV_MODE
			Debug.Log(ToString() + ".FormattedTextAsset.AddItemsToOpeningViewMenu");
			#endif
			
			if(AllowSyntaxHighlighting)
			{
				if(menu.Contains("Syntax Formatting"))
				{
					#if DEV_MODE
					Debug.LogWarning(ToString()+".AddItemsToOpeningViewMenu aborting because menu already contained item \"Syntax Formatting\". This is normal in stacked multi-editing mode.");
					#endif
					return;
				}
				menu.Add("Syntax Formatting", ()=>displayUnformatted=!displayUnformatted, DisplayUnformatted);
			}
			base.AddItemsToOpeningViewMenu(ref menu);
		}

		/// <inheritdoc/>
		protected override void SelectNextFieldDown(int column, bool additive = false)
		{
			if(code.LineCount > 0)
			{
				SetSelectedLine(0, ReasonSelectionChanged.SelectControlDown);
				SelectHeaderPart(HeaderPart.None);
			}
			else
			{
				inspector.Select(GetNextSelectableDrawerDown(0, this), ReasonSelectionChanged.SelectControlDown);
			}
		}

		/// <inheritdoc/>
		protected override void SelectNextFieldUp(int column, bool additive = false)
		{
			int lineCount = code.LineCount;
			if(lineCount > 0)
			{
				SetSelectedLine(lineCount - 1, ReasonSelectionChanged.SelectControlUp);
				SelectHeaderPart(HeaderPart.None);
			}
			else
			{
				SetSelectedLine(-1, ReasonSelectionChanged.SelectControlUp);
				SelectHeaderPart(HeaderPart.Base);
			}
		}

		/// <inheritdoc/>
		protected override void SelectFirstField()
		{
			if(code.LineCount > 0)
			{
				SelectHeaderPart(HeaderPart.None);
				SetSelectedLine(0, ReasonSelectionChanged.SelectNextControl);
			}
			else
			{
				inspector.Select(GetNextSelectableDrawerRight(true, this), ReasonSelectionChanged.SelectNextControl);
			}
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			instanceId++;

			setupInProgress = false;
			setupDoneAndUnapplied = false;
			textUnformatted = "";
			selectedLine = -1;
			firstVisibleLine = 0;
			lastVisibleLine = 0;
			scrollPosition.x = 0f;
			scrollPosition.y = 0f;
			editDefaultReferences = false;
			assemblyDefinitionAssetReady = false;

			if(code != null)
			{
				code.Dispose();
				code = null;
			}

			if(threadGeneratedCode != null)
			{
				threadGeneratedCode.Dispose();
				threadGeneratedCode = null;
			}

			#if DEV_MODE && DEBUG_SETUP_TIME
			setupTimer.Reset();
			#endif

			if(unsavedChanges)
			{
				if(EditorUtility.DisplayDialog("Save Changes To Script?", "You have made unsaved changes to the script. Would you like to save them or discard them?", "Save Changes", "Discard Changes"))
				{
					SaveChanges();
				}
				unsavedChanges = false;
			}

			base.Dispose();
		}

		/// <inheritdoc />
		protected override void OnDeselectedInternal(ReasonSelectionChanged reason, IDrawer losingFocusTo)
		{
			if(selectedLine != -1)
			{
				SetSelectedLine(-1, reason);
			}
			base.OnDeselectedInternal(reason, losingFocusTo);
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			// NOTE: This can't be located in MonoScriptDrawer
			// since disabled scripts are not MonoScripts but just TextAssets
			if(IsDisabledAsset())
			{
				menu.AddSeparatorIfNotRedundant();
				menu.Add("Undisable", "Undisables targets by removing a supernumerary extension (like \".disabled\" or \".tmp\") from their filenames.", Undisable);
			}

			menu.AddSeparatorIfNotRedundant();
			menu.Add("Copy All Text", CopyTextToClipboard);
			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		private void CopyTextToClipboard()
		{
			Clipboard.Copy(textUnformatted);
			Clipboard.SendCopyToClipboardMessage("Copied{0} text", GetFieldNameForMessages(), "");
		}

		private void CopyLineToClipboard(int lineIndex)
		{
			string lineText = code.GetLineUnformatted(lineIndex);
			Clipboard.Copy(lineText);
			Clipboard.SendCopyToClipboardMessage("Copied line "+StringUtils.ToString(lineIndex + 1), "", lineText);
		}

		/// <inheritdoc cref="CustomEditorAssetDrawer.GetValue(int)" />
		public override object GetValue(int index)
		{
			return textUnformatted;
		}

		/// <summary>
		/// Restores disabled targets by removing a supernumerary extension (like ".disabled" or ".tmp") from their filenames.
		/// </summary>
		private void Undisable()
		{
			var cacheInspector = inspector;

			FileUtility.Undisable(targets);

			cacheInspector.RebuildDrawers(ArrayPool<Object>.ZeroSizeArray, true);
			cacheInspector.OnNextLayout(()=>cacheInspector.RebuildDrawers(false));
		}

		/// <inheritdoc />
		protected override void DrawImportedObjectGUI() { }

		/// <inheritdoc />
		protected override void GetHeaderSubtitle(ref GUIContent subtitle)
		{
			TextAssetUtility.GetHeaderSubtitle(ref subtitle, LocalPath);
		}
	}
}