#define PI_ENABLE_UI_SUPPORT
#define SAFE_MODE
#define UPDATE_VALUES_ON_INSPECTOR_UPDATE

//#define DEBUG_ON_SELECTION_CHANGE
//#define DEBUG_FOCUS
//#define DEBUG_MOUSEOVERED_INSTANCE
#define DEBUG_SPLIT_VIEW
//#define DEBUG_ON_HIERARCHY_CHANGE
#define DEBUG_PLAY_MODE_CHANGED
//#define DEBUG_ON_SCRIPTS_RELOADED
//#define DEBUG_ANY_INSPECTOR_PART_MOUSEOVERED
//#define DEBUG_KEY_DOWN
//#define DEBUG_REPAINT
//#define DEBUG_DRAW_SCREENSHOT
#define DEBUG_ABORT_ONGUI
//#define DEBUG_SETUP

#define ENABLE_INSPECTOR_NAME_CONFLICT_FIX

using System;
using UnityEditor;
using UnityEngine;
using JetBrains.Annotations;
using Object = UnityEngine.Object;
using Sisus.Compatibility;

#if UNITY_2019_1_OR_NEWER // UI Toolkit doesn't exist in older versions
using UnityEngine.UIElements;
#endif

namespace Sisus
{
	/// <summary>
	/// Class that can be inherited from to handle drawing of inspector views.
	/// </summary>
	public abstract class InspectorDrawerWindow<TInspectorDrawer, TInspector> : EditorWindow, ISplittableInspectorDrawer, ISerializationCallbackReceiver where TInspectorDrawer : InspectorDrawerWindow<TInspectorDrawer, TInspector> where TInspector : class, IInspector, new()
	{
		#if ENABLE_INSPECTOR_NAME_CONFLICT_FIX
		protected const string DEFAULT_WINDOW_TITLE = "lnspector"; // First letter is actually a lowercase 'L'. This prevents issues with changes to titleContent migrating between default inspector and power inspector windows.
		#else
		protected const string DEFAULT_WINDOW_TITLE = "Inspector";
		#endif

		#if DEV_MODE
		private static double? firstOnGUITime;
		#endif

		[CanBeNull]
		private static InspectorDrawerWindow<TInspectorDrawer, TInspector> mouseoveredInstance;

		[SerializeField]
		private IdProvider idProvider = new IdProvider();

		[CanBeNull, NonSerialized]
		protected IEditorWindowMessageDispenser messageDispenser;

		[SerializeField]
		private float lastUpdateTime;

		[SerializeField]
		protected EditorWindowMinimizer minimizer;

		[SerializeField]
		protected InspectorTargetingMode inspectorTargetingMode = InspectorTargetingMode.All;

		[SerializeField]
		public Editors editors = new Editors();

		[SerializeField]
		private float onUpdateDeltaTime;
		[SerializeField]
		private float onGUIDeltaTime;
		[SerializeField]
		private float lastOnGUITime;

		[SerializeField]
		private PrefixColumnWidths prefixColumnWidths = new PrefixColumnWidths();

		[SerializeField]
		private TInspector mainView;

		#if UNITY_2019_3_OR_NEWER
		[SerializeReference]
		#else
		[NonSerialized]
		#endif
		private TInspector splitView;
		[SerializeField]
		private bool viewIsSplit;
		#if !UNITY_2019_3_OR_NEWER
		// In older Unity versions splitView is serialized manually so that Unity won't assign an instance to it after every deserialization.
		[SerializeField]
		private string splitViewSerialized = "";
		#endif

		[NonSerialized]
		private bool isDirty = true;
		[NonSerialized]
		private bool swapSplitSidesOnNextRepaint;
		[NonSerialized]
		private bool subscribedToEvents;

		[NonSerialized]
		protected InspectorManager inspectorManager;

		[NonSerialized]
		private bool nowRestoringPreviousState;
		[NonSerialized]
		private bool nowClosing;
		[NonSerialized]
		private bool nowLosingFocus;

		[NonSerialized]
		private bool earlySetupDone = false;

		#if DEV_MODE && DEBUG_ON_SELECTION_CHANGE
		private float lastSelectionChangeTime = -1000f;
		#endif

		/// <inheritdoc/>
		public IdProvider IdProvider
        {
            get
            {
				return idProvider;
            }
        }

		/// <summary> Gets a value indicating whether we can split view. </summary>
		/// <value> True if we can split view, false if not. </value>
		public abstract bool CanSplitView
		{
			get;
		}

		/// <summary> Gets the type of the inspectors that are drawn by this class. </summary>
		/// <value> The type of the drawn inspector. </value>
		[NotNull]
		public abstract Type InspectorType
		{
			get;
		}

		/// <inheritdoc />
		public UpdateEvent OnUpdate { get; set; }

		/// <inheritdoc />
		public ISelectionManager SelectionManager
		{
			get;
			protected set;
		}

		/// <inheritdoc />
		public PrefixColumnWidths PrefixColumnWidths
		{
			get
			{
				return prefixColumnWidths;
			}
		}

		/// <inheritdoc />
		public bool UpdateAnimationsNow
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc />
		public float AnimationDeltaTime
		{
			get
			{
				return onGUIDeltaTime;
			}
		}

		/// <inheritdoc />
		public Object UnityObject
		{
			get
			{
				return this;
			}
		}

		/// <inheritdoc />
		public bool HasFocus
		{
			get
			{
				return focusedWindow == this && !nowLosingFocus;
			}
		}

		/// <inheritdoc/>
		public IInspectorManager Manager
		{
			get
			{
				return inspectorManager; 
			}
		}

		/// <inheritdoc/>
		public bool ViewIsSplit
		{
			get
			{
				return viewIsSplit;
			}
		}

		/// <summary> Returns true if cursor is currently over the window's viewport
		/// </summary>
		/// <value> True if mouse is over window, false if not. </value>
		public bool MouseIsOver
		{
			get
			{
				//Using if(mouseOverWindow == this) doesn't seem to be a reliable method.
				//It seemed to be null sometimes when dragging Object references over the window.
				//Also it's set even when mouseovering Window borders or the top Tab.
				return ReferenceEquals(mouseoveredInstance, this);
			}
		}

		/// <inheritdoc/>
		public IInspector MainView
		{
			get
			{
				return mainView;
			}
		}

		/// <inheritdoc/>
		public IInspector SplitView
		{
			get
			{
				return splitView;
			}
		}

		/// <inheritdoc/>
		public InspectorTargetingMode InspectorTargetingMode
		{
			get
			{
				return inspectorTargetingMode;
			}
		}

		/// <summary> Gets the title text displayed on the tab of the EditorWindow. </summary>
		/// <value> The title text. </value>
		protected virtual string TitleText
		{
			get
			{
				return DEFAULT_WINDOW_TITLE;
			}
		}

		/// <inheritdoc/>
		public bool SetupDone
		{
			get
			{
				return mainView != null && mainView.SetupDone;
			}
		}

		/// <inheritdoc/>
		public bool NowClosing
		{
			get
			{
				return nowClosing;
			}
		}

		/// <inheritdoc/>
		public Editors Editors
		{
			get
			{
				return editors;
			}
		}

		[NotNull]
		protected static T CreateNewWithoutShowing<T>(bool addAsTab) where T : TInspectorDrawer
		{
			var manager = InspectorUtility.ActiveManager;
			if(manager == null)
			{
				manager = InspectorManager.Instance();
			}

			// Support adding as new tab next to the default inspector or any other window named "Inspector"
			EditorWindow existingWindow;
			if(focusedWindow != null && string.Equals(focusedWindow.titleContent.text, "Inspector"))
			{
				existingWindow = focusedWindow;
			}
			// If no such window is currently focused, then try to find last inspector drawer of same as the created window.
			else
			{
				existingWindow = manager.GetLastSelectedInspectorDrawer(typeof(T)) as EditorWindow;
			}

			var created = (EditorWindow)CreateInstance(typeof(T));

			if(existingWindow != null)
			{
				var existingInspectorDrawerWindow = existingWindow as T;
				//Sometimes instance can refer to an invisible window with an invalid state.
				//This might happen e.g. when the editor is started with the window being open but with
				//scripts containing compile errors. The problem can also be fixed by reverting Layout to
				//factory preferences, but let's handle that manually.
				if(existingInspectorDrawerWindow != null && !existingInspectorDrawerWindow.SetupDone)
				{
					#if DEV_MODE
					Debug.LogError("!existingInstance.SetupDone");
					#endif
					
					//use Close or DestroyImmediate?
					DestroyImmediate(existingInspectorDrawerWindow);
				}
				//if there was an existing InspectorDrawerWindow instance
				//add the new instance as a new tab on the same HostView
				else if(addAsTab)
				{
					existingWindow.AddTab(created);
				}
			}
			
			return (T)created;
		}

		/// <inheritdoc/>
		public void FocusWindow()
		{
			#if DEV_MODE && DEBUG_FOCUS
			Debug.Log("FocusWindow");
			#endif
			Focus();
		}

		[NotNull]
		protected static T CreateNew<T>([NotNull]string title, [NotNull]Object[] inspect, bool lockView = false, bool addAsTab = true, Vector2 minSize = default(Vector2), Vector2 maxSize = default(Vector2), bool utility = false) where T : TInspectorDrawer
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Event.current != null, "Shouldn't call CreateNew<" + typeof(T).Name + "> with Event.current null, because can't perform full Setup immediately.");
			Debug.Assert(inspect != null);
			#endif

			#if DEV_MODE
			Debug.Log(typeof(T).Name + ".CreateNew(" + StringUtils.ToString(inspect) + ", lockView="+lockView);
			#endif

			var drawerWindow = CreateNewWithoutShowing<T>(addAsTab);
			if(!drawerWindow.SetupDone)
			{
				drawerWindow.Setup(lockView, Vector2.zero, minSize.x, minSize.y, maxSize.x, maxSize.y);
			}
			drawerWindow.MainView.State.ViewIsLocked = lockView;
			drawerWindow.MainView.RebuildDrawers(inspect, false);

			if(utility)
			{
				drawerWindow.ShowUtility();
			}
			else
			{
				drawerWindow.Show();
			}
			
			drawerWindow.FocusWindow();

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(drawerWindow.MainView.State.ViewIsLocked == lockView);
			#endif

			return drawerWindow;
		}
	
		[UsedImplicitly]
		private void Start()
		{
			#if DEV_MODE
			Debug.Log("Start with SetupDone="+StringUtils.ToColorizedString(SetupDone));
			#endif

			if(EditorUtility.scriptCompilationFailed && !SetupDone)
			{
				#if DEV_MODE
				Debug.LogError("InspectorDrawerWindow instance with probably an invalid state found. Destroy automatically? InspectorUtility.ActiveManager="+ StringUtils.ToString(InspectorUtility.ActiveManager));
				if(DrawGUI.Active.DisplayDialog("InspectorDrawerWindow with invalid state", "InspectorDrawerWindow instance with probably an invalid state found. Destroy automatically?\nInspectorUtility.ActiveManager=" + StringUtils.ToString(InspectorUtility.ActiveManager), "Destroy", "Leave It"))
				{
					DestroyImmediate(this);
				}
				#endif
			}
		}

		private void Setup()
		{
			if(!SetupDone)
			{
				Setup(false, Vector2.zero, -1f, -1f, -1f, -1f);
			}
		}
		
		protected virtual GUIContent GetTitleContent()
		{
			return GetTitleContent(HasFocus && !ApplicationUtility.IsQuitting);
		}

		protected virtual GUIContent GetTitleContent(bool hasFocus)
		{
			return GetTitleContent(inspectorTargetingMode, hasFocus);
		}
		
		protected virtual GUIContent GetTitleContent(InspectorTargetingMode mode, bool hasFocus)
		{
			if(!earlySetupDone)
			{
				InspectorManager.Instance().OnNextOnGUI(UpdateWindowIcon, this);
				return GUIContentPool.Create(TitleText);
			}
			return GUIContentPool.Create(TitleText, GetWindowIcon(mode, hasFocus));
		}

		private Texture GetWindowIcon()
		{
			return GetWindowIcon(HasFocus);
		}

		private Texture GetWindowIcon(bool hasFocus)
		{
			return GetWindowIcon(inspectorTargetingMode, hasFocus);
		}

		private Texture GetWindowIcon(InspectorTargetingMode mode, bool hasFocus)
		{
			if(!earlySetupDone)
			{
				return null;
			}

			switch(mode)
			{
				default:
					var preferences = GetPreferences();
					return hasFocus ? preferences.graphics.InspectorIconActive : preferences.graphics.InspectorIconInactive;
				case InspectorTargetingMode.Hierarchy:
					return EditorGUIUtility.Load("icons/UnityEditor.SceneHierarchyWindow.png") as Texture;
				case InspectorTargetingMode.Project:
					return EditorGUIUtility.Load("icons/Project.png") as Texture;
			}
		}

		/// <summary> Gets preferences for inspector drawer. </summary>
		/// <returns> The preferences. </returns>
		protected virtual InspectorPreferences GetPreferences()
		{
			return InspectorUtility.Preferences;
		}

		/// <summary> Gets default selection manager. </summary>
		/// <returns> The default selection manager. </returns>
		protected virtual ISelectionManager GetDefaultSelectionManager()
		{
			return EditorSelectionManager.Instance();
		}

		/// <summary> Setups the window and its views so they are ready to be used. </summary>
		/// <param name="lockView"> True to lock the view, false to leave it unlocked. </param>
		/// <param name="scrollPos"> (Optional) The scroll position for the main view's viewport. </param>
		/// <param name="minWidth"> (Optional) The minimum width to which the window can be resized. </param>
		/// <param name="minHeight"> (Optional) The minimum height to which the window can be resized. </param>
		/// <param name="maxWidth"> (Optional) The maximum width to which the window can be resized. </param>
		/// <param name="maxHeight"> (Optional) The maximum height to which the window can be resised. </param>
		protected virtual void Setup(bool lockView, Vector2 scrollPos = default(Vector2), float minWidth = 280f, float minHeight = 130f, float maxWidth = 0f, float maxHeight = 0f)
		{
			if(!earlySetupDone)
			{
				EarlySetup();
			}

			#if DEV_MODE
			var timer = new ExecutionTimeLogger();
			timer.Start(GetType().Name+".Setup");
			#endif

			#if DEV_MODE
			Debug.Assert(!SetupDone);
			#endif

			#if DEV_MODE
			timer.StartInterval("minimizer.Setup");
			#endif

			if(minimizer == null)
			{
				minimizer = new EditorWindowMinimizer(this, SelectionManager, false);
			}
			else
			{
				minimizer.Setup(this, SelectionManager, minimizer.AutoMinimize);
			}

			#if DEV_MODE
			timer.FinishInterval();
			#endif

			var preferences = GetPreferences();

			if(mainView == null)
			{
				#if DEV_MODE
				timer.StartInterval("Create mainView");
				#endif

				mainView = CreateInspector(ArrayPool<Object>.ZeroSizeArray, lockView, scrollPos);
			}
			else
			{
				#if DEV_MODE
				timer.StartInterval("Reuse existing mainView");
				#endif

				var state = mainView.State;

				#if DEV_MODE && DEBUG_SETUP
				Debug.Log("Reusing existing mainView instance with inspected="+StringUtils.ToString(state.inspected));
				#endif
				 
				mainView.Setup(this, preferences, state.inspected, state.ScrollPos, state.ViewIsLocked); 
			}

			#if DEV_MODE
			timer.FinishInterval();
			#endif

			if(splitView != null)
			{
				if(viewIsSplit)
				{
					#if DEV_MODE && DEBUG_SETUP
					Debug.Log("Setup called with viewIsSplit=True and splitView=NotNull");
					#endif

					#if DEV_MODE
					timer.StartInterval("create splitView");
					#endif

					var state = splitView.State;
					splitView.Setup(this, preferences, state.inspected, state.ScrollPos, state.ViewIsLocked);
				}
				else
				{
					#if DEV_MODE
					timer.StartInterval("reuse existing splitView");
					#endif

					#if DEV_MODE
					Debug.LogWarning("Setup called with viewIsSplit=false but splitView=NotNull. This can happen due Unity deserialization process setting things not null.");
					#endif
					inspectorManager.Dispose(ref splitView);
				}

				#if DEV_MODE
				timer.FinishInterval();
				#endif
			}
			else
			{
				#if DEV_MODE
				if(viewIsSplit) { Debug.LogWarning("Setup called with viewIsSplit=True but splitView=null"); }
				#endif
				viewIsSplit = false;
			}

			#if DEV_MODE
			timer.StartInterval("subscribe to events");
			#endif

			SubscribeToEvents();

			#if DEV_MODE
			timer.FinishInterval();
			#endif


			if(minWidth > 0f && minHeight > 0f)
			{
				minSize = new Vector2(minWidth, minHeight);
			}
			if(maxWidth > 0f && maxHeight > 0f)
			{
				maxSize = new Vector2(maxWidth, maxHeight);
			}

			#if DEV_MODE
			timer.StartInterval("GetTitleContent");
			#endif

			titleContent = GetTitleContent();

			#if DEV_MODE
			timer.FinishInterval();
			#endif

			if(HasFocus)
			{
				if(inspectorManager.SelectedInspector != mainView)
				{
					#if DEV_MODE
					Debug.LogWarning("Selecting mainView of newly created InspectorDrawerWindow instance because window had focus");
					#endif
					inspectorManager.Select(mainView, InspectorPart.Viewport, ReasonSelectionChanged.Initialization);
				}
			}

			if(!nowRestoringPreviousState)
			{
				//NEW
				inspectorManager.CancelOnNextLayout(RebuildDrawerAndExitGUIIfTargetsChanged);
				inspectorManager.OnNextLayout(RebuildDrawerAndExitGUIIfTargetsChanged, this);
			}

			#if DEV_MODE
			timer.StartInterval("SetupMessageDispenser");
			#endif

			SetupMessageDispenser(ref messageDispenser, preferences);

			#if DEV_MODE
			timer.FinishInterval();
			timer.FinishAndLogResults();
			#endif
		}

		/// <summary>
		/// Creates a new instance of a message dispenser responsible for displaying messages for the inspector drawer.
		/// </summary>
		/// <returns></returns>
		[CanBeNull]
		protected virtual void SetupMessageDispenser([CanBeNull]ref IEditorWindowMessageDispenser result, InspectorPreferences preferences)
		{
			var displayMethod = preferences.messageDisplayMethod;
			if(displayMethod.HasFlag(MessageDisplayMethod.Notification))
			{
				if((result as NotificationMessageDispenser) == null)
				{
					result = new NotificationMessageDispenser(this, preferences);
					return;
				}
				result.Setup(this, preferences);
				return;
			}

			if(displayMethod.HasFlag(MessageDisplayMethod.Console))
			{
				if((result as ConsoleMessageDispenser) == null)
				{
					result = new ConsoleMessageDispenser(this);
					return;
				}
				result.Setup(this, preferences);
				return;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(displayMethod == MessageDisplayMethod.None);
			#endif

			result = null;
		}

		private void SubscribeToEvents()
		{
			autoRepaintOnSceneChange = true;
			wantsMouseMove = true;
			wantsMouseEnterLeaveWindow = true;

			if(!subscribedToEvents)
			{
				subscribedToEvents = true;
				
				Undo.undoRedoPerformed += OnUndoOrRedo;
				PlayMode.OnStateChanged += OnEditorPlaymodeStateChanged;
				EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemCallback;
				EditorApplication.projectWindowItemOnGUI += ProjectWindowItemCallback;
				
				#if UNITY_2017_1_OR_NEWER
				AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
				AssemblyReloadEvents.afterAssemblyReload += OnScriptsReloaded;
				#endif
				
				#if UNITY_2018_2_OR_NEWER
				EditorApplication.quitting += OnEditorQuitting;
				#endif
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Undo.undoRedoPerformed != null);
			Debug.Assert(PlayMode.OnStateChanged != null);
			Debug.Assert(EditorApplication.hierarchyWindowItemOnGUI != null);
			Debug.Assert(EditorApplication.projectWindowItemOnGUI != null);
			#endif
		}

		private void OnUndoOrRedo()
		{
			#if DEV_MODE
			Debug.Log("Undo detected with Event="+StringUtils.ToString(Event.current));
			#endif
			
			inspectorManager.OnNextLayout(RefreshView);
		}

		private void OnEditorPlaymodeStateChanged(PlayModeStateChange playModeStateChange)
		{
			#if DEV_MODE && DEBUG_PLAY_MODE_CHANGED
			Debug.Log("OnEditorPlaymodeStateChanged("+ playModeStateChange + ") with SetupDone=" + SetupDone);
			#endif
			
			editors.CleanUp();

			#if UNITY_2020_1_OR_NEWER
			if(!EditorSettings.enterPlayModeOptionsEnabled || !EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableSceneReload))
			#endif
			{
				MainView.OnBeforeAssemblyReload();
				if(viewIsSplit)
				{
					SplitView.OnBeforeAssemblyReload();
				}
			}

			OnHierarchyOrProjectPossiblyChanged(OnChangedEventSubject.PlayModeChange, true, false);
        }

        private void HierarchyWindowItemCallback(int instanceId, Rect selectionRect)
		{
			var e = Event.current;
			var type = e.type;
			
			// NOTE: This currently only works via the menu item File/Copy or using the keyboard shortcut Ctrl+C. Copying via right-click menu is not supported.
			if(type == EventType.ValidateCommand && string.Equals(e.commandName, "Copy") && selectionRect.y < 1f)
			{
				var activeInspector = Manager.LastSelectedActiveOrDefaultInspector();
				if(activeInspector != null && activeInspector.InspectorDrawer as InspectorDrawerWindow<TInspectorDrawer, TInspector> != this)
				{
					return;
				}

				#if DEV_MODE
				Debug.Log("Copy selected Hierarchy ValidateCommand detected: " + StringUtils.ToString(e) + " with selectionRect="+ selectionRect);
				#endif

				DrawGUI.Use(e);

				var gameObjects = Selection.gameObjects;
				int count = gameObjects.Length;
				if(count == 1)
				{
					Clipboard.CopyObjectReference(gameObjects[0], Types.GameObject);
				}
				else if(count > 1)
				{
					Clipboard.CopyObjectReferences(gameObjects, Types.GameObject);
				}
			}
			//on middle mouse click on hierarchy view item, open that item in the split view
			else if(e.button == 2 && type == EventType.MouseDown && selectionRect.Contains(e.mousePosition))
			{
				var activeInspector = Manager.LastSelectedActiveOrDefaultInspector();
				if(activeInspector != null && activeInspector.InspectorDrawer as InspectorDrawerWindow<TInspectorDrawer, TInspector> != this)
				{
					return;
				}

				var target = EditorUtility.InstanceIDToObject(instanceId);

				var window = focusedWindow;
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(window != null && window.GetType().Name == "SceneHierarchyWindow");
				#endif

				if(!CanSplitView)
				{
					#if DEV_MODE
					Debug.LogWarning("Ignoring hierarchy item middle click because CanSplitView was false...");
					#endif
					return;
				}

				if(inspectorTargetingMode == InspectorTargetingMode.Project)
				{
					#if DEV_MODE
					Debug.LogWarning("Ignoring hierarchy item middle click because inspectorTargetingMode was set to Project...");
					#endif
					return;
				}

				DrawGUI.Use(e);
				
				var inspect = ArrayPool<Object>.CreateWithContent(target);

				#if DEV_MODE
				Debug.Log("Hierarchy item \""+target.Transform().GetHierarchyPath()+ "\" middle clicked, opening in split view... (viewIsSplit="+StringUtils.ToColorizedString((bool)viewIsSplit)+", splitView="+(splitView == null ? StringUtils.Null : StringUtils.Green("NotNull")) +")");
				#endif
				
				ShowInSplitView(inspect);

				#if DEV_MODE
				Debug.Assert(viewIsSplit);
				Debug.Assert(splitView != null);
				Debug.Assert(splitView.SetupDone);
				Debug.Assert(CanSplitView);
				Debug.Assert(splitView.State.drawers.Length == 1);
				Debug.Assert(splitView.State.drawers.Length == 0 || splitView.State.drawers[0].UnityObject == target);
				Debug.Assert(splitView.State.inspected.Length == 1);
				Debug.Assert(splitView.State.inspected.Length == 0 || splitView.State.inspected[0] == target);
				#endif

				ExitGUIUtility.ExitGUI(); // new test
			}
			// if window has a search filter, allow clearing it by clicking the element with control held down
			else if(e.button == 1 && e.control && type == EventType.MouseDown && selectionRect.Contains(e.mousePosition))
			{
				var window = focusedWindow;
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(window != null && window.GetType().Name == "SceneHierarchyWindow");
				#endif

				if(window != null)
				{
					var hasSearchFilterProperty = window.GetType().GetProperty("hasSearchFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
					if(hasSearchFilterProperty != null)
					{
						bool sceneViewHasFilter = (bool)hasSearchFilterProperty.GetValue(window, null);
						if(sceneViewHasFilter)
						{
							var clearSearchMethod = window.GetType().GetMethod("ClearSearchFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
							if(clearSearchMethod != null)
							{
								clearSearchMethod.Invoke(window, null);

								var target = EditorUtility.InstanceIDToObject(instanceId);
								Selection.activeGameObject = target.GameObject();
								DrawGUI.Active.PingObject(target);
								DrawGUI.Use(e);
								return;
							}
							#if DEV_MODE
							else{ Debug.LogError("clearSearchMethod null"); }
							#endif
						}
						#if DEV_MODE && DEBUG_MMB
						else{ Debug.Log("SceneHierarchyWindow sceneViewHasFilter false"); }
						#endif
					}
					#if DEV_MODE
					else{ Debug.LogError("hasSearchFilterProperty null"); }
					#endif
				}
			}
		}
		
		protected TInspector CreateInspector(bool lockView = false, Vector2 scrollPos = default(Vector2))
		{
			return CreateInspector(ArrayPool<Object>.ZeroSizeArray, lockView, scrollPos);
		}

		protected TInspector CreateInspector([NotNull]Object[] inspect, bool lockView = false, Vector2 scrollPos = default(Vector2))
		{
			TInspector result;
			inspectorManager.Create(out result, this, GetPreferences(), inspect, scrollPos, lockView);

			#if DEV_MODE && PI_ASSERTATIONS
			var state = result.State;
			Debug.Assert(state.ViewIsLocked == lockView, ToString() +" state.ViewIsLocked ("+StringUtils.ToColorizedString(state.ViewIsLocked)+") did not match lockView ("+StringUtils.ToColorizedString(lockView)+") after Create");
			Debug.Assert(state.ScrollPos.Equals(scrollPos), "state.ScrollPos "+state.ScrollPos+" != "+scrollPos);
			if(!state.inspected.ContentsMatch(inspect.RemoveNullObjects())) { Debug.LogError("state.inspected ("+StringUtils.TypesToString(state.inspected)+") != inspect.RemoveNullObjects ("+StringUtils.TypesToString(inspect.RemoveNullObjects())+")"); }
			#endif

			return result;
		}

		/// <inheritdoc />
		public void OnInspectedChanged(Object[] inspected, DrawerGroup drawerGroup)
		{
			inspectorManager.CancelOnNextLayout(RebuildDrawerAndExitGUIIfTargetsChanged);
		}
		
		private void ProjectWindowItemCallback(string guid, Rect selectionRect)
		{
			var e = Event.current;
			var type = e.type;

			// NOTE: This currently only works via the menu item File/Copy or using the keyboard shortcut Ctrl+C. Copying via right-click menu is not supported.
			if(type == EventType.ValidateCommand && string.Equals(e.commandName, "Copy") && selectionRect.y < 1f)
			{
				var activeInspector = Manager.LastSelectedActiveOrDefaultInspector();
				if(activeInspector != null && activeInspector.InspectorDrawer as InspectorDrawerWindow<TInspectorDrawer, TInspector> != this)
				{
					return;
				}

				#if DEV_MODE
				Debug.Log("Copy selected ProjectWindow item command detected: " + StringUtils.ToString(e) + " with selectionRect="+ selectionRect);
				#endif

				DrawGUI.Use(e);

				var objects = Selection.objects;
				int count = objects.Length;
				if(count == 1)
				{
					Clipboard.CopyObjectReference(objects[0], objects[0].GetType());
				}
				else if(count > 1)
				{
					Clipboard.CopyObjectReferences(objects, Types.UnityObject);
				}
			}
			//on middle mouse click on hierarchy view item, open that item in the split view
			else if(e.button == 2 && type == EventType.MouseDown && selectionRect.Contains(e.mousePosition))
			{
				var activeInspector = Manager.LastSelectedActiveOrDefaultInspector();
				if(activeInspector != null && activeInspector.InspectorDrawer as InspectorDrawerWindow<TInspectorDrawer, TInspector> != this)
				{
					return;
				}

				var window = focusedWindow;
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(window != null && window.GetType().Name == "ProjectBrowser");
				#endif

				if(!CanSplitView)
				{
					#if DEV_MODE
					Debug.LogWarning("Ignoring project item middle click because CanSplitView was false...");
					#endif
					return;
				}

				if(inspectorTargetingMode == InspectorTargetingMode.Hierarchy)
				{
					#if DEV_MODE
					Debug.LogWarning("Ignoring project item middle click because inspectorTargetingMode was set to Hierarchy...");
					#endif
					return;
				}

				DrawGUI.Use(e); 
				
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var target = AssetDatabase.LoadMainAssetAtPath(path);
				var inspect = ArrayPool<Object>.CreateWithContent(target);

				#if DEV_MODE
				Debug.Log("Project item \"" + path + "\" middle clicked, opening in split view...");
				#endif

				if(splitView == null)
				{
					splitView = CreateInspector(inspect, true);
				}
				else
				{
					splitView.State.ViewIsLocked = true;
					splitView.RebuildDrawers(inspect, false);
				}
				SetSplitView(true);
			}
			// if window has a search filter, allow clearing it by clicking the element with control held down
			else if(e.button == 1 && e.control && type == EventType.MouseDown && selectionRect.Contains(e.mousePosition))
			{
				var activeInspector = Manager.LastSelectedActiveOrDefaultInspector();
				if(activeInspector != null && activeInspector.InspectorDrawer as InspectorDrawerWindow<TInspectorDrawer, TInspector> != this)
				{
					return;
				}

				var window = focusedWindow;
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(window != null && window.GetType().Name == "ProjectBrowser");
				#endif

				if(window != null)
				{
					var searchFilterProperty = window.GetType().GetField("m_SearchFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
					if(searchFilterProperty != null)
					{
						var searchFilter = searchFilterProperty.GetValue(window);
						if(searchFilter != null)
						{
							var isSearchingMethod = searchFilter.GetType().GetMethod("IsSearching", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
							if(isSearchingMethod != null)
							{
								bool isSearching = (bool)isSearchingMethod.Invoke(searchFilter);
								if(isSearching)
								{
									var clearSearchMethod = window.GetType().GetMethod("ClearSearch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
									if(clearSearchMethod != null)
									{
										clearSearchMethod.Invoke(window, null);

										var path = AssetDatabase.GUIDToAssetPath(guid);
										var target = AssetDatabase.LoadMainAssetAtPath(path);
										Selection.activeObject = target;
										DrawGUI.Use(e);
										DrawGUI.Active.PingObject(target);
										return;
									}
									#if DEV_MODE
									else{ Debug.LogError("clearSearchMethod null"); }
									#endif
								}
								#if DEV_MODE && DEBUG_MMB
								else{ Debug.Log("ProjectBrowser isSearching false"); }
								#endif
							}
							#if DEV_MODE
							else{ Debug.LogError("isSearchingMethod null"); }
							#endif
						}
						#if DEV_MODE
						else{ Debug.LogError("searchFilter null"); }
						#endif
					}
					#if DEV_MODE
					else{ Debug.LogError("searchFilterProperty null"); }
					#endif
				}
			}
		}

		#if UNITY_2018_2_OR_NEWER
		private void OnEditorQuitting()
		{
			titleContent = GetTitleContent(false);
		}
		#endif
		
		private void UpdateCachedValuesFromFields()
		{
			mainView.UpdateCachedValuesFromFields();
			if(viewIsSplit)
			{
				splitView.UpdateCachedValuesFromFields();
			}

			Repaint();
		}

		/// <summary> Called when the window is closed. </summary>
		[UsedImplicitly]
		protected virtual void OnDestroy()
		{
			nowClosing = true;

			if(OnUpdate != null)
			{
				#if DEV_MODE
				Debug.Log("InspectorDrawerWindow.OnDestroy called with OnUpdate containing listeners. Calling it once more now.");
				#endif
				HandleOnUpdate();
			}

			#if DEV_MODE
			Debug.Log("InspectorDrawerWindow.OnDestroy with SetupDone="+SetupDone+", subscribedToEvents="+((bool)subscribedToEvents)+", mainView="+ StringUtils.TypeToString(mainView));
			#endif
			
			if(mainView != null)
			{
				InspectorManager.Instance().Dispose(ref mainView);
			}

			if(splitView != null)
			{
				InspectorManager.Instance().Dispose(ref splitView);
			}

			if(subscribedToEvents)
			{
				Undo.undoRedoPerformed -= OnUndoOrRedo;
				PlayMode.OnStateChanged -= OnEditorPlaymodeStateChanged;
				EditorApplication.hierarchyWindowItemOnGUI -= HierarchyWindowItemCallback;
				EditorApplication.projectWindowItemOnGUI -= ProjectWindowItemCallback;
				
				#if UNITY_2017_1_OR_NEWER
				AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
				AssemblyReloadEvents.afterAssemblyReload -= OnScriptsReloaded;
				#endif

				#if UNITY_2018_2_OR_NEWER
				EditorApplication.quitting -= OnEditorQuitting;
				#endif

				subscribedToEvents = false;
			}
		}
		
		[UsedImplicitly]
		private void OnHierarchyChange()
		{
			OnHierarchyOrProjectPossiblyChanged(OnChangedEventSubject.Hierarchy, true, false);
		}

		private void OnHierarchyOrProjectPossiblyChanged(OnChangedEventSubject changed, bool delayUntilNextOnGUI, bool forceRebuildDrawers)
		{
			#if DEV_MODE && DEBUG_ON_HIERARCHY_CHANGE
			Debug.Log("!!!!!!!! OnHierarchyOrProjectPossiblyChanged("+changed+") with delayUntilNextOnGUI=" + delayUntilNextOnGUI+", SetupDone = " + SetupDone+ ", forceRebuildDrawers=" + forceRebuildDrawers+"!!!!!!!!!");
			#endif

			var manager = Manager;

			if(delayUntilNextOnGUI)
			{
				if(manager == null)
				{
					manager = InspectorUtility.ActiveManager;
					if(manager == null)
					{
						manager = InspectorManager.Instance();
					}
				}

				manager.OnNextOnGUI(()=>OnHierarchyOrProjectPossiblyChanged(changed, false, forceRebuildDrawers));
				return;
			}

			bool hierarchiesHadNullTargets;
			LinkedMemberHierarchy.OnHierarchyChanged(out hierarchiesHadNullTargets);
			if(hierarchiesHadNullTargets)
			{
				forceRebuildDrawers = true;
			}

			// UPDATE: Setup can only be called during the first call to OnGUI
			// while this can sometimes get invoked before that.
			if(!SetupDone)
			{
				return;
			}

			manager.ActiveInspector = mainView;
			mainView.OnProjectOrHierarchyChanged(changed, forceRebuildDrawers);

			if(ViewIsSplit)
			{
				manager.ActiveInspector = splitView;
				splitView.OnProjectOrHierarchyChanged(changed, forceRebuildDrawers);
			}
		}
		
		[UsedImplicitly]
		private void OnProjectChange()
		{
			//UPDATE: Setup can only be called during the first call to OnGUI
			//while this can sometimes get invoked before that
			if(!SetupDone)
			{
				return;
			}

			OnHierarchyOrProjectPossiblyChanged(OnChangedEventSubject.Project, true, false);
		}
		
		protected void RebuildDrawerIfTargetsChanged()
		{
			RebuildDrawers(false);
		}

		protected void RebuildDrawerAndExitGUIIfTargetsChanged()
		{
			if(RebuildDrawers(false))
			{
				#if DEV_MODE
				if(KeyboardControlUtility.JustClickedControl != 0 || (Event.current != null && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.KeyDown))) { Debug.LogWarning("Calling ExitGUIUtility with JustClickedControl="+ KeyboardControlUtility.JustClickedControl+", Event="+StringUtils.ToString(Event.current)); }
				#endif

				ExitGUIUtility.ExitGUI();
			}
		}

		private bool RebuildDrawers(bool evenIfTargetsTheSame)
		{
			#if SAFE_MODE
			//UPDATE: Setup can only be called during the first call to OnGUI
			//if something should call this method before that, it will most
			//likely result in errors
			if(!SetupDone)
			{
				#if DEV_MODE
				Debug.LogError("RebuildDrawers("+evenIfTargetsTheSame+") was called with SetupDone false! This should not be called before the first OnGUI call!");
				#endif
				//Should we still make the call on next layout, or drop it?
				inspectorManager.OnNextLayout(()=>
				{
					if(!SetupDone)
					{
						Setup();
					}
					RebuildDrawers(evenIfTargetsTheSame);
				}, this);
				return false;
			}
			#endif

			bool viewChanged = mainView.RebuildDrawers(evenIfTargetsTheSame);

			if(viewIsSplit)
			{
				if(splitView == null)
				{
					#if DEV_MODE
					Debug.LogError("PowerInspectorWindow.splitView was true but splitBottom was null!");
					#endif
					SetSplitView(false);
				}

				if(splitView.RebuildDrawers(evenIfTargetsTheSame))
				{
					viewChanged = true;
				}
			}

			if(viewChanged)
			{
				RefreshView();
			}

			return viewChanged;
		}

		private void AddDefaultInspectorTab()
		{
			this.AddTab(Types.GetInternalEditorType("UnityEditor.InspectorWindow"));
		}
		
		[UsedImplicitly]
		private void Update()
		{
			HandleOnUpdate();
			
			if(LinkedMemberHierarchy.AnyHierarchyHasMissingTargets())
			{
				OnHierarchyOrProjectPossiblyChanged(OnChangedEventSubject.Hierarchy, false, false);
			}

			if(inspectorManager != null)
			{
				var mouseDownInfo = inspectorManager.MouseDownInfo;
				if(mouseDownInfo.MouseDownOverDrawer != null)
				{
					if(inspectorManager.MouseDownInfo.NowReordering)
					{
						Repaint();
					}
				}
			}
		}

		private void HandleOnUpdate()
		{
			float time = (float)EditorApplication.timeSinceStartup;
			onUpdateDeltaTime = time - lastUpdateTime;

			if(OnUpdate != null)
			{
				try
				{
					OnUpdate(onUpdateDeltaTime);
				}
				#if DEV_MODE
				catch(Exception e)
				{
					Debug.LogError(e);
				#else
				catch(Exception)
				{
				#endif
					lastUpdateTime = time;
					return;
				}
			}
			lastUpdateTime = time;
		}
		
		[UsedImplicitly]
		private void OnInspectorUpdate()
		{
			if(!SetupDone)
			{
				return;
			}
			
			if(isDirty)
			{
				#if DEV_MODE && DEBUG_REPAINT
				Debug.Log("Repaint");
				#endif

				GUI.changed = true;
			}
			
			#if UPDATE_VALUES_ON_INSPECTOR_UPDATE
			UpdateCachedValuesFromFields();
			#endif
		}
		
		[UsedImplicitly]
		private void OnSelectionChange()
		{
			if(!SetupDone)
			{
				return;
			}

			#if DEV_MODE && DEBUG_ON_SELECTION_CHANGE
			float time = Platform.Time;
			Debug.Log(StringUtils.ToColorizedString("On Selection Change: ", StringUtils.ToString(Selection.objects), " with selectTimeDiff=", (Platform.Time - lastSelectionChangeTime), ", mainView.Preferences.MergedMultiEditMode=", mainView.Preferences.MergedMultiEditMode));
			lastSelectionChangeTime = time;
			#endif

			
			inspectorManager.OnNextLayout(SwitchActiveInspectorAndUpdateContent, this);
		}

		private void SwitchActiveInspectorAndUpdateContent()
		{
			mainView.OnSelectionChange();
			if(viewIsSplit)
			{
				splitView.OnSelectionChange();
			}

			RefreshView();
		}

		[UsedImplicitly]
		private void OnFocus()
		{
			if(ObjectPicker.IsOpen)
			{
				#if DEV_MODE
				Debug.LogWarning("InspectorDrawerWindow.OnFocus was called with ObjectPicker open! mainView.Selected=" + StringUtils.ToColorizedString(mainView.Selected)+ ", mainView.FocusedDrawer=" + StringUtils.ToString(mainView.FocusedDrawer));
				#endif
				//TO DO: restore selected control after focus comes back? and before that, clear it?
				inspectorManager.Select(null, InspectorPart.None, null, ReasonSelectionChanged.LostFocus);
				return;
			}
			
			// this was added to fix issue where if window was focused with the cursor already inside its bounds
			// mouseoveredInstance would never get updated because the MouseEnterWindow was never received
			if(mouseOverWindow == this)
			{
				#if DEV_MODE && DEBUG_MOUSEOVERED_INSTANCE
				if(mouseoveredInstance != this) {  Debug.Log("mouseoveredInstance = "+mouseOverWindow); }
				#endif

				mouseoveredInstance = this;
			}

			if(!SetupDone)
			{
				//NOTE: It's important that this is called before Setup so that Setup will override the icon to use the inactive
				//one if setup has not been done yet since OnFocus gets called on the window on startup even if it does not have focus
				titleContent = GetTitleContent(inspectorTargetingMode, false);
				return;
			}

			var activeInspectorWas = inspectorManager.ActiveInspector;
			inspectorManager.ActiveInspector = mainView;

			//NOTE: It's important that this is called before Setup so that Setup will override the icon to use the inactive
			//one if setup has not been done yet since OnFocus gets called on the window on startup even if it does not have focus
			if(inspectorTargetingMode == InspectorTargetingMode.All)
			{
				titleContent = GetTitleContent(InspectorTargetingMode.All, true);
			}

			// make sure all other InspectorDrawerWindow instances have the inactive icon
			var activeInspectors = inspectorManager.ActiveInstances;
			for(int n = activeInspectors.Count - 1; n >= 0; n--)
			{
				var inspectorDrawer = activeInspectors[n].InspectorDrawer;
				if(!ReferenceEquals(inspectorDrawer, this))
				{
					var updateIconOfDrawer = inspectorDrawer as InspectorDrawerWindow<TInspectorDrawer, TInspector>;
					if(updateIconOfDrawer != null)
					{
						updateIconOfDrawer.UpdateWindowIcon();
					}
				}
			}

			#if DEV_MODE && DEBUG_FOCUS
			Debug.Log("OnFocus with selectedInspector="+StringUtils.ToString(inspectorManager.SelectedInspector)+ ", SelectedInspectorPart=" + inspectorManager.SelectedInspectorPart+", TimeSinceLastUpdate="+ (EditorApplication.timeSinceStartup - lastUpdateTime));
			#endif
			
			inspectorManager.selected.OnInspectorDrawerGainedFocus(this);

			var selectedInspector = inspectorManager.SelectedInspector;
			if(selectedInspector != mainView && (!viewIsSplit || splitView != selectedInspector))
			{
				inspectorManager.Select(mainView, InspectorPart.Viewport, ReasonSelectionChanged.GainedFocus);
			}			

			Repaint();
			
			// this is here so that if an EditorWindow tab was docked and not visible
			// and it gains focus, we check that the target that was being inspected
			// is still the selected target, exists etc.
			inspectorManager.OnNextLayout(RebuildDrawerAndExitGUIIfTargetsChanged, this);
		
			SubscribeToEvents();

			if(activeInspectorWas != null && activeInspectorWas != mainView)
			{
				inspectorManager.ActiveInspector = activeInspectorWas;
			}

			const double WasBackgroundTabThreshold = 1d;
			if(EditorApplication.timeSinceStartup - lastUpdateTime > WasBackgroundTabThreshold)
            {
				inspectorManager.OnNextLayout(()=>RebuildDrawers(true), this);
			}
			else
            {
				inspectorManager.OnNextLayout(RebuildDrawerAndExitGUIIfTargetsChanged, this);
			}
		}

		[UsedImplicitly]
		private void OnLostFocus()
		{
			nowLosingFocus = true;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!HasFocus);
			#endif

			DrawGUI.OnNextBeginOnGUI(ResetNowLosingFocus, false);

			#if DEV_MODE && DEBUG_FOCUS
			Debug.Log("OnLostFocus called with SetupDone="+StringUtils.ToColorizedString(SetupDone));
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			if(SetupDone)
			{
				if(mainView == null)
				{
					Debug.LogError("OnLostFocus was called with SetupDone but mainView null.");
				}
				else if(mainView.Preferences == null)
				{
					Debug.LogError("OnLostFocus was called with SetupDone but mainView.Preferences null.");
				}
			}
			#endif

			titleContent = GetTitleContent(false);

			Repaint();

			if(ReferenceEquals(mouseoveredInstance, this))
			{
				#if DEV_MODE && DEBUG_MOUSEOVERED_INSTANCE
				if(mouseoveredInstance != this)Debug.LogError("Setting mouseoveredInstance to null because OnLostFocus called");
				#endif

				mouseoveredInstance = null;
				GUI.changed = true;
			}

			if(inspectorManager == null)
			{
				return;
			}

			var activeInspectorWas = inspectorManager.ActiveInspector;
			inspectorManager.ActiveInspector = inspectorManager.selected.Inspector;
			inspectorManager.selected.OnInspectorDrawerLostFocus(this);

			var selectedInspector = inspectorManager.SelectedInspector;

			#if DEV_MODE && DEBUG_FOCUS
			Debug.Log("OnLostFocus with selectedInspector="+StringUtils.ToString(selectedInspector)+ ", SelectedInspectorPart=" + inspectorManager.SelectedInspectorPart);
			#endif

			// handle scenario where alt+tab is used to unfocus the editor application
			// and mouseover effects get left stuck
			var mouseoveredInspector = inspectorManager.MouseoveredInspector;
			if(mouseoveredInspector != null && ReferenceEquals(mouseoveredInspector.InspectorDrawer, this))
			{
				#if DEV_MODE && (DEBUG_FOCUS || DEBUG_MOUSEOVERED_INSTANCE)
				Debug.Log("Clearing mouseovered inspector and drawer because OnLostFocus called");
				#endif
				inspectorManager.SetMouseoveredInspector(null, InspectorPart.None);
			}

			if(activeInspectorWas != null && activeInspectorWas != mainView)
			{
				inspectorManager.ActiveInspector = activeInspectorWas;
			}
		}

		private void ResetNowLosingFocus()
		{
			nowLosingFocus = false;
		}
		
		#if UNITY_2017_1_OR_NEWER
		private void OnBeforeAssemblyReload()
		{
			#if DEV_MODE && DEBUG_ON_SCRIPTS_RELOADED
			Debug.Log("!!!!!!!! OnBeforeAssemblyReload !!!!!!!!!");
			#endif

			editors.OnBeforeAssemblyReload();

			MainView.OnBeforeAssemblyReload();
			if(viewIsSplit)
            {
				SplitView.OnBeforeAssemblyReload();
			}
		}
		#endif

		protected void OnScriptsReloaded()
		{
			#if DEV_MODE && DEBUG_ON_SCRIPTS_RELOADED
			Debug.Log("!!!!!!!! OnScriptsReloaded !!!!!!!!!");
			#endif
		
			if(!InspectorManager.InstanceExists())
			{
				return;
			}

			var manager = InspectorUtility.ActiveManager;

			nowRestoringPreviousState = true;

			#if UNITY_2019_1_OR_NEWER
			var uiHandler = GetUIElementDrawHandler(0);
			if(uiHandler != null)
			{
				uiHandler.OnScriptsReloaded();
			}
			if(ViewIsSplit)
			{
				uiHandler = GetUIElementDrawHandler(1);
				if(uiHandler != null)
				{
					uiHandler.OnScriptsReloaded();
				}
			}
			#endif

			manager.OnNextLayout(OnScriptsReloadedDelayed);
		}

		private void OnScriptsReloadedDelayed()
		{
			if(!SetupDone)
			{
				Setup();
			}

			editors.CleanUp();
		}

		/// <inheritdoc/>
		public void RefreshView()
		{
			//is dirty tells OnGUI to recalculate heights for all components.
			//Once it's done, the Update method is instructed to actually Repaint the view
			isDirty = true;
			GUI.changed = true;
			Repaint();

			#if UNITY_2019_1_OR_NEWER
			rootVisualElement.MarkDirtyRepaint();
			#endif
		}

		#if UNITY_2019_1_OR_NEWER
		[UsedImplicitly]
		private void OnEnable()
		{
			var root = rootVisualElement;
			root.Clear();
			var scrollView = new ScrollView(ScrollViewMode.Vertical);
			scrollView.style.position = new StyleEnum<Position>(Position.Absolute);

			#if UNITY_2021_1_OR_NEWER
			scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
			scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
			#else
			scrollView.showHorizontal = false;
			scrollView.showVertical = false;
			#endif

			HideRecursively(scrollView.horizontalScroller);
			HideRecursively(scrollView.verticalScroller);
			scrollView.RegisterCallback<WheelEvent>(OnVisualElementMouseWheelEvent, TrickleDown.TrickleDown);

			root.Add(scrollView);
			var drawHandler = new UIElementDrawHandler();
			drawHandler.style.position = new StyleEnum<Position>(Position.Absolute);
			scrollView.contentContainer.Add(drawHandler);

			#if DEV_MODE
			root.name = GetType().Name + ".Root";
			scrollView.name = "MainView.ScrollView";
			scrollView.contentContainer.name = "ContentContainer";
			drawHandler.name = "UIElementDrawHandler";
			#endif

			scrollView = new ScrollView();
			scrollView.style.position = new StyleEnum<Position>(Position.Absolute);

			#if UNITY_2021_1_OR_NEWER
			scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
			scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
			#else
			scrollView.showHorizontal = false;
			scrollView.showVertical = false;
			#endif

			HideRecursively(scrollView.horizontalScroller);
			HideRecursively(scrollView.verticalScroller);
			scrollView.RegisterCallback<WheelEvent>(OnVisualElementMouseWheelEvent, TrickleDown.TrickleDown);

			root.Add(scrollView);
			drawHandler = new UIElementDrawHandler();
			drawHandler.style.position = new StyleEnum<Position>(Position.Absolute);
			scrollView.contentContainer.Add(drawHandler);

			#if DEV_MODE
			scrollView.name = "SplitView.ScrollView";
			scrollView.contentContainer.name = "ContentContainer";
			drawHandler.name = "UIElementDrawHandler";
			#endif

			if(!ViewIsSplit)
			{
				scrollView.visible = false;
			}
		}

		private void OnVisualElementMouseWheelEvent(WheelEvent wheelEvent)
		{
			if(wheelEvent.delta.y > 0f || wheelEvent.delta.y < 0f)
			{
				const float ScrollSensitivity = 50f;
				if(MainView.MouseoveredPart == InspectorPart.Viewport)
				{
					MainView.State.SetScrollPosY(MainView.State.ScrollPos.y + wheelEvent.delta.y * ScrollSensitivity);
				}
				else if(ViewIsSplit && SplitView.MouseoveredPart == InspectorPart.Viewport)
				{
					SplitView.State.SetScrollPosY(SplitView.State.ScrollPos.y + wheelEvent.delta.y * ScrollSensitivity);
				}
			}
		}


		private void HideRecursively(VisualElement element)
		{
			element.visible = false;
			element.style.width = 0f;
			element.style.backgroundImage = null;

			for(int n = element.childCount - 1; n >= 0; n--)
			{
				HideRecursively(element.ElementAt(n));
			}
		}

		private ScrollView GetUIScrollView(int index)
		{
			var root = rootVisualElement;
			return root.childCount == 0 ? null : root.ElementAt(index) as ScrollView;
		}

		private UIElementDrawHandler GetUIElementDrawHandler(int index)
		{
			var scrollView = GetUIScrollView(index);
			return scrollView == null ? null : scrollView.contentContainer.ElementAt(0) as UIElementDrawHandler;
		}

		public void AddElement(VisualElement element, IDrawer drawer)
		{
			#if DEV_MODE
			Debug.Log("AddElement("+drawer+")");
			#endif

			#if DEV_MODE || PI_ENABLE_UI_SUPPORT
			var drawHandler = GetUIElementDrawHandler(drawer.Inspector == mainView ? 0 : 1);
			if(drawHandler == null)
			{
				#if DEV_MODE
				Debug.LogWarning("UIElementDrawHandler("+ (drawer.Inspector == mainView ? 0 : 1) + ") null");
				#endif
				return;
			}
			
			drawHandler.Add(element, drawer);
			rootVisualElement.MarkDirtyRepaint();
			#endif
		}

		public void RemoveElement(VisualElement element, IDrawer drawer)
		{
			#if DEV_MODE
			Debug.Log("RemoveElement("+drawer+")");
			#endif

			#if DEV_MODE || PI_ENABLE_UI_SUPPORT
			var drawHandler = GetUIElementDrawHandler(0);
			if(drawHandler == null)
			{
				return;
			}
			drawHandler.Remove(element);
			if(ViewIsSplit)
			{
				drawHandler = GetUIElementDrawHandler(1);
				if(drawHandler != null)
				{
					drawHandler.Remove(element);
				}
			}
			drawHandler.MarkDirtyRepaint();
			#endif
		}
		#endif

		private void EarlySetup()
		{
			if(earlySetupDone)
			{
				return;
			}

			earlySetupDone = true;
			
			#if DEV_MODE
			var timer = new ExecutionTimeLogger();
			timer.Start(GetType().Name+".EarlySetup");
			#endif

			#if DEV_MODE
			timer.StartInterval("GetDefaultSelectionManager");
			#endif

			SelectionManager = GetDefaultSelectionManager();

			#if DEV_MODE
			timer.FinishInterval();
			timer.StartInterval("InspectorManager.Instance()");
			#endif

			inspectorManager = InspectorManager.Instance();

			#if DEV_MODE
			timer.FinishInterval();
			#endif

			#if DEV_MODE
			timer.StartInterval("GetPreferences");
			#endif

			var preferences = GetPreferences();

			#if DEV_MODE
			timer.FinishInterval();
			timer.StartInterval("InspectorPreferences.Setup");
			#endif

			preferences.Setup();

			if(titleContent.image == null)
			{
				titleContent = GetTitleContent();
			}

			#if DEV_MODE
			timer.FinishInterval();
			#endif
		}

		[UsedImplicitly]
		internal void OnGUI()
		{
			if(!SetupDone)
			{
				if(!earlySetupDone)
				{
					EarlySetup();
				}

				// This should be pretty safe to call, because it won't invoke callbacks when !SetupDone.
				InspectorUtility.BeginInspectorDrawer(this, this);

				var rect = new Rect(Screen.width * 0.5f - 50f, Screen.height * 0.5f - DrawGUI.SingleLineHeight * 0.5f, 100f, DrawGUI.SingleLineHeight);
				switch((int)EditorApplication.timeSinceStartup % 4)
				{
					case 1:
						GUI.Label(rect, "Initializing.");
						break;
					case 2:
						GUI.Label(rect, "Initializing..");
						break;
					default:
						GUI.Label(rect, "Initializing...");
						break;
				}

				bool isReady = DefaultDrawerProviders.IsReady;

				if(!isReady)
				{
					#if DEV_MODE
					if(!firstOnGUITime.HasValue)
					{
						#if DEV_MODE
						if(!DefaultDrawerProviders.IsReady)
						{
							Debug.Log("Won't call " + GetType().Name + ".Setup yet because !DefaultDrawerProviders.IsReady..."); 
						}
						else
						{
							Debug.Log("Won't call " + GetType().Name + ".Setup yet because !ApplicationUtility.IsReady()..."); 
						}
						#endif
						firstOnGUITime = EditorApplication.timeSinceStartup;
					}
					#endif
					
					Repaint();
					return;
				}
				#if DEV_MODE
				else if(firstOnGUITime.HasValue)
				{
					Debug.Log("Time between first OnGUI call and when DefaultDrawerProviders IsReady: " + (EditorApplication.timeSinceStartup - firstOnGUITime.Value));
				}
				#endif

				if(Event.current.type != EventType.Layout)
				{
					inspectorManager.OnNextLayout(Setup, this);
					return;
				}
				
				Setup();
				GUI.changed = true;
				return;
			}

			Manager.ActiveInspector = MainView;
			
			var time = (float)EditorApplication.timeSinceStartup;
			onGUIDeltaTime = time - lastOnGUITime;
			lastOnGUITime = time;

			var e = Event.current;
			var type = e.type;
			switch(type)
			{
				case EventType.MouseMove:
					if(!HasFocus && MouseIsOver)
					{
						GUI.changed = true;
					}
					break;
				case EventType.DragUpdated:
					if(!HasFocus && MouseIsOver)
					{
						GUI.changed = true;
					}
					
					// This is important because MouseEnterWindow doesn't get called during UnityObject dragging!
					if(DrawGUI.IsUnityObjectDrag)
					{
						if(mouseOverWindow == this)
						{
							#if DEV_MODE && DEBUG_MOUSEOVERED_INSTANCE
							if(mouseoveredInstance != this)Debug.LogError("Custom MouseEnterWindow during UnityObjectDrag");
							#endif

							mouseoveredInstance = this;
							GUI.changed = true;
						}
						else if(mouseoveredInstance == this)
						{
							#if DEV_MODE && DEBUG_MOUSEOVERED_INSTANCE
							Debug.LogError("Custom MouseLeaveWindow during UnityObjectDrag");
							#endif

							mouseoveredInstance = null;
							GUI.changed = true;
						}
					}
					break;
				case EventType.MouseEnterWindow:
					#if DEV_MODE && DEBUG_MOUSEOVERED_INSTANCE
					Debug.Log("MouseEnterWindow with DrawGUI.IsUnityObjectDrag="+ DrawGUI.IsUnityObjectDrag+ ", mouseOverWindow="+StringUtils.ToString(mouseOverWindow));
					#endif
					if(mouseOverWindow == this)
					{
						mouseoveredInstance = this;
					}
					else if(mouseoveredInstance == this)
					{
						mouseoveredInstance = null;
					}
					GUI.changed = true;
					break;
				case EventType.MouseLeaveWindow:
					#if DEV_MODE && DEBUG_MOUSEOVERED_INSTANCE
					Debug.Log("MouseLeaveWindow with DrawGUI.IsUnityObjectDrag="+ DrawGUI.IsUnityObjectDrag+ ", mouseOverWindow="+StringUtils.ToString(mouseOverWindow));
					#endif

					// Entering or leaving a window while a mouse button is pressed does not trigger either event, as pressing the mouse button activates drag mode.
					// Instead the MouseLeaveWindow event gets called right after a drag event starts. We want to ignore this event.
					if(DrawGUI.IsUnityObjectDrag)
					{
						#if DEV_MODE
						if(mouseoveredInstance  != null || Manager.MouseoveredInspector != null)
						{
							Debug.LogWarning("MouseLeaveWindow was called but IsUnityObjectDrag was true, so won't clear mouseovered inspector.");
						}
						#endif

						#if DEV_MODE && DEBUG_ABORT_ONGUI
						Debug.LogWarning("Aborting OnGUI for event "+StringUtils.ToString(Event.current)+"...");
						#endif
						return;
					}

					if(mouseoveredInstance == this)
					{
						mouseoveredInstance = null;
					}
					
					var mouseoveredInspector = Manager.MouseoveredInspector;
					if(mouseoveredInspector != null && ReferenceEquals(this, mouseoveredInspector.InspectorDrawer))
					{
						Manager.SetMouseoveredInspector(null, InspectorPart.None);
					}
					GUI.changed = true;

					break;
				case EventType.Repaint:
					#if UNITY_2019_1_OR_NEWER
					var scrollView = GetUIScrollView(0);
					if(scrollView != null)
					{
						float width = position.width;
						if(mainView.State.HasScrollBar)
						{
							width -= DrawGUI.ScrollBarWidth;
						}
						scrollView.style.width = width;

						float height = mainView.State.WindowRect.height - mainView.ToolbarHeight - mainView.PreviewAreaHeight;
						scrollView.style.height = height;

						var setPosition = scrollView.transform.position;
						setPosition.x = 0f;
						setPosition.y = mainView.ToolbarHeight;
						scrollView.transform.position = setPosition;

						scrollView.contentContainer.style.width = width;
						scrollView.contentContainer.style.height = mainView.State.contentRect.height;

						var scrollOffset = scrollView.scrollOffset;
						scrollOffset.y = mainView.State.ScrollPos.y;
						scrollView.scrollOffset = scrollOffset;

						var uiHandler = scrollView.contentContainer.ElementAt(0) as UIElementDrawHandler;
						if(uiHandler != null)
						{
							scrollView.visible = uiHandler.ElementCount > 0;

							uiHandler.style.width = width;
							uiHandler.style.height = height;
							uiHandler.Update();
						}
						#if DEV_MODE
						else { Debug.Log("uiHandler(0) null"); }
						#endif
					}
					if(ViewIsSplit)
					{
						scrollView = GetUIScrollView(1);
						if(scrollView != null)
						{
							float width = position.width;
							if(splitView.State.HasScrollBar)
							{
								width -= DrawGUI.ScrollBarWidth;
							}
							scrollView.style.width = width;

							float height = splitView.State.WindowRect.height - splitView.ToolbarHeight - splitView.PreviewAreaHeight;
							scrollView.style.height = height;

							var setPosition = scrollView.transform.position;
							setPosition.x = 0f;
							setPosition.y = splitView.State.WindowRect.height + splitView.ToolbarHeight;
							scrollView.transform.position = setPosition;

							scrollView.contentContainer.style.width = width;
							scrollView.contentContainer.style.height = splitView.State.contentRect.height;

							var scrollOffset = scrollView.scrollOffset;
							scrollOffset.y = splitView.State.ScrollPos.y;
							scrollView.scrollOffset = scrollOffset;

							var uiHandler = scrollView.contentContainer.ElementAt(0) as UIElementDrawHandler;
							if(uiHandler != null)
							{
								scrollView.visible = uiHandler.ElementCount > 0;

								uiHandler.style.width = width;
								uiHandler.style.height = height;
								uiHandler.Update();
							}
							#if DEV_MODE
							else { Debug.Log("uiHandler(0) null"); }
							#endif
						}
					}
					#endif

					if(isDirty)
					{
						Repaint();
					}

					if(swapSplitSidesOnNextRepaint)
					{
						swapSplitSidesOnNextRepaint = false;
						if(splitView != null)
						{
							var temp = mainView;
							mainView = splitView;
							splitView = temp;
						}
					}
					break;
				case EventType.Layout:
					minimizer.OnLayout();
					break;
			}
			
			Profiler.BeginSample("PowerInspectorWindow.OnGUI");
			
			DrawGUI.BeginOnGUI(mainView.Preferences, true);

			InspectorUtility.BeginInspectorDrawer(this, this);

			// if window is minimized don't waste resources drawing things, when nothing is visible.
			if(minimizer.Minimized)
			{
				return;
			}

			//trying to fix a bug where the default inspector layout gets affected by things I do in there
			//by making sure all values that could affect it are restored back to normal
			//var indentLevelWas = EditorGUI.indentLevel;
			var labelWidthWas = EditorGUIUtility.labelWidth;
			var matrixWas = GUI.matrix;
			
			var windowRect = position;
			windowRect.x = 0f;
			windowRect.y = 0f;
			
			#if DEV_MODE && PI_ASSERTATIONS
			if(windowRect.width <= 0f) { Debug.LogError(GetType().Name+ " windowRect.width <= 0f: " + windowRect); }
			#endif

			bool mouseIsOverWindow = !Manager.IgnoreAllMouseInputs && MouseIsOver;

			if(mouseIsOverWindow)
			{
				Cursor.CanRequestLocalPosition = windowRect.Contains(Cursor.LocalPosition);
			}
			else
			{
				Cursor.CanRequestLocalPosition = !windowRect.Contains(Cursor.LocalPosition);
			}
			
			EditorGUI.BeginChangeCheck();
			{
				if(viewIsSplit)
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(CanSplitView);
					#endif

					#if SAFE_MODE || DEV_MODE
					if(splitView == null)
					{
						#if DEV_MODE
						Debug.LogError("splitView was null but viewIsSplit was true");
						#endif
						SetSplitView(true);
					}
					#endif

					var splitPos = windowRect;
					splitPos.height = Mathf.RoundToInt(windowRect.height * 0.5f);
					
					bool anyInspectorPartMouseovered;
					if(mouseIsOverWindow)
					{
						if(Cursor.CanRequestLocalPosition)
						{
							#if DEV_MODE && DEBUG_ANY_INSPECTOR_PART_MOUSEOVERED
							if((mainView.MouseoveredPart != InspectorPart.None) != splitPos.Contains(Cursor.LocalPosition)) { Debug.Log("mainView.AnyPartMouseovered = "+StringUtils.ToColorizedString(mainView.MouseoveredPart == InspectorPart.None)+" (cursor inside bounds check)");}
							#endif
							anyInspectorPartMouseovered = splitPos.Contains(Cursor.LocalPosition);
						}
						else
						{
							#if DEV_MODE && DEBUG_ANY_INSPECTOR_PART_MOUSEOVERED
							if((mainView.MouseoveredPart != InspectorPart.None) != (mainView.MouseoveredPart != InspectorPart.None)) { Debug.Log("mainView.AnyPartMouseovered = "+StringUtils.ToColorizedString(mainView.MouseoveredPart == InspectorPart.None)+" (could not request mouse pos)");}
							#endif
							anyInspectorPartMouseovered = mainView.MouseoveredPart != InspectorPart.None;
						}
					}
					else
					{
						#if DEV_MODE && DEBUG_ANY_INSPECTOR_PART_MOUSEOVERED
						if((mainView.MouseoveredPart != InspectorPart.None) != splitPos.Contains(Cursor.LocalPosition)) { Debug.Log("mainView.AnyPartMouseovered = "+StringUtils.ToColorizedString(mainView.MouseoveredPart == InspectorPart.None)+" (mouseIsOverWindow was false)");}
						#endif
						anyInspectorPartMouseovered = false;
					}
					
					mainView.OnGUI(splitPos, anyInspectorPartMouseovered);
					
					if(!anyInspectorPartMouseovered && (type == EventType.MouseDown || type == EventType.ContextClick) && splitPos.Contains(Event.current.mousePosition))
					{
						#if DEV_MODE
						Debug.LogWarning("Consumed click event via GUI.Button: "+StringUtils.ToString(Event.current));
						#endif
						DrawGUI.Use(Event.current);
					}
					
					splitPos.y += splitPos.height;

					if(anyInspectorPartMouseovered || !mouseIsOverWindow)
					{
						#if DEV_MODE && DEBUG_ANY_INSPECTOR_PART_MOUSEOVERED
						if(splitView.MouseoveredPart != InspectorPart.None) { Debug.Log("anyInspectorPartMouseovered = "+StringUtils.False+" (splitView.AnyPartMouseovered = "+StringUtils.ToColorizedString(splitView.MouseoveredPart == InspectorPart.None)+",  mouseIsOverWindow="+StringUtils.ToColorizedString(mouseIsOverWindow)+")");}
						#endif
						anyInspectorPartMouseovered = false;
					}
					else
					{
						if(Cursor.CanRequestLocalPosition)
						{
							#if DEV_MODE && DEBUG_ANY_INSPECTOR_PART_MOUSEOVERED
							if((splitView.MouseoveredPart != InspectorPart.None) != splitPos.Contains(Cursor.LocalPosition)) { Debug.Log("splitView.AnyPartMouseovered = "+StringUtils.ToColorizedString(splitView.MouseoveredPart == InspectorPart.None)+" (splitPos "+splitPos+" Contains cursorPos "+Cursor.LocalPosition+" test) with Event="+StringUtils.ToString(Event.current));}
							#endif
							anyInspectorPartMouseovered = splitPos.Contains(Cursor.LocalPosition);
						}
						else
						{
							anyInspectorPartMouseovered = splitView.MouseoveredPart != InspectorPart.None;
						}
					}

					var linePos = splitPos;
					linePos.y -= 1f;
					linePos.height = 1f;
					var lineColor = mainView.Preferences.theme.SplitViewDivider;
					DrawGUI.DrawLine(linePos, lineColor);
					linePos.y -= 1f;
					lineColor.a *= 0.5f;
					DrawGUI.DrawLine(linePos, lineColor);

					DrawGUI.BeginArea(splitPos);
					{
						splitPos.y = 0f;

						//it is possible for splitTop.OnGUI to change the splitView state
						if(viewIsSplit)
						{
							try
							{
								splitView.OnGUI(splitPos, anyInspectorPartMouseovered);
							}
							catch(Exception exception)
							{
								if(ExitGUIUtility.ShouldRethrowException(exception))
								{
									OnExitingGUI(labelWidthWas, matrixWas);
									throw;
								}
								#if DEV_MODE
								Debug.LogWarning(ToString()+" "+exception);
								#endif
							}

							if(!anyInspectorPartMouseovered && GUI.Button(splitPos, GUIContent.none, InspectorPreferences.Styles.Blank))
							{
								#if DEV_MODE
								Debug.LogWarning("Consumed click event via GUI.Button: "+StringUtils.ToString(Event.current));
								#endif
							}
						}
					}
					DrawGUI.EndArea();
				}
				else
				{
					if(splitView != null)
					{
						inspectorManager.Dispose(ref splitView);
					}

					bool anyInspectorPartMouseovered;
					if(mouseIsOverWindow)
					{
						if(Cursor.CanRequestLocalPosition)
						{
							anyInspectorPartMouseovered = windowRect.Contains(Cursor.LocalPosition);
						}
						else
						{
							anyInspectorPartMouseovered = mainView.MouseoveredPart != InspectorPart.None;
						}
					}
					else
					{
						anyInspectorPartMouseovered = false;
					}
					
					mainView.OnGUI(windowRect, anyInspectorPartMouseovered);

					if(!anyInspectorPartMouseovered && GUI.Button(windowRect, GUIContent.none, InspectorPreferences.Styles.Blank))
					{
						#if DEV_MODE
						Debug.LogWarning("Consumed click event via GUI.Button: "+StringUtils.ToString(Event.current));
						#endif
					}
				}
			
				//trying to fix a bug where the default inspector layout gets affected by things I do in there
				//by making sure all values that could affect it are restored back to normal
				EditorGUI.indentLevel = 0; //indentLevelWas;
				EditorGUIUtility.labelWidth = labelWidthWas;
				GUI.skin = null;
				GUI.matrix = matrixWas;

				if(EditorGUI.EndChangeCheck())
				{
					RefreshView();
				}
				else if(isDirty && e.type == EventType.Layout) //doing this only on Layout helps with default component editor unfolded update bug
				{
					isDirty = false;
					Repaint();
				}
				else if(GUI.changed)
				{
					Repaint();
				}
			}

			Cursor.CanRequestLocalPosition = true;

			Profiler.EndSample();
		}

		private void OnExitingGUI(float labelWidthWas, Matrix4x4 matrixWas)
		{
			EditorGUI.indentLevel = 0;
			EditorGUIUtility.labelWidth = labelWidthWas;
			GUI.skin = null;
			GUI.matrix = matrixWas;
		}

		/// <inheritdoc/>
		public void OpenSplitView()
		{
			SetSplitView(true);
		}

		/// <inheritdoc/>
		public void CloseSplitView()
		{
			SetSplitView(false);
		}

		/// <inheritdoc/>
		public void SetSplitView(bool enable)
		{
			#if DEV_MODE && DEBUG_SPLIT_VIEW
			Debug.Log(StringUtils.ToColorizedString("SetSplitView(", enable, ") with viewIsSplit=", viewIsSplit, ", splitView=", StringUtils.ToString(splitView), ", Event=", StringUtils.ToString(Event.current)), this);
			#endif
		
			#if SAFE_MODE || DEV_MODE
			if(!CanSplitView)
			{
				#if DEV_MODE
				Debug.LogError("SetSplitView("+StringUtils.ToColorizedString(enable)+") called for "+GetType().Name+" even though CanSplitView="+StringUtils.False);
				#endif
				return;
			}
			#endif

			// temporary fix for issue where peeking would not work
			// with viewIsSplit being true even though there is no
			// visible Split View to be seen
			if(viewIsSplit)
			{
				if(splitView == null)
				{
					#if DEV_MODE
					Debug.LogError("viewIsSplit was "+StringUtils.True+" but splitView was "+StringUtils.Null);
					#endif
					viewIsSplit = false;
				}
				else if(!splitView.SetupDone)
				{
					#if DEV_MODE
					Debug.LogError("viewIsSplit was "+StringUtils.True+" but splitView.SetupDone was "+StringUtils.False);
					#endif
					SetSplitView(false);
				}
			}

			if(enable != viewIsSplit)
			{
				viewIsSplit = enable;
				
				if(enable)
				{
					if(splitView == null)
					{
						splitView = CreateInspector(mainView.State.inspected, true, mainView.State.ScrollPos);
					}
				}
				else
				{
					var selectedPartWas = Manager.SelectedInspectorPart;
					var selectedToolbarItem = splitView.Toolbar.SelectedItem;
					
					if(splitView != null)
					{
						inspectorManager.ActiveInspector = splitView;
						inspectorManager.Dispose(ref splitView);
					}
					inspectorManager.ActiveInspector = mainView;

					inspectorManager.Select(mainView, selectedPartWas, ReasonSelectionChanged.Initialization);
					if(selectedToolbarItem != null)
					{
						var selectItem = mainView.Toolbar.GetItemByType(selectedToolbarItem.GetType());
						if(selectItem != null && selectItem.Selectable)
						{
							mainView.Toolbar.SetSelectedItem(selectItem, ReasonSelectionChanged.Initialization);
						}
					}
				}

				#if UNITY_2019_1_OR_NEWER
				var scrollView = GetUIScrollView(1);
				if(scrollView != null)
				{
					scrollView.visible = viewIsSplit;
				}
				if(viewIsSplit)
				{
					var uiHandler = GetUIElementDrawHandler(1);
					if(uiHandler != null)
					{
						uiHandler.Update();
					}
				}
				rootVisualElement.MarkDirtyRepaint();
				#endif

				RefreshView();
			}
			#if DEV_MODE && PI_ASSERTATIONS
			else { Debug.LogWarning("InspectorDrawerWindow.SetSplitView(" + StringUtils.ToColorizedString(enable) + ") called, but viewIsSplit was already "+StringUtils.ToColorizedString((bool)viewIsSplit)); }
			#endif
		}

		/// <inheritdoc/>
		public void ShowInSplitView(params Object[] inspect)
		{
			#if SAFE_MODE || DEV_MODE
			if(!CanSplitView)
			{
				#if DEV_MODE
				Debug.LogError("ShowInSplitView("+StringUtils.ToString(inspect)+") called for "+GetType().Name+" even though CanSplitView="+StringUtils.False);
				#endif
				return;
			}
			#endif

			if(splitView != null)
			{
				splitView.State.ViewIsLocked = true;
				splitView.RebuildDrawers(inspect, false);
			}
			else
			{
				splitView = CreateInspector(inspect, true, Vector2.zero);
			}
			SetSplitView(true);
		}

		public void SwapSplitViewSides()
		{
			swapSplitSidesOnNextRepaint = true;
			Repaint();
		}

		/// <inheritdoc/>
		public void CloseMainView()
		{
			if(!viewIsSplit)
			{
				#if DEV_MODE
				Debug.LogError("CloseMainView was called but SplitView was false!");
				#endif
				return;
			}

			var temp = mainView;
			mainView = splitView;
			splitView = temp;
			SetSplitView(false);

			// auto-unlock view if it is locked and selected targets match inspected targets.
			// This is to get rid of perceived issue of view becoming locked for no good apparent reason
			// if user clicks split view and then closes the main view.
			if(mainView.State.inspected.ContentsMatch(mainView.SelectedObjects))
			{
				mainView.State.ViewIsLocked = false;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!ViewIsSplit);
			#endif
		}
		
		/// <inheritdoc />
		public void CloseTab()
		{
			if(!ViewIsSplit)
			{
				nowClosing = true;
				Close();
				return;
			}

			if(MainView.Selected || Manager.LastSelectedActiveOrDefaultInspector() == MainView)
			{
				CloseMainView();
			}
			else
			{
				SetSplitView(false);
			}
		}

		public void ShowInSplitView(Object target, bool throwExitGUIException)
		{
			#if DEV_MODE
			Debug.Log("ShowInSplitView("+StringUtils.ToString(target)+")");
			#endif

			if(target != null)
			{
				var component = target as Component;
				if(component != null)
				{
					#if DEV_MODE
					Debug.Log("ShowInSplitView("+StringUtils.TypeToString(target)+") - was a Component");
					#endif

					var gameObject = component.gameObject;
					ShowInSplitView(ArrayPool<Object>.CreateWithContent(gameObject));

					// Wait one frame so that there's been time to cache all layout data during the next OnGUI call,
					// so that ScrollToShow can scroll to the correct position
					splitView.OnNextLayout(()=>
					{
						if(splitView == null)
						{
							return;
						}
						var show = splitView.State.drawers.FindDrawer(component);
						if(show == null)
						{
							return;
						}
						inspectorManager.Select(splitView, InspectorPart.Viewport, show, ReasonSelectionChanged.Peek);
						splitView.ScrollToShow(show);
						show.SetUnfolded(true, false, false);
						ExitGUIUtility.ExitGUI();
					});
				}
				else //GameObjects and Assets are okay to be shown as standalone
				{
					#if DEV_MODE
					Debug.Log("ShowInSplitView("+StringUtils.TypeToString(target)+") was not a Component");
					#endif

					ShowInSplitView(ArrayPool<Object>.CreateWithContent(target));
				}

				if(throwExitGUIException)
				{
					ExitGUIUtility.ExitGUI();
				}
			}
		}

		public IInspector SelectedOrDefaultView()
		{
			if(!ViewIsSplit)
			{
				return mainView;
			}

			var selected = inspectorManager.SelectedInspector;
			if(mainView == selected)
			{
				return mainView;
			}

			if(splitView == selected)
			{
				return splitView;
			}

			if(splitView == inspectorManager.ActiveInspector)
			{
				return splitView;
			}

			return mainView;
		}

		public void OnKeyDown(Event e)
		{
			#if DEV_MODE && DEBUG_KEYBOARD_INPUT
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".OnKeyDown(", e.keyCode, ") with HasFocus=", HasFocus, ", selectedControl=", StringUtils.ToString(SelectedOrDefaultView().FocusedDrawer), ", SelectedPart=", Manager.SelectedInspectorPart));
			#endif
			
			// First prioritize mouseovered inspector and then selected inspector.
			var inspector = Manager.MouseoveredInspector;
			if(inspector != null)
			{
				if(inspector.InspectorDrawer as Object != this)
				{
					return;
				}
			}
			else if(!HasFocus)
			{
				return;
			}
			else
			{
				inspector = SelectedOrDefaultView();
			}
			
			var keys = inspector.Preferences.keyConfigs;

			if(keys.stepBackInSelectionHistory.DetectAndUseInput(e))
			{
				inspector.StepBackInSelectionHistory();
				return;
			}

			if(keys.stepForwardInSelectionHistory.DetectAndUseInput(e))
			{
				inspector.StepForwardInSelectionHistory();
				return;
			}
			
			if(keys.closeSelectedView.DetectAndUseInput(e))
			{
				#if DEV_MODE
				Debug.Log("Closing because of input: "+StringUtils.ToString(e));
				#endif
				CloseTab();
				ExitGUIUtility.ExitGUI();
				return;
			}

			Repaint();

			DrawGUI.RegisterInputEvent(e);

			inspectorManager.ActiveInspector = inspector;

			// give controls time to react to selection changes, editing text field changes etc.
			// before cached values are updated. (e.g. unapplied changes in delayed fields are
			// not discarded before they have time to get applied
			inspector.ResetNextUpdateCachedValues();
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!HasFocus || inspector.Selected || inspector.MouseoveredPart != InspectorPart.None);
			#endif

			#if DEV_MODE && DEBUG_KEYBOARD_INPUT
			Debug.Log(StringUtils.ToColorizedString("OnKeyDown inspector.Selected=", inspector.Selected, ", inspector.FocusedDrawer=", inspector.FocusedDrawer+ ", Manager.SelectedInspector=", Manager.SelectedInspector, ", HasFocus=", HasFocus));
			#endif

			if(inspector.Toolbar.OnKeyboardInputGivenWhenNotSelected(e, inspector.Preferences.keyConfigs))
			{
				if(e.type != EventType.Used)
				{
					DrawGUI.Use(e);
					ExitGUIUtility.ExitGUI();
				}
			}

			IDrawer selectedControl = null;
			if(inspector.Selected)
			{
				selectedControl = inspector.FocusedDrawer;
				if(selectedControl != null)
				{
					var onKeyboardInputBeingGiven = selectedControl.OnKeyboardInputBeingGiven;
					if(onKeyboardInputBeingGiven != null)
					{
						#if DEV_MODE && DEBUG_KEYBOARD_INPUT
						Debug.Log("onKeyboardInputBeingGiven(" + StringUtils.ToString(e) + "): " +StringUtils.ToString(onKeyboardInputBeingGiven));
						#endif
						if(onKeyboardInputBeingGiven(selectedControl, e, selectedControl.Inspector.Preferences.keyConfigs))
						{
							return;
						}
					}

					if(selectedControl.OnKeyboardInputGiven(e, selectedControl.Inspector.Preferences.keyConfigs))
					{
						return;
					}
				}
				else if(inspectorManager.SelectedInspectorPart == InspectorPart.Toolbar)
				{
					inspector.Toolbar.OnKeyboardInputGiven(e, inspector.Preferences.keyConfigs);
				}
				else if(inspectorManager.SelectedInspectorPart == InspectorPart.Viewport || inspectorManager.SelectedInspectorPart == InspectorPart.None)
				{
					bool fieldChangeInputGiven;
					if(keys.DetectNextField(e, true) || keys.DetectPreviousField(e, true)
					|| keys.nextFieldLeft.DetectAndUseInput(e) || keys.nextFieldRight.DetectAndUseInput(e)
					|| keys.nextFieldDown.DetectAndUseInput(e) || keys.nextFieldUp.DetectAndUseInput(e))
					{
						fieldChangeInputGiven = true;
					}
					else if(e.modifiers == EventModifiers.FunctionKey)
					{
						switch(e.keyCode)
						{
							case KeyCode.DownArrow:
							case KeyCode.UpArrow:
							case KeyCode.LeftArrow:
							case KeyCode.RightArrow:
								fieldChangeInputGiven = true;
								break;
							default:
								fieldChangeInputGiven = false;
								break;
						}
					}
					else
					{
						fieldChangeInputGiven = false;
					}

					if(fieldChangeInputGiven)
					{
						var drawers = inspector.State.drawers;
						if(drawers.Length == 0)
						{
							if(inspector.Toolbar != null)
							{
								inspector.Toolbar.OnFindCommandGiven();
							}
							else
							{
								KeyboardControlUtility.SetKeyboardControl(0, 3);
							}
						}
						else
						{
							var first = drawers[0];
							var select = first.GetNextSelectableDrawerRight(true, null);

							#if DEV_MODE && DEBUG_NEXT_FIELD
							Debug.Log(first + ".GetNextSelectableDrawerRight: "+StringUtils.ToString(select));
							#endif

							if(select != null)
							{
								inspector.Select(select, ReasonSelectionChanged.SelectNextControl);
							}
						}
					}
				}
			}

			if(DrawGUI.EditingTextField && keys.DetectTextFieldReservedInput(e, TextFieldType.TextRow))
			{
				#if DEV_MODE && DEBUG_KEYBOARD_INPUT
				Debug.Log(StringUtils.ToColorizedString("OnKeyboardInputGiven( ", StringUtils.ToString(e), ") DetectTextFieldReservedInput: ", true, " with selectedControl=", selectedControl));
				#endif
				return;
			}

			if(keys.addComponent.DetectInput(e))
			{
				#if DEV_MODE
				Debug.Log("AddComponent shortcut detected");
				#endif

				if(AddComponentButtonDrawer.OpenSelectedOrFirstFoundInstance(inspector))
				{
					DrawGUI.Use(e);
				}
			}

			if(keys.toggleSplitView.DetectInput(e) && CanSplitView)
			{
				DrawGUI.Use(e);
				SetSplitView(!ViewIsSplit);
				ExitGUIUtility.ExitGUI();
			}

			if(keys.refresh.DetectAndUseInput(e))
			{
				var selectedInspector = inspectorManager.SelectedInspector;
				if(selectedInspector != null && selectedInspector.InspectorDrawer as Object == this)
				{
					selectedInspector.ForceRebuildDrawers();
					ExitGUIUtility.ExitGUI();
				}
				else
				{
					mainView.ForceRebuildDrawers();
					if(ViewIsSplit)
					{
						splitView.ForceRebuildDrawers();
						ExitGUIUtility.ExitGUI();
					}
				}
			}

			var keyCode = e.keyCode;
			switch(keyCode)
			{
				case KeyCode.Menu:
					if(selectedControl != null)
					{
						selectedControl.OpenContextMenu(e, selectedControl.RightClickArea, false, selectedControl.SelectedPart);
					}
					break;
				case KeyCode.Space:
					inspectorManager.RegisterKeyHeldDown(keyCode, "space");
					break;
				case KeyCode.F2:
					Repaint();
					if(!DrawGUI.EditingTextField)
					{
						DrawGUI.EditingTextField = true;
					}
					break;
				case KeyCode.Escape:

					#if DEV_MODE
					Debug.Log("!!! ESCAPE !!!");
					#endif

					//when dragging a control, allow aborting using the escape key
					if(inspectorManager.MouseDownInfo.MouseDownOverDrawer != null)
					{
						inspectorManager.MouseDownInfo.Clear();
					}

					if(DrawGUI.EditingTextField)
					{
						DrawGUI.Use(e);
						DrawGUI.EditingTextField = false;
					}
					break;
				case KeyCode.AltGr:
				case KeyCode.RightAlt:
					KeyConfig.OnAltGrDown();
					break;
				#if DEV_MODE
				case KeyCode.I:
					if(e.control && e.alt)
					{
						Debug.Log("INFO: FocusedDrawer="+StringUtils.ToString(SelectedOrDefaultView().FocusedDrawer)+ ", EditingTextField=" + DrawGUI.EditingTextField);
					}
				break;
				#endif
			}
		}

		public void OnKeyUp(Event e)
		{
			Manager.OnKeyUp(e);
		}

		public void OnValidateCommand(Event e)
		{
			if(DrawGUI.ExecutingCustomMenuCommand || !HasFocus)
			{
				#if DEV_MODE
				Debug.Log("Ignoring ValidateCommand with name: " + e.commandName);
				#endif
				return;
			}

			#if DEV_MODE
			Debug.Log("Detected ValidateCommand with name: " + e.commandName);
			#endif

			var selectedInspector = Manager.SelectedInspector;
			if(selectedInspector != null && selectedInspector.InspectorDrawer as Object == this && HasFocus)
			{
				selectedInspector.OnValidateCommand(e);
			}
		}

		public void OnExecuteCommand(Event e)
		{
			if(DrawGUI.ExecutingCustomMenuCommand || !HasFocus)
			{
				#if DEV_MODE && DEBUG_IGNORED_COMMANDS
				Debug.LogWarning(StringUtils.ToColorizedString("Ignoring ExecuteCommand with name \"", e.commandName + "\"\nHasFocus=", HasFocus, ", ExecutingCustomMenuCommand=", DrawGUI.ExecutingCustomMenuCommand, ", FocusedDrawer=" + Manager.FocusedDrawer));
				#endif
				return;
			}

			#if DEV_MODE && DEBUG_EXECUTE_COMMAND
			if(!string.Equals(e.commandName, "NewKeyboardFocus", System.StringComparison.Ordinal))
			{
				Debug.Log("Detected ExecuteCommand with name: " + e.commandName+ "\nFocusedControl=" + StringUtils.ToString(inspectorDrawer.Manager.FocusedControl)+", keyCode="+e.keyCode);
			}
			#endif
			
			#if DEV_MODE && DEBUG_NEW_KEYBOARD_FOCUS
			if(string.Equals(e.commandName, "NewKeyboardFocus", System.StringComparison.Ordinal))
			{
				var m = inspectorDrawer.Manager;
				Debug.Log(StringUtils.ToColorizedString("NewKeyboardFocus part=", m.SelectedInspectorPart, ", control=", m.FocusedControl, ", KeyboardControl=", KeyboardControlUtility.KeyboardControl));
			}
			#endif

			var selectedView = SelectedOrDefaultView();
			if(selectedView != null && HasFocus)
			{
				selectedView.OnExecuteCommand(e);
			}
		}

		/// <inheritdoc />
		public void Message(GUIContent message, Object context = null, MessageType messageType = MessageType.Info, bool alsoLogToConsole = true)
		{
			if(messageDispenser != null)
			{
				messageDispenser.Message(message, context, messageType, alsoLogToConsole);
			}
			#if DEV_MODE
			else
			{
				Debug.Log("Won't show message because messageDispenser null: \""+message.text+"\". This is normal if all messaging has been disabled in the preferences.");
				#if PI_ASSERTATIONS
				Debug.Assert(GetPreferences().messageDisplayMethod == MessageDisplayMethod.None);
				#endif
			}
			#endif
		}

		protected void UpdateWindowIcon()
		{
			titleContent = GetTitleContent();
		}

		
		public void OnBeforeSerialize()
		{
			#if !UNITY_2019_3_OR_NEWER
			splitViewSerialized = ViewIsSplit ? JsonUtility.ToJson(splitView) : "";
			#endif
		}

		public void OnAfterDeserialize()
		{
			#if !UNITY_2019_3_OR_NEWER
			if(ViewIsSplit && !string.IsNullOrEmpty(splitViewSerialized))
			{
				splitView = JsonUtility.FromJson<TInspector>(splitViewSerialized);
			}
			else
			{
				splitView = null;
			}
			#endif
		}
    }
}