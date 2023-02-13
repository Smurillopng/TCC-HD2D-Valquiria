//#define DEBUG_IGNORE_ALL_MOUSE_INPUTS
//#define DEBUG_ON_NEXT_ONGUI
//#define DEBUG_ON_NEXT_LAYOUT
//#define DEBUG_ON_NEXT_LAYOUT_DETAILED
//#define DEBUG_SET_ACTIVE_INSPECTOR
//#define DEBUG_SET_MOUSEOVERED_INSPECTOR
//#define DEBUG_DRAG
//#define DEBUG_DRAG_N_DROP_REFERENCES
//#define DEBUG_CLEAR

//#define DEBUG_ENSURE_ON_GUI_CALLBACKS
//#define DEBUG_COLLECT_EXISTING_INSPECTORS

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	public delegate void OnActiveInspectorChangedCallback(IInspector wasActive, IInspector isActive);

	/// <summary>
	/// A shared manager for all Inspectaculars instances.
	/// Handles the creation and pooling of new instances as well as holding shared state data.
	/// </summary>
	public class InspectorManager : Singleton<InspectorManager>, IInspectorManager, IDisposable
	{
		private IInspector activeInspector;

		private readonly Mouseovered mouseovered = new Mouseovered();
		public readonly Selected selected = new Selected();
		
		private readonly MouseDownInfo mouseDownInfo = new MouseDownInfo();
		private readonly RightClickInfo rightClickInfo = new RightClickInfo();

		private readonly List<IInspector> activeInstances = new List<IInspector>(1);
		private readonly Dictionary<Type, List<IInspector>> activeInstancesByType = new Dictionary<Type, List<IInspector>>(1);
		private readonly PolymorphicPool<IInspector> pool = new PolymorphicPool<IInspector>(1,4);

		private bool ignoreAllMouseInputs;

		private Action onNextLayout;
		
		private Queue<IDrawerDelayableAction> onNextLayoutDelayed = new Queue<IDrawerDelayableAction>(3);
		private Queue<IDrawerDelayableAction> onNextLayoutDelayedSwapTarget = new Queue<IDrawerDelayableAction>(3);

		private const float KeyHoldInitiateThreshold = 1f;
		private const float KeyHoldSendEventInterval = 0.25f;
		private KeyCode keyHeldDown;
		private string keyHeldDownName;
		private float sendNextHoldEventAt;

		public OnActiveInspectorChangedCallback OnActiveInspectorChanged { get; set; }
		public Action<IInspector> OnNewInspectorRegistered { get; set; }

		private readonly List<string> instanceUniqueNames = new List<string>();

		/// <inheritdoc/>
		public SelectionChange OnSelectionChanged
		{
			get
			{
				return selected.OnSelectionChanged;
			}

			set
			{
				selected.OnSelectionChanged = value;
			}
		}

		/// <inheritdoc/>
		public bool IgnoreAllMouseInputs
		{
			get
			{
				return ignoreAllMouseInputs || ObjectPicker.IsOpen;
			}

			set
			{
				#if DEV_MODE && DEBUG_IGNORE_ALL_MOUSE_INPUTS
				if(ignoreAllMouseInputs != value) { Debug.Log("IgnoreAllMouseInputs = "+StringUtils.ToColorizedString(value)); }
				#endif

				ignoreAllMouseInputs = value;
			}
		}

		/// <inheritdoc/>
		[CanBeNull]
		public IInspector ActiveInspector
		{
			get
			{
				return activeInspector;
			}

			set
			{
				#if DEV_MODE && DEBUG_SET_ACTIVE_INSPECTOR
				if(activeInspector != value) Debug.Log("activeInspector = "+(value == null ? "null" : value.ToString()));
				#endif
				
				if(activeInspector != value)
				{
					var wasActive = activeInspector;

					activeInspector = value;

					if(OnActiveInspectorChanged != null)
					{
						OnActiveInspectorChanged(wasActive, value);
					}
				}
			}
		}

		/// <inheritdoc/>
		[NotNull]
		public List<IInspector> ActiveInstances
		{
			get
			{
				return activeInstances;
			}
		}

		/// <inheritdoc/>
		[CanBeNull]
		public IInspector FirstInspector
		{
			get
			{
				return activeInstances.Count > 0 ? activeInstances[0] : null;
			}
		}

		/// <inheritdoc/>
		[CanBeNull]
		public IInspector LastInspector
		{
			get
			{
				int count = activeInstances.Count;
				return count > 0 ? activeInstances[count - 1] : null;
			}
		}

		/// <inheritdoc/>
		[CanBeNull]
		public IInspectorDrawer FirstInspectorDrawer
		{
			get
			{
				return activeInstances.Count > 0 ? activeInstances[0].InspectorDrawer : null;
			}
		}

		/// <inheritdoc/>
		[NotNull]
		public List<string> InstanceUniqueNames
		{
			get
			{
				return instanceUniqueNames;
			}
		}

		/// <inheritdoc/>
		[CanBeNull]
		public IDrawer FocusedDrawer
		{
			get
			{
				return selected.FocusedDrawer;
			}
		}

		/// <inheritdoc/>
		[NotNull]
		public List<IDrawer> MultiSelectedControls
		{
			get
			{
				return selected.MultiSelection;
			}
		}

		/// <inheritdoc/>
		public bool HasMultiSelectedControls
		{
			get
			{
				return selected.IsMultiSelection;
			}
		}

		/// <inheritdoc/>
		public IInspector SelectedInspector
		{
			get
			{
				return selected.Inspector;
			}
		}

		/// <inheritdoc/>
		public IInspector LastSelectedInspector
		{
			get
			{
				return selected.LastSelectedInspector;
			}
		}

		/// <inheritdoc/>
		public InspectorPart SelectedInspectorPart
		{
			get
			{
				return selected.InspectorPart;
			}
		}

		/// <inheritdoc/>
		public IDrawer MouseoveredSelectable
		{
			get
			{
				return mouseovered.Selectable;
			}
		}

		/// <inheritdoc/>
		public IDrawer MouseoveredRightClickable
		{
			get
			{
				return mouseovered.RightClickable;
			}
		}

		/// <inheritdoc/>
		public IInspector MouseoveredInspector
		{
			get
			{
				return mouseovered.Inspector;
			}
		}

		/// <inheritdoc/>
		public InspectorPart MouseoveredInspectorPart
		{
			get
			{
				return mouseovered.InspectorPart;
			}
		}

		/// <inheritdoc/>
		public MouseDownInfo MouseDownInfo
		{
			get
			{
				return mouseDownInfo;
			}
		}

		/// <inheritdoc/>
		public RightClickInfo RightClickInfo
		{
			get
			{
				return rightClickInfo;
			}
		}

		public InspectorManager()
		{
			if(instance == null)
			{
				instance = this;
			}
			InspectorUtility.ActiveManager = this;
			DrawGUI.OnDragAndDropObjectReferencesChanged += OnDragAndDropObjectReferencesChanged;

			mouseovered.onInspectorChanged += OnMouseoveredInspectorChanged;

			CollectExistingInspectorWindows();
		}

		/// <inheritdoc/>
		public void SetMouseoveredSelectable(IInspector inspector, IDrawer control)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(control != null && inspector == null) { Debug.LogError("control was "+StringUtils.ToString(control)+" but inspector was "+StringUtils.Null); }
			#endif
			mouseovered.SetSelectable(inspector, control);
		}

		/// <inheritdoc/>
		public void SetMouseoveredInspector(IInspector inspector, InspectorPart inspectorPart)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(inspectorPart == InspectorPart.None) { if(inspector != null) { Debug.LogError("SetMouseoveredInspector was called with inspector "+inspector+" but inspectorPart "+ StringUtils.ToColorizedString(inspectorPart) + "."); } }
			else if(inspector == null) { Debug.LogError("SetMouseoveredInspector was called with inspector "+StringUtils.Null+" but inspectorPart "+ StringUtils.ToColorizedString(inspectorPart) + "."); }
			#endif

			#if DEV_MODE && DEBUG_SET_MOUSEOVERED_INSPECTOR
			if(mouseovered.Inspector != inspector || inspectorPart != mouseovered.InspectorPart) { Debug.Log(StringUtils.ToColorizedString("SetMouseoveredInspector(", inspector, ", part=", inspectorPart + ") with IgnoreAllMouseInputs=", IgnoreAllMouseInputs, ", Cursor.CanRequestLocalPosition=", Cursor.CanRequestLocalPosition, ", Cursor.LocalPosition=", Cursor.LocalPosition)); }
			#endif

			bool mouseoveredViewportChanged = false;

			if(mouseovered.Inspector != inspector)
			{
				mouseoveredViewportChanged = true;
				mouseovered.SetInspectorAndPart(inspector, inspectorPart, true);
			}
			else if(mouseovered.InspectorPart != inspectorPart)
			{
				if(inspectorPart == InspectorPart.Viewport || mouseovered.InspectorPart == InspectorPart.Viewport)
				{
					mouseoveredViewportChanged = true;
				}
				mouseovered.SetInspectorPart(inspectorPart);
			}

			if(mouseoveredViewportChanged && mouseDownInfo.IsDrag())
			{
				if(inspectorPart != InspectorPart.Viewport)
				{
					mouseDownInfo.OnCursorLeftInspectorViewportDuringDrag();
				}
				else
				{
					#if DEV_MODE && DEBUG_DRAG
					Debug.Log(StringUtils.ToColorizedString("OnCursorEnteredInspectorViewportDuringDrag(", inspector, ") with NowReordering=", mouseDownInfo.NowReordering, ", DrawGUI.IsUnityObjectDrag=", DrawGUI.IsUnityObjectDrag));
					#endif

					mouseDownInfo.OnCursorEnteredInspectorViewportDuringDrag(inspector);
				}
			}
		}

		/// <inheritdoc/>
		public void SetMouseoveredRightClickable(IInspector inspector, IDrawer control)
		{
			mouseovered.SetRightClickable(inspector, control);
		}

		/// <inheritdoc/>
		public void AddToActiveInstances([NotNull]IInspector inspector)
		{
			#if DEV_MODE
			Debug.Assert(!activeInstances.Contains(inspector));
			#endif

			activeInstances.Add(inspector);
			
			var inspectorType = inspector.GetType();

			List<IInspector> activeInstancesOfSameType;
			if(!activeInstancesByType.TryGetValue(inspectorType, out activeInstancesOfSameType))
			{
				activeInstancesOfSameType = new List<IInspector>(1);
				activeInstancesByType[inspectorType] = activeInstancesOfSameType;
			}
			activeInstancesOfSameType.Add(inspector);
			
			InstanceUniqueNames.Add(GenerateUniqueName(inspector));

			#if DEV_MODE
			Debug.Assert(activeInstances.Count == InstanceUniqueNames.Count);
			#endif

			if(OnNewInspectorRegistered != null)
			{
				OnNewInspectorRegistered(inspector);
			}
		}

		public string GetUniqueName([NotNull]IInspector inspector)
		{
			int index = activeInstances.IndexOf(inspector);
			return index == -1 ? inspector.GetType().Name : instanceUniqueNames[index];
		}

		private string GenerateUniqueName(IInspector inspector)
		{
			var type = inspector.GetType();

			List<IInspector> activeInstancesOfSameType;
			if(!activeInstancesByType.TryGetValue(type, out activeInstancesOfSameType))
			{
				#if DEV_MODE
				Debug.LogError("GenerateUniqueName for inspector but activeInstancesByType did not yet contain the inspector");
				#endif
				return type.Name;
			}
			
			int count = activeInstancesOfSameType.Count;
			if(count <= 1)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(count == 1 && activeInstancesOfSameType[0] == inspector);
				#endif
				return type.Name;
			}

			return type.Name + " (" + StringUtils.ToString(count) + ")";
		}

		/// <summary>
		/// Method used for creating new Inspector instances. All instances should be
		/// created through the Manager, so that the IInspectorManager->IInspectorDrawer->IInspector
		/// hierarchy can be properly set up.
		/// </summary>
		/// <typeparam name="TInspector"> Type of the inspector. </typeparam>
		/// <param name="result">[out]The created inspector</param>
		/// <param name="drawer"> The drawer of the inspector. </param>
		/// <param name="preferences"> Preferences for the inspector. </param>
		/// <param name="inspected"> The inspected Unity Objects. </param>
		/// <param name="scrollPos"> The current scroll position of the inspector. </param>
		/// <param name="viewIsLocked"> True if view is locked. </param>
		/// <param name="setup"> Delegate to the Setup method for the Inspector. </param>
		public void Create<TInspector>(out TInspector result, IInspectorDrawer drawer, InspectorPreferences preferences, Object[] inspected, Vector2 scrollPos, bool viewIsLocked, SetupForInspected<TInspector> setup) where TInspector : class, IInspector, new()
		{
			if(!pool.TryGet(out result))
			{
				result = new TInspector();
			}
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!activeInstances.Contains(result));
			Debug.Assert(!activeInstances.Contains(null));
			#endif
			AddToActiveInstances(result);
			setup(result, preferences, drawer, inspected, scrollPos, viewIsLocked);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(activeInstances.Contains(result), "Created inspector "+result+" not found in InspectorManager.activeInstances");
			Debug.Assert(!pool.Contains(result), "Created inspector "+result+" still found in InspectorManager.pool");
			#endif
		}

		[NotNull]
		public TInspector Create<TInspector>(IInspectorDrawer drawer, InspectorPreferences preferences) where TInspector : class, IInspector, new()
		{
			TInspector result;
			Create(out result, drawer, preferences, ArrayPool<Object>.ZeroSizeArray, Vector2.zero, false);
			return result;
		}

		/// <summary>
		/// Method used for creating new Inspector instances. All instances should be
		/// created through the Manager, so that the IInspectorManager->IInspectorDrawer->IInspector
		/// hierarchy can be properly set up.
		/// </summary>
		/// <param name="inspectorType"> Type of the inspector that should be created. </param>
		/// <param name="drawer"> The drawer of the inspector. </param>
		/// <param name="preferences"> Preferences for the inspector. </param>
		/// <param name="inspected"> The inspected Unity Objects. </param>
		/// <param name="scrollPos"> The current scroll position of the inspector. </param>
		/// <param name="viewIsLocked"> True if view is locked. </param>
		/// <returns> The created inspector. </returns>
		[NotNull]
		public void Create<TInspector>(out TInspector result, IInspectorDrawer drawer, InspectorPreferences preferences, Object[] inspected, Vector2 scrollPos, bool viewIsLocked) where TInspector : class, IInspector, new()
		{
			object instanceFromPool;
			if(pool.TryGet(typeof(TInspector), out instanceFromPool))
			{
				result = (TInspector)instanceFromPool;
			}
			else
			{
				result = (TInspector)typeof(TInspector).CreateInstance();
			}
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!activeInstances.Contains(result));
			Debug.Assert(!activeInstances.Contains(null));
			#endif
			AddToActiveInstances(result);
			ActiveInspector = result;
			result.Setup(drawer, preferences, inspected, scrollPos, viewIsLocked);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(activeInstances.Contains(result), "Created inspector "+result+" not found in InspectorManager.activeInstances");
			Debug.Assert(!pool.Contains(result), "Created inspector "+result+" still found in InspectorManager.pool");
			#endif
		}

		/// <summary>
		/// Handles disposing references to the inspector from everywhere in InspectorManager such as from lists active and last selected instances.
		/// 
		/// It is important that this gets called every time an inspector is no longer used (i.e. it's drawer is closed).
		/// </summary>
		/// <typeparam name="TInspector"> Type of the inspector. </typeparam>
		/// <param name="disposing"> [in,out] The disposing. </param>
		/// <param name="poolForReuse"> (Optional) True to pool for reuse. </param>
		public void Dispose<TInspector>(ref TInspector disposing, bool poolForReuse = true) where TInspector : class, IInspector
		{
			#if DEV_MODE
			Debug.Log("Disposing inspector: "+disposing+"...");
			#endif

			var activeInspectorWas = activeInspector;
			ActiveInspector = disposing;

			int index = activeInstances.IndexOf(disposing);
			if(index != -1)
			{
				activeInstances.RemoveAt(index);
				InstanceUniqueNames.RemoveAt(index); 
			}

			var type = disposing.GetType();
			List<IInspector> activeInstancesOfSameType;
			if(activeInstancesByType.TryGetValue(type, out activeInstancesOfSameType))
			{
				activeInstancesOfSameType.Remove(disposing);
			}
		
			if(ReferenceEquals(selected.Inspector, disposing))
			{
				#if DEV_MODE
				Debug.Log("Clearing selected because selected inspector ("+StringUtils.ToString(selected.Inspector)+") was equal to inspector being disposed ("+ StringUtils.ToString(selected.Inspector) + ")");
				#endif
				selected.Clear(ReasonSelectionChanged.Dispose, null);
			}

			if(mouseovered.Inspector == disposing)
			{
				mouseovered.Clear();
			}

			if(mouseDownInfo.Inspector == disposing)
			{
				mouseDownInfo.Clear();
			}

			if(rightClickInfo.Inspector == disposing)
			{
				rightClickInfo.Clear();
			}

			selected.OnDisposing(disposing);
			mouseovered.OnDisposing(disposing);

			disposing.Dispose();

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!activeInstances.Contains(disposing));
			Debug.Assert(activeInstancesOfSameType != null && !activeInstancesOfSameType.Contains(disposing));
			Debug.Assert(!pool.Contains(disposing));
			Debug.Assert(LastSelectedInspector != disposing);
			#endif

			if(poolForReuse)
			{
				IInspector dispose = disposing;
				pool.Pool(ref dispose);
			}

			if(activeInspector == disposing)
			{
				if(activeInspectorWas != null && activeInspectorWas != disposing)
				{
					ActiveInspector = activeInspectorWas;
				}
				else
				{
					int count = activeInstances.Count;
					ActiveInspector = count > 0 ? activeInstances[count - 1] : null;
				}
			}

			disposing = null;
		}

		/// <inheritdoc/>
		public void Select(IInspector inspector, InspectorPart part, ReasonSelectionChanged reason)
		{
			selected.SetInspectorAndPart(inspector, part, reason);
		}

		/// <inheritdoc/>
		public void Select(IInspector inspector, InspectorPart part, IDrawer control, ReasonSelectionChanged reason)
		{
			Select(inspector, part, control, reason, 0);
		}

		/// <inheritdoc/>
		public void AddToSelection(IDrawer control, ReasonSelectionChanged reason)
		{
			Select(control.Inspector, InspectorPart.Viewport, control, reason, 1);
		}

		/// <inheritdoc/>
		public void RemoveFromSelection(IDrawer control, ReasonSelectionChanged reason)
		{
			Select(control.Inspector, InspectorPart.Viewport, control, reason, -1);
		}

		private void Select(IInspector inspector, InspectorPart part, [CanBeNull]IDrawer control, ReasonSelectionChanged reason, int multiSelect)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!(multiSelect != 0 && control == null), "Select was called with multiSelect "+multiSelect+" but control null. Reason: "+reason);
			Debug.Assert(control == null || part == InspectorPart.Viewport, "Select was called with InspectorPart "+part+" but control ("+ control + ") not being null. Reason: " + reason);
			Debug.Assert(control == null || inspector != null, "Select was called with control null but inspector ("+inspector+") not null. Reason: " + reason);
			Debug.Assert((part == InspectorPart.None) == (inspector == null), "Select was called with InspectorPart " + StringUtils.ToColorizedString(part) + " and inspector "+StringUtils.ToColorizedString(StringUtils.ToString(inspector))+" (control = "+StringUtils.ToString(control)+"). Reason: " + reason);
			Debug.Assert(control == null || control.Inspector == inspector, "control.Inspector != inspector");
			#endif
			
			selected.Set(inspector, part, control, multiSelect, reason);
		}

		/// <inheritdoc/>
		public void AddToMultiSelection(IInspector setInspector, InspectorPart setPart, IDrawer add, ReasonSelectionChanged reason)
		{
			Select(setInspector, setPart, add, reason, 1);
		}

		/// <inheritdoc/>
		public void RemoveFromMultiSelection(IInspector setInspector, InspectorPart setPart, IDrawer remove, ReasonSelectionChanged reason)
		{
			Select(setInspector, setPart, remove, reason, -1);
		}

		/// <inheritdoc/>
		public bool IsSelected(IDrawer target)
		{
			return selected.IsSelected(target);
		}

		/// <inheritdoc/>
		public bool IsFocusedButNotSelected(IDrawer testControl)
		{
			return selected.IsFocusedButNotSelected(testControl);
		}

		/// <inheritdoc/>
		public IInspector ActiveSelectedOrDefaultInspector()
		{
			if(activeInspector != null)
			{
				return activeInspector;
			}

			if(LastSelectedInspector != null)
			{
				return LastSelectedInspector;
			}

			int count = activeInstances.Count;
			if(count == 0)
			{
				return null;
			}

			if(count == 1)
			{
				return activeInstances[0];
			}

			for(int n = count - 1; n >= 0; n--)
			{
				var instance = activeInstances[n];
				var editorWindow = instance.InspectorDrawer as EditorWindow;
				if(editorWindow == null || editorWindow.IsVisible())
				{
					return instance;
				}
			}

			return activeInstances[count - 1];
		}

		/// <inheritdoc/>
		public IInspector LastSelectedActiveOrDefaultInspector()
		{
			if(LastSelectedInspector != null)
			{
				return LastSelectedInspector;
			}

			if(activeInspector != null)
			{
				return activeInspector;
			}

			int count = activeInstances.Count;
			if(count == 0)
			{
				return null;
			}

			if(count == 1)
			{
				return activeInstances[0];
			}

			for(int n = count - 1; n >= 0; n--)
			{
				var instance = activeInstances[n];
				var editorWindow = instance.InspectorDrawer as EditorWindow;
				if(editorWindow == null || editorWindow.IsVisible())
				{
					return instance;
				}
			}

			return activeInstances[count - 1];
		}

		/// <inheritdoc/>
		public IInspector LastSelectedActiveOrDefaultInspector(InspectorTargetingMode targetingMode)
		{
			if(targetingMode == InspectorTargetingMode.All)
			{
				return LastSelectedActiveOrDefaultInspector();
			}
			
			var wrongTargetingMode = targetingMode == InspectorTargetingMode.Hierarchy ? InspectorTargetingMode.Project : InspectorTargetingMode.Hierarchy;
			
			if(LastSelectedInspector != null && LastSelectedInspector.InspectorDrawer.InspectorTargetingMode != wrongTargetingMode)
			{
				return LastSelectedInspector;
			}

			if(activeInspector != null && activeInspector.InspectorDrawer.InspectorTargetingMode != wrongTargetingMode)
			{
				return activeInspector;
			}

			for(int n = activeInstances.Count - 1; n >= 0; n--)
			{
				if(activeInstances[n].InspectorDrawer.InspectorTargetingMode != wrongTargetingMode)
				{
					return activeInstances[n];
				}
			}

			return null;
		}

		/// <inheritdoc/>
		public IInspector LastSelectedActiveOrDefaultInspector(InspectorTargetingMode targetingMode, InspectorSplittability splittability)
		{
			if(targetingMode == InspectorTargetingMode.All)
			{
				return LastSelectedActiveOrDefaultInspector(splittability);
			}

			if(splittability == InspectorSplittability.Any)
			{
				return LastSelectedActiveOrDefaultInspector(targetingMode);
			}
			
			var wrongTargetingMode = targetingMode == InspectorTargetingMode.Hierarchy ? InspectorTargetingMode.Project : InspectorTargetingMode.Hierarchy;
			bool splittable = splittability == InspectorSplittability.IsSplittable;

			if(LastSelectedInspector != null && LastSelectedInspector.InspectorDrawer.InspectorTargetingMode != wrongTargetingMode && LastSelectedInspector.InspectorDrawer is ISplittableInspectorDrawer == splittable)
			{
				return LastSelectedInspector;
			}

			if(activeInspector != null && activeInspector.InspectorDrawer.InspectorTargetingMode != wrongTargetingMode && activeInspector.InspectorDrawer is ISplittableInspectorDrawer == splittable)
			{
				return activeInspector;
			}

			for(int n = activeInstances.Count - 1; n >= 0; n--)
			{
				if(activeInstances[n].InspectorDrawer.InspectorTargetingMode != wrongTargetingMode && activeInstances[n].InspectorDrawer is ISplittableInspectorDrawer == splittable)
				{
					return activeInstances[n];
				}
			}

			return null;
		}

		/// <inheritdoc/>
		public IInspector LastSelectedActiveOrDefaultInspector(InspectorSplittability splittability)
		{
			if(splittability == InspectorSplittability.Any)
			{
				return LastSelectedActiveOrDefaultInspector();
			}
			
			bool splittable = splittability == InspectorSplittability.IsSplittable;
			
			if(LastSelectedInspector != null && LastSelectedInspector.InspectorDrawer is ISplittableInspectorDrawer == splittable)
			{
				return LastSelectedInspector;
			}

			if(activeInspector != null && activeInspector.InspectorDrawer is ISplittableInspectorDrawer == splittable)
			{
				return activeInspector;
			}

			for(int n = activeInstances.Count - 1; n >= 0; n--)
			{
				if(activeInstances[n].InspectorDrawer is ISplittableInspectorDrawer == splittable)
				{
					return activeInstances[n];
				}
			}

			return null;
		}

		/// <summary>
		/// Disposes the InspectorManager and unsubscribes from all events.
		/// </summary>
		public void Dispose()
		{
			if(instance == this)
			{
				instance = null;
			}
			DrawGUI.OnDragAndDropObjectReferencesChanged -= OnDragAndDropObjectReferencesChanged;
			mouseovered.onInspectorChanged -= OnMouseoveredInspectorChanged;
			mouseDownInfo.Dispose();
			rightClickInfo.Dispose();
		}

		private void OnMouseoveredInspectorChanged(IInspector from, IInspector to)
		{
			mouseDownInfo.OnMouseoveredInspectorChanged(from, to);
			if(to == null)
			{
				#if DEV_MODE && DEBUG_CLEAR
				Debug.LogWarning("Cursor left " + from + " bounds. Clearing mouseovered.");
				#endif
				mouseovered.Clear();
			}
		}

		private void OnDragAndDropObjectReferencesChanged(Object[] draggedObjects)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(instance == this);
			#endif

			#if DEV_MODE && DEBUG_DRAG_N_DROP_REFERENCES
			Debug.Log("OnDragAndDropObjectReferencesChanged("+StringUtils.ToString(draggedObjects) +") with Event="+StringUtils.ToString(Event.current));
			#endif

			if(draggedObjects.Length > 0)
			{
				mouseDownInfo.OnDragStarted(mouseovered.Inspector, draggedObjects);
			}
			else
			{
				mouseDownInfo.Clear(false);
			}
		}

		/// <inheritdoc/>
		public void RegisterKeyHeldDown(KeyCode keyCode, string key)
		{
			keyHeldDown = keyCode;
			keyHeldDownName = key;
			sendNextHoldEventAt = Platform.Time  + KeyHoldInitiateThreshold;
		}

		public IDrawer FindDrawer(Object target)
		{
			var inspector = ActiveSelectedOrDefaultInspector();
			if(inspector != null)
			{
				var drawers = inspector.State.drawers.FindDrawer(target);
				if(drawers != null)
				{
					return drawers;
				}
				
				for(int n = activeInstances.Count - 1; n >= 0; n--)
				{
					var otherInspector = activeInstances[n];
					if(otherInspector != inspector)
					{
						drawers = otherInspector.State.drawers.FindDrawer(target);
						if(drawers != null)
						{
							return drawers;
						}
					}
				}
			}
			return null;
		}

		/// <inheritdoc/>
		public void OnLayout()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(onNextLayoutDelayedSwapTarget.Count > 0) { Debug.LogWarning("onNextLayoutDelayedSwapTarget not empty: " + StringUtils.ToString(onNextLayoutDelayedSwapTarget)); }
			#endif

			// Swap onNextLayoutDelayed with an empty Queue before invoking actions in onNextLayout or onNextLayoutDelayed.
			// This is to avoid possible infinite loops and other problems if an invoked action should add new actions to onNextLayoutDelayed.
			var applyTargeted = onNextLayoutDelayed;
			onNextLayoutDelayed = onNextLayoutDelayedSwapTarget;
			onNextLayoutDelayedSwapTarget = applyTargeted;

			if(onNextLayout != null)
			{
				#if DEV_MODE && DEBUG_ON_NEXT_LAYOUT_DETAILED
				Debug.Log("Applying OnNextLayout Action: " + StringUtils.ToString(onNextLayout));
				#endif

				var invocationList = onNextLayout.GetInvocationList();
				onNextLayout = null;
				Exception exception = null;
				for(int n = 0, count = invocationList.Length; n < count; n++)
				{
					try
					{
						var invoke = invocationList[n] as Action;
						invoke();
					}
					catch(Exception e)
					{
						#if DEV_MODE
						if(!ExitGUIUtility.ShouldRethrowException(e)) { Debug.LogError(e); }
						#endif

						exception = e;
					}
				}
				
				if(exception != null && ExitGUIUtility.ShouldRethrowException(exception))
				{
					throw exception;
				}
			}

			int applyTargetedCount = applyTargeted.Count;
			if(applyTargetedCount > 0)
			{
				for(int n = 0; n < applyTargetedCount; n++)
				{
					var apply = applyTargeted.Dequeue();

					#if DEV_MODE && DEBUG_ON_NEXT_LAYOUT_DETAILED
					Debug.Log("Applying OnNextLayout DelayedAction: " + StringUtils.ToString(apply));
					#endif

					// Only invoke targeted action if target drawers instance still exist.
					apply.InvokeIfInstanceReferenceIsValid();
				}

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(applyTargeted.Count == 0);
				#endif
			}

			if(keyHeldDown != KeyCode.None)
			{
				GUI.changed = true;
				if(Platform.Time > sendNextHoldEventAt)
				{
					sendNextHoldEventAt = Platform.Time + KeyHoldSendEventInterval;
					OnKeyDown(Event.KeyboardEvent(keyHeldDownName));
				}
			}
		}

		/// <inheritdoc/>
		public void CancelOnNextLayout(Action action)
		{
			onNextLayout -= action;
		}

		/// <inheritdoc/>
		public void OnNextLayout(Action action, IInspectorDrawer changingInspectorDrawer = null)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(action != null);
			#endif

			#if DEV_MODE && DEBUG_ON_NEXT_LAYOUT
			Debug.Log("OnNextLayout(" + StringUtils.ToString(action)+")", changingInspectorDrawer as Object);
			#endif

			onNextLayout += action;

			if(changingInspectorDrawer != null)
			{
				changingInspectorDrawer.RefreshView();
			}
			else
			{
				EnsureOnGUICallbacks(true);
			}
		}

		/// <inheritdoc/>
		public void OnNextOnGUI(Action action, IInspectorDrawer changingInspectorDrawer = null)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(action != null);
			#endif

			#if DEV_MODE && DEBUG_ON_NEXT_ONGUI
			Debug.Log("OnNextOnGUI(" + StringUtils.ToString(action)+ ") with changingInspectorDrawer="+StringUtils.ToString(changingInspectorDrawer), changingInspectorDrawer as Object);
			#endif

			DrawGUI.OnNextBeginOnGUI(action, false);

			if(changingInspectorDrawer != null)
			{
				changingInspectorDrawer.RefreshView();
			}
			else
			{
				EnsureOnGUICallbacks(false);
			}
		}

		public void EnsureOnGUICallbacks(bool needsLayoutEvent)
		{
			if(!IsAnyInspectorVisible())
			{
				OnGUIUtility.EnsureOnGUICallbacks(NeedsOnGUIHelperWindow, needsLayoutEvent);
			}
			else
			{
				DrawGUI.EnsureOnGUICallbackOnExistingInspectorIfAny();
			}
		}

		public bool IsAnyInspectorVisible()
		{
			return GetFirstVisibleInspector() != null;
		}

		[CanBeNull]
		public IInspector GetFirstVisibleInspector()
		{
			for(int n = 0, count = activeInstances.Count; n < count; n++)
			{
				var inspector = activeInstances[n];
				var editorWindow = inspector.InspectorDrawer as EditorWindow;
				if(editorWindow == null || editorWindow.IsVisible())
				{
					return inspector;
				}
			}
			return null;
		}

		/// <inheritdoc/>
		public IInspector GetLastSelectedInspector(Type inspectorType)
		{
			return selected.GetLastSelectedInspector(inspectorType);
		}

		/// <inheritdoc/>
		public IInspectorDrawer GetLastSelectedInspectorDrawer(Type inspectorDrawerType)
		{
			return selected.GetLastSelectedInspectorDrawer(inspectorDrawerType);
		}

		/// <inheritdoc/>
		public EditorWindow GetLastSelectedEditorWindow()
		{
			return selected.GetLastSelectedEditorWindow();
		}

		private bool NeedsOnGUIHelperWindow()
		{
			#if DEV_MODE && DEBUG_ENSURE_ON_GUI_CALLBACKS
			Debug.Log(StringUtils.ToColorizedString("InspectorManager.NeedsOnGUIHelperWindow: InspectorManager.InstanceExists=", true, ", ActiveInstances=", activeInstances.Count, ", onNextLayout = ", onNextLayout, ", onNextLayoutDelayed=", onNextLayoutDelayed, ", result=", activeInstances.Count == 0 && (onNextLayout != null || onNextLayoutDelayed.Count > 0)));
			#endif

			return activeInstances.Count == 0 && (onNextLayout != null || onNextLayoutDelayed.Count > 0);
		}

		/// <inheritdoc/>
		public void OnNextLayout(IDrawerDelayableAction action, IInspectorDrawer changingInspectorDrawer = null)
		{
			onNextLayoutDelayed.Enqueue(action);

			if(changingInspectorDrawer != null)
			{
				changingInspectorDrawer.RefreshView();
			}
			else
			{
				EnsureOnGUICallbacks(true);
			}
		}

		private void OnKeyDown(Event e)
		{
			// First prioritize mouseovered inspector.
			var mouseoveredInspector = MouseoveredInspector;
			var mouseoveredInspectorDrawer = mouseoveredInspector == null ? null : mouseoveredInspector.InspectorDrawer;
			if(mouseoveredInspectorDrawer != null)
			{
				mouseoveredInspectorDrawer.OnKeyDown(e);
			}

			// Then the selected inspector.
			var selectedInspector = SelectedInspector;
			var selectedInspectorDrawer = selectedInspector == null ? null : selectedInspector.InspectorDrawer;
			if(selectedInspectorDrawer != null && selectedInspectorDrawer != mouseoveredInspectorDrawer)
			{
				selectedInspectorDrawer.OnKeyDown(e);
			}

			// Also call for all other active instances, in case they are listening to any shortcut keys
			// that should work even if the window is not mouseovered or selected.
			int count = activeInstances.Count;
			if(count > 1)
			{
				var handledInspectorDrawers = new HashSet<IInspectorDrawer>();
				if(mouseoveredInspectorDrawer != null)
				{
					handledInspectorDrawers.Add(mouseoveredInspectorDrawer);
				}
				if(selectedInspectorDrawer != null)
				{
					handledInspectorDrawers.Add(selectedInspectorDrawer);
				}

				for(int n = activeInstances.Count - 1; n >= 0; n--)
				{
					var inspectorDrawer = activeInstances[n].InspectorDrawer;
					if(handledInspectorDrawers.Add(inspectorDrawer))
					{
						inspectorDrawer.OnKeyDown(e);
					}
				}
			}
		}

		public void OnKeyUp(Event e)
		{
			switch(e.keyCode)
			{
				case KeyCode.AltGr:
				case KeyCode.RightAlt:
				{
					KeyConfig.OnAltGrUp();
				}
				break;
			}

			if(e.keyCode == keyHeldDown)
			{
				keyHeldDown = KeyCode.None;
			}
		}

		/// <summary>
		/// Finds inspector drawer instances amongst currently open EditorWindows, and collects their inspector views to active instances.
		/// Also calls Setup for instances where it hasn't yet been called.
		/// </summary>
		private static void CollectExistingInspectorWindows()
		{
			var editorWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
			int foundCount = 0;

			int count = editorWindows.Length;
			for(int n = count - 1; n >= 0; n--)
			{
				var window = editorWindows[n];

				// Skip closing EditorWindows.
				if(window == null)
                {
					continue;
                }

				var drawer = window as IInspectorDrawer;
				if(drawer == null)
				{
					continue;
				}
				foundCount++;
				var hostView = window.ParentHostView();
				if(hostView == null)
				{
					#if DEV_MODE
					Debug.LogWarning("m_Parent HostView of PowerInspectorWindow \""+window.name+"\" #"+ foundCount + ") was null. Should we destroy this window?", window);
					#endif
					// UPDATE: This can happen with utility windows! So don't destroy it!
					//Object.DestroyImmediate(window);
					return;
				}
				
				#if DEV_MODE && DEBUG_COLLECT_EXISTING_INSPECTORS
				Debug.Log("PowerInspectorWindow \"" + window.name + "\" #" + foundCount + ") info:\nIsDocked:"+window.IsDocked()+", maximized:" + window.maximized+", position:" + window.position, window);
				#endif

				var mainView = drawer.MainView;
				if(mainView != null)
				{
					#if DEV_MODE
					if(!mainView.SetupDone)
					{
						int thisIndex = foundCount;
						UnityEditor.EditorApplication.delayCall +=()=> UnityEditor.EditorApplication.delayCall += ()=>
						{
							if(!mainView.SetupDone)
							{
								Debug.LogWarning("mainView of PowerInspectorWindow \"" + window.name + "\" #" + thisIndex + ") SetupDone was false. This can happen after assembly reloads, and should get fixed during next OnGUI call.", window);
							}
						};
					}
					#endif
					
					if(!Instance().activeInstances.Contains(mainView))
					{
						#if DEV_MODE
						Debug.LogWarning("mainView of PowerInspectorWindow \"" + window.name + "\" #" + foundCount + ") not found in activeInstances. This can happen after assembly reloads. Adding it to activeInstances now.", window);
						#endif

						instance.AddToActiveInstances(mainView);
					}

					#if DEV_MODE
					// this has never happened
					if(instance.pool.Contains(mainView))
					{
						Debug.LogWarning("mainView of PowerInspectorWindow \"" + window.name + "\" #" + foundCount + ") found in pool. Should we destroy this window?", window);
					}
					#endif
				}
				#if DEV_MODE
				else
				{
					var logCount = foundCount;
					UnityEditor.EditorApplication.delayCall +=()=> UnityEditor.EditorApplication.delayCall += ()=>
					{
						if(!window.Equals(null))
						{
							mainView = drawer.MainView;
							if(mainView == null)
							{
								Debug.LogWarning("mainView of PowerInspectorWindow \"" + window.name + "\" #" + logCount + " was null.\nIsDocked:" + window.IsDocked() + ", maximized:" + window.maximized + ", position:" + window.position, window);
							}
						}
					};
				}
				#endif
			}

			#if DEV_MODE
			if(foundCount > 1)
			{
				// having more than one PowerInspectorWindow open is supported, but rare enough that it
				// can be useful to see this warning in case there have been leaks.
				Debug.LogWarning("CleanUpHiddenWindows found " + foundCount + " PowerInspectorWindows.\nactiveInstances="+StringUtils.ToString(instance.activeInstances));
			}
			#endif
		}
	}
}