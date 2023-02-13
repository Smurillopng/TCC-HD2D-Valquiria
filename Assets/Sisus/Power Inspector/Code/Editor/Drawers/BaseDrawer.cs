#define SAFE_MODE
#define EXPAND_SELECTION_RECT_WIDTH

//#define DEBUG_GET_OPTIMAL_PREFIX_LABEL_WIDTH
//#define DEBUG_EMPTY_LABEL
//#define DEBUG_ON_CLICK
//#define DEBUG_ON_RIGHT_CLICK
//#define DEBUG_DISPOSE
//#define ENABLE_RIGHT_CLICK_AREA_MOUSEOVER_EFFECT
//#define DEBUG_NEXT_FIELD
//#define DEBUG_PASSES_SEARCH_FILTER
//#define DEBUG_SET_MOUSEOVERED
//#define DEBUG_MOUSEOVER_DETECTION
//#define DEBUG_ON_SELECTED
//#define DEBUG_ON_BECAME_INVISIBLE

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sisus.Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor;

namespace Sisus
{
	public delegate void OnValueChanged(IDrawer changed, object value);

	/// <summary> Base class for all Drawer from assets to components to fields. </summary>
	[Serializable]
	public abstract class BaseDrawer : IDrawer, IDisposable, IEquatable<BaseDrawer>
	{
		/// <summary> Reusable list of ints. Used by GenerateMemberIndexPath. </summary>
		private static readonly List<int> ReusableIntList = new List<int>(5);

		protected int controlId = -1;

		/// <summary> The prefix label text and tooltip. </summary>
		[SerializeField]
		protected GUIContent label = new GUIContent();

		/// <summary>
		/// The display name of the drawer in text form.
		/// Usually this equates to label.text, but when using image-based labels this can
		/// instead equate to LinkedMemberInfo.DisplayName.
		/// </summary>
		[SerializeField]
		private string name = "";

		/// <summary> If not null, this action will override the normal effects of the Reset function. </summary>
		public Action<IDrawer> overrideReset;

		/// <summary> If not null, this action will override the normal data validation function. </summary>
		protected Func<object[], bool> overrideValidateValue;

		/// <summary> Invoked when the Drawer were selected and then lost focus. </summary>
		public Action<IDrawer> onLostFocus;

		/// <summary> Invoked when keyboard input is being given. Allows external actors to capture and
		/// extend the functionality of Drawer in relation to responding to keyboard inputs. </summary>
		private KeyboardInputBeingGiven onKeyboardInputBeingGiven;

		/// <summary> The parent. </summary>
		[SerializeField]
		protected IParentDrawer parent;

		/// <summary> The last draw position. </summary>
		[SerializeField]
		protected Rect lastDrawPosition;

		/// <summary>
		/// Tells whether or not the drawer passed the last search box filter check.
		/// This should be updated during every OnFilterchanged event.
		/// </summary>
		[SerializeField]
		protected bool passedLastFilterCheck = true;

		/// <summary>
		/// Contains the type of the last filter check that the drawer passed.
		/// This can be useful to know when determining which part of the drawer should be highlighted.
		/// This should be updated during every OnFilterchanged event.
		/// </summary>
		protected FilterTestType lastPassedFilterTestType;

		/// <summary> True if data is valid. </summary>
		[SerializeField]
		private bool dataIsValid = true;

		/// <summary>
		/// True when drawers have been disposed to the object pool, or when Setup phase is still in progress.
		/// Sometimes also set true temporarily when changing children without wanting OnMemberValueChanged etc. to
		/// cause undesired side effects. </summary>
		protected bool inactive = true;

		/// <summary> The on value changed. </summary>
		private OnValueChanged onValueChanged;

		/// <summary> True to enable, false to disable the graphical user interface. </summary>
		private bool guiEnabled = true;

		/// <summary>
		/// This is incremented by one every time this class instance is pooled.
		/// Can be used to detect if this instance has been pooled after an async
		/// task has been started, before applying it's effects.
		/// </summary>
		private int instanceId = 1;
		
		// If was control drawn inside something like BeginArea or BeginScroll view, this should contain the offset of left corner of that area from (0,0).
		// This can be useful when need to draw something like mouseover effects outside of the main GUI with the correct offset.
		protected Vector2 localDrawAreaOffset;

		/// <inheritdoc/>
		public int ControlID
		{
			get
			{
				return controlId;
			}
		}

		/// <inheritdoc/>
		public int InstanceId
		{
			get
			{
				return instanceId;
			}
		}

		/// <inheritdoc/>
		public virtual string FullClassName
		{
			get
			{
				return Name;
			}
		}

		/// <inheritdoc/>
		public Func<object[], bool> OverrideValidateValue
		{
			set
			{
				#if DEV_MODE
				Debug.Log(ToString()+ ".OverrideValidateValue = "+StringUtils.ToString(value));
				#endif

				overrideValidateValue = value;
				OnValidate();
			}
		}

		/// <inheritdoc/>
		public virtual bool CachedValuesNeedUpdating
		{
			get
			{
				return ShouldConstantlyUpdateCachedValues();
			}
		}

		/// <inheritdoc/>
		public KeyboardInputBeingGiven OnKeyboardInputBeingGiven
		{
			get
			{
				return onKeyboardInputBeingGiven;
			}

			set
			{
				onKeyboardInputBeingGiven = value;
			}
		}

		/// <inheritdoc/>
		public virtual bool IsAnimated
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public virtual bool RequiresConstantRepaint
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public virtual bool HasUnappliedChanges
		{
			get
			{
				return false;
			}

			protected set
			{
				throw new InvalidOperationException("Can't set HasUnappliedChanges for "+ToString());
			}
		}

		/// <inheritdoc/>
		public virtual bool IsReorderable
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public virtual bool ReadOnly
		{
			get
			{
				return !guiEnabled;
			}

			set
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+"Can't set ReadOnly to "+value);
				#endif
			}
		}

		/// <summary>
		/// Is it safe to read the value of this field without the risk of there being undesired side effects?
		/// <para>
		/// Returns true for all fields, false for properties and methods that aren't considered
		/// safe based on their attributes and current display preferences. Returns false for
		/// drawers that don't have a value;
		/// </para>
		/// </summary>
		/// <value>
		/// True if we can read from field without risk of undesired side effects, false if not.
		/// </value>
		public virtual bool CanReadFromFieldWithoutSideEffects
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public virtual bool PrefixResizingEnabledOverControl
		{
			get
			{
				return true;
			}
		}

		/// <summary> Gets the last draw position of the control component of these drawers (as
		/// opposed to the prefix label/header component). If these drawers don't contain separate
		/// control and prefix components, this should return the  whole bounds of the control. </summary>
		/// <value> The control position. </value>
		public virtual Rect ControlPosition
		{
			get
			{
				return lastDrawPosition;
			}
		}

		/// <inheritdoc/>
		public virtual Part MouseoveredPart
		{
			get
			{
				return Mouseovered ? Part.Base : Part.None;
			}
		}

		/// <inheritdoc/>
		public virtual Part SelectedPart
		{
			get
			{
				return Selected ? Part.Base : Part.None;
			}
		}

		/// <summary> Gets the last draw position of the prefix label or header component of these
		/// drawers (as opposed to the control component). If these drawers don't contain
		/// separate control and prefix components, this should return the  whole bounds of the control. </summary>
		/// <value> The prefix label position. </value>
		protected virtual Rect PrefixLabelPosition
		{
			get
			{
				return lastDrawPosition;
			}
		}

		/// <summary>
		/// Should menu items for context menu be added starting from root base class or from leaf
		/// extending class?
		/// class. </summary>
		/// <value>
		/// True if context menu items should be added base-class first, false if they should
		/// be added extending class first.
		/// </value>
		protected virtual bool BuildContextMenuItemsStartingFromBaseClass
		{
			get
			{
				return true;
			}
		}

		/// <summary> Is GUI currently enabled for user interactions? </summary>
		/// <value> False if GUI.enabled has been set to false, otherwise true. </value>
		protected bool GuiEnabled
		{
			get
			{
				return guiEnabled;
			}
		}

		/// <summary> Rect for box that is drawn when control is selected. </summary>
		/// <value> The selection rectangle. </value>
		[JsonIgnore]
		protected virtual Rect SelectionRect
		{
			get
			{
				#if EXPAND_SELECTION_RECT_WIDTH
				var pos = lastDrawPosition;

				pos.y += 1f;
				pos.height = Height - 1f;

				//make full inspector width controls' selection rects look better
				if(IsFullInspectorWidth)
				{
					pos.x += DrawGUI.LeftPadding;
					pos.width -= DrawGUI.LeftPadding + DrawGUI.RightPadding;
				}

				return pos;
				#else
				var pos = ClickToSelectArea;
				pos.height = Height;
				pos.width = lastDrawPosition.width - pos.x - DrawGUI.rightPadding;
				return pos;
				#endif
			}
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public virtual float Height
		{
			get
			{
				return DrawGUI.SingleLineHeight;
			}
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public virtual float Width
		{
			get
			{
				return lastDrawPosition.width;
			}
		}

		/// <inheritdoc/>
		public bool Inactive
		{
			get
			{
				return inactive;
			}
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public IParentDrawer Parent
		{
			get{ return parent; }
		}

		public IUnityObjectDrawer UnityObjectDrawer
		{
			get
			{
				var self = this as IUnityObjectDrawer;
				if(self != null)
				{
					return self;
				}
				return parent != null ? parent.UnityObjectDrawer : null;
			}
		}

		/// <inheritdoc/>
		public virtual LinkedMemberInfo MemberInfo
		{
			get
			{
				return null;
			}
		}

		/// <inheritdoc/>
		public GUIContent Label
		{
			get { return label; }
			
			set
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(value != null);
				#endif

				if(label != value)
				{
					if(label != null)
					{
						GUIContentPool.Dispose(ref label);
					}
					label = value;
					OnLabelChanged();
				}
			}
		}
		
		/// <inheritdoc/>
		public virtual string Name
		{
			get
			{
				return name;
			}
		}

		/// <inheritdoc/>
		public virtual string Tooltip
		{
			get
			{
				return label.tooltip;
			}

			set
			{
				label.tooltip = value;
			}
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public virtual bool ShouldShowInInspector
		{ 
			get
			{
				return passedLastFilterCheck;
			}
		}
		
		/// <inheritdoc/>
		public virtual OnValueChanged OnValueChanged
		{
			get { return onValueChanged; }
			set { onValueChanged = value; }
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public virtual Object UnityObject
		{
			get
			{
				if(parent != null)
				{
					return parent.UnityObject;
				}
				return null;
			}
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public virtual Object[] UnityObjects
		{
			get
			{
				if(parent != null)
				{
					return parent.UnityObjects;
				}
				return ArrayPool<Object>.ZeroSizeArray;
			}
		}

		/// <inheritdoc/>
		public virtual Transform[] Transforms
		{
			get
			{
				var objs = UnityObjects;
				if(objs.Length == 0 || objs[0] == null)
				{
					if(parent != null)
					{
						objs = parent.UnityObjects;
					}

					if(objs.Length == 0 || objs[0] == null)
					{
						var hierarchy = MemberHierarchy;
						if(hierarchy != null)
						{
							objs = hierarchy.Targets;
						}

						if(objs.Length == 0 || objs[0] == null)
						{
							return ArrayPool<Transform>.ZeroSizeArray;
						}
					}
				}

				Transform[] transforms;
				if(!ArrayPool<Object>.TryCast(objs, false, out transforms))
				{
					int count = objs.Length;
					transforms = ArrayPool<Transform>.Create(count);
					for(int n = count - 1; n >= 0; n--)
					{
						transforms[n] = objs[n].Transform();
					}
				}

				return transforms;
			}
		}

		/// <inheritdoc/>
		public virtual Transform Transform
		{
			get
			{
				var obj = UnityObject;
				if(obj != null)
				{
					return obj.Transform();
				}

				if(parent != null)
				{
					obj = parent.UnityObject;
					if(obj != null)
					{
						return obj.Transform();
					}
				}
				
				var hierarchy = MemberHierarchy;
				if(hierarchy == null)
				{
					return null;
				}

				obj = hierarchy.Target;
				return obj == null ? null : obj.Transform();
			}
		}
		
		/// <inheritdoc/>
		public abstract Type Type
		{
			get;
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public bool DataIsValid
		{
			get
			{
				return dataIsValid;
			}
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public virtual bool Selectable
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public virtual bool Clickable
		{
			get
			{
				return Selectable;
			}
		}


		/// <inheritdoc/>
		[JsonIgnore]
		public Rect Bounds
		{
			get
			{
				var result = lastDrawPosition;
				result.height = Height;
				return result;
			}
		}

		/// <inheritdoc/>
		public virtual Rect ClickToSelectArea
		{
			get
			{
				var selectionRect = lastDrawPosition;
				if(IsFullInspectorWidth)
				{
					#if EXPAND_SELECTION_RECT_WIDTH
					DrawGUI.AddMargins(ref selectionRect);
					#else
					DrawGUI.AddMarginsAndIndentation(ref selectionRect);
					#endif
				}
				return selectionRect;
			}
		}

		/// <inheritdoc/>
		public Vector2 LocalDrawAreaOffset
		{
			get
			{
				return localDrawAreaOffset;
			}
		}
		

		/// <inheritdoc/>
		[JsonIgnore]
		public virtual Rect RightClickArea
		{
			get
			{
				return lastDrawPosition;
			}
		}

		protected bool SelectedAndInspectorHasFocus
		{
			get
			{
				return Selected && Inspector.InspectorDrawer.HasFocus;
			}
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public bool Selected
		{
			get
			{
				return Manager.IsSelected(this);
			}
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public virtual IInspector Inspector
		{
			get
			{
				#if DEV_MODE && PI_ASSERTATIONS
				if(parent != null && parent.Inspector != InspectorUtility.ActiveInspector) { Debug.LogError("parent "+parent+" inspector " + (parent.Inspector == null ? "null" : parent.Inspector.ToString())+ " != ActiveInspector "+ (InspectorUtility.ActiveInspector == null ? "null" : InspectorUtility.ActiveInspector.ToString())); }
				#endif

				return InspectorUtility.ActiveInspector;
			}
		}

		/// <inheritdoc/>
		public virtual bool IsPrefab
		{
			get 
			{
				return parent != null && parent.IsPrefab;
			}
		}

		/// <inheritdoc/>
		public virtual bool IsPrefabInstance
		{
			get 
			{
				return parent != null && parent.IsPrefabInstance;
			}
		}

		protected IInspectorManager Manager
		{
			get
			{
				return InspectorUtility.ActiveManager;
			}
		}
		
		protected InspectorPreferences Preferences
		{
			get
			{
				return InspectorUtility.Preferences;
			}
		}

		/// <summary> Gets the linked member hierarchy for target UnityEngine.Object(s) </summary>
		/// <value> The linked member hierarchy. </value>
		[JsonIgnore]
		public virtual LinkedMemberHierarchy MemberHierarchy
		{
			get
			{
				return parent == null ? null : parent.MemberHierarchy;
			}
		}

		/// <summary> Gets or sets a value indicating whether this drawer is currently being mouseovered. </summary>
		/// <value> True if mouseovered, false if not. </value>
		[JsonIgnore]
		protected bool Mouseovered
		{
			get
			{
				var manager = Manager;
				return manager.MouseoveredSelectable == this;
			}

			set
			{
				var manager = Manager;

				#if DEV_MODE && DEBUG_SET_MOUSEOVERED
				if(Mouseovered != value) { Debug.Log(StringUtils.ToColorizedString(ToString(), ".Mouseovered = ", value, " (was: ", Mouseovered, ")")); }
				#endif

				if(value)
				{
					manager.SetMouseoveredSelectable(manager.MouseoveredInspector, this);
				}
				else if(manager.MouseoveredSelectable == this)
				{
					manager.SetMouseoveredSelectable(manager.MouseoveredInspector, null);
				}
			}
		}

		/// <inheritdoc/>
		public virtual string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetTerminologyUrl("drawer");
			}
		}

		/// <summary>
		/// Returns value indicating whether or not this drawer is currently shown in the inspector.
		/// 
		/// This is done by checking if parent visible members array contains this drawer.
		/// </summary>
		protected virtual bool ShownInInspector
		{
			get
			{
				return parent == null || Array.IndexOf(parent.VisibleMembers, this) != -1;
			}
		}

		/// <summary>
		/// Returns value indicating whether or not this drawer has a documentation page set up for it.
		/// </summary>
		protected bool HasDocumentationPage
		{
			get
			{
				return DocumentationPageUrl.Length > 0;
			}
		}

		/// <summary> Gets or sets a value indicating whether the right click area mouseovered. </summary>
		/// <value> True if right click area mouseovered, false if not. </value>
		protected bool RightClickAreaMouseovered
		{
			get
			{
				return Manager.MouseoveredRightClickable == this;
			}

			set
			{
				var manager = Manager;
				if(value)
				{
					manager.SetMouseoveredRightClickable(manager.MouseoveredInspector, this);
				}
				else if(manager.MouseoveredRightClickable == this)
				{
					manager.SetMouseoveredRightClickable(manager.MouseoveredInspector, null);
				}
			}
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public virtual bool DebugMode
		{
			get
			{
				return Inspector.State.DebugMode || (parent != null && parent.DebugMode);
			}
		}

		/// <summary> Gets a value indicating whether this object is full inspector width. </summary>
		/// <value> True if this object is full inspector width, false if not. </value>
		[JsonIgnore]
		protected bool IsFullInspectorWidth
		{
			get
			{
				return DrawGUI.IsFullInspectorWidth(Width);
			}
		}

		/// <summary> Gets or sets the mouse down position. </summary>
		/// <value> The mouse down position. </value>
		[JsonIgnore]
		protected Vector2 MouseDownPosition
		{
			get
			{
				return InspectorUtility.ActiveManager.MouseDownInfo.MouseDownPos;
			}
		}

		protected virtual IDrawerProvider DrawerProvider
		{
			get
			{
				return Inspector.DrawerProvider;
			}
		}

		/// <summary>
		/// Sets up the Drawer so that it is ready to be used.
		/// LateSetup should be called right after this.
		/// </summary>
		/// <param name="setParent"> Drawer whose member these Drawer. </param>
		/// <param name="setLabel"> The set label. </param>
		protected virtual void Setup([CanBeNull]IParentDrawer setParent, [CanBeNull]GUIContent setLabel)
		{
			controlId = GetUniqueControlId();
			parent = setParent;
			
			GUIContentPool.CopyOver(ref label, setLabel != null ? setLabel : GUIContentPool.Empty());
			OnLabelChanged();

			#if DEV_MODE && PI_ASSERTATIONS
			if(setLabel != null)
			{
				Debug.Assert(setLabel.image == label.image);
				Debug.Assert(string.Equals(setLabel.tooltip, label.tooltip, StringComparison.Ordinal));
				Debug.Assert(string.Equals(setLabel.text, label.text, StringComparison.Ordinal));
			}
			else
			{
				Debug.Assert(label != null);
				Debug.Assert(label == null || label.text.Length == 0);
			}
			Debug.Assert(inactive);
			#endif
		}
		
		/// <inheritdoc/>
		public virtual void LateSetup()
		{
			var filter = Inspector.State.filter;
			if(filter.HasFilterAffectingInspectedTargetContent)
			{
				OnFilterChanged(filter);
			}
			
			UpdateDataValidity(false);

			if(parent != null)
			{
				OnValueChanged += SendOnMemberValueChangedEventToParent;
			}
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inactive);
			#endif
			inactive = false;
		}

		/// <inheritdoc/>
		public virtual void OnParentAssigned(IParentDrawer newParent)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Assert(Array.IndexOf(newParent.Members, this) != -1, this, ".OnParentAssigned: index in members ", newParent.Members, " of parent ", newParent, " was -1");
			#endif
		}

		/// <summary> Sends an on member value changed event to parent. </summary>
		/// <param name="changed">The drawers that changed. </param>
		/// <param name="newvalue"> The newvalue. </param>
		private void SendOnMemberValueChangedEventToParent(IDrawer changed, object newvalue)
		{
			if(!inactive)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Assert(MemberInfo == null || !MemberInfo.MixedContent, this, ".SendOnMemberValueChangedEventToParent was called but subject had mixed content.");
				#endif
				parent.OnMemberValueChanged(Array.IndexOf(parent.Members, this), newvalue, MemberInfo);
			}
		}

		/// <inheritdoc/>
		public virtual bool Draw(Rect position)
		{
			switch(Event.current.type)
			{
				case EventType.Layout:
					OnLayoutEvent(position);
					break;
				case EventType.Repaint:
					break;
			}
			return DrawPrefix(PrefixLabelPosition);
		}

		/// <summary> get unique controlID which coupled with ToString()
		/// can be used for focusing the next field via DrawGUI.FocusControl. </summary>
		protected int GetUniqueControlId()
		{
			return Inspector.InspectorDrawer.IdProvider.Next();
		}

		/// <summary> This is called at the beginning of the Draw method whenever it's a layout event. </summary>
		/// <param name="position"> The position. </param>
		protected virtual void OnLayoutEvent(Rect position)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(position.width <= 0f){ Debug.LogError(Msg(ToString()+".OnLayoutEvent position ", position, " width <= 0f")); }
			#endif

			guiEnabled = GUI.enabled;

			GetDrawPositions(position);

			if(ShouldShowInInspector)
			{
				Inspector.State.HandleOnNextLayoutForVisibleDrawers(this);
			}
		}

		/// <summary> This is called at the beginning of the Draw method whenever it's a layout event. Here
		/// draw Rects for all interactive elements should be calculated and cached, and then Draw
		/// methods can use these cached values when drawing elements. Moreover, if one needs to test
		/// whether the cursor is over a specific Rect, it should also be done here, because then it's
		/// guaranteed that the cached draw positions and the cursor position are accessed within the
		/// same GUILayout context, and so the results will be accurate. </summary>
		/// <param name="position"> The position. </param>
		protected virtual void GetDrawPositions(Rect position)
		{
			lastDrawPosition = position;
			lastDrawPosition.height = Height;

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}
		
		/// <summary> Draw prefix. </summary>
		/// <param name="position"> The position. </param>
		/// <returns> True if it succeeds, false if it fails. </returns>
		public virtual bool DrawPrefix(Rect position) { return false; }

		/// <summary> Draw body. </summary>
		/// <param name="position"> The position. </param>
		/// <returns> True if it succeeds, false if it fails. </returns>
		public virtual bool DrawBody(Rect position) { return false; }

		/// <inheritdoc/>
		public virtual void OnMouseoverBeforeDraw() { }

		/// <inheritdoc/>
		public virtual void OnMouseover()
		{
			if(Inspector.Preferences.mouseoverEffects.prefixLabel)
			{
				DrawGUI.DrawLeftClickAreaMouseoverEffect(ClickToSelectArea, localDrawAreaOffset);
			}
		}


		/// <inheritdoc/>
		public bool BeingAnimated()
		{
			if(parent == null)
			{
				return false;
			}
			if(parent.Unfoldedness < 1f && parent.Unfoldedness > 0f)
			{
				return true;
			}
			return parent.BeingAnimated();
		}


		/// <inheritdoc/>
		public virtual void DrawFilterHighlight(SearchFilter filter, Color color) { }

		/// <inheritdoc/>
		public virtual void OnMouseoverDuringDrag(MouseDownInfo mouseDownInfo, Object[] dragAndDropObjectReferences)
		{
			// Don't do DragAndDropVisualMode.Rejected during click events, only if cursor has moved.
			if(mouseDownInfo.CursorMovedAfterMouseDown)
			{
				DrawGUI.Active.DragAndDropVisualMode = DragAndDropVisualMode.Rejected;
			}
		}

		/// <inheritdoc/>
		public virtual void OnRightClickAreaMouseover()
		{
			#if ENABLE_RIGHT_CLICK_AREA_MOUSEOVER_EFFECT
			if(DrawGUI.IsDrag)
			{
				return;
			}

			DrawGUI.RightClickAreaDrawMouseoverEffect(RightClickArea);
			#endif
		}

		/// <inheritdoc/>
		public virtual void DrawSelectionRect() { }

		/// <inheritdoc/>
		public virtual float GetOptimalPrefixLabelWidth(int indentLevel)
		{
			#if DEV_MODE && DEBUG_GET_OPTIMAL_PREFIX_LABEL_WIDTH
			Debug.Log(GetType().Name + ".GetOptimalPrefixLabelWidth(" + indentLevel + "): MinAutoSizedPrefixLabelWidth "+ DrawGUI.MinAutoSizedPrefixLabelWidth);
			#endif
			return 0f;
		}

		/// <inheritdoc/>
		public float GetOptimalPrefixLabelWidth(int indentLevel, bool sanitize)
		{
			float result = GetOptimalPrefixLabelWidth(indentLevel);

			#if DEV_MODE && DEBUG_GET_OPTIMAL_PREFIX_LABEL_WIDTH
			Debug.Log(ToString() + " result: " + result + " with sanitize: " + sanitize);
			#endif

			if(result <= DrawGUI.MinAutoSizedPrefixLabelWidth)
			{
				if(sanitize)
				{
					return Screen.width * 0.3f;
				}

				return DrawGUI.MinAutoSizedPrefixLabelWidth;
			}
			else if(result > DrawGUI.MaxAutoSizedPrefixLabelWidth)
			{
				return DrawGUI.MaxAutoSizedPrefixLabelWidth;
			}

			return result;
		}

		#if DEV_MODE && PI_ASSERTATIONS
		/// <summary> Determines if we can assert data is valid. </summary>
		/// <returns> True if it succeeds, false if it fails. </returns>
		protected virtual bool AssertDataIsValid()
		{
			if(!DataIsValid)
			{
				Debug.LogError(GetType().Name + " - Invalid data detected!");
				return false;
			}
			return true;
		}
		#endif

		/// <inheritdoc/>
		public virtual bool SetValue(object setValue)
		{
			#if DEV_MODE
			Debug.LogError("SetValue is not supported for "+ToString());
			#endif
			return false;
		}

		/// <inheritdoc/>
		public virtual bool SetValue(object setValue, bool applyToField, bool updateMembers)
		{
			#if DEV_MODE
			Debug.LogError("SetValue is not supported for "+ToString());
			#endif
			return false;
		}

		/// <inheritdoc/>
		public virtual object GetValue()
		{
			return GetValue(0);
		}

		/// <inheritdoc/>
		public virtual object GetValue(int index)
		{
			#if DEV_MODE
			Debug.LogError("GetValue is not supported for " + ToString());
			#endif
			return null;
		}

		/// <inheritdoc/>
		public virtual object[] GetValues()
		{
			#if DEV_MODE
			Debug.LogError("GetValues is not supported for " + ToString());
			#endif
			return ArrayPool<object>.ZeroSizeArray;
		}

		/// <inheritdoc/>
		public virtual void UpdateCachedValuesFromFieldsRecursively() {	}

		/// <inheritdoc/>
		public virtual void OnFilterChanged(SearchFilter filter)
		{
			lastPassedFilterTestType = FilterTestType.None;

			if(!filter.HasFilterAffectingInspectedTargetContent)
			{
				passedLastFilterCheck = true;
			}
			else if(PassesSearchFilter(filter))
			{
				passedLastFilterCheck = true;
				if(lastPassedFilterTestType == FilterTestType.None)
				{
					lastPassedFilterTestType = FilterTestType.Indetermined;
				}
			}
			else
			{
				passedLastFilterCheck = false;
			}

			#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
			Debug.Log(StringUtils.ToColorizedString(ToString()+".passedLastFilterCheck = ", passedLastFilterCheck, " with lastPassedFilterTestType=", lastPassedFilterTestType));
			#endif
		}

		/// <summary>
		/// Does this specific Instruction pass the given search filter? Should not consider
		/// child drawers, only self. This is used to set passedLastFilterCheck value when the
		/// OnFilterChanged event is fired.
		/// </summary>
		/// <param name="filter"> Specifies the filter. </param>
		/// <returns> True if it succeeds, false if it fails. </returns>
		public virtual bool PassesSearchFilter(SearchFilter filter)
		{
			#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
			if(filter.HasFilter) { Debug.Log(ToString()+".PassesSearchFilter(\""+filter.RawInput+"\"): "+StringUtils.False+" (default)"); }
			#endif

			//drawers are hidden by default when any filter has been entered
			if(filter.HasFilterAffectingInspectedTargetContent)
			{
				return false;
			}

			lastPassedFilterTestType = FilterTestType.None;
			return true;
		}
		
		/// <summary> Deselects the given reason. </summary>
		/// <param name="reason"> The reason. </param>
		public void Deselect(ReasonSelectionChanged reason)
		{
			var manager = Manager;
			if(manager != null && manager.FocusedDrawer == this)
			{
				// active inspector might be null when this gets called from the Dispose method when an inspector view
				// is being closed, which is why we need to handle that case.
				var inspector = InspectorUtility.ActiveInspector;
				if(inspector != null)
				{
					manager.Select(inspector, manager.SelectedInspectorPart, null, reason);
				}
				else
				{
					var inspectorDrawer = InspectorUtility.ActiveInspectorDrawer;
					if(inspectorDrawer == null)
					{
						manager.Select(null, InspectorPart.None, null, reason);
					}
					else
					{
						manager.Select(inspectorDrawer.MainView, InspectorPart.Viewport, null, reason);
					}
				}
			}
		}

		/// <inheritdoc/>
		public void OnDeselected(ReasonSelectionChanged reason, IDrawer losingFocusTo)
		{
			OnDeselectedInternal(reason, losingFocusTo);
			if(onLostFocus != null)
			{
				onLostFocus(this);
			}
			OnValidate();
		}

		/// <inheritdoc/>
		public void Select(ReasonSelectionChanged reason)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(!Selectable) { Debug.LogError(ToString() + ".Select called for drawer that is not selectable: "+this); }
			#endif

			Inspector.Select(this, reason);
		}

		/// <inheritdoc/>
		public void OnSelected(ReasonSelectionChanged reason, IDrawer previous, bool isMultiSelection)
		{
			var documentationUrl = DocumentationPageUrl;
			if(documentationUrl.Length > 0)
			{
				PowerInspectorDocumentation.ShowUrlIfWindowOpen(documentationUrl);
			}

			OnSelectedInternal(reason, previous, isMultiSelection);
			OnValidate();
		}

		/// <summary> Called after the drawers was deselected after being selected. </summary>
		/// <param name="reason"> The reason why the drawers was deselected. </param>
		/// <param name="losingFocusTo"> The drawers which are gaining focus (if any). </param>
		protected virtual void OnDeselectedInternal(ReasonSelectionChanged reason, [CanBeNull]IDrawer losingFocusTo) { }

		/// <summary> Called after the drawers was selected. </summary>
		/// <param name="reason"> The reason why the drawers was eselected. </param>
		/// <param name="previous"> Drawer which were previously focused but lost focus in the process of this control becoming selected. </param>
		/// <param name="isMultiSelection"> True if these drawers are one of multiple selected controls, false if the only selected control. </param>
		protected virtual void OnSelectedInternal(ReasonSelectionChanged reason, IDrawer previous, bool isMultiSelection)
		{
			#if DEV_MODE && DEBUG_ON_SELECTED
			Debug.Log(StringUtils.ToColorizedString("OnSelectedInternal, ", reason, ", previous=", previous, ", isMultiSelection=", isMultiSelection));
			#endif

			switch(reason)
			{
				case ReasonSelectionChanged.SelectControlUp:
				case ReasonSelectionChanged.SelectControlLeft:
				case ReasonSelectionChanged.SelectControlDown:
				case ReasonSelectionChanged.SelectControlRight:
				case ReasonSelectionChanged.SelectNextControl:
				case ReasonSelectionChanged.SelectPrevControl:
				case ReasonSelectionChanged.KeyPressOther:
				case ReasonSelectionChanged.KeyPressShortcut:
					DrawGUI.EditingTextField = false;
					KeyboardControlUtility.SetKeyboardControl(0, 3);
					ScrollToShow();
					return;
				case ReasonSelectionChanged.ControlClicked:
					if(ClearKeyboardControlWhenControlClicked())
					{
						KeyboardControlUtility.KeyboardControl = 0;
						DrawGUI.EditingTextField = false;
					}
					return;
				case ReasonSelectionChanged.ThisClicked:
					if(ClearKeyboardControlWhenThisClicked())
					{
						if(GUIUtility.hotControl == 0)
						{
							KeyboardControlUtility.KeyboardControl = 0;
						}
						DrawGUI.EditingTextField = false;
					}
					return;
				case ReasonSelectionChanged.PrefixClicked:
					if(ClearKeyboardControlWhenPrefixClicked())
					{
						if(GUIUtility.hotControl == 0)
						{
							KeyboardControlUtility.KeyboardControl = 0;
						}
						DrawGUI.EditingTextField = false;
					}
					return;
				default:
					DrawGUI.EditingTextField = false;
					if(GUIUtility.hotControl == 0)
					{
						KeyboardControlUtility.KeyboardControl = 0;
					}
					return;
			}
		}
		
		/// <inheritdoc/>
		public virtual IDrawer GetNextSelectableDrawerUp(int column, IDrawer requester)
		{
			if(requester == this || !Selectable)
			{
				#if DEV_MODE || SAFE_MODE
				if(parent == null)
				{
					#if DEV_MODE
					Debug.LogError(ToString()+ ".GetNextSelectableDrawerUp(" + StringUtils.ToString(requester)+") parent was null - selecting self!");
					#endif
					return Selectable ? null : this;
				}
				#endif

				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("\""+Name+"\" - "+GetType().Name + ".GetNextSelectableDrawerUp("+column+", "+requester.Name+"): parent (because requester was self)");
				#endif
				return parent.GetNextSelectableDrawerUp(column, this);
			}
			#if DEV_MODE && DEBUG_NEXT_FIELD
			Debug.Log("\""+Name+"\" - "+GetType().Name + ".GetNextSelectableDrawerUp("+column+", "+requester.Name+"): this (because requester not self)");
			#endif
			return this;
		}

		/// <inheritdoc/>
		public virtual IDrawer GetNextSelectableDrawerDown(int column, IDrawer requester)
		{
			if(requester == this || !Selectable)
			{
				#if DEV_MODE || SAFE_MODE
				if(parent == null)
				{
					#if DEV_MODE
					Debug.LogError(ToString()+ ".GetNextSelectableDrawerDown(" + StringUtils.ToString(requester)+") parent was null - selecting self!");
					#endif
					return Selectable ? null : this;
				}
				#endif

				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("\""+Name+"\" - "+GetType().Name + ".GetNextSelectableDrawerDown("+column+", "+requester.Name+"): parent (because requester was self)");
				#endif
				return parent.GetNextSelectableDrawerDown(column, this);
			}

			#if DEV_MODE && DEBUG_NEXT_FIELD
			Debug.Log("\""+Name+"\" - "+GetType().Name + ".GetNextSelectableDrawerDown("+column+", "+requester.Name+"): this (because requester not self)");
			#endif

			return this;
		}

		/// <inheritdoc/>
		public virtual IDrawer GetNextSelectableDrawerLeft(bool moveToNextControlAfterReachingEnd, IDrawer requester)
		{
			#if DEV_MODE && DEBUG_NEXT_FIELD
			Debug.Log("GetNextSelectableDrawerLeft(" + moveToNextControlAfterReachingEnd+", "+ requester + ") with FocusedDrawer="+StringUtils.ToString(Manager.FocusedControl));
			#endif

			//really use focused control and not requester?
			if(Manager.FocusedDrawer == this || !Selectable)
			{
				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("\""+Name+"\" - "+GetType().Name + ".GetNextSelectableDrawerLeft("+moveToNextControlAfterReachingEnd+"): calling rersively in parent");
				#endif

				#if DEV_MODE || SAFE_MODE
				if(parent == null)
				{
					#if DEV_MODE
					Debug.LogError(ToString()+ ".GetNextSelectableDrawerLeft(" + StringUtils.ToString(requester)+") parent was null!");
					#endif
					return this;
				}
				#endif

				return parent.GetNextSelectableDrawerLeft(moveToNextControlAfterReachingEnd, this);
			}
			#if DEV_MODE && DEBUG_NEXT_FIELD
			Debug.Log("\""+Name+"\" - "+GetType().Name + ".GetNextSelectableDrawerLeft("+moveToNextControlAfterReachingEnd+"): this");
			#endif

			return this;
		}

		/// <inheritdoc/>
		public virtual IDrawer GetNextSelectableDrawerRight(bool moveToNextControlAfterReachingEnd, IDrawer requester)
		{
			//really use focused control and not requester?
			if(Manager.FocusedDrawer == this || !Selectable)
			{
				#if DEV_MODE && DEBUG_NEXT_FIELD
				Debug.Log("\""+Name+"\" - "+GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): calling rersively in parent");
				#endif

				#if DEV_MODE || SAFE_MODE
				if(parent == null)
				{
					#if DEV_MODE
					Debug.LogError(ToString()+".GetNextSelectableDrawerRight("+StringUtils.ToString(requester)+") parent was null!");
					#endif
					return this;
				}
				#endif

				return parent.GetNextSelectableDrawerRight(moveToNextControlAfterReachingEnd, this);
			}
			#if DEV_MODE && DEBUG_NEXT_FIELD
			Debug.Log("\""+Name+"\" - "+GetType().Name + ".GetNextSelectableDrawerRight("+moveToNextControlAfterReachingEnd+"): this");
			#endif
			return this;
		}

		/// <summary> Gets a value indicating whether we can reset. </summary>
		/// <value> True if we can reset, false if not. </value>
		protected bool CanReset
		{
			get
			{
				return !ReadOnly || overrideReset != null;
			}
		}

		public void Reset()
		{
			Reset(false);
		}

		public void ResetWithMessage()
		{
			Reset(true);
		}

		/// <inheritdoc/>
		public void Reset(bool messageUser)
		{
			InspectorUtility.OnResettingFieldValue(this);

			if(overrideReset != null)
			{
				overrideReset(this);
				return;
			}

			#if SAFE_MODE || DEV_MODE
			if(ReadOnly)
			{
				#if DEV_MODE
				Debug.LogWarning("Reset disabled for "+ToString()+" because ReadOnly");
				#endif
				return;
			}
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(CanReset);
			#endif

			DoReset();

			GUI.changed = true;

			if(messageUser)
			{
				SendResetMessage();
			}
		}

		/// <summary> Resets field value. </summary>
		protected virtual void DoReset()
		{
			SetValue(DefaultValue());
		}

		/// <inheritdoc/>
		public virtual string ValueToStringForFiltering()
		{
			return null;
		}

		/// <param name="mousePosition"></param>
		/// <inheritdoc/>
		public virtual bool DetectMouseover(Vector2 mousePosition)
		{
			if(!Clickable)
			{
				#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
				Debug.Log(StringUtils.ToColorizedString(ToString()+ ".DetectMouseover: ", false, " (because !Clickable)"));
				#endif
				return false;
			}

			#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
			Debug.Log(StringUtils.ToColorizedString(ToString()+ ".DetectMouseover: ", ClickToSelectArea.Contains(mousePosition)));
			#endif

			if(!ClickToSelectArea.Contains(mousePosition))
			{
				#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
				Debug.Log(StringUtils.ToColorizedString(ToString()+ ".DetectMouseover: ", false, " with ClickToSelectArea=", ClickToSelectArea, ", mousePosition=", mousePosition));
				#endif

				return false;
			}

			if(!PrefixResizingEnabledOverControl)
			{
				var unityObject = UnityObjectDrawer;
				if(unityObject != null && unityObject.PrefixResizerMouseovered)
				{
					#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
					Debug.LogWarning(StringUtils.ToColorizedString(ToString()+ ".DetectMouseover: ", false, " ignoring detected mouseover because PrefixResizerMouseovered=", true));
					#endif

					return false;
				}
			}

			#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
			Debug.LogWarning(StringUtils.ToColorizedString(ToString()+ ".DetectMouseover: ", true, " with ClickToSelectArea=", ClickToSelectArea, ", mousePosition=", mousePosition));
			#endif

			return true;
		}

		/// <param name="mousePosition"></param>
		/// <inheritdoc/>
		public virtual bool DetectMouseoverForSelfAndChildren(Vector2 mousePosition)
		{
			if(!Clickable)
			{
				#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
				Debug.Log(StringUtils.ToColorizedString(ToString()+".DetectMouseoverForSelfAndChildren: ", false, " (because !Clickable)"));
				#endif
				return false;
			}

			#if DEV_MODE && DEBUG_MOUSEOVER_DETECTION
			Debug.Log(StringUtils.ToColorizedString(ToString()+".DetectMouseoverForSelfAndChildren: ", ClickToSelectArea.Contains(mousePosition), " with ClickToSelectArea=", ClickToSelectArea, ", mousePosition=", mousePosition));
			#endif

			return ClickToSelectArea.Contains(mousePosition);
		}

		/// <param name="mousePosition"></param>
		/// <inheritdoc/>
		public virtual bool DetectRightClickAreaMouseover(Vector2 mousePosition)
		{
			return RightClickArea.MouseIsOver() && !Inspector.IsOutsideViewport(Bounds);
		}

		/// <inheritdoc/>
		public virtual void OnMouseoverEnter(Event inputEvent, bool isDrag)
		{
			Mouseovered = true;
		}

		/// <inheritdoc/>
		public virtual void OnMouseoverExit(Event inputEvent)
		{
			Mouseovered = false;
		}

		/// <inheritdoc/>
		public virtual bool OnClick(Event inputEvent)
		{
			HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ThisClicked);
			return false;
		}

		/// <inheritdoc/>
		public virtual bool OnDoubleClick(Event inputEvent)
		{
			return false;
		}

		/// <summary> Resets the on double click. </summary>
		/// <returns> True if it succeeds, false if it fails. </returns>
		protected virtual bool ResetOnDoubleClick()
		{
			return InspectorUtility.Preferences.doubleClickPrefixToReset && CanReset;
		}

		protected virtual bool IsMultiSelectable
		{
			get
			{
				return IsReorderable;
			}
		}

		/// <summary> Handles selection of drawers during during an OnClick call. </summary>
		/// <param name="inputEvent"> The input event. </param>
		/// <param name="reason"> Information about which part of drawers were clicked for this to get called. </param>
		public void HandleOnClickSelection(Event inputEvent, ReasonSelectionChanged reason)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			//it shouldn't even be possible to click unselectable drawers
			Assert(Clickable, this, ".OnClick was called but Clickable was false");
			#endif

			if(Selectable)
			{
				bool multiSelectable = IsMultiSelectable;
				if(inputEvent.control && multiSelectable)
				{
					if(Selected)
					{
						Inspector.RemoveFromSelection(this, reason);
					}
					else
					{
						Inspector.AddToSelection(this, reason);
					}
				}
				else if(inputEvent.shift && multiSelectable)
				{
					var focusedControl = Inspector.Manager.FocusedDrawer;
					if(focusedControl == this || focusedControl == null || focusedControl.Parent != parent)
					{
						//select only the clicked target
						Select(reason);
					}
					else if(parent == null)
					{
						//select only the clicked target
						Select(reason);
					}
					else
					{
						var members = parent.VisibleMembers;
						int focusedIndex = Array.IndexOf(members, focusedControl);
						if(focusedIndex != -1)
						{
							int myIndex = Array.IndexOf(members, this);
							int min = Mathf.Min(myIndex, focusedIndex);
							int max = Mathf.Max(myIndex, focusedIndex);

							//select all targets from focused control up to (and including) this control
							for(int n = min; n <= max; n++)
							{
								Inspector.AddToSelection(members[n], reason);
							}
						}
						else
						{
							//select only the clicked target
							Select(reason);
						}
					}
				}
				else
				{
					Select(reason);
				}
			}
			else
			{
				#if DEV_MODE
				Debug.Log("HandleOnClickSelection: passing selection through to parent because Selectable was false");
				#endif
				var passThrough = parent as BaseDrawer;
				if(passThrough != null)
				{
					passThrough.HandleOnClickSelection(inputEvent, reason);
				}
			}
		}
		
		/// <inheritdoc/>
		public virtual bool OnRightClick(Event inputEvent)
		{
			if(ContextMenuUtility.MenuIsOpening)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+ "OnRightClick - ignoring because ContextMenuUtility.MenuIsOpening already true.");
				#endif
				return true;
			}

			HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ThisClicked);

			#if DEV_MODE && DEBUG_ON_RIGHT_CLICK
			Debug.Log(ToString()+".OnRightClick(inputEvent)");
			#endif

			return OpenContextMenu(inputEvent, null, false, MouseoveredPart);
		}

		/// <inheritdoc/>
		public bool OpenContextMenu(Event inputEvent, Rect? openAtPosition, bool isLocalPosition, Part subjectPart)
		{
			bool extendedMenu = (inputEvent != null && inputEvent.control) || DebugMode;

			var menu = Menu.Create();

			BuildContextMenu(ref menu, extendedMenu);

			#if DEV_MODE
			if(extendedMenu)
			{
				AddDevModeDebuggingEntriesToRightClickMenu(ref menu);
			}
			#endif

			if(menu == null || menu.Count == 0)
			{
				return false;
			}

			if(inputEvent != null)
			{
				DrawGUI.Use(inputEvent);
			}
			
			if(Selectable && !Selected)
			{
				Select(ReasonSelectionChanged.ThisClicked);
			}

			// to do: expose as optional parameter if necessary
			var onMenuClosed = Selectable ? ContextMenuUtility.SelectLastContextMenuSubject : null as Action<object>;

			if(openAtPosition.HasValue)
			{
				var openAt = openAtPosition.Value;
				if(!isLocalPosition)
				{
					var offset = GUISpace.ConvertPoint(localDrawAreaOffset, Space.Screen, Space.Local);
					openAt.x += offset.x;
					openAt.y += offset.y;
				}
				ContextMenuUtility.OpenAt(menu, openAt, true, Inspector, InspectorPart.Viewport, this, subjectPart, onMenuClosed);
			}
			else
			{
				ContextMenuUtility.Open(menu, true, Inspector, InspectorPart.Viewport, this, subjectPart, onMenuClosed);
			}

			return true;
		}

		/// <summary>
		/// Builds the context menu that should be shown when the control is right clicked.
		/// </summary>
		/// <param name="menu"> Menu into which the built items should be added. </param>
		/// <param name="extendedMenu">
		/// Add advanced items to the menu? Some more rarely used items are only added to the
		/// context menu in extended mode, to reduce clutter.
		/// </param>
		protected virtual void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			var inspector = Inspector;
			if(inspector != null && inspector.HasFilterAffectingInspectedTargetContent)
			{
				menu.AddSeparatorIfNotRedundant();
				menu.Add("Scroll To Show", ClearFilterAndScrollToShow);
			}

			#if DEV_MODE // still WIP
			if(UnityObjects.Length > 1 && inspector != null && UserSettings.MergedMultiEditMode)
			{
				menu.AddSeparatorIfNotRedundant();
				menu.Add("View In Stacked Mode", ViewInStackedMode);
			}
			#endif

			if(extendedMenu)
			{
				var type = Type;
				if(type.IsGenericType)
				{
					#if DEV_MODE
					Debug.Log("type.ToString(): "+type+ "\r\nGetGenericTypeDefinition().ToString(): "+ type.GetGenericTypeDefinition()+ "\r\nToStringSansNamespace(type): " + StringUtils.ToStringSansNamespace(type) + "\r\nToStringSansNamespace(GetGenericTypeDefinition()): " + StringUtils.ToStringSansNamespace(type.GetGenericTypeDefinition()));
					#endif
					type = type.GetGenericTypeDefinition();
				}

				if(!(this is ClassDrawer))
				{
					if(type == Types.MonoScript)
					{
						var monoScript = GetValue() as MonoScript;
						if(monoScript != null)
						{
							var classType = monoScript.GetClass();
							if(classType != null)
							{
								type = classType;
							}
						}
					}

					menu.AddSeparatorIfNotRedundant();

					menu.Add("Inspect "+StringUtils.ToStringSansNamespace(type)+" Static Members", ()=>Inspector.RebuildDrawers(null, type));
				}
			}
		}
		
		/// <summary>
		/// Displays only this drawer in stacked multi-editing mode.
		/// </summary>
		protected virtual void ViewInStackedMode()
		{
			Inspector.SetFilter("l:\""+FullClassName+"\"");
			UserSettings.MergedMultiEditMode = false;
			GUI.changed = true;
			ExitGUIUtility.ExitGUI();
		}

		protected bool HasOnValueChangedBreakPoint()
		{
			if(onValueChanged == null)
			{
				return false;
			}
			
			var list = onValueChanged.GetInvocationList();
			for(int n = list.Length - 1; n >= 0; n--)
			{
				var method = list[n].Method;
				if(method != null && string.Equals(method.Name, "OnValueChangedBreakPoint"))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Adds debugging entries meant for developers only to opening right click menu.
		/// Invoked when control is right clicked with DEV_MODE preprocessor directive
		/// and the control key is held down or  or Debug Mode+ is enabled.
		/// </summary>
		/// <param name="menu"> [in,out] The opening menu into which to add the menu items. </param>
		protected virtual void AddDevModeDebuggingEntriesToRightClickMenu(ref Menu menu)
		{
			#if DEV_MODE
			string scriptFilename = FileUtility.FilenameFromType(GetType());
			menu.Add("Debugging/Edit "+scriptFilename+".cs", ()=>
			{
				var script = FileUtility.FindScriptFile(GetType());
				if(script != null)
				{
					UnityEditor.AssetDatabase.OpenAsset(script);
				}
				else
				{
					Debug.LogError("FileUtility.FindScriptFilepath could not find file "+scriptFilename+".cs");
				}
			});
			menu.Add("Debugging/Print Info", PrintDevInfo);
			menu.Add("Debugging/Print Full State", PrintFullStateForDevs);
			menu.Add("Debugging/Print ControlID", ()=>Debug.Log(GetType().Name + " ControlID: "+controlId));
			#endif
		}

		#if DEV_MODE
		/// <summary>
		/// Prints information about the drawers for developers to use in debugging.
		/// Can be invoked via the right click menu with DEV_MODE preprocessor directive
		/// and the control key is held down.
		/// </summary>
		private void PrintDevInfo()
		{
			Debug.Log(StringUtils.ToColorizedString(GetDevInfo()));
		}

		/// <summary>
		/// Gets information about the drawers that might be useful for developers when debugging
		/// in object array format, where all elements will be converted to string format and joined together.
		/// </summary>
		protected virtual object[] GetDevInfo()
		{
			if(Selected)
			{
				return new object[] { GetType(), ": Type=", Type, ", Name=\"", Name, "\", Inactive=", Inactive, ", controlId=", controlId, ", KeyboardControl=", KeyboardControlUtility.KeyboardControl };
			}
			return new object[]{ GetType(), ": Type=", Type, ", Name=\"", Name, "\", Inactive=", Inactive };
		}

		private void PrintFullStateForDevs()
		{
			DebugUtility.PrintFullStateInfo(this);
		}
		#endif
		
		/// <inheritdoc/>
		public virtual void OnMiddleClick(Event inputEvent)
		{
			var inspector = Inspector;
			if(inspector != null && inspector.HasFilterAffectingInspectedTargetContent)
			{
				ClearFilterAndScrollToShow();
				DrawGUI.Use(inputEvent);
			}
		}

		/// <inheritdoc/>
		public virtual void OnDrag(Event inputEvent) { }

		/// <inheritdoc/>
		public virtual void OnMouseUpAfterDownOverControl(Event inputEvent, bool isClick)
		{

		}

		/// <inheritdoc/>
		public virtual void Duplicate()
		{
			throw new NotSupportedException("Duplicate command is not supported for drawers of type "+StringUtils.ToString(GetType()));
		}

		/// <inheritdoc/>
		public virtual bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			if(keys.prevOfType.DetectAndUseInput(inputEvent))
			{
				SelectPreviousOfType();
				return true;
			}
			
			if(keys.nextOfType.DetectAndUseInput(inputEvent))
			{
				SelectNextOfType();
				return true;
			}

			if(keys.prevComponent.DetectAndUseInput(inputEvent))
			{
				SelectPreviousComponent();
				return true;
			}

			if(keys.nextComponent.DetectAndUseInput(inputEvent))
			{
				SelectNextComponent();
				return true;
			}

			if(keys.activate.DetectInput(inputEvent))
			{
				if(OnKeyboardActivate(inputEvent))
				{
					if(inputEvent.type != EventType.Used)
					{
						DrawGUI.Use(inputEvent);
						return true;
					}
				}
				return false;
			}

			if(keys.scrollToTop.DetectAndUseInput(inputEvent))
			{
				// Find top-most selectable Drawer by traversing up the parent-chain
				IDrawer select = this;
				IDrawer selectParent = select.Parent;
				while(selectParent != null)
				{
					if(!selectParent.Selectable)
					{
						break;
					}
					select = selectParent;
					selectParent = selectParent.Parent;
				}
				
				select.Select(ReasonSelectionChanged.KeyPressShortcut);
				Inspector.ScrollToShow(select);
				return true;
			}

			if(keys.scrollToBottom.DetectAndUseInput(inputEvent))
			{
				// Starting from the last top-level inspected target
				// traverse down members until last one is found.
				var inspected = Inspector.State.drawers;
				var select = inspected[inspected.Length - 1];
				var selectAsParent = select as IParentDrawer;
				while(selectAsParent != null)
				{
					var members = selectAsParent.VisibleMembers;
					if(members.Length == 0)
					{
						break;
					}

					IDrawer member = null;
					for(int n = members.Length - 1; n >= 0; n--)
					{
						var test = members[n];
						if(test.Selectable)
						{
							member = test;
							break;
						}
					}
					
					if(member == null)
					{
						break;
					}

					select = member;
					selectAsParent = select as IParentDrawer;
				}
				
				select.Select(ReasonSelectionChanged.KeyPressShortcut);
				Inspector.ScrollToShow(select);
				return true;
			}


			// It's important not to use keyboard focus altering events when override field focusing is false.
			// Otherwise Unity cannot handle the focus changes internally.
			bool overrideFieldFocusing = OverrideFieldFocusing();

			if(keys.nextFieldUp.DetectInput(inputEvent, overrideFieldFocusing))
			{
				SelectNextFieldUp(GetSelectedRowIndex());
				return true;
			}

			if(keys.nextFieldLeft.DetectInput(inputEvent, overrideFieldFocusing))
			{
				SelectNextFieldLeft(false);
				return true;
			}

			if(keys.nextFieldDown.DetectInput(inputEvent, overrideFieldFocusing))
			{
				SelectNextFieldDown(GetSelectedRowIndex());
				return true;
			}

			if(keys.nextFieldRight.DetectInput(inputEvent, overrideFieldFocusing))
			{
				SelectNextFieldRight(false);
				return true;
			}
			
			if(keys.DetectNextField(inputEvent, overrideFieldFocusing))
			{
				SelectNextField();
				return true;
			}

			if(keys.DetectPreviousField(inputEvent, overrideFieldFocusing))
			{
				SelectPreviousField();
				return true;
			}

			if(keys.reset.DetectAndUseInput(inputEvent))
			{
				if(CanReset)
				{
					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					Reset();
					SendResetMessage();
					return true;
				}
				return false;
			}

			if(keys.randomize.DetectAndUseInput(inputEvent))
			{
				if(!ReadOnly)
				{
					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					Randomize();
					return true;
				}
				return false;
			}
			
			switch(inputEvent.keyCode)
			{
				case KeyCode.UpArrow:
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						DrawGUI.Use(inputEvent);
						SelectNextFieldUp(GetSelectedRowIndex());
						return true;
					}
					return false;
				case KeyCode.DownArrow:
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						DrawGUI.Use(inputEvent);
						SelectNextFieldDown(GetSelectedRowIndex());
						return true;
					}
					return false;
				case KeyCode.LeftArrow:
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						DrawGUI.Use(inputEvent);
						SelectNextFieldLeft();
						return true;
					}
					return false;
				case KeyCode.RightArrow:
					#if DEV_MODE
					Debug.Log(StringUtils.ToColorizedString(ToString(), " RightArrow with modifiers=", StringUtils.ToString(inputEvent.modifiers)));
					#endif
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						DrawGUI.Use(inputEvent);
						SelectNextFieldRight();
						return true;
					}
					return false;
			}

			return false;
		}

		/// <summary> Handle activate keyboard input being given for this drawer when it is selected. </summary>
		protected virtual bool OnKeyboardActivate(Event inputEvent)
		{
			return false;
		}

		/// <summary> Select previous field. </summary>
		protected void SelectPreviousField()
		{
			SelectNextFieldLeft(true);
		}

		/// <summary> Select next field left. </summary>
		private void SelectNextFieldLeft()
		{
			SelectNextFieldLeft(false);
		}

		/// <summary> Select next field left. </summary>
		/// <param name="moveToNextControlAfterReachingEnd">
		/// If false and left-most field on this row is already selected, then selection won't change.
		/// Otherwise will keep moving to find next control, even if it's on another row. </param>
		/// <param name="additive">
		/// (Optional) add next field to selection instead of replacing previously selected item(s)? </param>
		protected virtual void SelectNextFieldLeft(bool moveToNextControlAfterReachingEnd, bool additive = false)
		{
			var inspector = Inspector;
			var manager = inspector.InspectorDrawer.Manager;

			var reason = moveToNextControlAfterReachingEnd ? ReasonSelectionChanged.SelectPrevControl : ReasonSelectionChanged.SelectControlLeft;

			var select = GetNextSelectableDrawerLeft(moveToNextControlAfterReachingEnd, this);
			if(moveToNextControlAfterReachingEnd)
			{
				OnNextLayout(()=>
				{
					OnNextLayout(() => inspector.Select(select, reason));
				});
			}
			else if(select != null && select != this)
			{
				if(additive)
				{
					if(select.Parent != parent || !IsReorderable || !select.IsReorderable)
					{
						return;
					}

					if(manager.IsSelected(select))
					{
						OnNextLayout(()=>
						{
							manager.RemoveFromSelection(this, reason);
							manager.AddToSelection(select, reason);
						});
					}
					else
					{
						OnNextLayout(()=>manager.AddToSelection(select, reason));
					}
				}
				else
				{
					//OnNextLayout(()=>inspector.Select(select, reason));
					inspector.Select(select, reason);
					ExitGUIUtility.ExitGUI(); // new test
				}
			}
		}

		/// <summary> Select next field. </summary>
		protected void SelectNextField()
		{
			SelectNextFieldRight(true);
		}

		/// <summary> Select next field right. </summary>
		private void SelectNextFieldRight()
		{
			SelectNextFieldRight(false);
		}

		/// <summary> Select next field right. </summary>
		/// <param name="moveToNextControlAfterReachingEnd">
		/// True to move to next control after reaching end. </param>
		/// <param name="additive"> (Optional) True to additive. </param>
		protected virtual void SelectNextFieldRight(bool moveToNextControlAfterReachingEnd, bool additive = false)
		{
			var inspector = Inspector;
			var manager = inspector.InspectorDrawer.Manager;

			var select = GetNextSelectableDrawerRight(moveToNextControlAfterReachingEnd, this);
			var reason = moveToNextControlAfterReachingEnd ? ReasonSelectionChanged.SelectNextControl : ReasonSelectionChanged.SelectControlRight;

			if(additive)
			{
				if(select == this || select == null || select.Parent != parent || !IsReorderable || !select.IsReorderable)
				{
					return;
				}

				if(manager.IsSelected(select))
				{
					OnNextLayout(()=>
					{
						manager.RemoveFromSelection(this, reason);
						manager.AddToSelection(select, reason);
					});
				}
				else
				{
					OnNextLayout(()=>manager.AddToSelection(select, reason));
				}
			}
			else
			{
				if(!moveToNextControlAfterReachingEnd && (select == this || select == null))
				{
					return;
				}

				//OnNextLayout(()=>inspector.Select(select, reason));
				inspector.Select(select, reason);
				ExitGUIUtility.ExitGUI(); // new test
			}
		}

		/// <summary> Select next field up. </summary>
		/// <param name="column"> The column. </param>
		/// <param name="additive"> (Optional) True to additive. </param>
		protected virtual void SelectNextFieldUp(int column, bool additive = false)
		{
			var inspector = Inspector;
			var manager = inspector.InspectorDrawer.Manager;

			var select = GetNextSelectableDrawerUp(column, this);
			if(select != null && select != this)
			{
				if(additive)
				{
					if(select.Parent != parent || !IsReorderable || !select.IsReorderable)
					{
						return;
					}

					if(manager.IsSelected(select))
					{
						OnNextLayout(() =>
						{
							manager.RemoveFromSelection(this, ReasonSelectionChanged.SelectControlUp);
							manager.AddToSelection(select, ReasonSelectionChanged.SelectControlUp);
						});
					}
					else
					{
						OnNextLayout(() => manager.AddToSelection(select, ReasonSelectionChanged.SelectControlUp));
					}
				}
				else
				{
					//OnNextLayout(() => inspector.Select(select, ReasonSelectionChanged.SelectControlUp));
					inspector.Select(select, ReasonSelectionChanged.SelectControlUp);
					ExitGUIUtility.ExitGUI(); // new test
				}
			}
		}

		/// <summary> Select next field down. </summary>
		/// <param name="column"> The column. </param>
		/// <param name="additive"> (Optional) True to additive. </param>
		protected virtual void SelectNextFieldDown(int column, bool additive = false)
		{
			var inspector = Inspector;
			var manager = inspector.InspectorDrawer.Manager;

			var select = GetNextSelectableDrawerDown(column, this);
			if(select != null && select != this)
			{
				if(additive)
				{
					if(select.Parent != parent || !IsReorderable || !select.IsReorderable)
					{
						return;
					}

					if(manager.IsSelected(select))
					{
						OnNextLayout(() =>
						{
							manager.RemoveFromSelection(this, ReasonSelectionChanged.SelectControlDown);
							manager.AddToSelection(select, ReasonSelectionChanged.SelectControlDown);
						});
					}
					else
					{
						OnNextLayout(() => manager.AddToSelection(select, ReasonSelectionChanged.SelectControlDown));
					}
				}
				else
				{
					//OnNextLayout(() => inspector.Select(select, ReasonSelectionChanged.SelectControlDown));
					inspector.Select(select, ReasonSelectionChanged.SelectControlDown);
					ExitGUIUtility.ExitGUI(); // new test
				}
			}
		}

		/// <inheritdoc/>
		public virtual void SelectPreviousComponent()
		{
			if(parent != null)
			{
				var comp = parent as IComponentDrawer;
				if(comp != null)
				{
					Inspector.Select(parent, ReasonSelectionChanged.SelectPrevComponent);
				}
				else
				{
					parent.SelectPreviousComponent();
				}
			}
		}

		/// <inheritdoc/>
		public virtual void SelectNextComponent()
		{
			if(parent != null)
			{
				parent.SelectNextComponent();
			}
		}

		/// <inheritdoc/>
		public virtual void SelectPreviousOfType()
		{
			if(parent != null)
			{
				parent.SelectPreviousOfType();
			}
		}

		/// <inheritdoc/>
		public virtual void SelectNextOfType()
		{
			if(parent != null)
			{
				parent.SelectNextOfType();
			}
		}

		/// <summary> Called whenever anything related to the control is changed (value, selected state,
		/// folded state) </summary>
		protected virtual void OnValidate()
		{
			UpdateDataValidity(false);
		}

		/// <inheritdoc/>
		public virtual void ApplyInChildren(Action<IDrawer> action)
		{
			action(this);
		}

		/// <inheritdoc/>
		public virtual void ApplyInVisibleChildren(Action<IDrawer> action)
		{
			action(this);
		}

		/// <inheritdoc/>
		public virtual IDrawer TestChildrenUntilTrue(Func<IDrawer, bool> test)
		{
			return test(this) ? this : null;
		}

		/// <inheritdoc/>
		public virtual IDrawer TestVisibleChildrenUntilTrue(Func<IDrawer, bool> test)
		{
			return test(this) ? this : null;
		}

		/// <inheritdoc cref="IDrawer.Dispose" />
		public virtual void Dispose()
		{
			#if DEV_MODE && DEBUG_DISPOSE
			Debug.Log(ToString() +".Dispose()");
			#endif

			unchecked
			{
				instanceId++;
			}

			controlId = 0;
			inactive = true;
			onKeyboardInputBeingGiven = null;
			guiEnabled = true;

			Deselect(ReasonSelectionChanged.Dispose);
			
			var manager = Manager;
			var mouseDownInfo = manager.MouseDownInfo;
			if(mouseDownInfo.MouseDownOverDrawer == this)
			{
				mouseDownInfo.Clear();
			}
			
			controlId = 0;
			Mouseovered = false;
			RightClickAreaMouseovered = false;
			parent = null;
			passedLastFilterCheck = true;
			onValueChanged = null;
			onLostFocus = null;
			dataIsValid = true;
			overrideValidateValue = null;
			overrideReset = null;
			lastDrawPosition.y = -100f;
			lastDrawPosition.width = 0f;
			lastDrawPosition.height = 0f;
			lastPassedFilterTestType = FilterTestType.None;
			name = "";

			if(label != null)
			{
				GUIContentPool.Dispose(ref label);
			}
			DrawerPool.Pool(this);
		}

		/// <inheritdoc/>
		public virtual void CutToClipboard()
		{
			CutToClipboard(0);
		}

		/// <summary> Cuts field value from target by index to clipboard. </summary>
		/// <param name="index"> Zero-based index of the target. </param>
		public virtual void CutToClipboard(int index)
		{
			Clipboard.Cut(GetValue(index), MemberInfo);
			SendCopyToClipboardMessage();
		}

		/// <inheritdoc/>
		public virtual void CopyToClipboard()
		{
			CopyToClipboard(0);
		}

		/// <summary> Copies field value from target by index to clipboard. </summary>
		/// <param name="index"> Zero-based index of the target. </param>
		public virtual void CopyToClipboard(int index)
		{
			Clipboard.TryCopy(GetValue(index), Type);
			SendCopyToClipboardMessage();
		}

		/// <inheritdoc/>
		public virtual bool CanPasteFromClipboard()
		{
			return false;
		}

		/// <inheritdoc/>
		public void PasteFromClipboard()
		{
			#if SAFE_MODE || DEV_MODE
			if(!CanPasteFromClipboard())
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+".PasteFromClipboard() called but CanPasteFromClipboard()="+StringUtils.False+". Aborting...");
				#endif
				return;
			}
			#endif

			// If you're pasting into a text field, and it's currently selected
			// with editing text field true, the value won't get updated.
			var textField = this as ITextFieldDrawer;
			if(textField != null)
			{
				textField.StopEditingField();
			}
			else
			{
				DrawGUI.EditingTextField = false;
			}

			// this fixes some problems like when you paste on an array field with its resize field being selected,
			// the array won't update its contents. It could theoretically also work by only changing the selection
			// if the selected object is grandchild of this field, but then that would break consistency (sometimes
			// selection changes, sometimes not). It could also work by deselecting whatever was previously selected,
			// but that would be weird when the pasting is done via a keyboard shortcut (ctrl+V).
			Select(ReasonSelectionChanged.Unknown);

			var targets = UnityObjects;
			if(targets.Length > 0)
			{
				UndoHandler.RegisterUndoableAction(targets, "Paste From Clipboard");
			}
			
			DoPasteFromClipboard();
			
			SendPasteFromClipboardMessage();
		}

		/// <inheritdoc/>
		public virtual string GetFieldNameForMessages()
		{
			string name = Name;

			if(name.Length == 0)
			{
				// If drawer has no name, use parent name, since some drawers consist of multiple parts, some of which might have no name.
				if(parent != null)
				{
					return parent.Name;
				}

				// If has no parent either generate display name from class type.
				name = GetType().Name;
				if(name.EndsWith("Drawer", StringComparison.Ordinal))
				{
					name = name.Substring(name.Length, name.Length - "Drawer".Length);
				}
				name = StringUtils.SplitPascalCaseToWords(name);
			}
			return name;
		}

		/// <summary> Pastes value from clipboard to field. </summary>
		protected virtual void DoPasteFromClipboard()
		{
			SetValue(Clipboard.Paste(Type));
		}

		/// <summary> Gets randomize message. </summary>
		/// <returns> The randomize message. </returns>
		protected virtual string GetRandomizeMessage()
		{
			return "{0}value was randomized.";
		}

		/// <summary> Sends message to user about randomization of drawer values having succesfully taken place. </summary>
		private void SendRandomizeMessage()
		{
			string name = GetFieldNameForMessages();
			string message = GetRandomizeMessage();
			if(name.Length > 0)
			{
				message = string.Format(message, StringUtils.Concat("\"", name, "\" "));
				
			}
			else
			{
				message = string.Format(message, "");
				char firstLetter = message[0];
				firstLetter = char.ToUpper(firstLetter);
				message = firstLetter + message.Substring(1);
			}
			Inspector.Message(message);
		}

		/// <summary> Gets reset message. </summary>
		/// <returns> The reset message. </returns>
		protected virtual string GetResetMessage()
		{
			return "{0}value was reset.";
		}

		/// <summary> Sends the reset message. </summary>
		private void SendResetMessage()
		{
			string name = GetFieldNameForMessages();
			string message = GetResetMessage();
			if(name.Length > 0)
			{
				message = string.Format(message, StringUtils.Concat("\"", name, "\" "));
			}
			else
			{
				message = string.Format(message, "");
				char firstLetter = message[0];
				firstLetter = char.ToUpper(firstLetter);
				message = firstLetter + message.Substring(1);
			}
			Inspector.Message(message);
		}

		/// <summary> Gets copy to clipboard message. </summary>
		/// <returns> The copy to clipboard message. </returns>
		protected virtual string GetCopyToClipboardMessage()
		{
			return "Copied{0} values";
		}

		/// <summary> Sends the copy to clipboard message. </summary>
		protected void SendCopyToClipboardMessage()
		{
			var messagePrefix = GetCopyToClipboardMessage();
			SendCopyToClipboardMessage(messagePrefix);
		}

		/// <summary> Sends a copy to clipboard message. </summary>
		/// <param name="messageBody"> The message body. </param>
		protected void SendCopyToClipboardMessage(string messageBody)
		{
			var name = GetFieldNameForMessages();
			SendCopyToClipboardMessage(messageBody, name);
		}

		/// <summary> Sends a copy to clipboard message. </summary>
		/// <param name="messageBody"> The message body. </param>
		/// <param name="name"> The name. </param>
		protected void SendCopyToClipboardMessage(string messageBody, string name)
		{
			Clipboard.SendCopyToClipboardMessage(messageBody, name, Clipboard.Content);
		}

		/// <summary> Gets body for message that is shown after value has been pasted from clipboard. </summary>
		/// <returns> The message. </returns>
		protected virtual string GetPasteFromClipboardMessage()
		{
			return "Pasted value{0}.";
		}

		/// <summary> Sends the paste from clipboard message. </summary>
		protected void SendPasteFromClipboardMessage()
		{
			var messagePrefix = GetPasteFromClipboardMessage();
			SendPasteFromClipboardMessage(messagePrefix);
		}

		/// <summary> Sends a paste from clipboard message. </summary>
		/// <param name="messageBody"> The message body. </param>
		protected void SendPasteFromClipboardMessage(string messageBody)
		{
			var name = GetFieldNameForMessages();
			Clipboard.SendPasteFromClipboardMessage(messageBody, name);
		}

		/// <summary>
		/// Updates the dataIsValid fields value based on whether or not the field's data is valid.
		/// Uses either overrideValidateValue if it is not null, or otherwise uses GetDataIsValidUpdated()
		/// to get the value for the field.
		/// </summary>
		protected virtual void UpdateDataValidity(bool evenIfCanHaveSideEffects)
		{
			if(overrideValidateValue != null)
			{
				if(!evenIfCanHaveSideEffects && !CanReadFromFieldWithoutSideEffects)
				{
					#if DEV_MODE
					Debug.LogWarning("Ignoring "+ToString()+".UpdateDataValidity("+ evenIfCanHaveSideEffects+ " with overrideValidateValue="+StringUtils.ToString(overrideValidateValue)+ " because evenIfCanHaveSideEffects=false and CanReadFromFieldWithoutSideEffects=true");
					#endif
					return;
				}

				object visualizedValue;
				if(TryGetSingleValueVisualizedInInspector(out visualizedValue))
				{
					dataIsValid = overrideValidateValue(ArrayExtensions.TempObjectArray(visualizedValue));
				}
				else
				{
					dataIsValid = overrideValidateValue(GetValues());
				}
			}
			else
			{
				dataIsValid = GetDataIsValidUpdated();
			}
		}

		/// <summary>
		/// In some instances the value visualized in the Inspector might be an unapplied preview,
		/// or a non-value representing mixed content.
		/// 
		/// This method can be useful when doing data validation, as it can be more intuitive for the end user,
		/// when checks are done against the value that is actually visualized in the inspector.
		/// </summary>
		/// <param name="visualizedValue"> [out] The value visualized in the inspector. Null if has no value or targets have mixed content. </param>
		/// <returns> True if has single visualized value, false if not. </returns>
		protected virtual bool TryGetSingleValueVisualizedInInspector([CanBeNull]out object visualizedValue)
		{
			visualizedValue = null;
			return false;
		}

		/// <summary> Gets value indicating whether or not the field's data is valid.
		/// E.g. if a field has NotNull attribute but value is null, return false. </summary>
		/// <returns> True if field data is valid, false if it not. </returns>
		protected virtual bool GetDataIsValidUpdated()
		{
			return true;
		}

		/// <inheritdoc/>
		public virtual void OnSelfOrParentBecameVisible() { }

		/// <inheritdoc/>
		public virtual void OnBecameInvisible()
		{
			#if DEV_MODE && DEBUG_ON_BECAME_INVISIBLE
			Debug.Log(GetType().Name + ".OnBecameInvisible()");
			#endif

			var inspector = Inspector;

			IInspectorManager manager;
			if(inspector == null)
			{
				manager = InspectorUtility.ActiveManager;
				inspector = manager.ActiveInspector;
			}
			else
			{
				manager = inspector.Manager;
			}

			if(inspector != null)
			{
				if(manager.FocusedDrawer == this)
				{
					Deselect(ReasonSelectionChanged.BecameInvisible);
				}

				if(manager.MouseoveredSelectable == this)
				{
					manager.SetMouseoveredSelectable(manager.MouseoveredInspector, this);
				}

				if(manager.MouseoveredRightClickable == this)
				{
					RightClickAreaMouseovered = false;
				}
			}			

			lastDrawPosition.width = 0f;
		}

		/// <inheritdoc cref="IDrawer.ToString" />
		public override string ToString()
		{
			string shortTypeName = StringUtils.ToStringSansNamespace(GetType());
			if(shortTypeName.EndsWith("Drawer", StringComparison.Ordinal))
			{
				shortTypeName = shortTypeName.Substring(0, shortTypeName.Length - "Drawer".Length);
			}

			string shortLabel = Name;
			int i = shortLabel.IndexOf('\n');
			if(i != -1)
			{
				shortLabel = shortLabel.Substring(0, i);
			}
			return StringUtils.Concat(shortTypeName, "(\"", shortLabel, "\")");
		}

		/// <summary> Convert this object into a string representation. </summary>
		/// <param name="useLabel"> The use label. </param>
		/// <param name="useMemberInfo"> (Optional) Information describing the use member. </param>
		/// <returns> A string that represents this object. </returns>
		protected string ToString(GUIContent useLabel, LinkedMemberInfo useMemberInfo = null)
		{
			string name;
			if(useLabel != null)
			{
				name = useLabel.text;
			}
			else if(useMemberInfo != null)
			{
				name = useMemberInfo.Name;
			}
			else
			{
				name = "";
			}
			return StringUtils.Concat(StringUtils.ToStringSansNamespace(GetType()), "(\"", name, "\")");
		}

		/// <inheritdoc/>
		public virtual void AddPreviewWrappers(ref List<IPreviewableWrapper> previews) { }

		/// <inheritdoc/>
		public virtual object DefaultValue(bool preferNotNull = false)
		{
			return Type.DefaultValue();
		}

		/// <inheritdoc/>
		public virtual int GetSelectedRowIndex()
		{
			if(parent != null && parent.DrawInSingleRow)
			{
				return parent.GetMemberRowIndex(this);
			}
			return 0;
		}

		/// <inheritdoc/>
		public virtual int GetRowSelectableCount()
		{
			if(parent != null && parent.DrawInSingleRow)
			{
				return parent.VisibleMembers.Length + 1;
			}
			return 1;
		}

		/// <summary>
		/// Should KeyboardControlUtility.KeyboardControl value be reset to zero when any
		/// part of these drawers are clicked?
		/// </summary>
		/// <returns> True if should clear keyboard control when clicked. </returns>
		protected virtual bool ClearKeyboardControlWhenThisClicked()
		{
			return ClearKeyboardControlWhenControlClicked() && ClearKeyboardControlWhenPrefixClicked();
		}

		/// <summary>
		/// Should KeyboardControlUtility.KeyboardControl value be reset to zero when the
		/// control portion of these drawers is clicked?
		/// 
		/// The control portion means the "body" of the drawers, without the header.
		///  </summary>
		/// <returns> True if should clear keyboard control when prefix is clicked. </returns>
		protected virtual bool ClearKeyboardControlWhenControlClicked()
		{
			return false;
		}

		/// <summary>
		/// Should KeyboardControlUtility.KeyboardControl value be reset to zero when the
		/// prefix portion of these drawers is clicked?
		/// </summary>
		/// <returns> True if should clear keyboard control when prefix is clicked. </returns>
		protected virtual bool ClearKeyboardControlWhenPrefixClicked()
		{
			return false;
		}

		/// <inheritdoc/>
		public void Randomize()
		{
			Randomize(true);
		}

		/// <inheritdoc/>
		public void Randomize(bool alsoShowMessage)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!ReadOnly);
			#endif

			try
			{
				DoRandomize();
			}
			#if DEV_MODE
			catch(NotSupportedException e)
			{
				Debug.LogError(e);
			#else
			catch(NotSupportedException)
			{
			#endif
				if(alsoShowMessage)
				{
					Inspector.Message("Target does not supported randomization.");
				}
				return;
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogError(e);
			#else
			catch
			{
			#endif
				return;
			}

			if(alsoShowMessage)
			{
				SendRandomizeMessage();
			}
		}

		/// <inheritdoc/>
		protected virtual void DoRandomize()
		{
			throw new NotSupportedException("Target does not supported randomization.");
		}

		/// <summary> Determine if we should constantly update cached values. </summary>
		/// <returns> True if it succeeds, false if it fails. </returns>
		protected virtual bool ShouldConstantlyUpdateCachedValues()
		{
			return false;
		}

		/// <inheritdoc/>
		public virtual void OnExecuteCommand(Event commandEvent) { }

		/// <inheritdoc/>
		public int[] GenerateMemberIndexPath(IParentDrawer stopAfter)
		{
			if(this == stopAfter)
			{
				return ArrayPool<int>.ZeroSizeArray;
			}

			IDrawer current = this;
			var currentParent = current.Parent;
			while(currentParent != null)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(current != currentParent, "GenerateMemberIndexPath: member was parented to itself: "+current);
				#endif

				ReusableIntList.Add(Array.IndexOf(currentParent.Members, current));

				if(currentParent == stopAfter)
				{
					break;
				}

				current = currentParent;
				currentParent = current.Parent;
			}
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(stopAfter == currentParent, "GenerateMemberIndexPath: stopAfter was never found: " + stopAfter);
			#endif

			if(stopAfter != currentParent)
			{
				ReusableIntList.Clear();
				return null;
			}

			var result = ArrayPool<int>.Create(ReusableIntList);
			ReusableIntList.Clear();
			return result;
		}

		/// <inheritdoc/>
		public void SelectMemberAtIndexPath(int[] memberIndexPath, ReasonSelectionChanged reason)
		{
			IDrawer member = this;
			for(int n = 0, count = memberIndexPath.Length; n < count; n++)
			{
				var currentParent = member as IParentDrawer;
				if(currentParent == null)
				{
					break;
				}

				if(!currentParent.Unfolded)
				{
					currentParent.SetUnfolded(true);
				}

				int index = memberIndexPath[n];
				var members = currentParent.Members;

				int memberCount = members.Length;
				if(index >= memberCount)
				{
					#if DEV_MODE
					Debug.LogWarning("members.Length ("+ members.Length + ") < index ("+index+") in parent "+currentParent+" and index #"+n+ " in memberIndexPath "+StringUtils.ToString(memberIndexPath));
					#endif

					if(memberCount == 0)
					{
						break;
					}
					
					//if member at index doesn't exist, select closest match
					//this can be desired behaviour e.g. if the parent is an array
					//with a different number of members.
					index = memberCount - 1;
				}
				
				member = members[index];
			}

			if(!member.Selectable)
			{
				#if DEV_MODE
				Debug.LogWarning("SelectMemberAtIndexPath subject "+member+" not selectable. Trying to find a selectable parent instead....");
				#endif
				for(member = member.Parent; member != null && !member.Selectable; member = member.Parent);
				#if DEV_MODE
				Debug.LogWarning("SelectMemberAtIndexPath selecting "+StringUtils.ToString(member)+" instead.");
				#endif
				if(member == null)
				{
					Inspector.Select(null, reason);
					return;
				}
			}

			member.Select(reason);
		}

		/// <summary>
		/// Clears any filter string the inspector has and then scroll the inspector
		/// scroll view,  if necessary, just enough so that these drawers are visible.
		/// </summary>
		protected void ClearFilterAndScrollToShow()
		{
			var inspector = Inspector;
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inspector.HasFilterAffectingInspectedTargetContent);
			#endif

			// drawers under CustomEditors only exist while there's a filter
			// because of this we can't scroll to show fields that no longer
			// exist after the filter has been removed. In these cases we instead
			// scroll to show the parent CustomEditor.
			IDrawer firstStableSubject = this;
			for(var testParent = Parent; testParent != null; testParent = testParent.Parent)
			{
				if(testParent is ICustomEditorDrawer)
				{
					firstStableSubject = testParent;
					break;
				}
			}

			// Clear the filter
			inspector.SetFilter(string.Empty);

			// The effects of the filter being cleared are only applied during the next Layout phase,
			// so we need to wait until that to scroll to show the subject
			OnNextLayout(()=>inspector.ScrollToShow(firstStableSubject));
		}

		/// <summary> Scroll to show. </summary>
		protected virtual void ScrollToShow()
		{
			InspectorUtility.ActiveInspector.ScrollToShow(this);
		}

		/// <summary> Tests if this IDrawer is considered equal to another. </summary>
		/// <param name="other"> The igui drawers to compare to this object. </param>
		/// <returns> True if the objects are considered equal, false if they are not. </returns>
		public bool Equals(IDrawer other)
		{
			return ReferenceEquals(other, this);
		}

		/// <summary> Tests if this BaseDrawer is considered equal to another. </summary>
		/// <param name="other"> The base graphical user interface drawers to compare to this object. </param>
		/// <returns> True if the objects are considered equal, false if they are not. </returns>
		public bool Equals(BaseDrawer other)
		{
			return ReferenceEquals(other, this);
		}

		/// <summary>
		/// Because instanceID is incremented every time an drawers' class instance is pooled,
		/// it can be used to detect if the instance has been pooled after an async task has been started,
		/// before applying it's effects.
		/// </summary>
		public bool InstanceIdEquals(int requiredInstanceId)
		{
			return instanceId == requiredInstanceId;
		}

		/// <inheritdoc/>
		public void OnNextLayout(Action action)
		{
			var inspector = Inspector;

			// inspector might be null if calling this method was delayed
			// and reference to the inspector was set to null due to the drawers
			// being disposed to the object pool
			if(inspector != null)
			{
				inspector.Manager.OnNextLayout(MakeDelayable(action), inspector.InspectorDrawer);
			}
			else
			{
				InspectorUtility.ActiveManager.OnNextLayout(MakeDelayable(action));
			}
		}

		public void OnNextLayout(Action<IDrawer> action)
		{
			var inspector = Inspector;

			// inspector might be null if calling this method was delayed
			// and reference to the inspector was set to null due to the drawers
			// being disposed to the object pool
			if(inspector != null)
			{
				inspector.Manager.OnNextLayout(MakeDelayable(action), inspector.InspectorDrawer);
			}
			else
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+".OnNextLayout called with inspector null. Using ActiveManager instead.");
				#endif

				InspectorUtility.ActiveManager.OnNextLayout(MakeDelayable(action));
			}
		}

		/// <summary>
		/// Wraps delegate with container that ties its invocation to a specific Drawer instance.
		/// Drawer might get Disposed at any moment if the inspected targets of an inspector are changed,
		/// and DrawerDelayableAction has the information needed to check whether these drawers
		/// still exist right before a delayed action is about to be invoked.
		/// </summary>
		/// <returns> A delegate targeting a specific drawers instance. </returns>
		protected DrawerDelayableAction MakeDelayable(Action action)
		{
			return new DrawerDelayableAction(this, action);
		}

		/// <inheritdoc cref="MakeDelayable(Action)" />
		protected DrawerDelayableTargetedAction MakeDelayable(Action<IDrawer> action)
		{
			return new DrawerDelayableTargetedAction(this, action);
		}

		#if DEV_MODE && PI_ASSERTATIONS
		protected static void Assert(bool assertation, params object[] message)
		{
			if(!assertation)
			{
				Debug.LogError(StringUtils.ToColorizedString(message));
			}
		}

		protected static void AssertWarn(bool assertation, params object[] message)
		{
			if(!assertation)
			{
				Debug.LogWarning(StringUtils.ToColorizedString(message));
			}
		}
		#endif

		#if DEV_MODE
		/// <inheritdoc cref="StringUtils.ToColorizedString(object[])" />
		[Pure]
		protected string Msg(params object[] message)
		{
			return StringUtils.ToColorizedString(message);
		}
		#endif

		/// <summary>
		/// Determines if keyboard focusing logic should be overridden when control is selected,
		/// or if should allow Unity to handle it internally.
		/// 
		/// When true, inputs like arrow keys, tab etc. and used, and changes to keyboard selection
		/// are done manually.
		/// </summary>
		/// <returns> True if overriding, false if using built-in logic. </returns>
		protected virtual bool OverrideFieldFocusing()
		{
			return true;
		}

		/// <inheritdoc />
		public virtual void OnInspectorGainedFocusWhileSelected() { }

		/// <inheritdoc />
		public virtual void OnInspectorLostFocusWhileSelected() { }

		/// <summary> Caleld whenever the label of the drawer is changed. </summary>
		protected virtual void OnLabelChanged()
		{
			// If label text is empty (this can e.g. happen with image-only labels),
			// then try to figure out name for drawer from MemberInfo.
			if(label.text.Length == 0)
			{
				var memberInfo = MemberInfo;
				if(memberInfo != null)
				{
					name = memberInfo.DisplayName;
					#if DEV_MODE && DEBUG_EMPTY_LABEL
					Debug.LogWarning(ToString()+".OnLabelChanged - label.text was empty so fetched name from MemberInfo: \""+name+"\"");
					#endif
					return;
				}
				#if DEV_MODE && DEBUG_EMPTY_LABEL
				Debug.LogWarning(ToString()+".OnLabelChanged - label.text was empty and had no MemberInfo, so name will be set to empty string.");
				#endif
			}

			name = label.text; 
		}

		/// <inheritdoc />
		public virtual void OnSiblingValueChanged(int memberIndex, object memberValue, [CanBeNull] LinkedMemberInfo memberLinkedMemberInfo) { }
	}
}