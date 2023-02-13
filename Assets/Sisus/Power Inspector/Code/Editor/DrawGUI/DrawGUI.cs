#define ALWAYS_RUNTIME_FIELDS_IN_EDITOR
#define SAFE_MODE

//#define ENABLE_TOOLTIP_ICONS
//#define DEBUG_USE_ANY_EVENT
//#define DEBUG_USE_LMB_EVENT
//#define DEBUG_USE_MOUSE_EVENT
//#define DEBUG_USE_RETURN_EVENT
//#define DEBUG_USE_KEYBOARD_EVENT
#define DEBUG_FOCUS_CONTROL
//#define DEBUG_SET_INDENT_LEVEL
//#define DEBUG_SETUP_TIME
#define DEBUG_SET_EDITING_TEXT_FIELD
//#define DEBUG_LAST_INPUT_EVENT
//#define DEBUG_SET_PREFIX_LABEL_WIDTH
//#define DEBUG_BEGIN_ONGUI
//#define DEBUG_ENSURE_ON_GUI_CALLBACKS
//#define DEBUG_SET_CURSOR

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public abstract class DrawGUI
	{
		public const float SingleLetterPrefixWidth = 14f;
		public const float MinWidthWithSingleLetterPrefix = 35f;

		public const float MinPrefixLabelWidth = 9f;
		public const float MinControlFieldWidth = 80f; // UPDATE: changed from 74 to 80 to fix property Get and Set buttons not having enough space in the modern UI
		public const float DefaultPrefixLabelWidth = 125f;

		#if UNITY_2019_3_OR_NEWER
		public const float ScrollBarWidth = 13f;
		#else
		public const float ScrollBarWidth = 15f;
		#endif
		
		public const float SingleLineHeight = SingleLineHeightWithPadding;
		public const float SingleLineHeightWithoutPadding = 18f;
		public const float SingleLineHeightWithPadding = SingleLineHeightWithoutPadding + SpaceBetweenLines / 2;

		#if UNITY_2019_3_OR_NEWER
		public const float SpaceBetweenLines = 4f;
		#else
		public const float SpaceBetweenLines = 0f;
		#endif

		public const float LeftPadding = 16f;
		public const float RightPadding = 5f;
		public const float TopPadding = 1f;
		public const float BottomPadding = 2f; //UPDATE: Changed from 4 to 2 because of Extended Transform Editor
		public const float MiddlePadding = 8f;
		public const float IndentWidth = 15f;

		public static Action<Object[]> OnDragAndDropObjectReferencesChanged;
		public static bool drawBackgroundBehindFoldouts = true;

		public static bool ExecutingCustomMenuCommand;

		public static GUIStyle prefixLabel;
		public static GUIStyle prefixLabelSelected;
		public static GUIStyle prefixLabelSelectedUnfocused;
		public static GUIStyle prefixLabelWhite;
		public static GUIStyle prefixLabelMouseovered;
		public static GUIStyle prefixLabelMouseoveredModified;
		public static GUIStyle prefixLabelModified;
		public static GUIStyle prefixLabelSelectedModified;
		public static GUIStyle prefixLabelSelectedModifiedUnfocused;
		public static GUIStyle foldoutStyle;
		public static GUIStyle foldoutStyleSelected;
		public static GUIStyle foldoutStyleSelectedUnfocused;
		public static GUIStyle foldoutStyleMouseovered;
		public static GUIStyle foldoutStyleSelectedMouseovered;
		public static GUIStyle foldoutStyleSelectedMouseoveredUnfocused;

		public static GUIStyle prefixLabelBoldCentered;
		public static GUIStyle richTextLabel;
		public static GUIStyle richTextLabelWhite;
		public static GUIStyle tooltipStyle;
		public static GUIStyle mouseoverFxStyle;

		private static Action onBeginOnGUI;
		private static Action onNextBeginOnGUI;
		public static Action<EventType, Event> OnEventUsed;

		protected static readonly GUIContent CachedLabel = new GUIContent("");
		
		private static float prefixLabelWidth = DefaultPrefixLabelWidth;
		private static Event lastInputEvent;
		private static EventType lastInputEventType = EventType.Ignore;
		
		private static bool editingTextField;
		
		private static bool hadDragAndDropObjectReferencesLastFrame;
		
		private static int indentLvl;

		public static bool setupDone;
		
		private static List<Rect> currentAreaRects = new List<Rect>(1);
		private static Material selectionLineMaterial;
		private static Texture2D darkTexture;
		private static Vector2 p1;
		private static Vector2 p2;
		
		private static List<Vector2> activeScrollViewScrollPosition = new List<Vector2>();
		private static List<Rect> activeScrollViewViewportRect = new List<Rect>();
		private static List<Rect> activeScrollViewContentRect = new List<Rect>();
		private static int activeScrollViewDepth;

		/// <summary>
		/// Color tint to apply to GUI.backgroundColor during play mode, or white if not currently in play mode.
		/// </summary>
		public static Color UniversalColorTint = new Color(1f, 1f, 1f, 1f);

		/// <summary>
		/// Background color multiplied with play mode tinting when in play mode.
		/// </summary>
		public static Color TintedBackgroundColor = new Color32(194, 194, 194, 255);

		/// <summary>
		/// True if play mode is active and user has configured a color tint to be used when in play mode.
		/// </summary>
		public static bool ShouldApplyPlayModeTinting;

		public static bool IsProSkin;
		
		#if DEV_MODE && DEBUG_SETUP_TIME
		private static ExecutionTimeLogger setupTimer = new ExecutionTimeLogger();
		#endif

		private static Vector2 onWindowBeginScreenPoint;
		private static Vector2 localDrawAreaoffset;

		public static readonly GUILayoutOption[] ZeroHeight;

		static DrawGUI()
		{
			PlayMode.OnStateChanged += OnEditorPlaymodeStateChanged;
			StaticCoroutine.OnNextBeginOnGUI = OnNextBeginOnGUI;
			ZeroHeight = new GUILayoutOption[] { GUILayout.Height(0f) };
		}

		public static int OnGUICallCount
		{
			get;
			private set;
		}

		public static Vector2 ActiveScrollViewScrollPosition
		{
			get
			{
				int lastIndex = activeScrollViewScrollPosition.Count - 1;
				if(lastIndex >= 0)
				{
					return activeScrollViewScrollPosition[0];
				}
				return Vector2.zero;
			}
		}

		public static Rect ActiveScrollViewViewportRect
		{
			get
			{
				int lastIndex = activeScrollViewViewportRect.Count - 1;
				if(lastIndex >= 0)
				{
					return activeScrollViewViewportRect[0];
				}
				var inspector = InspectorUtility.ActiveInspector;
				if(inspector != null)
				{
					return inspector.State.WindowRect;
				}
				return new Rect(0f, 0f, Screen.width, Screen.height);
			}
		}

		public static Vector2 OnWindowBeginScreenPoint
		{
			get
			{
				return onWindowBeginScreenPoint;
			}
		}

		public static DrawGUI Active
		{
			get
			{
				return Platform.GUIDrawer;
			}
		}

		public static DrawGUI Runtime
		{
			get
			{
				return Platform.RuntimeGUIDrawer;
			}
		}

		public static DrawGUI Editor
		{
			get
			{
				return Platform.Editor.GUI;
			}
		}

		public Color InspectorBackgroundColor
		{
			get
			{
				return InspectorUtility.Preferences.theme.Background;
			}
		}

		public static bool ActionKey
		{
			get
			{
				return EditorGUI.actionKey;
			}
		}
		
		public static int IndentLevel
		{
			get
			{
				return indentLvl;
			}

			set
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(value >= 0, value);
				Debug.Assert(EditorGUI.indentLevel >= 0, EditorGUI.indentLevel);
				#endif

				#if DEV_MODE && DEBUG_SET_INDENT_LEVEL
				if(indentLvl != value || EditorGUI.indentLevel != value) { Debug.Log("indentLvl = "+value); }
				#endif

				indentLvl = value;
				EditorGUI.indentLevel = value;
			}
		}

		/// <summary> Height of Inspector Titlebar, such as the header or Components. </summary>
		/// <value> The height of the inspector titlebar. </value>
		public abstract float InspectorTitlebarHeight
		{
			get;
		}
		
		public static bool ShowMixedValue
		{
			get
			{
				return EditorGUI.showMixedValue;
			}

			set
			{
				#if DEV_MODE && DEBUG_SHOW_MIXED_VALUE
				if(EditorGUI.showMixedValue != value || value) { Debug.Log("EditorGUI.showMixedValue = " + StringUtils.ToColorizedString(value)); }
				#endif

				EditorGUI.showMixedValue = value;
			}
		}

		/// <summary>
		/// Sets drag and drop object references and handles calling
		/// DragAndDrop.StartDrag("Drag") and DragAndDrop.PrepareStartDrag.
		/// 
		/// This should only be called during MouseDown and MouseDrag events.
		/// </summary>
		public abstract Object[] DragAndDropObjectReferences
		{
			get;
			set;
		}

		public abstract DragAndDropVisualMode DragAndDropVisualMode
		{
			get;
			set;
		}


		public static bool IsFullInspectorWidth(float width)
		{
			return width == GetCurrentDrawAreaWidth();
		}
		
		public static float InspectorWidth
		{
			get
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(InspectorUtility.ActiveInspector.State.contentRect.width > 0f, "InspectorWidth value was <= 0: "+ InspectorUtility.ActiveInspector.State.contentRect.width);
				#endif

				return InspectorUtility.ActiveInspector.State.contentRect.width;
			}
		}
		
		public static float InspectorHeight
		{
			get
			{
				return InspectorUtility.ActiveInspector.State.WindowRect.height;
			}
		}
		
		public static float MinAutoSizedPrefixLabelWidth
		{
			get
			{
				return InspectorUtility.Preferences.minAutoSizedPrefixColumnWidth;
			}
		}

		public static float MaxAutoSizedPrefixLabelWidth
		{
			get
			{
				return GetCurrentDrawAreaWidth() * InspectorUtility.Preferences.maxAutoSizedPrefixColumnWidth;
			}
		}

		/// <summary>
		/// Gets or sets the width of the prefix label column.
		/// 
		/// This is the left side of the inspector.
		/// 
		/// You can calculate the width of the control column
		/// by subtracting PrefixLabelWidth from InspectorWidth.
		/// </summary>
		/// <value> The width of the prefix label column. </value>
		public static float PrefixLabelWidth
		{
			get
			{
				return Mathf.Clamp(prefixLabelWidth, MinPrefixLabelWidth, GetCurrentDrawAreaWidth() - MinControlFieldWidth);
			}

			set
			{
				#if DEV_MODE && DEBUG_SET_PREFIX_LABEL_WIDTH
				if(prefixLabelWidth != value){ Debug.Log("prefixLabelWidth = "+value); }
				#endif

				PrefixResizeUtility.ApplyPrefixLabelWidthToEditorGUIUtility(value);
				prefixLabelWidth = value;
			}
		}

		public static bool IsUnityObjectDrag
		{
			get
			{
				return Platform.Active.GUI.DragAndDropObjectReferences.Length > 0;
			}
		}
		
		public static bool EditingTextField
		{
			get
			{
				#if DEV_MODE && PI_ASSERTATIONS
				if(EditorGUIUtility.editingTextField != editingTextField && InspectorUtility.ActiveManager != null && InspectorUtility.ActiveManager.FocusedDrawer != null && InspectorUtility.ActiveManager.SelectedInspector.InspectorDrawer.HasFocus)
				{
					Debug.LogWarning("DrawGUI.EditingTextField (" + StringUtils.ToColorizedString(editingTextField) + ") != EditorGUIUtility.editingTextField (" + StringUtils.ToColorizedString(EditorGUIUtility.editingTextField) + ") with Event=" + StringUtils.ToString(Event.current));
				}
				#endif

				return editingTextField;
			}

			set
			{
				#if DEV_MODE
				Debug.Assert(!value || InspectorUtility.ActiveManager == null || !InspectorUtility.ActiveManager.HasMultiSelectedControls);
				#endif

				#if DEV_MODE && DEBUG_SET_EDITING_TEXT_FIELD
				if(editingTextField != value) { Debug.Log(StringUtils.ToColorizedString("EditingTextField = ", value, " (with KeyboardControl=", KeyboardControlUtility.KeyboardControl, ", hotControl="+GUIUtility.hotControl + ")")); }
				#endif

				editingTextField = value;

				if(EditorGUIUtility.editingTextField == value)
				{
					return;
				}

				// There's an issue where we might want to revert EditorGUIUtility.editingTextField to false e.g. when a text field drawer loses focus,
				// but this might happen in reaction to the user clicking a text field on another EditorWindow. To avoid this issue, EditorGUIUtility.editingTextField is not set to false
				// Unless no window is focused or the focused window is one of our own.
				// TODO: Improve this with smarter methods that maybe take an expectedEditorWindow argument, and only apply their effects to EditorGUIUtility.editingTextField if the
				// selected window matches the expected window.
				if(!value && EditorWindow.focusedWindow != null)
				{
					var windowNamespace = EditorWindow.focusedWindow.GetType().Namespace;
					if(windowNamespace == null || !windowNamespace.StartsWith("Sisus", StringComparison.Ordinal))
					{
						#if DEV_MODE
						Debug.Log(StringUtils.ToColorizedString("EditingTextField = ", value, " called when possibly editing text field on EditorWindow "+ EditorWindow.focusedWindow.GetType().FullName+ ". Won't set EditorGUIUtility.editingTextField to avoid interrupting editing."));
						#endif
						return;
					}
				}

				#if DEV_MODE && DEBUG_SET_EDITING_TEXT_FIELD
				Debug.Log(StringUtils.ToColorizedString("EditorGUIUtility.editingTextField = ", value, " (with EditorWindow.focusedWindow=", EditorWindow.focusedWindow, ")"));
				#endif

				EditorGUIUtility.editingTextField = value;
			}
		}

		public static Texture2D WhiteTexture
		{
			get
			{
				return EditorGUIUtility.whiteTexture;
			}
		}

		public static Texture2D DarkTexture
		{
			get
			{
				if(darkTexture == null)
				{
					darkTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
					darkTexture.SetPixel(0, 0, InspectorUtility.Preferences.themes.Pro.Background);
					darkTexture.filterMode = FilterMode.Point;
					darkTexture.Apply();
				}
				return darkTexture;
			}
		}
		
		public static void Setup([NotNull]InspectorPreferences preferences)
		{
			if(!setupDone)
			{
				DoSetup(preferences);
			}
		}
		
		private static void DoSetup([NotNull]InspectorPreferences preferences)
		{
			#if DEV_MODE && DEBUG_SETUP_TIME
			setupTimer.Start("Setup");
			setupTimer.StartInterval("Generate Styles");
			#endif

			Profiler.BeginSample("Setup");
			
			#if UNITY_EDITOR
			IsProSkin = EditorGUIUtility.isProSkin;
			#endif
			
			ApplyPlayModeTinting(EditorApplication.isPlayingOrWillChangePlaymode);

			var theme = preferences.theme;

			var prefixMouseoveredTextColor = preferences.PrefixMouseoveredTextColor;
			var prefixSelectedAndMouseoveredTextColor = preferences.PrefixSelectedAndMouseoveredTextColor;
			
			if(prefixLabel == null)
			{
				prefixLabel = new GUIStyle(GUI.skin.label);
				prefixLabel.fontStyle = FontStyle.Normal;
				prefixLabel.SetAllTextColors(theme.PrefixIdleText);
			}

			if(prefixLabelSelected == null)
			{
				prefixLabelSelected = new GUIStyle(GUI.skin.label);
				prefixLabelSelected.fontStyle = FontStyle.Normal;
				prefixLabelSelected.SetAllTextColors(theme.PrefixSelectedText);

				prefixLabelSelectedUnfocused = new GUIStyle(prefixLabelSelected);
				prefixLabelSelectedUnfocused.SetAllTextColors(theme.PrefixSelectedUnfocusedText);
			}

			if(prefixLabelWhite == null)
			{
				prefixLabelWhite = new GUIStyle(GUI.skin.label);
				prefixLabelWhite.fontStyle = FontStyle.Normal;
				prefixLabelWhite.SetAllTextColors(Color.white);
			}

			if(prefixLabelMouseovered == null)
			{
				prefixLabelMouseovered = new GUIStyle(GUI.skin.label);
				prefixLabelMouseovered.fontStyle = FontStyle.Normal;
				prefixLabelMouseovered.SetAllTextColors(prefixMouseoveredTextColor);
			}

			if(prefixLabelMouseoveredModified == null)
			{
				prefixLabelMouseoveredModified = new GUIStyle(GUI.skin.label);
				prefixLabelMouseoveredModified.fontStyle = FontStyle.Bold;
				prefixLabelMouseoveredModified.SetAllTextColors(prefixMouseoveredTextColor);
			}

			if(prefixLabelModified == null)
			{
				prefixLabelModified = new GUIStyle(GUI.skin.label);
				prefixLabelModified.fontStyle = FontStyle.Bold;
				prefixLabelModified.SetAllTextColors(theme.PrefixIdleText);
			}

			if(prefixLabelSelectedModified == null)
			{
				prefixLabelSelectedModified = new GUIStyle(GUI.skin.label);
				prefixLabelSelectedModified.fontStyle = FontStyle.Bold;
				prefixLabelSelectedModified.SetAllTextColors(theme.PrefixSelectedText);

				prefixLabelSelectedModifiedUnfocused = new GUIStyle(prefixLabelSelectedModified);
				prefixLabelSelectedModifiedUnfocused.SetAllTextColors(theme.PrefixSelectedUnfocusedText);

			}

			if(foldoutStyle == null)
			{
				foldoutStyle = new GUIStyle(preferences.GetStyle("Foldout"));
				#if UNITY_EDITOR
				foldoutStyle.font = EditorStyles.whiteLabel.font;
				#endif
				foldoutStyle.richText = true;
				foldoutStyle.SetAllTextColors(theme.PrefixIdleText);
			}

			if(foldoutStyleSelected == null)
			{
				foldoutStyleSelected = new GUIStyle(foldoutStyle);
				foldoutStyleSelected.SetAllTextColors(theme.PrefixSelectedText);

				var bg = foldoutStyleSelected.focused.background;
				foldoutStyleSelected.normal.background = bg;
				foldoutStyleSelected.hover.background = bg;
				bg = foldoutStyleSelected.onFocused.background;
				foldoutStyleSelected.onNormal.background = bg;
				foldoutStyleSelected.onHover.background = bg;

				foldoutStyleSelectedUnfocused = new GUIStyle(foldoutStyleSelected);
				foldoutStyleSelectedUnfocused.SetAllTextColors(theme.PrefixSelectedUnfocusedText);
			}

			if(foldoutStyleMouseovered == null)
			{
				foldoutStyleMouseovered = new GUIStyle(foldoutStyle);
				foldoutStyleMouseovered.SetAllTextColors(prefixMouseoveredTextColor);
			}

			if(foldoutStyleSelectedMouseovered == null)
			{
				foldoutStyleSelectedMouseovered = new GUIStyle(foldoutStyle);
				foldoutStyleSelectedMouseovered.SetAllTextColors(prefixSelectedAndMouseoveredTextColor);

				foldoutStyleSelectedMouseoveredUnfocused = new GUIStyle(foldoutStyleSelectedMouseovered);
				foldoutStyleSelectedMouseoveredUnfocused.SetAllTextColors(theme.PrefixSelectedUnfocusedText);
			}

			if(prefixLabelBoldCentered == null)
			{
				prefixLabelBoldCentered = new GUIStyle(GUI.skin.label);
				prefixLabelBoldCentered.fontStyle = FontStyle.Bold;
				prefixLabelBoldCentered.SetAllTextColors(theme.PrefixIdleText);
				prefixLabelBoldCentered.alignment = TextAnchor.MiddleCenter;
			}

			if(richTextLabel == null)
			{
				richTextLabel = new GUIStyle(GUI.skin.label)
				{
					richText = true
				};
			}

			if(richTextLabelWhite == null)
			{
				richTextLabelWhite = new GUIStyle(GUI.skin.label)
				{
					richText = true
				};
				richTextLabelWhite.SetAllTextColors(Color.white);
			}

			if(tooltipStyle == null)
			{
				tooltipStyle = new GUIStyle(preferences.GetStyle("ObjectFieldThumb"))
				{
					wordWrap = true
				};
				tooltipStyle.normal.background = preferences.GetStyle("Tooltip").normal.background;
			}

			if(mouseoverFxStyle == null)
			{
				mouseoverFxStyle = preferences.GetStyle("MouseoverFx");
			}

			setupDone = true;

			#if DEV_MODE && DEBUG_SETUP_TIME
			setupTimer.FinishInterval();
			setupTimer.FinishAndLogResults();
			#endif
			
			Profiler.EndSample();
		}

		private static void ApplyPlayModeTinting(bool isPlayingOrWillChangePlaymode)
		{
			if(isPlayingOrWillChangePlaymode)
			{
				var playModeTintString = EditorPrefs.GetString("Playmode tint", "");
				#if DEV_MODE && DEBUG_PLAY_MODE_TINT
				Debug.Log(EditorPrefs.GetString("Playmode tint", ""));
				#endif

				// Read play mode color tint user preference.
				// format: "Playmode tint;0.7688679;1;0.9791414;1"
				if(playModeTintString.StartsWith("Playmode tint;", StringComparison.Ordinal))
				{
					playModeTintString = playModeTintString.Substring("Playmode tint;".Length);
				}
				#if DEV_MODE
				else { Debug.LogError("Playmode tint EditorPrefs string \"" + playModeTintString + "\" did not start with \"Playmode tint;\""); }
				#endif

				Color setPlayModeTint;
				if(PrettySerializer.TryParseColor(playModeTintString, out setPlayModeTint, ';'))
				{
					if(setPlayModeTint == Color.white)
					{
						UniversalColorTint = Color.white;
						TintedBackgroundColor = Active.InspectorBackgroundColor;
						ShouldApplyPlayModeTinting = false;
					}
					else
					{
						UniversalColorTint = setPlayModeTint;
						TintedBackgroundColor = UniversalColorTint * Active.InspectorBackgroundColor;
						ShouldApplyPlayModeTinting = true;
					}
				}
				else
				{
					UniversalColorTint = new Color(1f, 1f, 1f, 1f);
					TintedBackgroundColor = Active.InspectorBackgroundColor;
					ShouldApplyPlayModeTinting = false;
				}
			}
			else
			{
				UniversalColorTint = new Color(1f, 1f, 1f, 1f);
				TintedBackgroundColor = Active.InspectorBackgroundColor;
				ShouldApplyPlayModeTinting = false;
			}
		}

		private static void OnEditorPlaymodeStateChanged(PlayModeStateChange playModeState)
		{
			switch(playModeState)
			{
				case PlayModeStateChange.ExitingPlayMode:
				case PlayModeStateChange.EnteredEditMode:
					ApplyPlayModeTinting(false);
					return;
				case PlayModeStateChange.ExitingEditMode:
				case PlayModeStateChange.EnteredPlayMode:
					ApplyPlayModeTinting(true);
					return;
			}
		}

		public static Rect FirstLine(Rect position)
		{
			position.height = SingleLineHeight;
			return position;
		}

		public static void NextLine(ref Rect position)
		{
			position.y += SingleLineHeight;
			position.height = SingleLineHeight;
		}

		public static void RemoveFirstLine(ref Rect position)
		{
			position.y += SingleLineHeight;
			position.height -= SingleLineHeight;
		}
		
		public static void AddMarginsAndIndentation(ref Rect position)
		{
			AddMargins(ref position);
			AddIndentation(ref position);
		}

		public static void AddIndentation(ref Rect position)
		{
			float indent = IndentLevel * IndentWidth;
			position.x += indent;
			position.width -= indent;
		}

		public static void AddIndentation(ref Rect position, int indentCount)
		{
			float indent = indentCount * IndentWidth;
			position.x += indent;
			position.width -= indent;
		}

		public static void AddMargins(ref Rect position)
		{
			position.x += LeftPadding;
			position.width = position.width - LeftPadding - RightPadding;
		}

		public static void AddMarginsAndIndentToPrefixWidth(ref float width, int indentCount)
		{
			AddLeftMargin(ref width);
			AddIndentation(ref width, indentCount);
			AddMiddlePadding(ref width);
		}
		
		public static void AddIndentation(ref float width, int indentCount)
		{
			float indent = indentCount * IndentWidth;
			width += indent;
		}

		public static void AddLeftMargin(ref float width)
		{
			width += LeftPadding;
		}

		public static void AddMiddlePadding(ref float width)
		{
			width += MiddlePadding;
		}
		
		/// <summary>
		/// This should be called at the beginning of all OnGUI methods.
		/// 
		/// Handles things like setting the editor mode, saving last input event,
		/// setting active preferences and broadcasting some events.
		/// 
		/// SEE ALSO: InspectorUtility.BeginInspectorDrawer and InspectorUtility.BeginInspector.
		/// </summary>
		public static void BeginOnGUI(InspectorPreferences preferences, bool editorMode)
		{
			OnGUICallCount = OnGUICallCount >= int.MaxValue ? 0 : OnGUICallCount + 1;

			onWindowBeginScreenPoint = GUIUtility.GUIToScreenPoint(Vector2.zero);
			GUISpace.Current = Space.Window;
			
			if(currentAreaRects.Count > 0)
			{
				#if DEV_MODE
				Debug.LogWarning("BeginOnGUI - Clearing currentAreaRects because it had "+currentAreaRects.Count+" items");
				#endif
				currentAreaRects.Clear();
				localDrawAreaoffset = Vector2.zero;

				activeScrollViewDepth = 0;
				activeScrollViewScrollPosition.Clear();
				activeScrollViewViewportRect.Clear();
				activeScrollViewContentRect.Clear();
			}

			var e = Event.current;
			
			#if UNITY_EDITOR
			TextFieldUtility.SyncEditingTextField();
			#endif

			//this can happen e.g. when the color picker is opened
			if(IndentLevel > 0)
			{
				#if DEV_MODE
				Debug.LogWarning("IndentLevel > 0 at the beginning of OnGUI");
				#endif
				IndentLevel = 0;
			}
			InspectorUtility.Preferences = preferences;
			
			if(e.rawType != EventType.Used)
			{
				RegisterInputEvent(e);
			}

			Setup(preferences);

			if(onBeginOnGUI != null)
			{
				onBeginOnGUI();
			}

			if(onNextBeginOnGUI != null)
			{
				#if DEV_MODE && DEBUG_BEGIN_ONGUI
				Debug.Log("BeginOnGUI(" + StringUtils.ToString(Event.current.type)+") invoking onNextBeginOnGUI: "+StringUtils.ToString(onNextBeginOnGUI));
				#endif

				var invoke = onNextBeginOnGUI;
				onNextBeginOnGUI = null;
				invoke();
			}

			SendOnDragAndDropObjectReferencesChangedEventIfNeeded(false);

			// Call OnEventUsed(MouseDrag) to cause MouseDownInfo.mouseDownEventWasUsed to be set to true.
			// This is so that if Esc is used to break out of a drag, then any upcoming MouseUp events
			// will get ignored properly.
			if(e.type == EventType.DragExited && OnEventUsed != null)
			{
				var manager = InspectorUtility.ActiveManager;
				// Don't do this if cursor hasn't moved at all before mouse being pressed down and DragExited
				// being called, because this happens when the user clicks something that also starts a drag,
				// and sometimes we want to allow the user to interact with a GUI element both via MouseUp
				// and Drag events.
				if(manager == null || manager.MouseDownInfo.CursorMovedAfterMouseDown)
				{ 
					#if DEV_MODE && DEBUG_DRAG
					Debug.LogWarning("Calling OnEventUsed(MouseDrag) because EventType=DragExited. Event.keyCode="+e.keyCode+", lastInputEvent="+StringUtils.ToString(lastInputEvent)+", ObjectReferences="+StringUtils.ToString(Active.DragAndDropObjectReferences));
					#endif

					OnEventUsed(EventType.MouseDrag, e);

					#if DEV_MODE
					Debug.Assert(InspectorUtility.ActiveManager.MouseDownInfo.MouseDownEventWasUsed);
					#endif
				}
			}
		}
		
		protected static void SendOnDragAndDropObjectReferencesChangedEventIfNeeded(bool calledBySetDragAndDropObjectReferences)
		{
			var objectReferences = Active.DragAndDropObjectReferences;
			if(objectReferences.Length > 0)
			{
				var mouseDownInfo = InspectorUtility.ActiveManager.MouseDownInfo;
				if(!mouseDownInfo.MouseButtonIsDown)
				{

				}
				else if(objectReferences.ContainsNullObjects())
				{
					// There was a strange infinite loop bug when a Component was dragged from Power Inspector to Hierarchy view.
					// Even if DragAndDrop.objectReferences was set to be an empty array again and again, it didn't seem to have any effect.
					if(calledBySetDragAndDropObjectReferences)
					{
						#if DEV_MODE
						Debug.LogError("DragAndDropObjectReferences - Prevented possible infinite loop! ContainsNullObjects seems to constantly return "+StringUtils.True+" even after RemoveNullObjects is called for "+StringUtils.ToString(objectReferences));
						#endif

						if(!hadDragAndDropObjectReferencesLastFrame)
						{
							hadDragAndDropObjectReferencesLastFrame = true;
							OnDragAndDropObjectReferencesChanged(Active.DragAndDropObjectReferences);
						}
					}
					else
					{
						Active.DragAndDropObjectReferences = objectReferences.RemoveNullObjects();
					}
				}
				else if(!hadDragAndDropObjectReferencesLastFrame)
				{
					hadDragAndDropObjectReferencesLastFrame = true;
					OnDragAndDropObjectReferencesChanged(Active.DragAndDropObjectReferences);
				}
			}
			else if(hadDragAndDropObjectReferencesLastFrame)
			{
				hadDragAndDropObjectReferencesLastFrame = false;
				OnDragAndDropObjectReferencesChanged(Active.DragAndDropObjectReferences);
			}
		}
		
		private static bool MouseWasClicked(bool mouseUp = false, int mouseButton = 0)
		{
			return MouseWasClicked(Event.current.type, mouseUp, mouseButton);
		}

		private static bool MouseWasClicked(EventType testEvent, bool mouseUp = false, int mouseButton = 0)
		{
			return (mouseUp ? testEvent == EventType.MouseUp : testEvent == EventType.MouseDown) && Event.current.button == mouseButton;
		}

		public static bool MouseWasClickedInsideRect(Rect position, bool mouseUp = false, int mouseButton = 0)
		{
			return MouseWasClicked(mouseUp, mouseButton) && position.Contains(Event.current.mousePosition);
		}

		public static bool MouseWasClickedOutsideRect(Rect position, int mouseButton = 0)
		{
			return MouseWasClicked(false, mouseButton) && (!position.Contains(Event.current.mousePosition));
		}

		public static void FocusControl(string internalName)
		{
			#if DEV_MODE && DEBUG_FOCUS_CONTROL
			Debug.Log("FocusControl(\""+ internalName + "\")");
			#endif

			#if DEV_MODE && PI_ASSERTATIONS && UNITY_EDITOR
			Debug.Assert(InspectorUtility.ActiveManager != null, internalName);
			Debug.Assert(!InspectorUtility.ActiveManager.HasMultiSelectedControls || internalName.Length == 0, internalName);
			Debug.Assert(!ObjectPicker.IsOpen, internalName);
			Debug.Assert(EditorWindow.focusedWindow == InspectorUtility.ActiveManager.GetLastSelectedEditorWindow(), internalName);
			#endif

			if(ObjectPicker.IsOpen)
			{
				return;
			}

			GUI.FocusControl(internalName);

			if(InspectorUtility.ActiveInspector != null)
			{
				InspectorUtility.ActiveInspector.RefreshView();
			}
		}
		
		public static void BeginArea(Rect screenRect)
		{
			GUILayout.BeginArea(screenRect);
			BeginVirtualArea(screenRect);
		}

		public static void EndArea()
		{
			EndVirtualArea();
			GUILayout.EndArea();
		}
		
		public static void BeginScrollView(Rect viewportRect, Rect contentRect, ref Vector2 scrollPosition)
		{
			var virtualArea = viewportRect;
			virtualArea.width = contentRect.width;
			BeginVirtualArea(virtualArea);

			scrollPosition = GUI.BeginScrollView(viewportRect, scrollPosition, contentRect);

			activeScrollViewScrollPosition.Add(scrollPosition);
			activeScrollViewViewportRect.Add(viewportRect);
			activeScrollViewContentRect.Add(contentRect);
			activeScrollViewDepth++;
		}

		public static void EndScrollView()
		{
			GUI.EndScrollView();
			EndVirtualArea();

			activeScrollViewDepth--;
			activeScrollViewScrollPosition.RemoveAt(activeScrollViewDepth);
			activeScrollViewViewportRect.RemoveAt(activeScrollViewDepth);
			activeScrollViewContentRect.RemoveAt(activeScrollViewDepth);
		}

		public static void BeginVirtualArea(Rect screenRect)
		{
			currentAreaRects.Add(screenRect);

			localDrawAreaoffset.x += screenRect.x;
			localDrawAreaoffset.y += screenRect.y;
		}

		public static void EndVirtualArea()
		{
			int lastIndex = currentAreaRects.Count - 1;
			var screenRect = currentAreaRects[lastIndex];
			
			localDrawAreaoffset.x -= screenRect.x;
			localDrawAreaoffset.y -= screenRect.y;

			currentAreaRects.RemoveAt(lastIndex);
		}

		public static float GetCurrentDrawAreaWidth()
		{
			return GetCurrentDrawArea().width;
		}

		public static Rect GetCurrentDrawArea()
		{
			if(currentAreaRects.Count > 0)
			{
				return currentAreaRects[currentAreaRects.Count - 1];
			}
			var inspector = InspectorUtility.ActiveInspector;
			if(inspector != null)
			{
				return inspector.State.contentRect;
			}
			return Rect.zero;
		}
		
		/// <summary>
		/// Gets the current local draw area offset.
		/// This is the difference between (0,0) point in the current local space and in screen space. 
		/// </summary>
		/// <value> Difference between local drawer space and screenspace. </value>
		public static Vector2 GetLocalDrawAreaOffset()
		{
			return GUISpace.ConvertPoint(Vector2.zero, Space.Local, Space.Screen);
		}
		
		/// <summary>
		/// Invokes action during the beginning of every OnGUI event.
		/// 
		/// NOTE: This will cause the event to get called multiple times every frame possibly by multiple different systems that are using OnGUI methods!
		/// </summary>
		/// <param name="action"> Action to invoke. This can not be null. </param>
		/// <param name="ensureOnGUICallbacks">
		/// If true, then we makes that OnGUI function is called as soon as possible, even if it means
		/// creating a temporary EditorWindow.
		/// </param>
		public static void OnEveryBeginOnGUI([NotNull]Action action, bool ensureOnGUICallbacks)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(action != null);
			#endif

			#if DEV_MODE && DEBUG_BEGIN_ONGUI
			Debug.Log("OnBeginOnGUI(" + StringUtils.ToString(action)+")");
			#endif

			onBeginOnGUI += action;

			if(ensureOnGUICallbacks)
			{
				EnsureOnGUICallbacks(false);
			}
			else
			{
				EnsureOnGUICallbackOnExistingInspectorIfAny();
			}
		}

		
		public static void CancelOnEveryBeginOnGUI([NotNull]Action action)
		{
			onBeginOnGUI -= action;
		}

		/// <summary>
		/// Invokes action during the beginning of the next OnGUI event.
		/// </summary>
		/// <param name="action"> Action to invoke. This can not be null. </param>
		/// <param name="ensureOnGUICallbacks">
		/// If true, then we makes that OnGUI function is called as soon as possible, even if it means
		/// creating a temporary EditorWindow.
		/// </param>
		public static void OnNextBeginOnGUI([NotNull]Action action, bool ensureOnGUICallbacks)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(action != null);
			#endif

			#if DEV_MODE && DEBUG_BEGIN_ONGUI
			Debug.Log("OnBeginOnGUI(" + StringUtils.ToString(action)+")");
			#endif

			onNextBeginOnGUI += action;

			if(ensureOnGUICallbacks)
			{
				EnsureOnGUICallbacks(false);
			}
			else
			{
				EnsureOnGUICallbackOnExistingInspectorIfAny();
			}
		}

		public static void EnsureOnGUICallbacks(bool needsLayoutEvent)
		{
			if(!IsAnyInspectorVisible())
			{
				OnGUIUtility.EnsureOnGUICallbacks(NeedsOnGUIHelperWindow, needsLayoutEvent);
			}
			else
			{
				#if DEV_MODE && DEBUG_ENSURE_ON_GUI_CALLBACKS
				Debug.Log(StringUtils.ToColorizedString("EnsureOnGUICallbacks: not needed because ", InspectorManager.instance.ActiveInstances.Count, " inspector instances were found."));
				#endif

				EnsureOnGUICallbackOnExistingInspectorIfAny();
			}
		}

		private static bool IsAnyInspectorVisible()
		{
			var manager = InspectorUtility.ActiveManager;
			if(manager == null)
			{
				manager = InspectorManager.Instance();
			}
			return manager.GetFirstVisibleInspector() != null;
		}

		public static void EnsureOnGUICallbackOnExistingInspectorIfAny()
		{
			var manager = InspectorUtility.ActiveManager;
			if(manager == null)
			{
				return;
			}

			var inspector = manager.GetFirstVisibleInspector();
			if(inspector == null)
			{
				return;
			}
			
			inspector.RefreshView();
		}

		private static bool NeedsOnGUIHelperWindow()
		{
			#if DEV_MODE && DEBUG_ENSURE_ON_GUI_CALLBACKS
			if(!InspectorManager.InstanceExists()) {	Debug.Log(StringUtils.ToColorizedString("DrawGUI.NeedsOnGUIHelperWindow: InspectorManager.InstanceExists=", false, ", onBeginOnGUI=", onBeginOnGUI, ", onNextBeginOnGUI=", onNextBeginOnGUI, ", result=", (!InspectorManager.InstanceExists() || InspectorManager.instance.ActiveInstances.Count == 0) && (onBeginOnGUI != null || onNextBeginOnGUI != null))); }
			else {	Debug.Log(StringUtils.ToColorizedString("DrawGUI.NeedsOnGUIHelperWindow: InspectorManager.InstanceExists=", true, ", ActiveInstances=", InspectorManager.instance.ActiveInstances.Count, ", onBeginOnGUI = ", onBeginOnGUI, ", onNextBeginOnGUI=", onNextBeginOnGUI, ", result=", (!InspectorManager.InstanceExists() || InspectorManager.instance.ActiveInstances.Count == 0) && (onBeginOnGUI != null || onNextBeginOnGUI != null))); }
			#endif

			return (!InspectorManager.InstanceExists() || InspectorManager.instance.ActiveInstances.Count == 0) && (onBeginOnGUI != null || onNextBeginOnGUI != null);
		}

		/// <summary>
		/// Draw rectangle filled with given color.
		/// </summary>
		/// <param name="position">
		/// The position. </param>
		/// <param name="color">
		/// The color. </param>
		/// <param name="offset">
		/// Offset to apply to rect position based on local draw area.
		/// When called from OnMouseover, OnPrefixDragged etc. this should contain the offset from (0,0) of the drawer, if it was wrapped inside GUILayout.BeginArea or similar.
		/// You can get the offset for the drawer by calling GetLocalDrawAreaOffset() during GetDrawPositions and caching that value.
		/// When called from Draw methods you can use an override with no offset parameter.
		/// </param>
		public void ColorRect(Rect rect, Color color, Vector2 offset)
		{
			offset = GUISpace.ConvertPoint(offset, Space.Screen, Space.Local);

			rect.x += offset.x;
			rect.y += offset.y;

			EditorGUI.DrawRect(rect, color);
		}

		/// <summary>
		/// Draw rectangle filled with given color.
		/// </summary>
		/// <param name="position">
		/// The position. </param>
		/// <param name="color">
		/// The color. </param>
		public void ColorRect(Rect position, Color color)
		{
			EditorGUI.DrawRect(position, color);
		}

		public void TooltipBox(Rect position, GUIContent label)
		{
			GUI.Label(position, label, tooltipStyle);
		}

		public void TooltipBox(Rect position, string label)
		{
			GUI.Label(position, label, tooltipStyle);
		}
		
		public static Material SelectionLineMaterial
		{
			get
			{
				if(selectionLineMaterial == null)
				{
					// Unity has a built-in shader that is useful for drawing
					// simple colored things.
					var shader = Shader.Find("Hidden/Internal-Colored");
					selectionLineMaterial = new Material(shader);
					selectionLineMaterial.hideFlags = HideFlags.HideAndDontSave;
					// Turn on alpha blending
					selectionLineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					selectionLineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					// Turn backface culling off
					selectionLineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
					// Turn off depth writes
					selectionLineMaterial.SetInt("_ZWrite", 0);
				}
				return selectionLineMaterial;
			}
		}

		public static void DrawNonSelectedFocusedControlRect(Rect rect)
		{
			DrawSelectionOrFocusedControlRect(rect, Color.grey);
		}

		/// <summary>
		/// Draw effect for selected drawer using SelectedLineIndicator color as specified in preferences.
		/// </summary>
		/// <param name="rect"> The position and size for effect. </param>
		/// <param name="offset">
		/// Offset to apply to rect position based on local draw area.
		/// When called from OnMouseover, OnPrefixDragged etc. this should contain the offset from (0,0) of the drawer, if it was wrapped inside GUILayout.BeginArea or similar.
		/// You can get the offset for the drawer by calling GetLocalDrawAreaOffset() during GetDrawPositions and caching that value.
		/// When called from Draw methods you can use an override with no offset parameter.
		/// </param>
		public static void DrawSelectionRect(Rect rect, Vector2 offset)
		{
			offset = GUISpace.ConvertPoint(offset, Space.Screen, Space.Local);

			rect.x += offset.x;
			rect.y += offset.y;

			DrawSelectionRect(rect);
		}

		public static void DrawSelectionRect(Rect rect)
		{
			DrawSelectionOrFocusedControlRect(rect, GetSelectedLineIndicatorColor(InspectorUtility.ActiveInspector));
		}

		public static Color GetSelectedLineIndicatorColor()
		{
			return GetSelectedLineIndicatorColor(InspectorUtility.ActiveInspector);
		}

		public static Color GetSelectedLineIndicatorColor([CanBeNull]IInspector inspector)
		{
			#if UNITY_EDITOR
			if(inspector != null && inspector.InspectorDrawer as EditorWindow != EditorWindow.focusedWindow)
			{
				return InspectorUtility.Preferences.theme.SelectedLineIndicatorUnfocused;
			}
			#endif

			return InspectorUtility.Preferences.theme.SelectedLineIndicator;
		}

		public static void DrawEdgeSelectionIndicator(Rect rect)
		{
			rect.width = 3f;
			rect.x = 0f;
			rect.y -= 1f;
			rect.height += 1f;
			EditorGUI.DrawRect(rect, GetSelectedLineIndicatorColor());
		}

		public static void DrawControlSelectionIndicator(Rect rect)
		{
			// makes all selection indicators look more like the toolbar selection indicators
			if(rect.height > 2f)
			{
				rect.y += rect.height - 2f;
				rect.height = 2f;
				DrawRect(rect, InspectorUtility.Preferences.theme.ToolbarItemSelected);
				return;
			}

			DrawRect(rect, GetSelectedLineIndicatorColor());
		}

		public static void DrawControlSelectionIndicator(Rect rect, Vector2 offset)
		{
			offset = GUISpace.ConvertPoint(offset, Space.Screen, Space.Local);

			rect.x += offset.x;
			rect.y += offset.y;

			// makes all selection indicators look more like the toolbar selection indicators
			if(rect.height > 2f)
			{
				rect.y += rect.height - 2f;
				rect.height = 2f;
				DrawRect(rect, InspectorUtility.Preferences.theme.ToolbarItemSelected);
				return;
			}

			DrawRect(rect, GetSelectedLineIndicatorColor());
		}

		public static void DrawSelectionOrFocusedControlRect(Rect rect, Color color)
		{
			if(rect.x <= 16f)
			{
				rect.width = 3f;
				rect.x = 0f;
				rect.y -= 1f;
				rect.height += 1f;
				EditorGUI.DrawRect(rect, color);
			}
			else
			{
				DrawRect(rect, color);
			}
		}

		public static void DrawSelectionOrFocusedControlRect(Rect rect, Color color, Vector2 offset)
		{
			offset = GUISpace.ConvertPoint(offset, Space.Screen, Space.Local);

			rect.x += offset.x;
			rect.y += offset.y;

			if(rect.x <= 16f)
			{
				rect.width = 3f;
				rect.x = 0f;
				rect.y -= 1f;
				rect.height += 1f;
				EditorGUI.DrawRect(rect, color);
			}
			else
			{
				DrawRect(rect, color);
			}
		}

		/// <summary>
		/// Draw mouseover effect using ControlMouseoveredTint color as specified in preferences.
		/// </summary>
		/// <param name="rect"> The position and size for effect. </param>
		/// <param name="offset">
		/// Offset to apply to rect position based on local draw area.
		/// When called from OnMouseover, OnPrefixDragged etc. this should contain the offset from (0,0) of the drawer, if it was wrapped inside GUILayout.BeginArea or similar.
		/// You can get the offset for the drawer by calling GetLocalDrawAreaOffset() during GetDrawPositions and caching that value.
		/// When called from Draw methods you can use an override with no offset parameter.
		/// </param>
		public static void DrawMouseoverEffect(Rect rect, Vector2 offset)
		{
			DrawMouseoverEffect(rect, InspectorUtility.Preferences.theme.ControlMouseoveredTint, offset);
		}

		/// <summary> Draw mouseover effect using ControlMouseoveredTint color as specified in preferences.
		///
		/// This should only be called from Draw methods of Drawers, NOT from OnMouseover, OnPrefixDragged etc.
		/// Use the other overload that contains an offset in those situations.
		/// </summary>
		/// <param name="rect"> The position and size for effect. </param>
		public static void DrawMouseoverEffect(Rect rect)
		{
			DrawMouseoverEffect(rect, InspectorUtility.Preferences.theme.ControlMouseoveredTint);
		}

		/// <summary>
		/// Draw mouseover effect using given color.
		/// </summary>
		/// <param name="rect"> The position and size for effect. </param>
		/// <param name="color"> The color for the effect. </param>
		/// <param name="offset">
		/// Offset to apply to rect position based on local draw area.
		/// When called from OnMouseover, OnPrefixDragged etc. this should contain the offset from (0,0) of the drawer, if it was wrapped inside GUILayout.BeginArea or similar.
		/// You can get the offset for the drawer by calling GetLocalDrawAreaOffset() during GetDrawPositions and caching that value.
		/// When called from Draw methods you can use an override with no offset parameter.
		/// </param>
		public static void DrawMouseoverEffect(Rect rect, Color color, Vector2 offset)
		{
			offset = GUISpace.ConvertPoint(offset, Space.Screen, Space.Local);

			rect.x += offset.x;
			rect.y += offset.y;

			DrawMouseoverEffect(rect, color);
		}

		/// <summary> Draw mouseover effect. </summary>
		/// <param name="rect"> The position and size for effect. </param>
		/// <param name="color"> The color for the effect </param>
		public static void DrawMouseoverEffect(Rect rect, Color color)
		{
			#if DEV_MODE || SAFE_MODE
			if(rect.width == 0f)
			{
				#if DEV_MODE
				Debug.LogWarning("DrawMouseoverEffect("+rect+") called with width being zero");
				#endif
				return;
			}
			#endif

			rect.y += 1f;
			rect.height -= 2f;

			EditorGUI.DrawRect(rect, color);
		}

		/// <summary> Draw mouseover effect. </summary>
		/// <param name="rect"> The position and size for effect. </param>
		/// <param name="color"> The color for the effect </param>
		public static void DrawControlFilteringEffect(Rect rect, Color color, Vector2 offset)
		{
			offset = GUISpace.ConvertPoint(offset, Space.Screen, Space.Local);
			rect.x += offset.x;
			rect.y += offset.y;

			#if DEV_MODE || SAFE_MODE
			if(rect.width == 0f)
			{
				#if DEV_MODE
				Debug.LogWarning("DrawControlFilteringEffect(" + rect+") called with width being zero");
				#endif
				return;
			}
			#endif

			rect.y += 1f;
			rect.height -= 2f;

			EditorGUI.DrawRect(rect, color);
		}

		public static void RightClickAreaDrawMouseoverEffect(Rect rect)
		{
			rect.x += rect.width - 4f;
			rect.width = 4f;
			rect.height = 4f;
			GUI.DrawTexture(rect, InspectorUtility.Preferences.graphics.rightClickAreaIndicator, ScaleMode.ScaleToFit, true);
		}

		public static void DrawGLLine(Vector2 p1, Vector2 p2, Color lineColor)
		{
			SelectionLineMaterial.SetPass(0);
			GL.PushMatrix();
			GL.Begin(GL.LINES);
			GL.Color(lineColor);
			
			DrawGLLine(p1, p2);
			
			GL.End ();
			GL.PopMatrix ();
		}
		
		public static void DrawLine(Rect lineRect)
		{
			EditorGUI.DrawRect(lineRect, Color.white);
		}

		public static void DrawGLLine(Rect lineRect, Color lineColor)
		{
			SelectionLineMaterial.SetPass(0);
			GL.PushMatrix();
			GL.Begin(GL.LINES);
			GL.Color(lineColor);
			
			p1.x = lineRect.x;
			p1.y = lineRect.y;
			p2.x = lineRect.xMax;
			p2.y = lineRect.yMax;

			DrawGLLine(p1, p2);
			
			GL.End ();
			GL.PopMatrix ();
		}

		/// <summary>
		/// Draws a line with given color used for the lines.
		/// 
		/// This should only be called from Draw methods of Drawers, NOT from OnMouseover, OnPrefixDragged etc.
		/// Use an overload that contains an offset in those situations.
		/// </summary>
		public static void DrawLine(Rect lineRect, Color lineColor)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(lineRect.width == 1f) { Debug.Assert(lineRect.height >= 1f, lineRect.height); }
			else { Debug.Assert(lineRect.height == 1f, lineRect.height); }
			Debug.Assert(lineColor.a > 0f);
			#endif

			EditorGUI.DrawRect(lineRect, lineColor);
		}

		/// <summary>
		/// Draws a rectangle with given color used for the lines.
		/// </summary>
		/// <param name="rect">Rectangle whose edges define the lines that should be drawn</param>
		/// <param name="lineColor">Color to use for the lines</param>
		/// <param name="offset">
		/// Offset to apply to rect position based on local draw area.
		/// When called from OnMouseover, OnPrefixDragged etc. this should contain the offset from (0,0) of the drawer, if it was wrapped inside GUILayout.BeginArea or similar.
		/// You can get the offset for the drawer by calling GetLocalDrawAreaOffset() during GetDrawPositions and caching that value.
		/// When called from Draw methods you can use an override with no offset parameter.
		/// </param>
		public static void DrawRect(Rect rect, Color lineColor, Vector2 offset)
		{
			offset = GUISpace.ConvertPoint(offset, Space.Screen, Space.Local);

			rect.x += offset.x;
			rect.y += offset.y;

			DrawRect(rect, lineColor);
		}

		/// <summary>
		/// Draws a rectangle with given color used for the lines.
		/// 
		/// This should only be called from Draw methods of Drawers, NOT from OnMouseover, OnPrefixDragged etc.
		/// Use an overload that contains an offset in those situations.
		/// </summary>
		/// <param name="rect">Rectangle whose edges define the lines that should be drawn</param>
		/// <param name="lineColor">Color to use for the lines</param>
		public static void DrawRect(Rect rect, Color lineColor)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(rect.width > 0f, "DrawRect called with width <= 0f: " + rect);
			Debug.Assert(rect.height > 0f, "DrawRect called with height <= 0f: " + rect);
			Debug.Assert(lineColor.a > 0f);
			#endif

			var guiColorWas = GUI.color;

			GUI.color = lineColor;

			//top
			var lineRect = rect;
			lineRect.width = rect.width;
			lineRect.height = 1f;
			DrawLine(lineRect);

			//bottom
			lineRect.y += rect.height - 1f;
			DrawLine(lineRect);

			//left
			lineRect.y = rect.y;
			lineRect.width = 1f;
			lineRect.height = rect.height;
			DrawLine(lineRect);
			
			//right;
			lineRect.x += rect.width - 1f;
			DrawLine(lineRect);
			
			GUI.color = guiColorWas;
		}

		public static void Underline(Rect rect, Color lineColor)
		{
			//NOTE: drawing only top and bottom works quite well too for data set indication

			SelectionLineMaterial.SetPass(0);
			GL.PushMatrix();
			{
				GL.Begin(GL.LINES);
				{
					GL.Color(lineColor);
			
					//bottom
					p1.x = rect.x;// - 3f;
					p1.y = rect.y + rect.height;
					p2.x = rect.x + rect.width;
					p2.y = rect.y + rect.height;
					DrawGLLine(p1, p2);
				}
				GL.End();
			}
			GL.PopMatrix ();
		}

		public static void DrawLeftClickAreaMouseoverEffect(Rect rect, Vector2 offset)
		{
			offset = GUISpace.ConvertPoint(offset, Space.Screen, Space.Local);

			rect.x += offset.x;
			rect.y += offset.y;

			DrawLeftClickAreaMouseoverEffect(rect);
		}

		public static void DrawLeftClickAreaMouseoverEffect(Rect rect)
		{
			var inspector = InspectorUtility.ActiveInspector;
			
			var drawAreaWidth = GetCurrentDrawAreaWidth();

			if(drawAreaWidth - rect.width < 4f)
			{
				GUI.Label(rect, GUIContent.none, mouseoverFxStyle);
				return;
			}
			
			var theme = inspector.Preferences.theme;
			
			var guiColorWas = GUI.color;

			GUI.color = theme.PrefixMouseoveredRectHighlight;

			//top
			var lineRect = rect;
			lineRect.height = 1f;
			DrawLine(lineRect);

			//left
			lineRect.width = 1f;
			lineRect.height = rect.height;
			DrawLine(lineRect);

			GUI.color = theme.PrefixMouseoveredRectShadow;

			//right;
			lineRect.x += rect.width - 1f;
			DrawLine(lineRect);

			//bottom
			lineRect = rect;
			lineRect.height = 1f;
			lineRect.y += rect.height - 1f;
			DrawLine(lineRect);
			
			GUI.color = guiColorWas;
		}
		
		private static void DrawGLLine(Vector2 p1, Vector2 p2)
		{
			GL.Vertex (p1);
			GL.Vertex (p2);
		}
		
		public static void Ping(Object target)
		{
			Platform.Active.GUI.PingObject(target);
		}

		public static void Ping(Object[] targets)
		{
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				Platform.Active.GUI.PingObject(targets[n]);
			}
		}
		
		public void Message(string message)
		{
			if(InspectorUtility.ActiveInspector != null)
			{
				InspectorUtility.ActiveInspector.Message(message);
			}
			else
			{
				Debug.Log(message);
			}
		}
		
		public static void UseEvent()
		{
			Use(Event.current);
		}

		public static EventType LastInputEventType
		{
			get
			{
				var e = LastInputEvent();
				if(e != null)
				{
					var rawType = e.rawType;
					if(rawType != EventType.Used)
					{
						return rawType;
					}
				}
				return lastInputEventType;
			}
		}

		/// <summary>
		/// Last Mouse or Keyboard input event
		/// </summary>
		/// <returns>
		/// Event.current if it's a mouse or keyboard event. Otherwise returns lastInputEvent.
		/// </returns>
		public static Event LastInputEvent()
		{
			var e = Event.current;
			if(e != null)
			{
				switch(e.type)
				{
					case EventType.Used:
						if(e.rawType == EventType.Used)
						{
							#if DEV_MODE && DEBUG_LAST_INPUT_EVENT
							Debug.Log("LastInputEvent - because Event.current type and rawType were both Used, returning lastInputEvent "+StringUtils.ToString(lastInputEvent)+" which should be of type "+lastInputEventType);
							#endif
							return lastInputEvent;
						}
						return e;
					case EventType.KeyDown:
					case EventType.KeyUp:
					case EventType.MouseDown:
					case EventType.MouseUp:
					case EventType.MouseDrag:
					case EventType.DragPerform:
					case EventType.DragExited:
					case EventType.DragUpdated:
					case EventType.ContextClick:
						return e;
				}

				switch(e.rawType)
				{
					case EventType.KeyDown:
					case EventType.KeyUp:
					case EventType.MouseDown:
					case EventType.MouseUp:
					case EventType.MouseDrag:
					case EventType.DragPerform:
					case EventType.DragExited:
					case EventType.DragUpdated:
					case EventType.ContextClick:
						return e;
				}
			}

			if(lastInputEvent != null && lastInputEvent.rawType == EventType.Used)
			{
				if(lastInputEventType != EventType.Ignore && lastInputEventType != EventType.Used)
				{
					lastInputEvent.type = lastInputEventType;

					#if DEV_MODE
					Debug.LogWarning("LastInputEvent rawType was "+lastInputEvent.rawType+". Restored using lastInputEventType "+lastInputEventType+": "+StringUtils.ToString(lastInputEvent));
					#endif

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(lastInputEvent.type == lastInputEventType);
					#endif
				}
				else
				{
					#if DEV_MODE
					Debug.LogWarning("LastInputEvent rawType was "+lastInputEvent.rawType+" and could not restore using lastInputEventType.");
					#endif
				}
			}
			return lastInputEvent;
		}

		public static void RegisterInputEvent(Event e)
		{
			if(e != null)
			{
				switch(e.type)
				{
					case EventType.KeyDown:
					case EventType.KeyUp:
					case EventType.MouseDown:
					case EventType.MouseUp:
					//UPDATE: Now supporting drag too
					case EventType.MouseDrag:
					case EventType.DragPerform:
					case EventType.DragExited:
					case EventType.DragUpdated:
					case EventType.ContextClick:
						lastInputEvent = new Event(e);
						lastInputEventType = e.type;
						#if DEV_MODE
						Debug.Assert(e.type == lastInputEvent.type);
						Debug.Assert(e.keyCode == lastInputEvent.keyCode);
						#endif
						return;
					case EventType.Used:
						var rawType = e.rawType;
						if(rawType == EventType.Used)
						{
							#if DEV_MODE && PI_ASSERTATIONS
							Debug.LogWarning("RegisterInputEvent - both type and rawType of Event were Used. Ignoring...");
							#endif
							return;
						}

						#if DEV_MODE && PI_ASSERTATIONS
						Debug.LogWarning("RegisterInputEvent - type was already Used. Can't clone the Event, so using original event as is.");
						#endif

						lastInputEvent = e;
						lastInputEventType = rawType;
						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(lastInputEvent.type == rawType);
						#endif
						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(e.keyCode == lastInputEvent.keyCode);
						#endif
						return;
				}
			}
		}

		public static void Use(Event e)
		{
			if(e == null)
			{
				#if DEV_MODE
				Debug.LogWarning("Use was called for null event. Aborting...");
				#endif
				return;
			}

			#if SAFE_MODE || DEV_MODE
			if(e.type == EventType.Repaint || e.type == EventType.Layout)
			{
				#if DEV_MODE
				Debug.LogError("Use was called for event of type "+e.type+"! Aborting...");
				#endif
				return;
			}
			#endif
			
			RegisterInputEvent(e);
			
			var type = e.type;

			#if DEV_MODE && (DEBUG_USE_LMB_EVENT || DEBUG_USE_MOUSE_EVENT || DEBUG_USE_ANY_EVENT)
			var button = e.button;
			switch(button)
			{
				case 0:
				{
					switch(type)
					{
						case EventType.MouseDown:
						{
							Debug.Log("Event LMB Down - Use()");
							break;
						}
						case EventType.MouseUp:
						{
							Debug.Log("Event LMB Up - Use()");
							break;
						}
					}
					break;
				}
			}
			#endif

			#if DEV_MODE && (DEBUG_USE_MOUSE_EVENT || DEBUG_USE_ANY_EVENT)
			switch(type)
			{
				case EventType.MouseMove:
				{
					Debug.Log("Event Mouse Move - Use()");
					break;
				}
				case EventType.MouseDrag:
				{
					Debug.Log("Event Mouse Drag - Use()");
					break;
				}
			}
			#endif

			#if DEV_MODE && (DEBUG_USE_RETURN_EVENT || DEBUG_USE_KEYBOARD_EVENT || DEBUG_USE_ANY_EVENT)
			if(e.isKey)
			{
				if(e.keyCode == KeyCode.Return)
				{
					Debug.Log("Event Return - Use()");
				}
				else if(e.keyCode == KeyCode.KeypadEnter)
				{
					Debug.Log("Event Enter - Use()");
				}
				#if DEBUG_USE_KEYBOARD_EVENT || DEBUG_USE_ANY_EVENT
				else
				{
					Debug.Log("Event "+e.keyCode+" - Use()");
				}
				#endif
			}
			#if DEBUG_USE_ANY_EVENT
			else
			{
				Debug.Log("Event(type=" + e.type + ", keyCode"+e.keyCode+" char='"+StringUtils.ToString(e.character)+"') - Use()");
			}
			#endif
			#endif
			
			e.Use();

			if(OnEventUsed != null)
			{
				OnEventUsed(type, e);
			}
		}
		
		public static void ExecuteMenuItem(string menuItemPath)
		{
			#if UNITY_EDITOR
			
			#if DEV_MODE
			Debug.Log("ExecuteMenuItem: "+menuItemPath);
			#endif

			ExecutingCustomMenuCommand = true;
			EditorApplication.ExecuteMenuItem(menuItemPath);
			ExecutingCustomMenuCommand = false;
			#else
			Debug.LogWarning("ExecuteMenuItem not supported in build");
			#endif
		}

		public static void ExecuteMenuItem(string menuItemPath, Object[] context)
		{
			#if UNITY_EDITOR
			
			#if DEV_MODE
			Debug.Log("ExecuteMenuItem: "+ menuItemPath +" with context "+StringUtils.ToString(context));
			#endif

			var parameters = ArrayPool<object>.Create(2);
			parameters[0] = menuItemPath;
			parameters[1] = context;
			typeof(EditorApplication).GetMethod("ExecuteMenuItemWithTemporaryContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, parameters);
			ArrayPool<object>.Dispose(ref parameters);

			#endif
		}
		
		public static void ClearDragAndDropObjectReferences()
		{
			var arr = Active.DragAndDropObjectReferences;
			if(arr == null || arr.Length != 0)
			{
				Active.DragAndDropObjectReferences = null;
			}
		}
		
		public void SetCursor(MouseCursor cursor)
		{
			#if DEV_MODE && DEBUG_SET_CURSOR
			Debug.Log("SetCursor("+cursor+")");
			#endif

			var rect = InspectorUtility.ActiveInspector.State.ScreenSpaceWindowRect;
			rect.height += rect.y;
			rect.y = 0f;
			
			#if DEV_MODE && PI_ASSERTATIONS
			if(InspectorUtility.MouseoveredInspector == InspectorUtility.ActiveInspector && !rect.Contains(Cursor.LocalPosition) && InspectorUtility.ActiveManager.MouseDownInfo.DraggingPrefixOfDrawer == null)
			{
				Debug.LogWarning(InspectorUtility.MouseoveredInspector+".ScreenSpaceWindowRect(" + InspectorUtility.ActiveInspector.State.ScreenSpaceWindowRect + ") did not contain CursorPos "+Cursor.LocalPosition+" with Event="+StringUtils.ToString(Event.current)+ ", MouseButtonIsDown=" + InspectorUtility.ActiveManager.MouseDownInfo.MouseButtonIsDown);
			}
			#endif

			Platform.Active.GUI.AddCursorRect(rect, cursor);
		}

		public bool Toggle(Rect position, GUIContent label, bool value)
		{
			return Toggle(PrefixLabel(position, label), value);
		}

		public int IntField(Rect position, GUIContent label, int value)
		{
			return IntField(PrefixLabel(position, label), value);
		}

		public float MinFloatField(Rect position, float value, float min)
		{
			return Mathf.Min(FloatField(position, value), min);
		}

		public float MaxFloatField(Rect position, float value, float max)
		{
			return Mathf.Max(FloatField(position, value), max);
		}

		public int MinIntField(Rect position, int value, int min)
		{
			return Mathf.Max(IntField(position, value), min);
		}

		public int MinIntField(Rect position, GUIContent label, int value, int min)
		{
			return Mathf.Max(IntField(position, label, value), min);
		}

		public int MaxIntField(Rect position, int value, int max)
		{
			return Mathf.Min(IntField(position, value), max);
		}

		public int MaxIntField(Rect position, GUIContent label, int value, int max)
		{
			return Mathf.Min(IntField(position, label, value), max);
		}

		public int ClampedIntField(Rect position, int value, int min, int max)
		{
			return Mathf.Clamp(IntField(position, value), min, max);
		}

		public int ClampedIntField(Rect position, GUIContent label, int value, int min, int max)
		{
			return Mathf.Clamp(IntField(position, label, value), min, max);
		}

		public float ClampedFloatField(Rect position, float value, float min, float max)
		{
			return Mathf.Clamp(FloatField(position, value), min, max);
		}

		public float FloatField(Rect position, GUIContent label, float value)
		{
			return FloatField(PrefixLabel(position, label), value);
		}

		public string TextField(Rect position, GUIContent label, string value)
		{
			return TextField(PrefixLabel(position, label), value);
		}
		
		public void EnumPopup(Rect position, GUIContent label, EnumDrawer popupField)
		{
			EnumPopup(PrefixLabel(position, label), popupField);
		}
		
		public Color ColorField(Rect position, GUIContent label, Color value)
		{
			return ColorField(PrefixLabel(position, label), value);
		}
		
		public abstract void AssetHeader(Rect position, Object target, GUIContent label);
		
		/// <summary> Height of asset titlebar, i.e. the header for asset type Unity Objects and GameObjects. </summary>
		/// <value> The height of the asset titlebar. </value>
		/// <param name="toolbarHasTwoRowsOfButtons"> True if toolbar has two rows of buttons instead of the normal one. </param>
		/// <value> The height of the asset titlebar. </value>
		public abstract float AssetTitlebarHeight(bool toolbarHasTwoRowsOfButtons);

		public abstract bool ComponentHeader(Rect position, bool unfolded, Object[] targets, bool expandable, HeaderPart selectedPart, HeaderPart mouseoverPart);
		public abstract bool InspectorTitlebar(Rect position, bool unfolded, GUIContent label, bool expandable, HeaderPart selectedPart, HeaderPart mouseoverPart);
		public abstract void GameObjectHeader(Rect position, GameObject target);
		public abstract void AssetHeader(Rect position, Object target);
		public abstract bool Foldout(Rect position, GUIContent label, bool unfolded, bool selected, bool mouseovered, bool unappliedChanges, Rect? highlightRect = null);
		public abstract bool Foldout(Rect position, GUIContent label, bool unfolded, GUIStyle guiStyle, Rect? highlightRect = null);

		public abstract Rect PrefixLabel(Rect position, GUIContent label);
		public abstract void PrefixLabel(Rect position, GUIContent label, bool selected, bool unappliedChanges, out Rect labelRect, out Rect controlRect);
		public abstract Rect PrefixLabel(Rect position, GUIContent label, GUIStyle guiStyle);
		public abstract Rect PrefixLabel(Rect position, GUIContent label, bool selected);
		public abstract void InlinedPrefixLabel(Rect position, GUIContent label, bool selected, bool unappliedChanges);
		public abstract void InlinedPrefixLabel(Rect position, GUIContent label);
		public abstract void InlinedPrefixLabel(Rect position, string label);
		public abstract void InlinedSelectedPrefixLabel(Rect position, GUIContent label);
		public abstract void InlinedMouseoveredPrefixLabel(Rect position, GUIContent label);
		public abstract void InlinedModifiedPrefixLabel(Rect position, GUIContent label);
		public abstract void InlinedSelectedModifiedPrefixLabel(Rect position, GUIContent label);
		public abstract void InlinedMouseoveredModifiedPrefixLabel(Rect position, GUIContent label);

		public abstract void HandleHintIcon(Rect position, GUIContent label);
		public abstract void HintIcon(Rect position, string text);
		public abstract void HelpBox(Rect position, string message, MessageType messageType);

		public bool Button(Rect position, GUIContent label)
        {
			GUI.Label(position, label, GUI.skin.button);
			return Event.current.type == EventType.MouseDown && position.Contains(Event.current.mousePosition) && Event.current.button == 0;
		}

		public bool Button(Rect position, GUIContent label, [NotNull]GUIStyle guiStyle)
        {
			GUI.Label(position, label, guiStyle);
			return Event.current.type == EventType.MouseDown && position.Contains(Event.current.mousePosition) && Event.current.button == 0;
		}

		public abstract bool MouseDownButton(Rect position, GUIContent label);
		public abstract bool MouseDownButton(Rect position, GUIContent label, GUIStyle guiStyle);

		public abstract bool Toggle(Rect position, bool value);

		public abstract int IntField(Rect position, int value);
		public abstract int IntField(Rect position, int value, bool delayed);
		public abstract float FloatField(Rect position, float value);
		public abstract float FloatField(Rect position, float value, bool delayed);
		public abstract double DoubleField(Rect position, double value);
		public abstract decimal DecimalField(Rect position, decimal value);
		public abstract short ShortField(Rect position, short value);
		public abstract ushort UShortField(Rect position, ushort value);

		public abstract string TextField(Rect position, string value);
		public abstract string TextField(Rect position, string value, bool delayed);
		public abstract string TextField(Rect position, GUIContent label, string value, GUIStyle guiStyle);
		public abstract string TextField(Rect position, string value, GUIStyle guiStyle);
		public abstract string TextArea(Rect position, string value, bool wordWrapping);

		public abstract string TextArea(Rect position, string value, GUIStyle guiStyle);

		public abstract void Label(Rect position, GUIContent label, string styleName);
		public abstract void Label(Rect position, GUIContent label, GUIStyle guiStyle);

		public abstract void EnumPopup(Rect position, EnumDrawer popupField);
		public abstract void EnumFlagsPopup(Rect position, EnumDrawer popupField);
		public abstract LayerMask MaskPopup(Rect position, LayerMask value);
		public abstract void TypePopup(Rect position, TypeDrawer popupField);

		public abstract Object ObjectField(Rect position, Object target, Type objectType, bool allowSceneObjects);

		public abstract Color ColorField(Rect position, Color value);

		public abstract AnimationCurve CurveField(Rect position, AnimationCurve value);
		public abstract Gradient GradientField(Rect position, Gradient value);
		public abstract float Slider<TValue>(Rect position, TValue value, float min, float max) where TValue : IConvertible;
		public abstract float Slider(Rect position, float value, float min, float max);
		public abstract int Slider(Rect position, int value, int min, int max);

		public abstract void AddCursorRect(Rect position, MouseCursor cursor);

		public abstract void PingObject([NotNull]Object target);
		
		public abstract void AcceptDrag();

		public abstract int DisplayDialog(string title, string message, string button1, string button2, string button3);

		public abstract bool DisplayDialog(string title, string message, string ok, string cancel);
		public abstract void DisplayDialog(string title, string message, string ok);

		public int Toolbar(Rect position, int selectedTab, GUIContent[] tabLabels)
		{
			return GUI.Toolbar(position, selectedTab, tabLabels);
		}

		/// <summary>
		/// Returns a new GUIStyle instance for the given state.
		/// </summary>
		/// <param name="selected"> Is the foldout currently selected? </param>
		/// <param name="mouseovered"> Is the foldout currently mouseovered? </param>
		/// <param name="unappliedChanges"> Does the drawer target has unapplied changes in the prefab instance? </param>
		/// <param name="textClipping"> Should text be allowed to clip past the bounds of the foldout's draw rect? </param>
		/// <returns></returns>
		public static GUIStyle GetFoldoutStyle(bool selected, bool mouseovered, bool unappliedChanges, bool textClipping)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			//Debug.Assert(Event.current != null, "GetFoldoutStyle called with Event.current null. Unsafe?");
			Debug.Assert(foldoutStyleSelectedMouseovered != null, "foldoutStyleSelectedMouseovered null");
			Debug.Assert(foldoutStyleSelected != null, "foldoutStyleSelected null");
			Debug.Assert(setupDone, "!setupDone");
			#endif

			GUIStyle styleFoldout;
			if(selected)
			{
				var inspector = InspectorUtility.ActiveInspector;
				if(inspector == null || inspector.InspectorDrawer.HasFocus)
				{
					styleFoldout = new GUIStyle(mouseovered ? foldoutStyleSelectedMouseovered : foldoutStyleSelected);
				}
				else
				{
					styleFoldout = new GUIStyle(mouseovered ? foldoutStyleSelectedMouseoveredUnfocused : foldoutStyleSelectedUnfocused);
				}
			}
			else
			{
				styleFoldout = new GUIStyle(mouseovered ? foldoutStyleMouseovered : foldoutStyle);
			}

			if(unappliedChanges)
			{
				styleFoldout.fontStyle = FontStyle.Bold;
			}

			if(textClipping)
			{
				styleFoldout.clipping = TextClipping.Clip;
			}

			return styleFoldout;
		}

		public static GUIStyle GetFoldoutStyle(FontStyle fontStyle)
		{
			foldoutStyle.fontStyle = fontStyle;
			return foldoutStyle;
		}

		/// <summary> Executes action now if Event.current is not null, otherwise ends until next layout event and then executes the action. </summary>
		/// <param name="action"> The action to execute during OnGUI. </param>
		public static void DoAsSoonAsPossibleButDuringOnGUI(Action action)
		{
			if(Event.current != null)
			{
				action();
			}
			else
			{
				OnNextBeginOnGUI(action, true);
			}
		}

		public static void LayoutSpace(float unscaledPixels)
		{
			GUILayout.Space(unscaledPixels * MemberScaler.CurrentScale);
		}
	}
}