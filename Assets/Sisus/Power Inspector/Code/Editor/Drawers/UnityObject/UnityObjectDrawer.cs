#define ENABLE_UNFOLD_ANIMATIONS
#define ENABLE_UNFOLD_ANIMATIONS_ON_START

//#define DEBUG_VISUALIZE_HEADER_TOOLBAR

//#define DEBUG_SET_PREFIX_LABEL_WIDTH
//#define DEBUG_PASSES_SEARCH_FILTER
//#define DEBUG_SET_UNFOLDED
//#define DEBUG_CLOSEDNESS
#define DEBUG_ON_CLICK
//#define DEBUG_SELECT_HEADER_PART
//#define DEBUG_SELECT_NEXT_HEADER_PART
//#define DEBUG_SET_MOUSE_OVER_PART
//#define DEBUG_KEYBOARD_INPUT
//#define DEBUG_RESET_STEPS
//#define DEBUG_MIDDLE_CLICK
#define DEBUG_DESELECT

#define SAFE_MODE

using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;
using System.IO;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Abstract base class for drawers that represent Components or UnityEngine.Object assets (but not GameObject),
	/// being able to list all of their exposed fields, properties and methods.
	/// </summary>
	/// <typeparam name="TSelf"> Type of the drawer that extends this base class. </typeparam>
	/// <typeparam name="TTarget"> Type of the target(s) which the drawer represents. Must be UnityEngine.Object or an extending type. </typeparam>
	[Serializable]
	public abstract class UnityObjectDrawer<TSelf, TTarget> : ParentDrawer<LinkedMemberInfo>, IUnityObjectDrawer, IDebuggable where TSelf : UnityObjectDrawer<TSelf, TTarget> where TTarget : Object
	{
		protected const float AssetHeaderToolbarIconWidth = 16f;
		protected const float AssetHeaderToolbarIconHeight = 17f;
		protected const float AssetToolbarIconsTopOffset = 7f;
		protected const float AssetHeaderToolbarIconsRightOffset = 0f;
		protected const float AssetHeaderToolbarIconsOffset = 0f;

		protected const float ComponentHeaderToolbarIconWidth = 16f;
		protected const float ComponentHeaderToolbarIconHeight = 16f;
		protected const float ComponentToolbarIconsTopOffset = 4f;
		protected const float ComponentHeaderToolbarIconsRightOffset = 5f;
		protected const float ComponentHeaderToolbarIconsOffset = 4f;

		protected const float HeaderButtonsRightSidePadding = 5f;
		protected const float HeaderButtonsPadding = HeaderButtonsRightSidePadding + 5f;

		protected const float InternalOpenButtonWidth = 46f;

		/// <summary> Gets the control identifier of the debug mode icon control. </summary>
		private const int DebugModeIconControlId = 0;

		/// <summary> Gets the control identifier of the execute method icon control. </summary>
		private const int ExecuteMethodIconControlId = 0;
		
		private const float PingDuration = 1f;

		private static IUnityObjectDrawer ping;
		private static float pingProgress;

		/// <summary> Gets or sets the on widths changed. </summary>
		/// <value> The on widths changed. </value>
		public Action OnWidthsChanged { get; set; }

		public Action OnPrefixResizingFinished { get; set; }

		/// <summary> The targets. </summary>
		protected TTarget[] targets = new TTarget[0];

		/// <summary> control ID generated before the header was drawn. </summary>
		protected int beforeHeaderControlId;

		/// <summary> We need to cache a reference to the inspector that owns the Drawer because
		/// we need to unsubscribe from OnDebugModeChanged event during Dispose method, and we can't rely
		/// on DrawGUI.ActiveInspector returning the same Inspector at that point. </summary>
		protected IInspector inspector;

		/// <summary> The linked member hierarchy for target UnityEngine.Object(s) </summary>
		protected LinkedMemberHierarchy linkedMemberHierarchy;

		/// <summary>
		/// The main Editor of the drawer.
		/// This is used for drawing the body of CustomEditorBaseDrawers.
		/// This is also used for drawing the header in all cases except when this is an AssetImporterEditor.
		/// </summary>
		[CanBeNull]
		protected Editor editor;

		/// <summary> Width of the prefix label. </summary>
		private float prefixLabelWidth = DrawGUI.DefaultPrefixLabelWidth;

		/// <summary>
		/// Width of the prefix label that was last manually set by the user, or -1 if was never set.
		/// </summary>
		private float manullySetPrefixLabelWidth = -1f;

		protected readonly TweenedBool unfoldedness = new TweenedBool();

		/// <summary> True to enable debug mode, false to disable it. </summary>
		[SerializeField]
		private bool debugMode;

		/// <summary> The header part that was selected at the start of the OnClick event, before its
		/// effects were applied. </summary>
		private HeaderPartDrawer selectedHeaderPartOnClickStart; 

		/// <summary> The mouseovered header part. </summary>
		private HeaderPartDrawer mouseoveredPart;

		/// <summary> The selected header part. </summary>
		private HeaderPartDrawer selectedPart;

		/// <summary> The prefix icon background position. </summary>
		private Rect prefixIconBackgroundPosition;

		/// <summary>
		/// Header toolbar, that contains the icons drawn at the top right corner of the header.
		/// Filled by BuildHeaderToolbar during the Setup phase.
		/// </summary>
		protected HeaderParts headerParts = new HeaderParts(1);
		protected float afterComponentHeaderGUIHeight = 0f;

		[NonSerialized]
		protected DebugModeDisplaySettings debugModeDisplaySettings;

		/// <summary>
		/// Buttons drawn at the bottom right corner of the header.
		/// Filled by BuildHeaderButtons during the Setup phase.
		/// </summary>
		protected Buttons headerButtons = new Buttons(1);
		
		/// <summary> The mouseoverered header control. </summary>
		private Button mouseovereredHeaderButton;

		/// <summary> True if class has ExcludeFromPresetAttribute. </summary>
		private bool excludedFromPreset;

		private float headerButtonsWidth;

		private bool prefixResizerMouseovered;

		private PrefixResizer prefixResizer;

		#if DEV_MODE
		private Rect prefixResizerPosition; //todo: use this
		#endif

		private DrawerDelayableAction<bool> onDebugModeChanged;

		/// <summary> Gets a value indicating whether drawing the prefix should be skipped when Draw is
		/// called. </summary>
		/// <value> True if headless mode, false if not. </value>
		protected static bool HeadlessMode
		{
			get
			{
				return UnityObjectDrawerUtility.HeadlessMode;
			}
		}

		/// <inheritdoc cref="IUnityObjectDrawer.MemberHierarchy" />
		public sealed override LinkedMemberHierarchy MemberHierarchy
		{
			get
			{
				return linkedMemberHierarchy;
			}
		}

		/// <inheritdoc cref="IDrawer.ClickToSelectArea" />
		public override Rect ClickToSelectArea
		{
			get
			{
				// UPDATE: now can click anywhere on the component to select it.
				// Previously clicking in the space between members would cause
				// nothing to get selected. However, this did not work intuitively
				// with CustomEditor-based UnityObjects for one.
				return Bounds;
			}
		}

		/// <summary> Gets a value indicating whether the mouse down over reorder area. </summary>
		/// <value> True if mouse down over reorder area, false if not. </value>
		public bool MouseDownOverReorderArea
		{
			get
			{
				return selectedPart == HeaderPart.Base;
			}
		}

		/// <inheritdoc cref="IDrawer.Selectable" />
		public override bool Selectable
		{
			get
			{
				return (!HeadlessMode || CanBeSelectedWithoutHeaderBeingSelected) && ShownInInspector;
			}
		}

		/// <inheritdoc cref="IDrawer.DebugMode" />
		public override bool DebugMode
		{
			get
			{
				return debugMode;
			}
		}

		/// <inheritdoc/>
		protected override Rect SelectionRect
		{
			get
			{
				var pos = lastDrawPosition;
				pos.y += 1f;
				pos.height -= 1f;
				pos.width -= 1f;
				return pos;
			}
		}

		/// <inheritdoc cref="IParentDrawer.DrawInSingleRow" />
		public override bool DrawInSingleRow
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc cref="IParentDrawer.Unfoldedness" />
		public override float Unfoldedness
		{
			get
			{
				return unfoldedness;
			}
		}

		/// <inheritdoc cref="IParentDrawer.Unfolded" />
		public override bool Unfolded
		{
			get
			{
				return unfoldedness;
			}

			set
			{
				#if DEV_MODE && DEBUG_SET_UNFOLDED
				Debug.Log(StringUtils.ToColorizedString(ToString(), ".Unfolded = ", value, " (was: ", Unfolded, ") with inactive=", inactive, ", MembersAreVisible=", MembersAreVisible, ", memberBuildState=", memberBuildState), Target);
				#endif
			
				if(Unfolded == value)
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(value || unfoldedness <= 0f, ToString());
					Debug.Assert(!value || unfoldedness >= 1f, ToString());
					#endif
					return;
				}

				#if ENABLE_UNFOLD_ANIMATIONS
				if(value)
				{
					// skip folding state change animations when there's a filter,
					// to make the changes to the inspector feel snappier when the
					// user is altering the filter
					if(Inspector.HasFilterAffectingInspectedTargetContent)
					{
						unfoldedness.SetValueInstant(true);
					}
					else
					{
						unfoldedness.SetTarget(Inspector.InspectorDrawer, true);
					}
					OnFullClosednessChanged(true);
				}
				else
				{
					unfoldedness.SetTarget(Inspector.InspectorDrawer, false, OnFullClosednessChanged);
				}
				#else
				unfoldedness.SetValueInstant(value);
				OnFullClosednessChanged(value);
				#endif
			}
		}

		/// <inheritdoc cref="IDrawer.UnityObject" />
		public override Object UnityObject
		{
			get
			{
				return Target;
			}
		}

		/// <inheritdoc cref="IDrawer.UnityObjects" />
		public override Object[] UnityObjects
		{
			get
			{
				return targets;
			}
		}

		/// <inheritdoc/>
		public override Transform[] Transforms
		{
			get
			{
				Transform[] transforms;
				if(!ArrayPool<Object>.TryCast(targets, false, out transforms))
				{
					int count = targets.Length;
					transforms = ArrayPool<Transform>.Create(count);
					for(int n = count - 1; n >= 0; n--)
					{
						transforms[n] = targets[n].Transform();
					}
				}

				return transforms;
			}
		}

		/// <summary> Gets or sets the width of the prefix label. </summary>
		/// <value> The width of the prefix label. </value>
		public float PrefixLabelWidth
		{
			get
			{
				return prefixLabelWidth;
			}

			set
			{
				if(!prefixLabelWidth.Equals(value))
				{
					#if DEV_MODE && DEBUG_SET_PREFIX_LABEL_WIDTH
					Debug.Log(ToString()+" prefixLabelWidth = "+value + " (was: "+ prefixLabelWidth+")");
					#endif

					GUI.changed = true;

					prefixLabelWidth = value;

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(value >= MinPrefixLabelWidth, ToString() + ".PrefixLabelWidth = " + value + " - value was less than MinPrefixLabelWidth " + MinPrefixLabelWidth);
					Debug.Assert(value <= MaxPrefixLabelWidth, ToString() + ".PrefixLabelWidth = " + value + " - value was more than MaxPrefixLabelWidth " + MaxPrefixLabelWidth);
					#endif

					if(OnWidthsChanged != null)
					{
						OnWidthsChanged();
					}
				}
			}
		}
		
		/// <inheritdoc cref="IDrawer.Type" />
		public override Type Type
		{
			get
			{
				if(targets.Length > 0)
				{
					var target = targets[0];
					if(target != null)
					{
						return target.GetType();
					}
				}
				return typeof(TTarget);
			}
		}

		/// <inheritdoc />
		public override float Height
		{
			get
			{
				return base.Height + afterComponentHeaderGUIHeight;
			}
		}

		/// <inheritdoc cref="IUnityObjectDrawer.HeaderHeight" />
		public override float HeaderHeight
		{
			get
			{
				return HeadlessMode ? 0f : DrawGUI.Active.InspectorTitlebarHeight;
			}
		}

		/// <inheritdoc cref="IUnityObjectDrawer.AppendIndentLevel" />
		public override int AppendIndentLevel
		{
			get
			{
				return 0;
			}
		}
		
		/// <summary> Gets the prefix resizer. </summary>
		/// <value> The prefix resizer. </value>
		public virtual PrefixResizer PrefixResizer
		{
			get
			{
				if(UnityObjectDrawerUtility.HeadlessMode)
				{
					return PrefixResizer.Disabled;
				}
				return prefixResizer;
			}
		}

		/// <summary> Returns dimensions for a rectangle at the bottom of this control where another
		/// Component can be drag n dropped to trigger a reordering event. </summary>
		/// <value> The reorder drop rectangle. </value>
		public Rect ReorderDropRect
		{
			get
			{
				var result = lastDrawPosition;
				if(lastDrawPosition.height > 0f)
				{
					result.y += Height - 9f;
					result.height = 18f;
				}
				return result;
			}
		}

		/// <inheritdoc/>
		public override Part MouseoveredPart
		{
			get
			{
				return !Mouseovered ? Part.None : HeaderMouseovered ? (Part)(HeaderPart)mouseoveredPart : Part.Body;
			}
		}

		/// <inheritdoc/>
		public override Part SelectedPart
		{
			get
			{
				return !Selected ? Part.None : HeaderIsSelected ? (Part)(HeaderPart)selectedPart : Part.Body;
			}
		}

		/// <inheritdoc cref="IDrawer.HasUnappliedChanges" />
		public override bool HasUnappliedChanges
		{
			get
			{
				return MemberHierarchy.HasUnappliedChanges();
			}
		}

		/// <inheritdoc/>
		public override string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetDrawerInfoUrl("unity-object-drawer");
			}
		}

		/// <summary>
		/// Gets parent or parent of parent that implements IGameObjectDrawer.
		/// </summary>
		[CanBeNull]
		public IGameObjectDrawer GameObjectDrawer
		{
			get
			{
				var gameObjectDrawer = parent as IGameObjectDrawer;
				if(gameObjectDrawer != null)
				{
					return gameObjectDrawer;
				}

				if(parent == null)
				{
					return null;
				}

				return parent.Parent as IGameObjectDrawer;
			}
		}

		/// <summary>
		/// Target Unity Objects of the main Editor used for drawing the body.
		/// 
		/// Can be different from "targets" field values for targets that have asset importers,
		/// in which case editorTargets will refer to those importers.
		/// </summary>
		///  <value> Object array containing Unity Object targets. </value>
		[NotNull]
		protected virtual TTarget[] EditorTargets
		{
			get
			{
				return targets;
			}
		}

		/// <summary> Is unfoldedness state currently being tweened. </summary>
		/// <value> True if currenly in the process of being folded or unfolded, false if not. </value>
		protected bool NowTweeningUnfoldedness
		{
			get
			{
				return unfoldedness.NowTweening;
			}
		}

		/// <summary>
		/// Gets maximum height for the prefix resizer when drawn vertically, or zero if there's no cap.
		/// </summary>
		/// <value> Maximum height for a vertical prefix resizer. </value>
		protected virtual float PrefixResizerMaxHeight
		{
			get
			{
				return 0f;
			}
		}

		/// <summary> Gets the height used by the prefix resizer drag handle between the header and the body of the drawer. </summary>
		/// <value> Height of the prefix resizer drag handle located at the top. </value>
		protected virtual float PrefixResizerDragHandleHeight
		{
			get
			{
				return PrefixResizer == PrefixResizer.TopOnly ? PrefixResizeUtility.TopOnlyPrefixResizerHeight : 0f;
			}
		}

		/// <summary>
		/// Gets a value indicating whether an Editor (rather than the visible member drawers) is used for drawing the body of the drawer.
		/// </summary>
		/// <value> True if the an Editor is currently used for drawing the body, false if not. </value>
		protected virtual bool UsesEditorForDrawingBody
		{
			get
			{
				return false;
			}
		}

		/// <summary> Gets text for the context menu item used for Copying the target(s). </summary>
		protected virtual string CopyContextMenuText
		{
			get
			{
				return "Copy";
			}
		}

		/// <summary> Gets text for the context menu item used for pasting copied value on the target(s). </summary>
		protected virtual string PasteContextMenuText
		{
			get
			{
				return "Paste Values";
			}
		}

		/// <summary> Gets a value indicating whether this object is an asset. </summary>
		/// <value> True if this object is an asset, false if not. </value>
		protected abstract bool IsAsset
		{
			get;
		}

		/// <summary> Gets a value indicating whether the reordering. </summary>
		/// <value> True if reordering, false if not. </value>
		protected virtual bool Reordering
		{
			get
			{
				return this == Manager.MouseDownInfo.Reordering.Drawer && DrawGUI.IsUnityObjectDrag;
			}
		}

		/// <summary> Gets a value indicating whether we can be selected without header being selected. </summary>
		/// <value> True if we can be selected without header being selected, false if not. </value>
		protected virtual bool CanBeSelectedWithoutHeaderBeingSelected
		{
			get
			{
				return true;
			}
		}

		/// <summary> Gets the color of the prefix background. </summary>
		/// <value> The color of the prefix background. </value>
		protected abstract Color PrefixBackgroundColor
		{
			get;
		}

		/// <summary> Gets the first UnityEngine.Object Target. </summary>
		/// <value> Target UnityEngine.Object. </value>
		[CanBeNull]
		protected TTarget Target
		{
			get
			{
				return targets.Length == 0 ? null : targets[0];
			}
		}
		
		/// <summary> Gets the MonoScript associated with the target (if any). </summary>
		/// <value> MonoScript associated with the target; null if not applicable. </value>
		[CanBeNull]
		protected abstract MonoScript MonoScript
		{
			get;
		}

		/// <summary> Gets a value indicating whether this object has enabled flag. </summary>
		/// <value> True if this object has enabled flag, false if not. </value>
		protected virtual bool HasEnabledFlag
		{
			get
			{
				return false;
			}
		}

		/// <summary> Gets a value indicating whether this object has debug mode icon. </summary>
		/// <value> True if this object has debug mode icon, false if not. </value>
		protected virtual bool HasDebugModeIcon
		{
			get
			{
				#if DEV_MODE || SAFE_MODE
				if(inspector == null) // happened during OnFilterChanging callback for some reason
				{ 
					#if DEV_MODE
					Debug.LogWarning(ToString()+".HasDebugModeIcon called with inspector="+StringUtils.Null+ ", Inspector="+Inspector+" inactive=" + StringUtils.ToColorizedString(inactive));
					#endif
					return false;
				}
				#endif

				return (inspector.Preferences.drawDebugModeIcon || DebugMode) && !HeadlessMode;
			}
		}

		/// <summary> Gets a value indicating whether this object has execute method icon. </summary>
		/// <value> True if this object has execute method icon, false if not. </value>
		protected virtual bool HasExecuteMethodIcon
		{
			get
			{
				return (inspector.Preferences.drawQuickInvokeIcon || DebugMode) && !HeadlessMode;
			}
		}

		/// <summary> Gets a value indicating whether this object has preset icon. </summary>
		/// <value> True if this object has preset icon, false if not. </value>
		protected virtual bool HasPresetIcon
		{
			get
			{
				return !HeadlessMode && !excludedFromPreset && Editable;
			}
		}

		/// <summary> Gets a value indicating whether this object has the context menu icon. </summary>
		/// <value> True if this object has the context menu icon, false if not. </value>
		protected virtual bool HasContextMenuIcon
		{
			get
			{
				return !HeadlessMode;
			}
		}

		/// <summary>
		/// Can target component be removed from containing GameObject or target asset deleted from disk?
		/// </summary>
		protected virtual bool Destroyable
		{
			get
			{
				return Editable;
			}
		}

		/// <summary> Gets the enabled flag position. </summary>
		/// <value> The enabled flag position. </value>
		protected Rect EnabledFlagPosition
		{
			get
			{
				var rect = labelLastDrawPosition;
				rect.x += 40f;
				rect.width = HasEnabledFlag ? 16f : 0f;
				return rect;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the header should be drawn greyed out.
		/// This is mostly true when Editable is false, but the behaviour can
		/// be overriden for specific cases.
		/// </summary>
		/// <value> True if should draw greyed out, false if not. </value>
		protected virtual bool DrawGreyedOut
		{
			get
			{
				return (!Enabled && inspector.Preferences.drawDisabledGreyedOut != GreyOut.None) || !Editable || (Target != null && HasHideFlag(HideFlags.HideInInspector));
			}
		}

		/// <summary> Gets a value indicating whether this object is enabled. </summary>
		/// <value> True if enabled, false if not. </value>
		protected virtual bool Enabled
		{
			get
			{
				return true;
			}

			set
			{
				#if DEV_MODE
				Debug.LogError("Assigning to "+this+".Enabled not supported.");
				#endif
			}
		}

		/// <summary> Gets a value indicating whether this Object is editable through the inspector. </summary>
		/// <value> True if editable, false if read only. </value>
		protected virtual bool Editable
		{
			get
			{
				var gameObjectDrawer = GameObjectDrawer;
				if(gameObjectDrawer != null && gameObjectDrawer.ReadOnly)
				{
					return false;
				}

				if(!GuiEnabled)
				{
					return false;
				}

				if(Target == null)
				{
					return true;
				}

				if(HasHideFlag(HideFlags.NotEditable))
				{
					return false;
				}

				if(!IsAssetOpenForEdit())
				{
					return false;
				}

				return true;
			}
		}

		/// <summary>
		/// Offset in pixels from the right edge of the drawer from which the header toolbar buttons start.
		/// </summary>
		protected abstract float HeaderToolbarIconsRightOffset
		{
			get;
		}

		/// <inheritdoc cref="IDrawer.ReadOnly" />
		public override bool ReadOnly
		{
			get
			{
				return !Editable;
			}
		}

		/// <inheritdoc/>
		public virtual float MinPrefixLabelWidth
		{
			get
			{
				return DrawGUI.MinPrefixLabelWidth;
			}
		}

		/// <inheritdoc/>
		public virtual float MaxPrefixLabelWidth
		{
			get
			{
				return DrawGUI.InspectorWidth - DrawGUI.MinControlFieldWidth;
			}
		}

		/// <summary> Gets the width of the minimum automatic sized prefix label. </summary>
		/// <value> The width of the minimum automatic sized prefix label. </value>
		protected float MinAutoSizedPrefixLabelWidth
		{
			get
			{
				float result = DrawGUI.MinAutoSizedPrefixLabelWidth;
				float min = MinPrefixLabelWidth;
				if(result < min)
				{
					return min;
				}
				return result;
			}
		}

		/// <summary> Gets a value indicating whether the mouse is over header. </summary>
		/// <value> True if mouse is over header, false if not. </value>
		protected bool HeaderMouseovered
		{
			get
			{
				return mouseoveredPart != HeaderPart.None || mouseovereredHeaderButton != null;
			}
		}

		/// <summary> Gets or sets the mouse over header part. </summary>
		/// <value> The mouse over header part. </value>
		protected HeaderPartDrawer MouseoveredHeaderPart
		{
			get
			{
				return mouseoveredPart;
			}

			set
			{
				#if DEV_MODE && DEBUG_SET_MOUSE_OVER_PART
				if(mouseoveredPart != value){Debug.Log("mouseOverHeaderPart = "+(value == null ? "null" : value.ToString())+" (was: "+(mouseoveredPart == null ? "null" : mouseoveredPart.ToString())+") with Event="+StringUtils.ToString(Event.current)+ ", HeaderMouseovered=" + StringUtils.ToColorizedString(HeaderMouseovered)+", CursorPos = " + Cursor.LocalPosition+ " , CanRequestCursorPosition=" + StringUtils.ToColorizedString(Cursor.CanRequestLocalPosition));}
				#endif

				mouseoveredPart = value;

				//new test
				if(value != null)
				{
					mouseovereredHeaderButton = null;
				}
			}
		}

		/// <summary> Gets the selected header part. </summary>
		/// <value> The selected header part. </value>
		protected HeaderPartDrawer SelectedHeaderPart
		{
			get
			{
				return selectedPart;
			}

			private set
			{
				#if DEV_MODE && DEBUG_SELECT_HEADER_PART
				if((HeaderPart)selectedPart != value) { Debug.Log(StringUtils.ToColorizedString("selectedHeaderPart = ", ((HeaderPart)value), " (was: ", ((HeaderPart)selectedPart), ")")); }
				#endif

				selectedPart = value;
			}
		}

		/// <summary> Gets a value indicating whether this object is component. </summary>
		/// <value> True if this object is component, false if not. </value>
		protected virtual bool IsComponent
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public bool PrefixResizerMouseovered
		{
			get
			{
				return prefixResizerMouseovered && PrefixResizer != PrefixResizer.Disabled;
			}
		}

		/// <inheritdoc/>
		public bool HeaderIsSelected
		{
			get
			{
				return selectedPart != HeaderPart.None;
			}
		}

		/// <summary> Gets a value indicating whether this object has reference icon. </summary>
		/// <value> True if this object has reference icon, false if not. </value>
		protected virtual bool HasReferenceIcon
		{
			get
			{
				try
				{
					return inspector.Preferences.drawReferenceIcon && !HeadlessMode;
				}
				#if DEV_MODE
				catch(NullReferenceException e) // happened during OnFilterChanging callback for some reason
				{
					Debug.LogError(e);
				#else
				catch(NullReferenceException) // happened during OnFilterChanging callback for some reason
				{
				#endif
					return false;
				}
			}
		}

		/// <summary> Gets a value indicating whether all targets are of same type. </summary>
		/// <value> True if all targets are same type, false if not. </value>
		protected virtual bool AllTargetsAreSameType
		{
			get
			{
				return true;
			}
		}

		/// <summary> Gets the execute method icon position. </summary>
		/// <value> The execute method icon position. </value>
		protected Rect ExecuteMethodIconPosition
		{
			get
			{
				if(HasExecuteMethodIcon)
				{
					for(int n = headerParts.Count - 1; n >= 0; n--)
					{
						var part = headerParts[n];
						if(part.Part == HeaderPart.QuickInvokeMenuButton)
						{
							return part.Rect;
						}
					}
				}
				return default(Rect);
			}
		}
		
		/// <summary> Gets the identifier of the enabled flag control. </summary>
		/// <value> The identifier of the enabled flag control. </value>
		private int EnabledFlagControlId
		{
			get
			{
				return beforeHeaderControlId;
			}
		}

		/// <inheritdoc/>
		protected sealed override void Setup(IParentDrawer setParent, GUIContent setLabel)
		{
			throw new NotSupportedException("Please use the other Setup method of UnityObjectDrawer");
		}

		/// <summary>
		/// Sets up the Drawer so that it is ready to be used.
		/// LateSetup should be called right after this.
		/// </summary>
		/// <param name="setTargets"> The set targets. This cannot be null. </param>
		/// <param name="setParent"> The set parent. This may be null. </param>
		/// <param name="setLabel"> The set label. This may be null. </param>
		/// <param name="setInspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		protected virtual void Setup([NotNull]TTarget[] setTargets, [CanBeNull]IParentDrawer setParent, [CanBeNull]GUIContent setLabel, [NotNull]IInspector setInspector)
		{
			#if DEV_MODE //TEMP
			if(setTargets.Length > 0 && setTargets[0] != null)
			{
				var helps = setTargets[0].GetType().GetCustomAttributes(typeof(HelpURLAttribute), true);
				if(helps.Length > 0)
				{
					Debug.Log("Custom documentation URL: "+ (helps[0] as HelpURLAttribute).URL);
				}
			}
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setInspector != null, GetType().Name+".Create inspector was null for targets "+StringUtils.ToString(setTargets));
			Debug.Assert(headerButtons.Count == 0, this);
			Debug.Assert(headerParts.Count == 0, this);
			Debug.Assert(setTargets.Length > 0 || GetType() == typeof(ClassDrawer), this);
			Debug.Assert(!setTargets.ContainsNullObjects() || GetType() == typeof(MissingScriptDrawer), this);
			#endif

			// just making sure
			lastDrawPosition.width = 0f;
			labelLastDrawPosition.width = 0f;

			inspector = setInspector;

			onDebugModeChanged = new DrawerDelayableAction<bool>(this, SetDebugMode);
			inspector.State.OnDebugModeChanged += onDebugModeChanged.InvokeIfInstanceReferenceIsValid;

			if(inspector.State.DebugMode)
			{
				debugMode = true;
			}

			targets = setTargets;

			if(setTargets.Length > 0 && setTargets[0] == null)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(GetType() == typeof(MissingScriptDrawer), this);
				#endif

				linkedMemberHierarchy = LinkedMemberHierarchy.Get(ArrayPool<Object>.ZeroSizeArray);
			}
			else
			{
				linkedMemberHierarchy = LinkedMemberHierarchy.Get(ArrayPool<TTarget>.Cast<Object>(setTargets));
			}

			parent = setParent; // this can be references by Unfolded, so assign it now even though base.Setup will also do that

			int targetCount = setTargets.Length;
			if(targetCount > 0)
			{
				var target = setTargets[0];

				// In headless mode there's no unfold arrow visible, so it's important to always set the target unfolded on start.
				// Also non-component UnityObjects don't have foldout arrows, so make sure they're set internally as unfolded.
				// NOTE: Using IsComponent instead of !Foldable because component (un)foldability can't be determined before members have been built.
				if(!Foldable || HeadlessMode)
				{
					#if DEV_MODE
					if(HeadlessMode) {Debug.LogWarning("Unfolding " + ToString() + " because HeadlessMode=" + StringUtils.True); }
					#endif

					SetUnfolded(true, false, false);
				}
				// Start all disabled Components unfolded.
				else if(!Enabled || UserSettings.EditComponentsOneAtATime)
				{
					SetUnfolded(false, false, false);
				}
				else
				{
					SetUnfolded(ComponentUnfoldedUtility.GetIsUnfolded(target), false, false);
				}

				if(setLabel == null)
				{
					setLabel = AllTargetsAreSameType ? GenerateLabel(target.GetType()) : GUIContentPool.Empty();
				}
			}
			else
			{
				// make sure that targetless ClassDrawer start out unfolded
				// as they have no header for altering the folded state
				SetUnfolded(true, false, false);
			}

			// Skip initial foldout animations if user has configured so.
			// Also always skip folding animations when moving from initial unfoldedness to foldedness,
			// for example due to EditComponentsOneAtATime being enabled.
			if(!inspector.Preferences.animateInitialUnfolding || !Unfolded)
			{
				unfoldedness.SetValueInstant(Unfolded);
			}
			
			base.Setup(setParent, setLabel);

			inspector.State.OnWidthChanged += OnInspectorWidthChanged;
			
			excludedFromPreset = TypeToCheckForExcludeFromPresetAttribute().IsDefined(Types.ExcludeFromPresetAttribute, false);
		}

		#if SAFE_MODE
		/// <inheritdoc/>
		public override void LateSetup()
		{
			base.LateSetup();

			// temp ad-hoc fix for issue encountered where header buttons would sometimes disappear from the preferences view after scripts were reloaded and the window was open
			if(headerButtons.Count == 0 && !IsComponent && MembersAreVisible && memberBuildState != MemberBuildState.Unstarted)
			{
				#if DEV_MODE
				Debug.LogWarning("Force rebuilding header buttons"); 
				#endif

				ForceRebuildHeaderButtons();
			}

			if(visibleMembers.Length == 0)
			{
				prefixResizer = PrefixResizeUtility.GetPrefixResizerType(this, UsesEditorForDrawingBody);
			}
		}
		#endif

		protected virtual GUIContent GenerateLabel(Type type)
		{
			return GUIContentPool.Create(StringUtils.SplitPascalCaseToWords(type.Name));
		}

		/// <summary>
		/// Returns Type which should be checked for the ExcludeFromPreset attribute.
		/// Usually this matches the target's Type, but sometimes it is an AssetImporter's
		/// Type which should get tested instead.
		/// </summary>
		/// <returns> A Type. </returns>
		protected virtual Type TypeToCheckForExcludeFromPresetAttribute()
		{
			return Type;
		}
		
		/// <inheritdoc/>
		protected override void OnAfterMemberBuildListGenerated()
		{
			ForceRebuildHeaderToolbar();
			ForceRebuildHeaderButtons();
			base.OnAfterMemberBuildListGenerated();
		}

		/// <inheritdoc/>
		public override void OnAfterMembersBuilt()
		{
			base.OnAfterMembersBuilt();

			if(InspectorUtility.Preferences.autoResizePrefixLabels == PrefixAutoOptimization.AllSeparately || !IsComponent)
			{
				float optimalWidth = GetOptimalPrefixLabelWidth(0, true);
				PrefixLabelWidth = Mathf.Clamp(optimalWidth, MinAutoSizedPrefixLabelWidth, DrawGUI.MaxAutoSizedPrefixLabelWidth);
			}
		}

		/// <inheritdoc/>
		public override void OnVisibleMembersChanged()
		{
			prefixResizer = PrefixResizeUtility.GetPrefixResizerType(this, UsesEditorForDrawingBody);
			base.OnVisibleMembersChanged();
		}

		/// <inheritdoc/>
		protected sealed override Type GetMemberType(LinkedMemberInfo memberBuildListItem)
		{
			return memberBuildListItem == null ? null : memberBuildListItem.Type;
		}

		/// <inheritdoc/>
		protected sealed override object GetMemberValue(LinkedMemberInfo memberBuildListItem)
		{
			return memberBuildListItem == null ? null : memberBuildListItem.Type;
		}

		/// <inheritdoc cref="IDrawer.SetValue(object)" />
		public override bool SetValue(object newValue)
		{
			bool changed = false;
			var comp = newValue as TTarget;
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				if(targets[n] != comp)
				{
					changed = true;
					targets[n] = comp;
				}
			}
			return changed;
		}

		/// <inheritdoc cref="IDrawer.GetValue(int)" />
		public override object GetValue(int index)
		{
			return targets[index];
		}

		/// <inheritdoc cref="IDrawer.GetValue(int)" />
		public override object[] GetValues()
		{
			return targets;
		}
		
		/// <summary> Gets offset of the header toolbar icons from the top of the header. </summary>
		/// <value> The toolbar icons offset from top in pixels. </value>
		protected abstract float ToolbarIconsTopOffset
		{
			get;
		}

		/// <summary> Gets offset between the header toolbar icons. </summary>
		/// <value> Toolbar icon offset in pixels. </value>
		protected abstract float HeaderToolbarIconsOffset
		{
			get;
		}
		
		/// <summary> Gets the width of header toolbar menu icons. </summary>
		/// <value> The width of the context menu icon. </value>
		protected abstract float HeaderToolbarIconWidth
		{
			get;
		}

		/// <summary> Gets the height of the icons found on the toolbar on the top-right of Object headers. </summary>
		/// <value> The height of the header toolbar icons. </value>
		protected abstract float HeaderToolbarIconHeight
		{
			get;
		}

		/// <summary> If this drawer represents a Component, gets the GameObject that holds the Component. </summary>
		/// <value> The GameObject that holds the target UnityObject or null if not applicable. </value>
		protected GameObject GameObject
		{
			get
			{
				var component = Target as Component;
				if(component != null)
				{
					return component.gameObject;
				}
				
				if(IsComponent && parent != null)
				{
					return parent.GetValue() as GameObject;
				}
				return null;
			}
		}
		
		/// <summary> Gets the total width of all the buttons at the bottom right corner of the header. </summary>
		/// <returns> A float. </returns>
		protected float HeaderButtonsWidth
		{
			get
			{
				#if DEV_MODE && PI_ASSERTATIONS
				float headerButtonsWidthUpdated = headerButtons.Width();
				if(!headerButtonsWidth.Equals(headerButtonsWidthUpdated)) { Debug.LogError(StringUtils.Concat(headerButtonsWidth, " != ", headerButtonsWidthUpdated)); }
				#endif

				return headerButtonsWidth;
			}
		}

		private void OnAddressablesToolbarClicked(IUnityObjectDrawer containingDrawer, Rect buttonRect, Event inputEvent)
		{
			
		}


		private Rect GetAddressablesToolbarPosition(Rect headerRect)
		{
			var rect = headerRect;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(AddressablesUtility.IsInstalled);
			Debug.Assert(IsAsset);
			#endif

			var heightWithoutToolbar = DrawGUI.Active.AssetTitlebarHeight(false);
			var heightWithToolbar = DrawGUI.Active.AssetTitlebarHeight(true);

			rect.y += heightWithoutToolbar;
			rect.height = heightWithToolbar - heightWithoutToolbar;

			return rect;
		}

		private Rect GetFoldArrowPosition(Rect headerRect)
		{
			var foldArrowPosition = headerRect;
			foldArrowPosition.y += 1f;
			foldArrowPosition.width = 18f;
			return foldArrowPosition;
		}

		private Rect GetEnabledFlagPosition(Rect headerRect)
		{
			var enabledFlagPosition = headerRect;
			enabledFlagPosition.y += ToolbarIconsTopOffset;
			enabledFlagPosition.x += 39f;
			enabledFlagPosition.width = HasEnabledFlag ? 16f : 0f;
			enabledFlagPosition.height = 16f;
			return enabledFlagPosition;
		}

		private Rect GetBaseRect(Rect headerRect)
		{
			return default(Rect);
		}

		/// <inheritdoc/>
		protected override void GetDrawPositions(Rect position)
		{
			var labelRectWas = labelLastDrawPosition;

			lastDrawPosition = position;
			float totalHeight = Height;
			lastDrawPosition.height = totalHeight;
			
			if(HeadlessMode)
			{
				labelLastDrawPosition = lastDrawPosition;
				labelLastDrawPosition.width = 0f;
				labelLastDrawPosition.height = 0f;
				bodyLastDrawPosition = lastDrawPosition;
			}
			else
			{
				labelLastDrawPosition = position;
				float headerHeight = HeaderHeight;
				labelLastDrawPosition.height = headerHeight;

				bodyLastDrawPosition = position;
				float topHeight = headerHeight + PrefixResizerDragHandleHeight;
				bodyLastDrawPosition.y += topHeight;
				bodyLastDrawPosition.height = totalHeight - topHeight;
			}

			#if DEV_MODE
			prefixResizerPosition = PrefixResizeUtility.GetPrefixResizerBounds(this, PrefixResizerMaxHeight); //to do: use this
			#endif

			if(!labelRectWas.Equals(labelLastDrawPosition))
			{
				OnHeaderRectChanged(labelLastDrawPosition);
			}

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}

		/// <summary>
		/// Called whenever the UnityObject changes between being fully unfolded and not being fully unfolded.
		/// If SetUnfolded(true) is called for a folded drawer, OnFullClosednessChanged is called immediately.
		/// If however SetUnfolded(false) is called for an unfolded drawer, OnFullClosednessChanged only called
		/// once the unfolding animation for the drawer has finished.
		/// </summary>
		/// <param name="unfolded"></param>
		protected virtual void OnFullClosednessChanged(bool unfolded)
		{
			#if DEV_MODE && DEBUG_CLOSEDNESS
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".OnFullClosednessChanged(", unfolded, ") with Unfoldedness=", Unfoldedness+", Unfolded=", Unfolded, ", unfolded=", unfolded), Target);
			#endif

			if(Unfolded != unfolded)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+" Ignoring OnFullClosednessChanged because unfolded ("+StringUtils.ToColorizedString(unfolded)+") did not match Unfolded");
				#endif
				return;
			}

			if(inactive)
			{
				return;
			}

			ParentDrawerUtility.OnMemberVisibilityChanged(this, unfolded);
			
			// don't record changes to Component unfoldedness during the Setup and Dispose phases
			// also don't record when editing components one at a time, as they always start
			// unfolded in this mode anyways.
			if(!inactive || UserSettings.EditComponentsOneAtATime)
			{
				for(int n = targets.Length - 1; n >= 0; n--)
				{
					var target = targets[n];
					if(target != null)
					{
						ComponentUnfoldedUtility.SetIsUnfolded(target, unfolded);
					}
				}
			}

			if(MembersAreVisible && memberBuildState == MemberBuildState.BuildListGenerated)
			{
				BuildMembers();
			}
			else
			{
				UpdateVisibleMembers();
			}
		}

		private void OnHeaderRectChanged(Rect headerRect)
		{
			UpdateHeaderButtonPositions(headerRect);
			UpdateHeaderToolbarPositions(headerRect);
		}

		/// <inheritdoc/>
		protected override void OnLayoutEvent(Rect position)
		{
			base.OnLayoutEvent(position);
			
			if(Cursor.CanRequestLocalPosition)
			{
				UpdateMouseOverHeaderPart(Cursor.LocalPosition);
			}
		}

		/// <summary> Updates the mouse over header part described by mousePos. </summary>
		/// <param name="mousePos"> The mouse position. </param>
		protected virtual void UpdateMouseOverHeaderPart(Vector2 mousePos)
		{
			if(labelLastDrawPosition.Contains(mousePos) && !UnityObjectDrawerUtility.HeadlessMode)
			{
				HeaderPartDrawer setMouseoverHeaderPart;
				if(GetMouseoveredHeaderPart(out setMouseoverHeaderPart))
				{
					MouseoveredHeaderPart = setMouseoverHeaderPart;
					mouseovereredHeaderButton = null;
				}
				else if(GetMouseoveredHeaderButton(out mouseovereredHeaderButton))
				{
					MouseoveredHeaderPart = null;
				}
				else
				{
					// NEW TEST: mouseovered header part detection seemed to fail on Layout event right after click event.
					// This might be because headerButtons need time to update draw positions after being rebuilt.
					if(Inspector.Manager.MouseDownInfo.IsClick)
					{
						#if DEV_MODE
						Debug.LogWarning("Skipping UpdateMouseOverHeaderPart because IsClick is true...");
						#endif
						return;
					}

					MouseoveredHeaderPart = headerParts[HeaderPart.Base];
					mouseovereredHeaderButton = null;
				}
			}
			else
			{
				MouseoveredHeaderPart = null;
				mouseovereredHeaderButton = null;
			}
		}

		/// <summary>
		/// Determine if we should rebuild member drawers at this time, before continuing to draw the members.
		/// </summary>
		/// <returns> True if should reduild, false if no need. </returns>
		protected virtual bool ShouldRebuildDrawers()
		{
			return targets.ContainsNullObjects();
		}

		/// <inheritdoc cref="IDrawer.Draw" />
		public override bool Draw(Rect position)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("UnityObjectDrawer.Draw");
			#endif

			if(ShouldRebuildDrawers())
			{
				#if DEV_MODE
				Debug.LogWarning(this + ".Draw() - target was null, rebuilding");
				#endif
				
				// if an UnityObjectDrawer had a null target chances are that LinkedMemberHierarchy will have them too
				bool hierarchiesHadNullTargets;
				LinkedMemberHierarchy.OnHierarchyChanged(out hierarchiesHadNullTargets);
				inspector.ForceRebuildDrawers();

				#if DEV_MODE || PROFILE_POWER_INSPECTOR
				Profiler.EndSample();
				#endif
				return true;
			}

			DrawGUI.PrefixLabelWidth = prefixLabelWidth;
			
			var guiColorWas = GUI.color;
			if(DrawGreyedOut)
			{
				var color = GUI.color;
				color.a = 0.5f;
				GUI.color = color;
			}

			if(Event.current.type == EventType.Layout)
			{
				OnLayoutEvent(position);
			}
			// this helps make animations a little bit smoother when unfoldedness is being tweened
			else if(TweenedBool.AnyTweening || lastDrawPosition.height <= 0f)
			{
				GetDrawPositions(position);
			}

			bool dirty = !HeadlessMode && DrawPrefix(PrefixLabelPosition);
			DrawGUI.LayoutSpace(HeaderHeight);

			if(inspector.Preferences.drawDisabledGreyedOut == GreyOut.HeaderOnly)
			{
				GUI.color = guiColorWas;
			}

			if(MembersAreVisible && PrefixResizer != PrefixResizer.Disabled)
			{
				HandlePrefixColumnResizing();
				DrawGUI.LayoutSpace(PrefixResizerDragHandleHeight);
			}

			if(Event.current.type == EventType.Layout)
			{
				if(!HeadlessMode && IsComponent)
				{
					afterComponentHeaderGUIHeight = EditorGUIDrawer.InvokeAfterComponentHeaderGUI(PrefixLabelPosition, targets, selectedPart);
				}
				else
				{
					afterComponentHeaderGUIHeight = 0f;
				}
			}

			if(MembersAreVisible)
			{
				DrawBody(bodyLastDrawPosition);
			}

			GUI.color = guiColorWas;

			#if DEV_MODE && DEBUG_VISUALIZE_HEADER_TOOLBAR
			if(Event.current.control)
			{
				var c = Color.green;
				c.a = 0.5f;
				if(HasContextMenuIcon)
				{
					Platform.Active.GUI.ColorRect(ContextMenuIconPosition, c);
				}
				if(HasPresetIcon)
				{
					Platform.Active.GUI.ColorRect(PresetIconPosition, c);
				}
				if(HasReferenceIcon)
				{
					Platform.Active.GUI.ColorRect(ReferenceIconPosition, c);
				}
				if(HasDebugModeIcon)
				{
					Platform.Active.GUI.ColorRect(DebugModeIconPosition, c);
				}
				if(HasExecuteMethodIcon)
				{
					Platform.Active.GUI.ColorRect(ExecuteMethodIconPosition, c);
				}
				if(HasEnabledFlag)
				{
					Platform.Active.GUI.ColorRect(EnabledFlagPosition, c);
				}
			}
			#endif

			DrawGUI.LayoutSpace(Height - HeaderHeight - PrefixResizerDragHandleHeight - afterComponentHeaderGUIHeight);
			//DrawGUI.LayoutSpace(Height);

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
			
			return dirty;
		}

		/// <inheritdoc/>
		public override bool DrawBodyMultiRow(Rect position)
		{
			DrawGUI.IndentLevel = 0; // ensure that IndentLevel is correct
			return base.DrawBodyMultiRow(position);
		}

		/// <summary> Handles drawing the prefix column resizing control. </summary>
		protected void HandlePrefixColumnResizing()
		{
			#if DEV_MODE
			//prefixResizerPosition //to do: use this
			#endif

			float setPrefixLabelWidth = PrefixResizeUtility.HandleResizing(this, out prefixResizerMouseovered, PrefixResizerMaxHeight, MinPrefixLabelWidth, MaxPrefixLabelWidth);
			if(setPrefixLabelWidth != PrefixLabelWidth)
			{
				manullySetPrefixLabelWidth = setPrefixLabelWidth;
				PrefixLabelWidth = setPrefixLabelWidth;
			}
			//DrawGUI.PrefixLabelWidth = setPrefixLabelWidth;
		}

		/// <inheritdoc cref="IDrawer.DrawSelectionRect" />
		public override void DrawSelectionRect()
		{
			//UPDATE: Always draw the main selection rect
			//no matter which header part is selected.
			//Easier to see what is selected,
			//easier to understand that copy-paste works
			//no matter which header part is selected etc.
			DrawGUI.DrawEdgeSelectionIndicator(SelectionRect);

			if(selectedPart != null)
			{
				selectedPart.DrawSelectionRect();
			}
		}

		/// <inheritdoc/>
		public override bool DrawPrefix(Rect position)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("UnityObjectDrawer.Draw");
			#endif

			bool guiChangedWas = GUI.changed;
			GUI.changed = false;

			bool dirty = false;

			beforeHeaderControlId = KeyboardControlUtility.Info.LastControlID;
			
			#if DEV_MODE
			if(!position.height.Equals(HeaderHeight))
			{
				Debug.LogError(Msg("DrawPrefix position.height (", position.height, ") != HeaderHeight (", HeaderHeight, ") in ", ToString(), " with labelLastDrawPosition=", labelLastDrawPosition, " with HeadlessMode=", UnityObjectDrawerUtility.HeadlessMode, ", event=", Event.current));
			}
			#endif

			DrawHeaderBase(position);

			DrawHeaderParts();

			// Only draw buttons if header is high enough (i.e. asset header instead of the smaller component header)
			if(HeaderHeight > DrawGUI.Active.InspectorTitlebarHeight)
			{
				DrawHeaderButtons();
			}
			
			if(GUI.changed)
			{
				dirty = true;
			}
			GUI.changed = guiChangedWas;

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif

			if(IsComponent)
			{
				/*
				var group = position;
				group.height += 1000000f;
				GUI.BeginGroup(group);
				GUILayout.Space(position.height);
				//GUILayout.Space(500f);
				GUILayout.Button(GUIContent.none, EditorStyles.label, GUILayout.Height(0f));
				position.x = 0f;
				position.y = 0f;
				*/
				//EditorGUIDrawer.AfterComponentHeaderGUI?.Invoke(targets, position, selectedPart != HeaderPart.None);
				/*
				var y = GUILayoutUtility.GetLastRect().y;
				//GUILayout.Space(500f);
				GUI.EndGroup();
				GUILayout.Space(y - position.yMax);
				*/
			}

			return dirty;
		}

		/// <summary>
		/// Draws header parts, including the toolbar icons found on the top right corner of hte header.
		/// </summary>
		protected virtual void DrawHeaderParts()
		{
			if(Event.current.type != EventType.Repaint)
			{
				return;
			}

			DrawGUI.Active.ColorRect(prefixIconBackgroundPosition, PrefixBackgroundColor);
			headerParts.Draw();
		}

		/// <summary> Handles highlighting header text when there's a search filter. </summary>
		/// <param name="position"> The header draw position and dimensions. </param>
		/// <param name="xOffset"> The local x-axis offset for where the highlighting rect should be drawn. </param>
		/// <param name="yOffset"> The local x-axis offset for where the highlighting rect should be drawn. </param>
		protected void HandlePrefixHighlightingForFilter(Rect position, float xOffset, float yOffset)
		{
			if(lastPassedFilterTestType != FilterTestType.None && inspector.State.SearchFilter.HasFilterAffectingInspectedTargetContent)
			{
				var highlightRect = PrefixDrawer.GetTextHighlightRectForFilter(label.text, position, InspectorPreferences.Styles.TitleText, inspector.State.SearchFilter, label.text, lastPassedFilterTestType, xOffset, yOffset, 0f);
				if(highlightRect.HasValue)
				{
					var rect = highlightRect.Value;
					rect.height = DrawGUI.SingleLineHeight;
					Platform.Active.GUI.ColorRect(rect, inspector.Preferences.theme.FilterHighlight);
				}
			}
		}

		public void Ping()
		{
			ping = this;
			pingProgress = 0f;
			PingStep();
		}

		private void PingStep()
		{
			if(ping != this)
			{
				return;
			}

			pingProgress += Time.deltaTime;
			if(pingProgress >= PingDuration || inactive)
			{
				pingProgress = 0f;
				ping = null;
			}
			else
			{
				OnNextLayout(PingStep);
			}
		}

		/// <summary>
		/// Handles the drawing the basics of the header for the targets, without any additional
		/// bells and whistles added. Basically this only draws what the default Inspector would draw
		/// for the headers of a target.
		/// </summary>
		/// <param name="position"> The position. </param>
		protected virtual void DrawHeaderBase(Rect position)
		{
			#if DEV_MODE //testing ping effect
			if(ping == this)
			{
				GUIStyleUtility.SetInspectorTitlebarPinged();
			}
			#endif

			
			DrawGUI.Active.ComponentHeader(position, Unfolded, targets, Foldable, SelectedHeaderPart, MouseoveredHeaderPart);

			var enabledProperty = MemberHierarchy?.SerializedObject?.FindProperty("m_Enabled");
			if(enabledProperty != null)
			{
				var unappliedChangesRect = position;
				unappliedChangesRect.y += 1f;
				unappliedChangesRect.height -= 2f;
				EditorGUI.BeginProperty(unappliedChangesRect, GUIContent.none, enabledProperty);
				EditorGUI.EndProperty();
			}

			#if DEV_MODE //testing ping effect
			if(ping == this)
			{
				GUIStyleUtility.ResetInspectorTitlebarBackground();
			}
			#endif
		}

		/// <inheritdoc cref="IDrawer.OnRightClick" />
		public override bool OnRightClick(Event inputEvent)
		{
			if(mouseoveredPart != null && mouseoveredPart.OnRightClicked(this, inputEvent))
			{
				return true;
			}

			return base.OnRightClick(inputEvent);
		}
		
		/// <summary>
		/// If header parts have been built, rebuilds them and updates their positions.
		/// Otherwise does nothing.
		/// </summary>
		/// <returns> True if header parts had been built, false if not. </returns>
		protected bool RebuildHeaderToolbar()
		{
			if(headerParts.Count == 0)
			{
				#if DEV_MODE
				Debug.LogWarning("Ignoring RebuildHeaderToolbar because not yet built.");
				#endif
				return false;
			}

			ForceRebuildHeaderToolbar();
			return true;
		}

		/// <summary>
		/// Clears current header parts, rebuilds them and updates their positions.
		/// </summary>
		private void ForceRebuildHeaderToolbar()
		{
			#if DEV_MODE
			//Debug.Log(ToString()+".ForceRebuildHeaderToolbar");
			#endif

			headerParts.Clear();
			DoBuildHeaderToolbar();
			HeaderToolbarItemAttributeUtility.GetAdditionalHeaderToolbarItems(Type, headerParts);
			
			string baseTooltip = ""; //inspector.Preferences.enableTutorialTooltips ? (IsPrefab ? "Middle Mouse : Ping Prefab In Assets" : "Middle Mouse : Ping In Hierarchy") : "";
			AddHeaderToolbarItem(HeaderPartDrawer.Create(HeaderPart.Base, false, false, baseTooltip, null, null, true, GetBaseRect));

			if(HasAddressableBar())
			{
				AddHeaderToolbarItem(HeaderPartDrawer.Create(HeaderPart.AddressablesBar, false, false, "", OnAddressablesToolbarClicked, null, true, GetAddressablesToolbarPosition));
			}

			if(HasEnabledFlag)
			{
				// changing unfolded state is handled by OnMouseUpAfterDownOverControl
				AddHeaderToolbarItem(HeaderPartDrawer.Create(HeaderPart.EnabledFlag, true, true, "Change the enabled state of the Component.", null, null, true, GetEnabledFlagPosition));
			}

			if(Foldable)
			{
				AddHeaderToolbarItem(HeaderPartDrawer.Create(HeaderPart.FoldoutArrow, true, true, "", OnFoldArrowClicked, OnFoldArrowRightClicked, false, GetFoldArrowPosition));
			}

			#if DEV_MODE && PI_ASSERTATIONS
			bool hadEnabledFlag = HasEnabledFlag;
			OnNextLayout(()=>
			{
				if(hadEnabledFlag != HasEnabledFlag && !inactive)
				{
					Debug.LogError(ToString()+ ".BuildHeaderPartInfos was called before ready to call HasEnabledFlag.");
				}
			});
			bool wasExpandable = Foldable;
			OnNextLayout(()=>
			{
				if(wasExpandable != Foldable && !inactive)
				{
					Debug.LogError(ToString()+ ".BuildHeaderPartInfos was called before ready to call Foldable. wasExpandable="+ wasExpandable);
				}
			});
			#endif

			if(labelLastDrawPosition.width > 0f)
			{
				UpdateHeaderToolbarPositions(labelLastDrawPosition);
			}
		}

		/// <summary>
		/// Generates header parts for all selectable parts of the header.
		/// This can include things like enabled flag, debug mode toggle, reference icon, preset icon
		/// and context menu icon.
		/// </summary>
		protected virtual void DoBuildHeaderToolbar()
		{
			if(HasContextMenuIcon)
			{
				AddHeaderToolbarItem(HeaderPartDrawer.Create(HeaderPart.ContextMenuIcon, true, true, "Open context menu for target Object", OnContextMenuIconClicked));
			}

			#if UNITY_2018_1_OR_NEWER
			if(HasPresetIcon)
			{
				AddHeaderToolbarItem(HeaderPartDrawer.Create(HeaderPart.PresetIcon, true, true, "Select Preset to use for target Object or save current state to a new Preset.", null));
			}
			#endif

			if(HasReferenceIcon)
			{
				string title;
				string docUrl = OverrideDocumentationUrl(out title);
				if(docUrl.Length > 0)
				{
					AddHeaderToolbarItem(HeaderPartDrawer.Create(HeaderPart.ReferenceIcon, true, true, "Open documentation for "+title, null));
				}
				else
				{
					AddHeaderToolbarItem(HeaderPartDrawer.Create(HeaderPart.ReferenceIcon, true, true, "", OpenDocumentation));
				}
			}

			if(HasExecuteMethodIcon)
			{
				AddHeaderToolbarItem(HeaderPartDrawer.Create(HeaderPart.QuickInvokeMenuButton, true, true, InspectorUtility.Preferences.graphics.ExecuteIcon, "Invoke methods on target Object.\nRight-Click to also list invisible methods.", OnExecuteMethodIconClicked, OnExecuteMethodIconRightClicked));
			}
			
			if(HasDebugModeIcon)
			{
				AddHeaderToolbarItem(HeaderPartDrawer.Create(HeaderPart.DebugModePlusButton, true, true, debugMode ? InspectorUtility.Preferences.graphics.DebugModeOnIcon : InspectorUtility.Preferences.graphics.DebugModeOffIcon, debugMode ? "Disable Debug Mode+ for this target.\nAll hidden class members will no longer be shown." : "Enable Debug Mode+ for this target.\nAll hidden class members will be shown.", OnDebugModeIconClicked));
			}
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			ParentDrawerUtility.BuildMembers(DrawerProvider, this, memberBuildList, ref members);
			
			if(inspector.Preferences.drawScriptReferenceFields || DebugMode)
			{
				var monoScript = MonoScript;
				if(monoScript != null)
				{
					DrawerArrayPool.InsertAt(ref members, 0, ScriptReferenceDrawer.Create(monoScript, this, false), true);
				}
			}

			if(DebugMode && (members.Length == 0 || !(members[0] is DebugModeDisplaySettingsDrawer)))
			{
				#if DEV_MODE
				Debug.Log("InsertAt(0, DebugModeDisplaySettingsDrawer)");
				#endif

				DrawerArrayPool.InsertAt(ref members, 0, SpaceDrawer.Create(7f, this), true);
				DrawerArrayPool.InsertAt(ref members, 0, DebugModeDisplaySettingsDrawer.Create(this, debugModeDisplaySettings), true);
				DrawerArrayPool.InsertAt(ref members, 0, SpaceDrawer.Create(7f, this), true);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!members.ContainsNullMembers());
			#endif
		}

		private void OpenDocumentation(IUnityObjectDrawer containingDrawer, Rect buttonRect, Event inputEvent)
		{
			DrawGUI.Use(inputEvent);

			string title;
			string fullUrl;
			string urlBase = OverrideDocumentationUrl(out title);
			if(string.IsNullOrEmpty(urlBase))
			{
				urlBase = "class-" + Type.Name;

				// C:/Program Files/Unity/Hub/Editor/2020.2.0b13/Editor/ etc.
				var documentationPath = Path.GetDirectoryName(EditorApplication.applicationPath);
				documentationPath = Path.Combine(documentationPath, "Data/Documentation/En/Manual/");
				if(Directory.Exists(documentationPath))
				{
					fullUrl = "file:///" + Path.Combine(documentationPath, urlBase + ".html");

					if(!File.Exists(fullUrl))
					{
						fullUrl = "https://docs.unity3d.com/Manual/" + urlBase + ".html";
					}
				}
				else
				{
					fullUrl = "https://docs.unity3d.com/Manual/" + urlBase + ".html";
				}
			}
			else if(urlBase.StartsWith("unity/", StringComparison.Ordinal))
			{
				fullUrl ="https://docs.unity3d.com/Manual/" + urlBase.Substring(6) + ".html";
			}
			else if(urlBase.StartsWith("powerinspector/", StringComparison.Ordinal))
			{
				fullUrl = PowerInspectorDocumentation.GetUrl(urlBase.Substring(15));
			}
			else if(!urlBase.StartsWith("http", StringComparison.Ordinal))
			{
				fullUrl ="https://docs.unity3d.com/Manual/" + urlBase + ".html";
			}
			else
			{
				fullUrl = urlBase;
			}

			Application.OpenURL(fullUrl);
		}
		
		/// <summary>
		/// This can be overridden to have clicking the reference / documentation icon lead to a different webpage than the default one.
		/// </summary>
		/// <returns> Documentation url override, or an empty string if should use default url. </returns>
		/// <param name="documentationTitle"> Title of the documentation page. </param>
		/// <returns></returns>
		[NotNull]
		protected virtual string OverrideDocumentationUrl([NotNull]out string documentationTitle)
		{
			documentationTitle = "";
			return "";
		}

		/// <summary>
		/// Registers a new button that should appear at the base of the header.
		/// Buttons will be drawn in registered order, starting from right to left
		/// </summary>
		/// <param name="button"> The button to add the the header. </param>
		protected void AddHeaderToolbarItem(HeaderPartDrawer button)
		{
			headerParts.Add(button);
		}

		/// <summary>
		/// Registers a new button that should appear at the base of the header.
		/// Buttons will be drawn in registered order, starting from right to left.
		/// </summary>
		/// <param name="index"> Zero-based index of new header part, starting from the right. </param>
		/// <param name="button"> The button to add the the header. </param>
		protected void InsertHeaderPart(int index, HeaderPartDrawer button)
		{
			headerParts.Insert(index, button);
		}

		private void OnFoldArrowClicked(IUnityObjectDrawer containingDrawer, Rect clickedRect, Event inputEvent)
		{
			// use event and clear reordering info, even if Component is not currently expandable
			// otherwise systems might think that we are reordering the Component, even if the user
			// was just attempting to unfold the Component
			DrawGUI.Use(inputEvent);
			Manager.MouseDownInfo.Reordering.Clear();

			if(Foldable)
			{
				bool collapseAllOthers = inputEvent == null ? false : inputEvent.control;
				bool setChildrenAlso = inputEvent == null ? false : inputEvent.alt;
				SetUnfolded(!Unfolded, collapseAllOthers, setChildrenAlso);
				SelectHeaderPart(headerParts.Base);
				ExitGUIUtility.ExitGUI();
			}
		}

		private void OnFoldArrowRightClicked(IUnityObjectDrawer containingDrawer, Rect buttonRect, Event inputEvent)
		{
			if(Foldable)
			{
				DrawGUI.Use(inputEvent);
				Manager.MouseDownInfo.Clear();
				
				var menu = Menu.Create();
				menu.Add("Expand All Components", inspector.UnfoldAllComponents);
				menu.Add("Collapse All Components", inspector.FoldAllComponents);
				menu.AddSeparator();
				menu.Add("Expand All Children", () => SetUnfolded(true, false, true));
				menu.Add("Collapse All Children", () => SetUnfolded(false, false, true));
				SelectHeaderPart(headerParts.Base);
				var openPosition = buttonRect;
				openPosition.y += HeaderHeight;
				menu.OpenAt(openPosition);
			}
		}

		private void OnDebugModeIconClicked(IUnityObjectDrawer containingDrawer, Rect buttonRect, Event inputEvent)
		{
			DrawGUI.Use(inputEvent);
			if(debugMode)
			{
				DisableDebugMode();
			}
			else
			{
				EnableDebugMode();
			}

			//added to fix issue where first control would get selected
			//after changing debug mode
			KeyboardControlUtility.KeyboardControl = DebugModeIconControlId;

			ExitGUIUtility.ExitGUI();
		}

		private void OnExecuteMethodIconClicked(IUnityObjectDrawer containingDrawer, Rect buttonRect, Event inputEvent)
		{
			DrawGUI.Use(inputEvent);
			OpenExecuteMethodMenu(DebugMode || inputEvent.control);
			KeyboardControlUtility.KeyboardControl = ExecuteMethodIconControlId;

			ExitGUIUtility.ExitGUI();
		}

		private void OnExecuteMethodIconRightClicked(IUnityObjectDrawer containingDrawer, Rect buttonRect, Event inputEvent)
		{
			DrawGUI.Use(inputEvent);
			OpenExecuteMethodMenu(true);
			KeyboardControlUtility.KeyboardControl = ExecuteMethodIconControlId;
		}

		/// <summary> Updates the header button positions described by headerRect. </summary>
		/// <param name="headerRect"> The current bounds of the header. </param>
		protected virtual void UpdateHeaderToolbarPositions(Rect headerRect)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(HeadlessMode)
			{
				Debug.Assert(headerRect.width <= 0f, "UpdateHeaderToolbarPositions called with HeadlessMode="+StringUtils.True+" but headerRect=" + headerRect);
			}
			else
			{
				Debug.Assert(headerRect.width > 0f, "UpdateHeaderToolbarPositions called with HeadlessMode=" + StringUtils.False + " but headerRect " + headerRect);
				Debug.Assert(headerRect.height > 0f, "UpdateHeaderToolbarPositions called with HeadlessMode=" + StringUtils.False + " but headerRect " + headerRect);
			}
			#endif

			var firstRect = GetRectForFirstHeaderToolbarControl(headerRect);
			var nextRect = firstRect;
			
			prefixIconBackgroundPosition = firstRect;
			prefixIconBackgroundPosition.x += firstRect.width;
			prefixIconBackgroundPosition.width = 0f;

			float adjustPosition = nextRect.width + HeaderToolbarIconsOffset;

			for(int n = 0, count = headerParts.Count; n < count; n++)
			{
				var part = headerParts[n];
				var overrideCalculatePosition = part.OverrideCalculatePosition;
				if(overrideCalculatePosition != null)
				{
					part.Rect = overrideCalculatePosition(headerRect);
				}
				else
				{
					part.Rect = nextRect;

					nextRect.x -= adjustPosition;

					prefixIconBackgroundPosition.x -= adjustPosition;

					// If texture is null don't increase bg width; we don't want to obscure the built-in icons.
					if(part.Texture != null)
					{
						prefixIconBackgroundPosition.width += adjustPosition;
					}

					if(nextRect.x <= 0f)
					{
						#if DEV_MODE
						Debug.LogWarning("Not enough room for all header buttons. headerRect="+ headerRect+ ", nextRect="+ nextRect+", n="+n+", count="+count);
						#endif
						break;
					}
				}
			}
		}

		protected Rect GetRectForFirstHeaderToolbarControl(Rect headerRect)
		{
			var rect = headerRect;
			rect.x = headerRect.xMax - HeaderToolbarIconsRightOffset - HeaderToolbarIconWidth;
			rect.y += ToolbarIconsTopOffset;

			if(HeadlessMode)
			{
				rect.width = 0f;
				rect.height = 0f;
			}
			else
			{
				rect.width = HeaderToolbarIconWidth;
				rect.height = HeaderToolbarIconHeight;
			}

			rect.y -= (rect.height - 10f) * 0.25f;

			return rect;
		}

		private void OnContextMenuIconClicked(IUnityObjectDrawer containingDrawer, Rect buttonRect, Event inputEvent)
		{
			OpenContextMenu(inputEvent, buttonRect, false, (Part)HeaderPart.ContextMenuIcon);
		}

		/// <summary> Determines if mouse is currently over a header button AND updates
		/// mouseovereredHeaderButtonRect with Rect of button NOTE: This should only be called from
		/// inside the Draw method, e.g. from OnLayoutEvent, as otherwise comparing position values to
		/// mousePosition might not be accurate due to how GUILayout works. </summary>
		/// <param name="newMouseovereredPart"> [out] The part of the header that is now being mouseoverered. </param>
		/// <returns> True if cursor is over any header part, otherwise false. </returns>
		private bool GetMouseoveredHeaderPart(out HeaderPartDrawer newMouseovereredPart)
		{
			if(mouseoveredPart != null && !mouseoveredPart.RectIsValid)
			{
				if(headerParts.Contains(mouseoveredPart))
				{
					#if DEV_MODE
					Debug.LogWarning("GetMouseoveredHeaderPart called with !mouseoveredPart.RectIsValid. Returning previously mouseovered part until rect has time to update.");
					#endif
					newMouseovereredPart = mouseoveredPart;
					return true;
				}
				else
				{
					#if DEV_MODE
					Debug.LogWarning("GetMouseoveredHeaderPart called with !mouseoveredPart.RectIsValid. Returning part with id matching previously mouseovered part until rect has time to update.");
					#endif
					newMouseovereredPart = headerParts[mouseoveredPart.Part];
					return newMouseovereredPart != null;
				}
			}

			for(int n = headerParts.Count - 1; n >= 0; n--)
			{
				var part = headerParts[n];
				if(part.MouseIsOver())
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(part.Part != HeaderPart.Base);
					#endif

					newMouseovereredPart = part;
					return true;
				}
			}
			newMouseovereredPart = null;
			return false;
		}

		/// <summary>
		/// Registers a new button that should appear at the base of the header.
		/// Buttons will be drawn in registered order, starting from right to left
		/// </summary>
		/// <param name="button"> The button to add the the header. </param>
		protected void AddHeaderButton(Button button)
		{
			headerButtons.Add(button);
		}

		/// <summary>
		/// Draws buttons at the base of the header.
		/// </summary>
		protected virtual void DrawHeaderButtons()
		{
			DrawGUI.Active.ColorRect(headerButtons.Bounds, PrefixBackgroundColor);

			for(int n = 0, count = headerButtons.Count; n < count; n++)
			{
				headerButtons[n].Draw(InspectorPreferences.Styles.MiniButton);
			}
		}

		/// <inheritdoc />
		protected void HideInternalOpenButton()
		{
			var firstButtonRect = GetRectForFirstHeaderButtonEnd(labelLastDrawPosition);
			
			var internalOpenButtonRect = firstButtonRect;
			internalOpenButtonRect.x -= InternalOpenButtonWidth;
			internalOpenButtonRect.width = InternalOpenButtonWidth;
			internalOpenButtonRect.height = 18f;

			// Consume GUI.button based inputs before event reaches DrawHeaderBase and can invoke the internally drawn Open button.
			if(GUI.Button(internalOpenButtonRect, GUIContent.none, InspectorPreferences.Styles.Blank))
			{
				#if DEV_MODE
				Debug.LogWarning("Consumed GUI.Button click events to prevent internal Open button from reacting.");
				#endif
			}

			// Visually hide the internal Open button by painting over it with the background color
			DrawGUI.Active.ColorRect(internalOpenButtonRect, PrefixBackgroundColor);
		}

		/// <summary>
		/// If header buttons have been built, rebuilds them and updates their positions.
		/// Otherwise does nothing.
		/// </summary>
		/// <returns> True if header buttons had been built, false if not. </returns>
		protected bool RebuildHeaderButtons()
		{
			if(headerButtons.Count == 0)
			{
				#if DEV_MODE
				Debug.LogWarning("Ignoring RebuildHeaderButtons because not yet built.");
				#endif
				return false;
			}
			ForceRebuildHeaderButtons();
			return true;
		}

		/// <summary>
		/// Clears current header buttons, rebuilds them and updates their positions.
		/// </summary>
		private void ForceRebuildHeaderButtons()
		{
			headerButtons.onButtonRectsChanged = null;

			headerButtons.Clear();
			DoBuildHeaderButtons();
			UpdateHeaderButtonPositions(labelLastDrawPosition);

			headerButtons.onButtonRectsChanged = OnHeaderButtonRectsChanged;
			OnHeaderButtonRectsChanged(headerButtons);
		}
		
		/// <summary>
		/// Generates header buttons that should be drawn at the base of the header.
		/// Called by
		/// </summary>
		protected virtual void DoBuildHeaderButtons()
		{
			
		}

		private void OnHeaderButtonRectsChanged(Buttons buttons)
		{
			headerButtonsWidth = headerButtons.Width();
		}

		/// <summary> Updates positions of the all buttons found on the bottom right of the header, given the full rectangle of header. </summary>
		/// <param name="headerRect"> The header rectangle. </param>
		private void UpdateHeaderButtonPositions(Rect headerRect)
		{
			var pos = GetRectForFirstHeaderButtonEnd(headerRect);

			var fontSizes = Fonts.SmallSizes;
			if(fontSizes == null)
			{
				if(Event.current != null)
				{
					Fonts.Setup();
				}
				else
				{
					#if DEV_MODE
					Debug.LogError(ToString()+ ".UpdateHeaderButtonPositions called with Event.current null and Fonts setup not done.");
					#endif
					return;
				}
			}

			for(int n = 0, count = headerButtons.Count; n < count; n++)
			{
				var button = headerButtons[n];
				if(pos.x > 0f)
				{
					pos.width = fontSizes.GetLabelWidth(button.Label) + 12f; //"minibutton" style
					pos.x -= (pos.width + Buttons.ButtonDrawOffset);
					button.Rect = pos;
				}
			}
		}

		/// <summary> Gets rectangle that resides on the right side of the first header button (starting from the right). </summary>
		/// <param name="headerRect"> The rectangle for the whole header area. </param>
		/// <returns> Position on right side of first header button. </returns>
		protected Rect GetRectForFirstHeaderButtonEnd(Rect headerRect)
		{
			var pos = headerRect;
			pos.x = headerRect.width - Buttons.ButtonDrawOffset + 1f;
			pos.y += 28f;
			pos.height = 18f;
			return pos;
		}

		/// <summary> Determines if mouse is currently over a header button AND updates
		/// mouseovereredHeaderButtonRect with Rect of button NOTE: This should only be called from
		/// inside the Draw method, e.g. from OnLayoutEvent, as otherwise comparing position values to
		/// mousePosition might not be accurate due to how GUILayout works. </summary>
		/// <param name="mouseovereredButton"> [out] The mouseoverered button. </param>
		/// <returns> True if cursor is over any header button, otherwise false. </returns>
		private bool GetMouseoveredHeaderButton(out Button mouseovereredButton)
		{
			for(int n = headerButtons.Count - 1; n >= 0; n--)
			{
				var button = headerButtons[n];
				if(button.MouseIsOver())
				{
					mouseovereredButton = button;
					return true;
				}
			}
			mouseovereredButton = null;
			return false;
		}

		/// <inheritdoc cref="IDrawer.OnMouseover" />
		public override void OnMouseover()
		{
			if(HeaderMouseovered)
			{
				Rect mouseoveredRect;
				if(mouseoveredPart != null)
				{
					if(!mouseoveredPart.DrawMouseoverRect)
					{
						//if(inspector.Preferences.enableTutorialTooltips && mouseoveredPart == HeaderPart.Base)
						//{
						//	GUI.Label(labelLastDrawPosition, new GUIContent("", IsPrefab ? "Middle Mouse : Ping Prefab In Assets" : "Middle Mouse : Ping In Hierarchy"));
						//}
						return;
					}
					mouseoveredRect = mouseoveredPart.Rect;
				}
				else
				{
					if(mouseovereredHeaderButton != null)
					{
						mouseoveredRect = mouseovereredHeaderButton.Rect;
					}
					else
					{
						//if(inspector.Preferences.enableTutorialTooltips)
						//{
						//	GUI.Label(labelLastDrawPosition, new GUIContent("", IsPrefab ? "Middle Mouse : Ping Prefab In Assets" : "Middle Mouse : Ping In Hierarchy"));
						//}

						var rect = lastDrawPosition;
						rect.height = Height;
						rect.y += 1f;
						rect.height -= 2f;
						rect.width -= 1f;

						if(Foldable && (Selected || InspectorUtility.Preferences.changeFoldedStateOnFirstClick))
						{
							mouseoveredRect = rect;
						}
						else
						{
							if(InspectorUtility.Preferences.mouseoverEffects.unityObjectHeader)
							{
								DrawGUI.DrawLeftClickAreaMouseoverEffect(rect, localDrawAreaOffset);
							}
							return;
						}
					}
				}
				
				DrawGUI.Active.AddCursorRect(mouseoveredRect, MouseCursor.Link);
			}
		}

		/// <inheritdoc cref="IDrawer.OnMouseUpAfterDownOverControl" />
		public override void OnMouseUpAfterDownOverControl(Event inputEvent, bool isClick)
		{
			#if DEV_MODE && DEBUG_ON_CLICK
			Debug.Log(Msg(GetType().Name, ".OnMouseUpAfterDownOverControl(isClick=", isClick, ") with MouseoveredHeaderPart=", MouseoveredHeaderPart, ", HeaderMouseovered=", HeaderMouseovered, ", Selectable=", Selectable, ", SelectedHeaderPart=", (HeaderPart)SelectedHeaderPart, ", selectedHeaderPartOnClickStart=", selectedHeaderPartOnClickStart, ", event = ", StringUtils.ToString(inputEvent), ", mousePos=", inputEvent.mousePosition, ", CanBeSelectedWithoutHeaderBeingSelected=", CanBeSelectedWithoutHeaderBeingSelected, ", MouseDownEventWasUsed=", Inspector.Manager.MouseDownInfo.MouseDownEventWasUsed));
			#endif

			//handle button clicks and unfolded state change
			if(isClick)
			{
				HeaderPart selectedWas = selectedPart;

				// This fixes issue where component foldout arrow would get stuck black after being clicked once.
				if((selectedWas == HeaderPart.FoldoutArrow || selectedWas == HeaderPart.Base) && mouseoveredPart != HeaderPart.AddressablesBar)
				{
					KeyboardControlUtility.JustClickedControl = 0;
				}
				
				if(!Selected)
				{
					Select(HeaderMouseovered ? ReasonSelectionChanged.PrefixClicked : ReasonSelectionChanged.ControlClicked);
				}
				
				#if DEV_MODE && PI_ASSERTATIONS
				if(isClick && (SelectedHeaderPart != MouseoveredHeaderPart)) { Debug.LogWarning(Msg(GetType().Name, ".OnMouseUpAfterDownOverControl isClick=", isClick, " but SelectedHeaderPart (", SelectedHeaderPart, ") != MouseoveredHeaderPart (", MouseoveredHeaderPart, ")")); }
				#endif

				if(mouseoveredPart == HeaderPart.Base)
				//if(selectedPart == HeaderPart.Base)
				{
					if(mouseovereredHeaderButton != null)
					{
						DrawGUI.Use(inputEvent);
					}
					else if(Foldable)
					{
						if(InspectorUtility.Preferences.changeFoldedStateOnFirstClick || selectedHeaderPartOnClickStart != HeaderPart.None)
						{
							DrawGUI.Use(inputEvent);

							// allow bypassing EditComponentsOneAtATime functionality by holding down control when clicking the Component header
							bool collapseAllOthers = UserSettings.EditComponentsOneAtATime && !inputEvent.control;
							bool setChildrenAlso = Event.current == null ? false : Event.current.alt;
							bool setUnfolded = !Unfolded;
							SetUnfolded(setUnfolded, collapseAllOthers, setChildrenAlso);
							if(setUnfolded)
							{
								ExitGUIUtility.ExitGUI();
							}
						}
					}
				}
			}
			//handle drag n drop
			//TO DO: Cache the results of the mouse over check during OnLayoutEvent
			else
			{
				var myRect = lastDrawPosition;
				myRect.y -= 9f;
				myRect.height = Height + 18f;

				// if mouse was pressed down with the cursor on this control and then released
				// possibly with the cursor on another control, it might have been a
				// drag n drop reordering event. Send the OnMouseUpAfterDownOverControl event
				// up the chain to the GameObjectDrawer and let it handle it
				if(!myRect.Contains(inputEvent.mousePosition))
				{
					if(parent != null)
					{
						#if DEV_MODE && DEBUG_ON_CLICK
						Debug.Log(ToString()+" calling OnMouseUpAfterDownOverControl in parent "+parent);
						#endif

						parent.OnMouseUpAfterDownOverControl(inputEvent, false);
					}
				}
			}
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			var target = Target;

			if(Editable)
			{
				menu.AddSeparatorIfNotRedundant();
				menu.Add("Reset", Reset);

				if(IsPrefabInstance && HasUnappliedChanges)
				{
					TTarget prefabAncestor;
					for(prefabAncestor = PrefabUtility.GetCorrespondingObjectFromSource(target); PrefabUtility.IsPartOfVariantPrefab(prefabAncestor); prefabAncestor = PrefabUtility.GetCorrespondingObjectFromSource(prefabAncestor))
					{
						menu.Add("Instance Overrides/Apply Overrides To Prefab Variant '" + prefabAncestor.name + "'", ApplyOverridesToPrefab, prefabAncestor);
					}
					menu.Add("Instance Overrides/Apply Overrides To Prefab '" + prefabAncestor.name + "'", ApplyOverridesToPrefab, prefabAncestor);
					menu.Add("Instance Overrides/Revert Overrides", RevertOverridesFromPrefab);
				}
			}

			AddCopyPasteMenuItems(ref menu);

			bool isComponent = IsComponent;
			bool isTransform = target is Transform;
			
			if(isComponent)
			{
				menu.AddSeparatorIfNotRedundant();

				var gameObjectDrawer = GameObjectDrawer;
				if(gameObjectDrawer != null && Destroyable)
				{
					menu.Add("Remove Component", ()=>
					{
						gameObjectDrawer.DeleteMember(this);
					});
				}

				if(!isTransform)
				{
					if(CanMoveComponentUp(true))
					{
						menu.Add("Move Up", MoveUp);
					}
					if(CanMoveComponentDown(true))
					{
						menu.Add("Move Down", MoveDown);
					}
				}
			}

			menu.AddSeparatorIfNotRedundant();

			var monoScript = MonoScript;
			if(monoScript != null)
			{
				menu.Add("Edit Script", ()=>AssetDatabase.OpenAsset(monoScript));
				menu.Add("Select Script", ()=>
				{
					EditorGUIUtility.PingObject(monoScript);
					inspector.Select(monoScript);
				});
			}

			bool hasTarget = target != null;
			if(hasTarget)
			{
				menu.Add("Ping", ()=>DrawGUI.Ping(target));

				if(!isComponent && !inspector.State.ViewIsLocked)
				{
					var selectedObjects = inspector.SelectedObjects;
					int targetIndexInSelection = Array.IndexOf(selectedObjects, target);
					if(targetIndexInSelection != -1)
					{
						menu.Add("Deselect", ()=>
						{
							if(UserSettings.MergedMultiEditMode || selectedObjects.Length == 1)
							{
								inspector.Select(null as Object);
							}
							else
							{
								inspector.Select(inspector.SelectedObjects.RemoveAt(targetIndexInSelection));
							}
						});
					}
				}
			}

			menu.AddSeparatorIfNotRedundant();

			//these should also support missing components
			menu.Add("Select Previous Of Type", SelectPreviousOfType, false);
			menu.Add("Select Next Of Type", SelectNextOfType, false);
			
			// This is generated from MenuItemAttribute for Components
			if(hasTarget && !IsComponent)
			{
				menu.Add("Find References In Scene", FindReferencesInScene);
			}
			
			if(extendedMenu)
			{
				menu.AddSeparatorIfNotRedundant();

				if(hasTarget && !isTransform)
				{
					if(GuiEnabled)
					{
						menu.Add("Auto-Name", NameByType);
					}
					else
					{
						menu.AddDisabled("Auto-Name");
					}
				}

				menu.AddSeparatorIfNotRedundant();

				menu.Add("Debug Mode+/On", EnableDebugMode, DebugMode);
				menu.Add("Debug Mode+/Off", DisableDebugMode, !DebugMode);

				menu.Add("Randomize", Randomize);
			}
			
			if(hasTarget)
			{
				var hideFlags = target.hideFlags;
				if(hideFlags != HideFlags.None || extendedMenu)
				{
					if(!extendedMenu)
					{
						menu.AddSeparatorIfNotRedundant();
					}

					menu.Add("Hide Flags/None", ()=> ToggleHideFlags(HideFlags.None), hideFlags == HideFlags.None);
					menu.Add("Hide Flags/Hide In Hierarchy", () => ToggleHideFlags(HideFlags.HideInHierarchy), hideFlags.HasFlag(HideFlags.HideInHierarchy));
					menu.Add("Hide Flags/Hide In Inspector", () => ToggleHideFlags(HideFlags.HideInInspector), hideFlags.HasFlag(HideFlags.HideInInspector));
					menu.Add("Hide Flags/Don't Save In Editor", () => ToggleHideFlags(HideFlags.DontSaveInEditor), hideFlags.HasFlag(HideFlags.DontSaveInEditor));
					menu.Add("Hide Flags/Not Editable", () => ToggleHideFlags(HideFlags.NotEditable), hideFlags.HasFlag(HideFlags.NotEditable));
					menu.Add("Hide Flags/Don't Save In Build", () => ToggleHideFlags(HideFlags.DontSaveInBuild), hideFlags.HasFlag(HideFlags.DontSaveInBuild));
					menu.Add("Hide Flags/Don't Unload Unused Asset", () => ToggleHideFlags(HideFlags.DontUnloadUnusedAsset), hideFlags.HasFlag(HideFlags.DontUnloadUnusedAsset));
					menu.Add("Hide Flags/Don't Save", () => ToggleHideFlags(HideFlags.DontSave), hideFlags.HasFlag(HideFlags.DontSave));
					menu.Add("Hide Flags/Hide And Don't Save", () => ToggleHideFlags(HideFlags.HideAndDontSave), hideFlags.HasFlag(HideFlags.HideAndDontSave));
				}
			}

			#if DEV_MODE
			if(extendedMenu)
			{
				menu.Add("Debugging/Test Ping", Ping);
				menu.Add("Debugging/Headless Mode", () => UnityObjectDrawerUtility.HeadlessMode = !UnityObjectDrawerUtility.HeadlessMode, UnityObjectDrawerUtility.HeadlessMode);
			}
			#endif
			
			ParentDrawerUtility.AddMenuItemsFromContextMenuAttribute(GetValues(), ref menu);
			MenuItemAttributeUtility.AddItemsFromMenuItemAttributesToContextMenu(menu, targets);

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}
		
		/// <summary>
		/// Apply values to prefab.
		/// </summary>
		private void ApplyOverridesToPrefab(object prefabAncestor)
		{
			UndoHandler.RegisterUndoableAction(UnityObjects, StringUtils.Concat("Apply ", GetFieldNameForMessages(), " Overrides To Prefab"));

			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var target = targets[n];
				PrefabUtility.ApplyObjectOverride(target, AssetDatabase.GetAssetPath(prefabAncestor as Object), InteractionMode.UserAction);
			}

			var serializedObject = MemberHierarchy.SerializedObject;
			serializedObject.Update();
			UpdateCachedValuesFromFieldsRecursively();
			OnValidate();
		}

		/// <summary>
		/// Revert values to prefab.
		/// </summary>
		private void RevertOverridesFromPrefab()
		{
			UndoHandler.RegisterUndoableAction(UnityObjects, StringUtils.Concat("Revert ", GetFieldNameForMessages(), " Overrides"));

			for(int n = targets.Length - 1; n >= 0; n--)
			{
				PrefabUtility.RevertObjectOverride(targets[n], InteractionMode.UserAction);
			}

			var serializedObject = MemberHierarchy.SerializedObject;
			serializedObject.Update();
			UpdateCachedValuesFromFieldsRecursively();
			OnValidate();
		}

		private void ToggleHideFlags(HideFlags flag)
		{
			var target = targets[0];
			int index = targets.Length;
			if(flag == HideFlags.None || !target.hideFlags.HasFlag(flag))
			{
				do
				{
					UndoHandler.RegisterUndoableAction(target, "Add Hide Flag: "+flag);

					target.hideFlags = (HideFlags)target.hideFlags.SetFlag(flag);

					var component = target as Component;
					if(component != null)
					{
						ComponentModifiedCallbackUtility.OnComponentModified(component);
					}

					index--;
					target = targets[index];

					if(!target.IsSceneObject())
					{
						Platform.Active.SetDirty(target);
					}
				}
				while(index > 0);
			}
			else
			{
				do
				{
					UndoHandler.RegisterUndoableAction(target, "Remove Hide Flag: "+flag);

					target.hideFlags = (HideFlags)target.hideFlags.RemoveFlag(flag);

					var component = target as Component;
					if(component != null)
					{
						ComponentModifiedCallbackUtility.OnComponentModified(component);
					}

					index--;
					target = targets[index];

					if(!target.IsSceneObject())
					{
						Platform.Active.SetDirty(target);
					}
				}
				while(index > 0);
			}
		}

		#if DEV_MODE
		/// <inheritdoc/>
		protected override void AddDevModeDebuggingEntriesToRightClickMenu(ref Menu menu)
		{
			base.AddDevModeDebuggingEntriesToRightClickMenu(ref menu);

			menu.Add("Debugging/Select Header", ()=>KeyboardControlUtility.KeyboardControl = controlId);
			menu.Add("Debugging/Print Height", ()=>Debug.Log(ToString()+".height: "+Height));

			if(DrawGreyedOut)
			{
				menu.Add("Debugging/Debug Greyed Out", ()=>
				{
					Debug.Log(StringUtils.ToColorizedString("GreyedOut=", DrawGreyedOut, ", ReadOnly=" , ReadOnly, ", Editable=", Editable,", (Target != null)=", Target != null, "HasFlag(HideFlags.NotEditable)=", (Target == null ? false : Target.hideFlags.HasFlag(HideFlags.NotEditable))));
				});
			}
		}

		/// <inheritdoc/>
		protected override object[] GetDevInfo()
		{
			if(targets.Length > 1)
			{
				return base.GetDevInfo().Add(", Targets=", targets);
			}
			return base.GetDevInfo().Add(", Target=", Target);
		}
		#endif

		protected abstract void FindReferencesInScene();

		/// <inheritdoc/>
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			base.UpdateCachedValuesFromFieldsRecursively();

			if(IsPrefabInstance && MembersAreVisible)
			{
				MemberHierarchy.UpdatePrefabOverrides();
			}
		}

		/// <summary> Copies the asset path clipboard. </summary>
		private void CopyAssetPathClipboard()
		{
			int count = targets.Length;
			if(count >= 2)
			{
				var copy = StringArrayPool.Create(count);
				for(int n = count - 1; n >= 0; n--)
				{
					copy[n] = AssetDatabase.GetAssetPath(targets[n]);
				}
				Clipboard.Copy(copy);
			}
			else
			{
				Clipboard.Copy(AssetDatabase.GetAssetPath(UnityObject));
			}

			SendCopyToClipboardMessage("Copied{0} asset path");
		}

		/// <summary> Copies the hierarchy path to clipboard. </summary>
		private void CopyHierarchyPathToClipboard()
		{
			int count = targets.Length;
			if(count >= 2)
			{
				var transforms = Transforms;
				var copy = StringArrayPool.Create(count);
				for(int n = count - 1; n >= 0; n--)
				{
					copy[n] = transforms[n].GetHierarchyPath();
				}
				Clipboard.Copy(copy);
			}
			else
			{
				
				var firstTransform = targets[0].Transform();
				Clipboard.Copy(firstTransform.GetHierarchyPath());
			}

			SendCopyToClipboardMessage("Copied{0} hierarchy path");
		}

		/// <summary> Determine if we can paste from clipboard. </summary>
		/// <returns> True if we can paste from clipboard, false if not. </returns>
		public override bool CanPasteFromClipboard()
		{
			return !ReadOnly && Clipboard.CanPasteAs(Type);
		}

		/// <summary> Adds a copy paste menu items. </summary>
		/// <param name="menu"> [in,out] The menu. </param>
		protected virtual void AddCopyPasteMenuItems(ref Menu menu)
		{
			menu.AddSeparatorIfNotRedundant();

			menu.Add(CopyContextMenuText, CopyToClipboard);
			
			bool multipleTargets = targets.Length >= 2;
			if(IsAsset)
			{
				menu.Add(multipleTargets ? "Copy Asset Paths" : "Copy Asset Path", CopyAssetPathClipboard);
			}
			else
			{
				menu.Add(multipleTargets ? "Copy Hierarchy Paths" : "Copy Hierarchy Path", CopyHierarchyPathToClipboard);
			}

			#if DEV_MODE && DEBUG_OPEN_MENU
			Debug.Log("AddCopyPasteMenuItems: ReadOnly=" + StringUtils.ToColorizedString(ReadOnly)+ ", Clipboard.HasObjectReference="+ Clipboard.HasObjectReference()+ ", CanPasteValuesFromType("+(Clipboard.CopiedType == null ? "n/a" : StringUtils.ToColorizedString(CanPasteFromClipboard()))+")");
			#endif

			if(!ReadOnly)
			{
				if(Clipboard.HasObjectReference())
				{
					if(CanPasteFromClipboard())
					{
						menu.Add(PasteContextMenuText, PasteFromClipboard);
					}
					
					var copiedType = Clipboard.CopiedType;
					if(IsComponent && Types.Component.IsAssignableFrom(copiedType) && !Types.Transform.IsAssignableFrom(copiedType))
					{
						var gameObjectDrawer = GameObjectDrawer;
						if(gameObjectDrawer != null)
						{
							if(!AddComponentUtility.HasConflictingMembers(copiedType, gameObjectDrawer))
							{
								menu.Add("Paste As New", ()=>
								{
									var pasted = gameObjectDrawer.AddComponent(copiedType, true, Array.IndexOf(parent.Members, this) + 1);
									var values = pasted.GetValues();
									for(int n = values.Length - 1; n >= 0; n--)
									{
										var val = values[n];
										Clipboard.TryPaste(Type, ref val);
									}
									inspector.ForceRebuildDrawers();
								});
							}
							else
							{
								menu.AddDisabled("Paste As New", "Not possible because of conflicting components.");
							}
						}
					}
				}
			}
		}

		/// <inheritdoc cref="IRootDrawer.AddItemsToOpeningViewMenu(ref Menu)" />
		public virtual void AddItemsToOpeningViewMenu(ref Menu menu)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(this is IRootDrawer, "AddItemsToOpeningViewMenu was called for "+ToString()+" which does not implement IRootDrawer.");
			#endif

			if(menu.Contains("Field Visibility/Serialized Only"))
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+".AddItemsToOpeningViewMenu aborting because menu already contained item \"Field Visibility/Serialized Only\". This is normal in stacked multi-editing mode.");
				#endif
				return;
			}
		
			menu.Add("Help/Drawer/Unity Object", PowerInspectorDocumentation.ShowDrawerInfo, "unity-object-drawer");
			menu.Add("Help/Features/Dynamic Prefix Column", PowerInspectorDocumentation.ShowFeature, "dynamic-prefix-column");
			menu.Add("Help/Features/Quick Invoke Menu", PowerInspectorDocumentation.ShowFeature, "quick-invoke-menu");
			menu.Add("Help/Features/Improved Tooltips", PowerInspectorDocumentation.ShowFeature, "tooltips");
			menu.Add("Help/Features/Keyboard Navigation", PowerInspectorDocumentation.ShowFeature, "keyboard-navigation");

			var enumGUIInstruction = TestVisibleChildrenUntilTrue(IsEnumDrawer);
			if(enumGUIInstruction != null)
			{
				menu.Add("Help/Drawer/Enum Field", PowerInspectorDocumentation.ShowDrawerInfo, "enum-drawer");
			}

			var objectReferenceDrawer = TestVisibleChildrenUntilTrue(IsObjectReferenceDrawer);
			if(objectReferenceDrawer != null)
			{
				menu.Add("Help/Drawer/Object Reference Field", PowerInspectorDocumentation.ShowDrawerInfo, "object-reference-drawer");
			}
		
			ViewMenuUtility.AddFieldVisibilityItems(ref menu, inspector, MonoScript != null);
			ViewMenuUtility.AddPreviewAreaItems(ref menu);
		}

		private static bool IsEnumDrawer([NotNull]IDrawer subject)
		{
			return subject.GetType() == typeof(EnumDrawer);
		}

		private static bool IsObjectReferenceDrawer([NotNull]IDrawer subject)
		{
			return subject.GetType() == typeof(ObjectReferenceDrawer);
		}

		public bool CanMoveComponentUp(bool allowMovingComponentAboveDownInstead)
		{
			if(parent == null)
			{
				return false;
			}

			if(!IsComponent)
			{
				return false;
			}

			if(!(parent is IReorderableParent))
			{
				return false;
			}

			var parentMembers = parent.Members;
			int fromIndex = Array.IndexOf(parentMembers, this);
			
			// Can't move Transform Component nor the Component below it.
			if(fromIndex <= 1)
			{
				return false;
			}

			bool isMissingComponent = targets[0] == null;

			// If this is not a missing component, then can move Component up.
			if(!isMissingComponent)
			{
				return true;
			}
			
			// If this is a missing component, it's not possible to move it up.
			// However if allowMovingComponentAboveDownInstead is true, we can still
			// try moving the Component above the target down instead.
			if(allowMovingComponentAboveDownInstead)
			{
				var memberAbove = parentMembers[fromIndex - 1] as IComponentDrawer;
				return memberAbove != null && memberAbove.CanMoveComponentDown(false);
			}

			return false;
		}

		public bool CanMoveComponentDown(bool allowMovingComponentBelowUpInstead)
		{
			if(parent == null)
			{
				return false;
			}

			if(!IsComponent)
			{
				return false;
			}

			if(!(parent is IReorderableParent))
			{
				return false;
			}

			var parentMembers = parent.Members;
			int lastMemberIndex = parentMembers.Length - 1;
			int fromIndex = Array.IndexOf(parentMembers, this);
			
			if(fromIndex >= lastMemberIndex)
			{
				return false;
			}

			bool isMissingComponent = targets[0] == null;

			// If this is not a missing component, then can move Component down.
			if(!isMissingComponent)
			{
				return true;
			}

			// If this is a missing component, it's not possible to move it down.
			// However if allowMovingComponentBelowUpInstead is true, we can still
			// try moving the Component below the target up instead.
			if(allowMovingComponentBelowUpInstead)
			{
				var memberBelow = parentMembers[fromIndex + 1] as IComponentDrawer;
				return memberBelow != null && memberBelow.CanMoveComponentUp(false);
			}

			return false;
		}

		public void MoveUp()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(CanMoveComponentUp(true));
			#endif

			if(parent == null)
			{
				Debug.LogWarning("Can't move Component because parent was null.");
				return;
			}

			var parentMembers = parent.Members;
			int fromIndex = Array.IndexOf(parentMembers, this);
			
			if(fromIndex < 2)
			{
				Debug.LogWarning("Can't move Components above Transform.");
				return;
			}

			if(!IsComponent)
			{
				Debug.LogWarning("Can't move non-Components.");
				return;
			}
			
			var components = targets as Component[];

			if(components == null)
			{
				Debug.LogWarning("Can't move non-Components.");
				return;
			}

			for(int n = components.Length - 1; n >= 0; n--)
			{
				var component = components[n];

				// handle missing scripts
				if(component == null)
				{
					if(fromIndex >= 2)
					{
						var memberAbove = parentMembers[fromIndex - 1] as IComponentDrawer;
						if(memberAbove != null && memberAbove.CanMoveComponentDown(false))
						{
							memberAbove.MoveDown();
							return;
						}
					}
					
					Debug.LogWarning("Can't move missing script.");
					return;
				}

				UnityEditorInternal.ComponentUtility.MoveComponentUp(component);
			}

			parent.SetMembers(parentMembers.Swap(fromIndex, fromIndex - 1));
			inspector.ScrollToShow(this);
		}
		
		public void MoveDown()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(CanMoveComponentDown(true));
			#endif

			if(parent == null)
			{
				Debug.LogWarning("Can't move Component because parent was null.");
				return;
			}

			var parentMembers = parent.Members;
			int lastMemberIndex = parentMembers.Length - 1;
			int fromIndex = Array.IndexOf(parentMembers, this);
			
			if(fromIndex >= lastMemberIndex)
			{
				Debug.LogWarning("Can't move Component because it is already the last Component.");
				return;
			}
			
			if(!IsComponent)
			{
				Debug.LogWarning("Can't move non-Components.");
				return;
			}
			
			var components = targets as Component[];

			if(components == null)
			{
				Debug.LogWarning("Can't move non-Components.");
				return;
			}
			
			for(int n = components.Length - 1; n >= 0; n--)
			{
				var component = components[n];

				// handle missing scripts
				if(component == null)
				{
					var memberBelow = parentMembers[fromIndex + 1] as IComponentDrawer;
					if(memberBelow != null && memberBelow.CanMoveComponentUp(false))
					{
						memberBelow.MoveUp();
						return;
					}
					
					Debug.LogWarning("Can't move missing script.");
					return;
				}

				UnityEditorInternal.ComponentUtility.MoveComponentDown(component);
			}

			parent.SetMembers(parentMembers.Swap(fromIndex, fromIndex + 1));
			inspector.ScrollToShow(this);
		}

		/// <summary> Name by type. </summary>
		protected abstract void NameByType();

		/// <summary> Sets debug mode. </summary>
		/// <param name="setEnabled"> True to enable, false to disable the set. </param>
		private void SetDebugMode(bool setEnabled)
		{
			if(setEnabled)
			{
				EnableDebugMode();
			}
			else
			{
				DisableDebugMode();
			}
		}
		
		/// <inheritdoc/>
		public void ApplyDebugModeSettings(DebugModeDisplaySettings settings)
		{
			#if DEV_MODE
			Debug.Log("ApplyDebugModeSettings(" + settings + ")");
			#endif

			debugModeDisplaySettings = settings;

			var selectedControl = inspector.Manager.FocusedDrawer;
			var selectedIndexPath = selectedControl == null ? null : selectedControl.GenerateMemberIndexPath(this);

			#if DEV_MODE
			Debug.Log("selectedIndexPath: " + StringUtils.ToString(selectedIndexPath));
			#endif

			RebuildMemberBuildListAndMembers();

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!inactive);
			#endif

			if(selectedIndexPath != null)
			{
				SelectMemberAtIndexPath(selectedIndexPath, ReasonSelectionChanged.Initialization);
			}
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			if(DebugMode)
			{
				if(debugModeDisplaySettings == null)
				{
					debugModeDisplaySettings = new DebugModeDisplaySettings();
				}

				#if DEV_MODE
				Debug.Log("Generating members using DebugModeDisplaySettings "+debugModeDisplaySettings);
				#endif

				ParentDrawerUtility.GetMemberBuildList(this, linkedMemberHierarchy, ref memberBuildList, debugModeDisplaySettings);
				return;
			}

			ParentDrawerUtility.GetMemberBuildList(this, linkedMemberHierarchy, ref memberBuildList, false);
		}

		/// <summary>
		/// Enables Debug mode+ if currently disabled.
		/// Also throws an ExitGUIException if mode was changed. This should not be caught (Unity will handle suppressing it internally).
		/// </summary>
		public virtual void EnableDebugMode()
		{
			if(!debugMode)
			{
				#if DEV_MODE
				Debug.Log("Enabling Debug Mode...");
				#endif

				debugMode = true;
				
				if(inspector == null)
				{
					#if DEV_MODE
					Debug.LogWarning(ToString()+".EnableDebugMode called with inspector null");
					#endif
					return;
				}
				
				PowerInspectorDocumentation.ShowFeatureIfWindowOpen("debug-mode");

				headerParts[HeaderPart.DebugModePlusButton].Texture = inspector.Preferences.graphics.DebugModeOnIcon;

				// Delay actually rebuilding the members  by a couple of frames.
				// This is so that the clicked icon will immediately visually respond to the click,
				// even if the rebuilding process takes some time to finish.
				OnNextLayout(()=>
				{
					// Added to fix issue where first control would get selected after changing debug mode.
					if(KeyboardControlUtility.JustClickedControl == 0)
					{
						KeyboardControlUtility.KeyboardControl = 0;
					}

					OnNextLayout(() =>
					{
						if(!inactive)
						{
							RebuildMemberBuildListAndMembers();
						}
					});
				});

				OnDebugModeChanged(true);

				ExitGUIUtility.ExitGUI();
			}
		}

		/// <summary>
		/// Called whenever Debug Mode+ state is changed.
		/// </summary>
		/// <param name="nowEnabled"></param>
		protected virtual void OnDebugModeChanged(bool nowEnabled) { }

		/// <summary>
		/// Disables Debug mode+ if currently enabled.
		/// Also throws an ExitGUIException if mode was changed. This should not be caught (Unity will handle suppressing it internally).
		/// </summary>
		public virtual void DisableDebugMode()
		{
			if(inspector.State.DebugMode)
			{
				if(DrawGUI.Active.DisplayDialog("Disable Debug Mode For All?", "Debug Mode is currently enabled for all inspector targets. Would you like to disable debug mode for whole inspector, or just for this one target?", "Disable For All", "Just This One"))
				{
					inspector.DisableDebugMode();
					return;
				}
			}

			if(debugMode)
			{
				#if DEV_MODE
				Debug.Log("Disabling Debug Mode...");
				#endif

				debugMode = false;

				if(inspector == null)
				{
					return;
				}

				headerParts[HeaderPart.DebugModePlusButton].Texture = inspector.Preferences.graphics.DebugModeOffIcon;

				// Delay actually rebuilding the members
				// by a couple of frames. This is so that the
				// clicked icon will immediately visually respond
				// to the click, even if the rebuilding process
				// takes some time to finish
				OnNextLayout(() =>
				{
					//added to fix issue where first control would get selected after changing debug mode
					KeyboardControlUtility.KeyboardControl = 0;

					OnNextLayout(() =>
					{
						if(!inactive)
						{
							RebuildMemberBuildListAndMembers();
						}
					});
				});

				OnDebugModeChanged(false);

				ExitGUIUtility.ExitGUI();
			}
		}

		/// <summary> Searches for the first objects of type. </summary>
		/// <returns> The found objects of type. </returns>
		protected abstract Object[] FindObjectsOfType();

		/// <summary> Attempts to get previous of type. </summary>
		/// <param name="result"> [out] The result. </param>
		/// <returns> True if it succeeds, false if it fails. </returns>
		protected bool TryGetPreviousOfType(out TTarget result)
		{
			Object[] all;
			int index = FindObjectsOfTypeFromHierarchyAndTargetIndex(out all);
			int count = all.Length;
			if(count == 0)
			{
				result = null;
				return false;
			}
			
			if(index == -1)
			{
				result = all[0] as TTarget;
			}
			else if(index == 0)
			{
				result = all[count-1] as TTarget;
			}
			else
			{
				result = all[index-1] as TTarget;
			}
			return true;
		}

		/// <summary> Attempts to get next of type. </summary>
		/// <param name="setTarget"> [out] The set target. </param>
		/// <returns> True if it succeeds, false if it fails. </returns>
		protected bool TryGetNextOfType(out TTarget setTarget)
		{
			Object[] all;
			int index = FindObjectsOfTypeFromHierarchyAndTargetIndex(out all);
			int lastIndex = all.Length - 1;
			if(lastIndex == -1)
			{
				setTarget = null;
				return false;
			}
		
			if(index == -1 || index >= lastIndex)
			{
				setTarget = all[0] as TTarget;
			}
			else
			{
				setTarget = all[index+1] as TTarget;
			}
			return setTarget != Target;
		}

		/// <summary> Searches for the first objects of type from hierarchy and target index. </summary>
		/// <param name="targetsOfType"> [out] Type of the targets of. </param>
		/// <returns> The found objects of type from hierarchy and target index. </returns>
		private int FindObjectsOfTypeFromHierarchyAndTargetIndex(out Object[] targetsOfType)
		{
			var target = Target;
			targetsOfType = FindObjectsOfType();
			Array.Sort(targetsOfType, SortObjectsByHierarchyOrder.Instance);

			int lastIndex = targetsOfType.Length - 1;
			if(lastIndex > -1)
			{
				for(int n = lastIndex; n >= 0; n--)
				{
					if(targetsOfType[n] == target)
					{
						return n;
					}
				}
			}
			return -1;
		}
		
		/// <inheritdoc/>
		protected override void OnSelectedInternal(ReasonSelectionChanged reason, IDrawer previous, bool isMultiSelection)
		{
			switch(reason)
			{
				case ReasonSelectionChanged.PrefixClicked:
					var selectHeaderPart = MouseoveredHeaderPart;
					if(selectHeaderPart == HeaderPart.None && !CanBeSelectedWithoutHeaderBeingSelected)
					{
						selectHeaderPart = headerParts[HeaderPart.Base];
					}
					if(selectHeaderPart != null && selectHeaderPart.Selectable)
					{
						SelectHeaderPart(selectHeaderPart);
					}
					break;
				case ReasonSelectionChanged.ControlClicked:
					var deselectHeader = CanBeSelectedWithoutHeaderBeingSelected || HeadlessMode;
					SelectHeaderPart(deselectHeader ? null : headerParts[HeaderPart.Base], !deselectHeader);
					return;
				case ReasonSelectionChanged.SelectControlDown:
					SelectHeaderPart(headerParts[HeaderPart.Base]);
					break;
				case ReasonSelectionChanged.SelectNextControl:
					SelectHeaderPart(headerParts.FirstSelectable);
					break;
				case ReasonSelectionChanged.SelectPrevControl:
					SelectHeaderPart(headerParts.LastSelectable);
					break;
				default:
					if((KeyboardControlUtility.KeyboardControl == 0 || !CanBeSelectedWithoutHeaderBeingSelected) && !HeadlessMode)
					{
						SelectHeaderPart(headerParts[HeaderPart.Base]);
					}
					break;
			}

			base.OnSelectedInternal(reason, previous, isMultiSelection);
		}

		/// <inheritdoc/>
		protected override void OnDeselectedInternal(ReasonSelectionChanged reason, IDrawer losingFocusTo)
		{
			#if DEV_MODE && DEBUG_DESELECT
			Debug.Log(ToString() + " losing focus to " + StringUtils.ToString(losingFocusTo) + " (reason=" + reason + ")");
			#endif

			SelectHeaderPart(null);

			switch(reason)
			{
				case ReasonSelectionChanged.SelectNextControl:
				case ReasonSelectionChanged.SelectPrevControl:
					break;
				case ReasonSelectionChanged.LostFocus:
					KeyboardControlUtility.KeyboardControl = 0;
					break;
				default:
					if(OverrideFieldFocusing())
					{
						KeyboardControlUtility.KeyboardControl = 0;
					}
					break;
			}

			base.OnDeselectedInternal(reason, losingFocusTo);
		}

		/// <inheritdoc cref="IDrawer.OnKeyboardInputGiven" />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			#if DEV_MODE && DEBUG_KEYBOARD_INPUT
			Debug.Log("UnityObjectDrawer<TValue>(" + StringUtils.ToString(label) + ".OnKeyboardInputGiven(" + inputEvent.keyCode + ") mods=" + inputEvent.modifiers + ", char='" + StringUtils.ToString(inputEvent.character) + "', EditingTextField=" + DrawGUI.EditingTextField);
			#endif
		
			// When a field inside UnityObject is selected ignore shortcuts like Reset
			// (or redirect them to target selected fields if possible)
			if(!HeaderIsSelected)
			{
				if(keys.reset.DetectAndUseInput(inputEvent))
				{
					return false;
				}

				if(keys.duplicate.DetectAndUseInput(inputEvent))
				{
					return false;
				}

				if(keys.randomize.DetectAndUseInput(inputEvent))
				{
					return false;
				}
			}

			switch(inputEvent.keyCode)
			{
				case KeyCode.Return:
				case KeyCode.Space:
				case KeyCode.KeypadEnter:
					if(inputEvent.modifiers != EventModifiers.None)
					{
						return false;
					}

					switch((HeaderPart)SelectedHeaderPart)
					{
						case HeaderPart.None:
							return true;
						case HeaderPart.Base:
						case HeaderPart.FoldoutArrow:
							break; // let ParentDrawer handle changing unfolded state
						case HeaderPart.EnabledFlag:
							Enabled = !Enabled;
							return true;
						case HeaderPart.ReferenceIcon:
							OpenDocumentation(this, SelectedHeaderPart.Rect, inputEvent);
							return true;
						case HeaderPart.ContextMenuIcon:
							OnContextMenuIconClicked(this, SelectedHeaderPart.Rect, inputEvent);
							return true;
						case HeaderPart.PresetIcon:
							UnityEditor.Presets.PresetSelector.ShowSelector(EditorTargets, null, true);
							return true;
						default:
							SelectedHeaderPart.OnClicked(this, inputEvent);
							return true;
					}
					break;
				case KeyCode.UpArrow:
					if(HasAddressableBar())
					{
						if(SelectedHeaderPart == HeaderPart.AddressablesBar)
						{
							DrawGUI.Use(inputEvent);
							SelectHeaderPart(HeaderPart.Base);
							return true;
						}
					}
					break;
				case KeyCode.DownArrow:
					if(HasAddressableBar())
					{
						if(SelectedHeaderPart != HeaderPart.AddressablesBar)
						{
							DrawGUI.Use(inputEvent);
							SelectHeaderPart(HeaderPart.AddressablesBar);
							return true;
						}
					}
					break;
				case KeyCode.LeftArrow:
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						//handle moving between header parts
						if(HeaderIsSelected)
						{
							// Left arrow key is reserved for folding Component unless already folded,
							// cannot be folded, or another portion of the component is selected besides the base or the fold arrow.
							if((SelectedHeaderPart != HeaderPart.Base && SelectedHeaderPart != HeaderPart.FoldoutArrow) || !Foldable || !Unfolded)
							{
								DrawGUI.Use(inputEvent);
								SelectNextHeaderPartLeft(false);
								return true;
							}
						}
						else
						{
							// Prevent base.OnKeyboardInputGiven from reacting to the input event or using it
							// when body of drawers is selected.
							return true;
						}
					}
					break;
				case KeyCode.RightArrow:
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						//handle moving between header parts
						if(HeaderIsSelected)
						{
							// Right arrow is used for unfolding component unless already unfolded,
							// cannot be unfolded or another portion of the component is selected besides the base or the fold arrow.
							if((SelectedHeaderPart != HeaderPart.Base && SelectedHeaderPart != HeaderPart.FoldoutArrow) || !Foldable || Unfolded)
							{
								DrawGUI.Use(inputEvent);
								SelectNextHeaderPartRight(false);
								return true;
							}
						}
						else
						{
							// Prevent base.OnKeyboardInputGiven from reacting to the input event or using it
							// when body of drawers is selected.
							return true;
						}
					}
					break;
				case KeyCode.Delete:
					if(IsComponent)
					{
						var gameObjectDrawer = GameObjectDrawer;
						if(gameObjectDrawer != null && Editable)
						{
							gameObjectDrawer.DeleteMember(this);
						}
					}
					break;
			}

			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <summary>
		/// True if this is a drawer for an addressable asset and the Addressables package has been installed.
		/// </summary>
		/// <returns> True for addressable assets, otherwise false. </returns>
		protected virtual bool HasAddressableBar()
		{
			return IsAsset && AddressablesUtility.IsInstalled;
		}

		/// <inheritdoc/>
		protected override void SelectNextFieldLeft(bool moveToNextControlAfterReachingEnd, bool additive = false)
		{
			if(HeaderIsSelected)
			{
				SelectNextHeaderPartLeft(moveToNextControlAfterReachingEnd);
			}
			else
			{
				base.SelectNextFieldLeft(moveToNextControlAfterReachingEnd);
			}
		}

		/// <inheritdoc/>
		protected override void SelectNextFieldRight(bool moveToNextControlAfterReachingEnd, bool additive = false)
		{
			if(HeaderIsSelected)
			{
				SelectNextHeaderPartRight(moveToNextControlAfterReachingEnd);

				//Unity changes focused field as a reaction to tab being pressed after a delay
				//to counter that we need to unselect the internally selected control after a delay
				if(moveToNextControlAfterReachingEnd)
				{
					KeyboardControlUtility.KeyboardControl = 0;
				}
			}
			else
			{
				base.SelectNextFieldRight(moveToNextControlAfterReachingEnd);
			}
		}

		/// <summary> Select next header part left. </summary>
		/// <param name="moveToNextControlAfterReachingEnd"> True to move to next control after reaching end. </param>
		protected void SelectNextHeaderPartLeft(bool moveToNextControlAfterReachingEnd)
		{
			var nextPart = headerParts.GetNextSelectableHeaderPartLeft(selectedPart);
			if(nextPart != null)
			{
				SelectHeaderPart(nextPart);
				return;
			}

			if(moveToNextControlAfterReachingEnd)
			{
				var nextControl = GetNextSelectableDrawerLeft(true, this);
				if(nextControl != this)
				{
					inspector.Select(nextControl, ReasonSelectionChanged.SelectPrevControl);
				}
				else if(inspector.Toolbar != null)
				{
					Manager.Select(inspector, InspectorPart.Toolbar, null, ReasonSelectionChanged.SelectPrevControl);
				}
			}
		}
		
		/// <summary> Select next header part right. </summary>
		/// <param name="moveToNextControlAfterReachingEnd"> True to move to next control after reaching end. </param>
		protected void SelectNextHeaderPartRight(bool moveToNextControlAfterReachingEnd)
		{
			var next = headerParts.GetNextSelectableHeaderPartRight(selectedPart);

			#if DEV_MODE && DEBUG_SELECT_NEXT_HEADER_PART
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".SelectNextHeaderPartRight(", moveToNextControlAfterReachingEnd, ") next: ", next, " with SelectedHeaderPart=", SelectedHeaderPart, ", HeaderIsSelected=", HeaderIsSelected+ ", headerParts.Count=", headerParts.Count));
			#endif

			if(next != null)
			{
				SelectHeaderPart(next);
				return;
			}

			if(moveToNextControlAfterReachingEnd)
			{
				SelectedHeaderPart = null;
				if(Unfolded && Height > HeaderHeight)
				{
					SelectFirstField();
				}
				else
				{
					inspector.Select(GetNextSelectableDrawerRight(true, this), ReasonSelectionChanged.SelectNextControl);
				}
			}
		}

		/// <summary> Select first field. </summary>
		protected virtual void SelectFirstField()
		{
			inspector.Select(GetNextSelectableDrawerRight(true, this), ReasonSelectionChanged.SelectNextControl);
		}

		/// <inheritdoc/>
		protected override void ScrollToShow()
		{
			inspector.ScrollToShow(PrefixLabelPosition);
		}

		protected void SelectHeaderPart(HeaderPart select, bool setKeyboardControl = true)
		{
			SelectHeaderPart(select == HeaderPart.None ? null : headerParts[select], setKeyboardControl);
		}

		/// <summary> Select header part. </summary>
		/// <param name="select"> The select. </param>
		/// <param name="setKeyboardControl"> (Optional) True to set keyboard control. </param>
		protected virtual void SelectHeaderPart(HeaderPartDrawer select, bool setKeyboardControl = true)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(select == null || select.Selectable, select+ " is not selectable!");
			#endif

			#if DEV_MODE && DEBUG_SELECT_HEADER_PART
			Debug.Log(StringUtils.ToColorizedString("SelectHeaderPart(", ((HeaderPart)select), ", setKeyboardControl=", setKeyboardControl, ") with KeyboardControl=", KeyboardControlUtility.KeyboardControl, ", JustClickedControl=", KeyboardControlUtility.JustClickedControl));
			#endif

			if(HeadlessMode && select != null)
			{
				#if DEV_MODE && DEBUG_SELECT_HEADER_PART
				Debug.LogError("SelectHeaderPart(" + select + ") was called in HeadlessMode");
				#endif
				select = null;
			}

			SelectedHeaderPart = select;

			if(setKeyboardControl)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!inactive);
				#endif

				SetKeyboardControlForHeaderPart(select);
			}
		}

		/// <summary> Sets keyboard control for header part. </summary>
		/// <param name="select"> The select. </param>
		/// <param name="repeatTimes"> (Optional) List of times of the repeats. </param>
		private void SetKeyboardControlForHeaderPart(HeaderPart select, int repeatTimes = 3)
		{
			if(inactive)
			{
				#if DEV_MODE
				Debug.LogWarning("Aborting SetKeyboardControlForHeaderPart with repeatTimes "+repeatTimes+" because inactive was true");
				#endif
				return;
			}

			GUI.changed = true;
			
			switch(select)
			{
				case HeaderPart.EnabledFlag:
					if(GUIUtility.hotControl == 0)
					{
						KeyboardControlUtility.KeyboardControl = EnabledFlagControlId;
					}
					break;
				case HeaderPart.None:
					return;
				default:
					//these controls aren't selectable in Unity by default so we must handle them manually
					if(GUIUtility.hotControl == 0)
					{
						#if DEV_MODE
						Debug.LogWarning("SetKeyboardControlForHeaderPart("+select+") - Setting KeyboardControl to 0 (was " + KeyboardControlUtility.KeyboardControl + ") with Event="+(Event.current == null ? "null" : Event.current.rawType.ToString()));
						#endif

						KeyboardControlUtility.KeyboardControl = 0;
					}
					break;
			}

			if(repeatTimes > 0)
			{
				repeatTimes--;
				OnNextLayout(()=>SetKeyboardControlForHeaderPart(select, repeatTimes));
			}
		}

		/// <inheritdoc cref="IParentDrawer.SetUnfolded(bool, bool)" />
		public override void SetUnfolded(bool setUnfolded, bool setChildrenAlso)
		{
			SetUnfolded(setUnfolded, false, setChildrenAlso);
		}

		/// <inheritdoc cref="IComponentDrawer.SetUnfolded(bool, bool, bool)" />
		public virtual void SetUnfolded(bool setUnfolded, bool collapseAllOthers, bool setChildrenAlso)
		{
			if((Unfolded != setUnfolded || setChildrenAlso) && ((!UnityObjectDrawerUtility.HeadlessMode && Foldable) || setUnfolded))
			{
				#if DEV_MODE && DEBUG_SET_UNFOLDED
				Debug.Log(StringUtils.ToColorizedString(ToString(), " - SetUnfolded(setUnfolded=", setUnfolded, ", collapseAllOthers=", collapseAllOthers, ", setChildrenAlso=", setChildrenAlso, ") with Foldable=", Foldable), Target);
				#endif

				if(collapseAllOthers)
				{
					inspector.FoldAllComponents(this as IComponentDrawer);
				}

				ParentDrawerUtility.SetUnfolded(this, setUnfolded, setChildrenAlso);
			}
		}

		/// <inheritdoc cref="IDrawer.OnClick" />
		public override bool OnClick(Event inputEvent)
		{
			#if DEV_MODE && DEBUG_ON_CLICK
			Debug.Log(StringUtils.ToColorizedString(GetType(), ".OnClick() with MouseoveredHeaderPart=", MouseoveredHeaderPart, "(", (HeaderPart)MouseoveredHeaderPart, "), HeaderMouseovered=", HeaderMouseovered, ", Selectable=", Selectable, ", event=", StringUtils.ToString(inputEvent), ", mousePos=", inputEvent.mousePosition, ", mouseovereredHeaderButton=", mouseovereredHeaderButton));
			#endif

			selectedHeaderPartOnClickStart = selectedPart;
			
			// clicking header with ctrl held down won't select the header, but keep previous selection
			if(!inputEvent.control && (mouseoveredPart == HeaderPart.Base || mouseoveredPart == HeaderPart.FoldoutArrow))
			{
				var selectHeaderPart = mouseoveredPart;
				if(selectHeaderPart != null)
				{
					if(!selectHeaderPart.Selectable)
					{
						selectHeaderPart = headerParts.Base;
					}
				}
				else if(!CanBeSelectedWithoutHeaderBeingSelected || KeyboardControlUtility.KeyboardControl == 0)
				{
					selectHeaderPart = headerParts.Base;
				}
				
				if(selectedPart != selectHeaderPart)
				{
					SelectHeaderPart(selectHeaderPart, selectHeaderPart != HeaderPart.PresetIcon);
				}
			
				if(Selectable && !Selected)
				{
					if(HeaderMouseovered)
					{
						DrawGUI.EditingTextField = false;
						Select(ReasonSelectionChanged.PrefixClicked);
					}
					else
					{
						Select(ReasonSelectionChanged.ControlClicked);
					}
				}
			}

			if(mouseovereredHeaderButton != null)
			{
				if(mouseovereredHeaderButton.OnClicked())
				{
					DrawGUI.Use(inputEvent);
					return true;
				}
				return false;
			}
			
			if(mouseoveredPart != null)
			{
				if(mouseoveredPart.OnClicked(this, inputEvent) && inputEvent.type == EventType.Used)
				{
					return true;
				}

				// Don't set DragAndDropObjectReferences in edit mode manually, because that's already handled
				// by the Editor for the header internally.
				if(!Platform.EditorMode)
				{
					DrawGUI.Active.DragAndDropObjectReferences = UnityObjects;
				}
				return true;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(CanBeSelectedWithoutHeaderBeingSelected);
			#endif

			SelectHeaderPart(HeaderPart.None, false);

			return false;
		}

		/// <summary> Opens popup menu from which it's possible to invoke methods. Will only contain invisible methods in debug mode. </summary>
		private void OpenExecuteMethodMenu(bool includeInvisible)
		{
			var openPosition = ExecuteMethodIconPosition;
			openPosition.y += openPosition.height;
			InvokeMethodUtility.OpenExecuteMethodMenu(this, openPosition, includeInvisible);
		}

		/// <summary> Query if this object has execute method menu items. </summary>
		/// <returns> True if execute method menu items, false if not. </returns>
		protected bool HasExecuteMethodMenuItems()
		{
			return InvokeMethodUtility.HasExecuteMethodMenuItems(this);
		}

		/// <inheritdoc cref="IParentDrawer.OnChildLayoutChanged" />
		public override void OnChildLayoutChanged()
		{
			if(ShouldOptimizePrefixLabelWidthOnLayoutChanged())
			{
				OptimizePrefixLabelWidth();
			}
			base.OnChildLayoutChanged();
		}

		/// <summary>
		/// Optimize prefix label width to be just wide enough to display prefixes of member drawers
		/// upto a certain maximum width threshold.
		/// </summary>
		protected void OptimizePrefixLabelWidth()
		{
			float optimalWidth = GetOptimalPrefixLabelWidth(0, true);
			float absoluteMin = MinPrefixLabelWidth;
			float absoluteMax = MaxPrefixLabelWidth;
			
			if(optimalWidth > absoluteMax)
			{
				optimalWidth = absoluteMax;
			}
			else if(optimalWidth < absoluteMin)
			{
				optimalWidth = absoluteMin;
			}

			if(optimalWidth < DrawGUI.MinAutoSizedPrefixLabelWidth)
			{
				// if user has manually resized prefix label, then adjust auto-sizing min value to be at least as small as the manually set value
				float min = manullySetPrefixLabelWidth > 0f && manullySetPrefixLabelWidth < DrawGUI.MinAutoSizedPrefixLabelWidth ? manullySetPrefixLabelWidth : DrawGUI.MinAutoSizedPrefixLabelWidth;

				// only change prefix label width if moving in the right direction
				if(PrefixLabelWidth > min)
				{
					PrefixLabelWidth = min;
				}
			}
			else if(optimalWidth > DrawGUI.MaxAutoSizedPrefixLabelWidth)
			{
				// if user has manually resized prefix label, then adjust auto-sizing max value to be at least as large as the manually set value
				float max = manullySetPrefixLabelWidth > 0f && manullySetPrefixLabelWidth > DrawGUI.MaxAutoSizedPrefixLabelWidth ? manullySetPrefixLabelWidth : DrawGUI.MaxAutoSizedPrefixLabelWidth;

				// only change prefix label width if moving in the right direction
				if(PrefixLabelWidth < max)
				{
					PrefixLabelWidth = max;
				}
			}
			else
			{
				PrefixLabelWidth = optimalWidth;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(PrefixLabelWidth > 0, ToString()+ ".OptimizePrefixLabelWidth(0) result " + PrefixLabelWidth+" less than zero");
			#endif
		}

		/// <inheritdoc/>
		protected override void DoPasteFromClipboard()
		{
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				#if DEV_MODE
				bool success = Clipboard.TryPasteUnityObject(targets[n]);
				Debug.Log("Clipboard.TryPaste(" + Type.Name + ", ref " + (targets[n] == null ? "null" : targets[n].name) + "): "+StringUtils.ToColorizedString(success));
				#else
				Clipboard.TryPasteUnityObject(targets[n]);
				#endif
			}
		}

		/// <inheritdoc/>
		protected override string GetPasteFromClipboardMessage()
		{
			return "Pasted values{0}.";
		}

		/// <inheritdoc/>
		protected override bool ResetOnDoubleClick()
		{
			return false;
		}
		
		/// <summary> Called whenver the width of the inspector changes. </summary>
		protected virtual void OnInspectorWidthChanged()
		{
			var preferences = InspectorUtility.Preferences;
			if(ShouldOptimizePrefixLabelWidthOnLayoutChanged())
			{
				OptimizePrefixLabelWidth();
			}

			if(OnWidthsChanged != null)
			{
				OnWidthsChanged();
			}
		}

		private bool ShouldOptimizePrefixLabelWidthOnLayoutChanged()
		{
			var preferences = InspectorUtility.Preferences;
			if(preferences.autoResizePrefixLabelsInterval == PrefixAutoOptimizationInterval.OnLayoutChanged)
			{
				if(preferences.autoResizePrefixLabels == PrefixAutoOptimization.AllTogether)
				{
					if(!IsComponent)
					{
						return true;
					}
				}
				else if(preferences.autoResizePrefixLabels == PrefixAutoOptimization.AllSeparately)
				{
					return true;
				}
			}
			return false;
		}

		/// <inheritdoc cref="IDrawer.Dispose" />
		public override void Dispose()
		{
			afterComponentHeaderGUIHeight = 0f;
			manullySetPrefixLabelWidth = -1f;

			unfoldedness.SetValueInstant(false);
			
			headerParts.Clear();
			headerButtons.Clear();

			prefixResizer = PrefixResizer.Disabled;
			prefixResizerMouseovered = false;

			debugMode = false;
			mouseoveredPart = null;
			selectedPart = null;
			selectedHeaderPartOnClickStart = null;

			#if SAFE_MODE
			if(inspector == null)
			{
				#if DEV_MODE
				Debug.LogError(ToString()+ ".Dispose - inspector was null, can't unsubscribe InvokeIfInstanceReferenceIsValid from State.OnDebugModeChanged.");
				#endif
			}
			else if(onDebugModeChanged == null)
			{
				#if DEV_MODE
				Debug.LogError(ToString()+ ".Dispose - onDebugModeChanged was null, can't unsubscribe InvokeIfInstanceReferenceIsValid from State.OnDebugModeChanged.");
				#endif
			}
			else
			#endif
			{
				inspector.State.OnDebugModeChanged -= onDebugModeChanged.InvokeIfInstanceReferenceIsValid;
			}
			
			headerButtonsWidth = 35f;

			if(PrefixResizeUtility.NowResizing == this)
			{
				PrefixResizeUtility.NowResizing = null;
			}

			#if SAFE_MODE
			if(inspector == null)
			{
				#if DEV_MODE
				Debug.LogError(ToString() + ".Dispose - inspector was null, can't unsubscribe OnInspectorWidthChanged from State.OnWidthChanged.");
				#endif
			}
			else
			#endif
			{
				inspector.State.OnWidthChanged -= OnInspectorWidthChanged;
			}
			OnWidthsChanged = null;

			ArrayPool<TTarget>.Resize(ref targets, 0);

			base.Dispose();
			
			linkedMemberHierarchy = null;
			inspector = null;
		}

		/// <inheritdoc cref="IDrawer.OnMiddleClick" />
		public override void OnMiddleClick(Event inputEvent)
		{
			#if DEV_MODE && DEBUG_MIDDLE_CLICK
			Debug.Log(ToString()+".OnMiddleClick("+StringUtils.ToString(inputEvent)+") with HeaderMouseovered="+StringUtils.ToColorizedString(HeaderMouseovered)+ ", Target="+StringUtils.ToString(Target)+", MonoScript="+StringUtils.ToString(MonoScript));
			#endif

			if(!HeaderMouseovered)
			{
				return;
			}

			DrawGUI.Use(inputEvent);

			var target = Target;

			// For MonoBehaviours, prefer pinging MonoScript above GameObject (since can already ping those by middle clicking Transform or GameObject headers).
			// Also if target is null (e.g. when inspecting static members of a class), ping the MonoScript if has one.
			if(target == null || IsComponent)
			{
				var monoScript = MonoScript;
				if(monoScript != null)
				{
					DrawGUI.Active.PingObject(monoScript);
					return;
				}
			}

			if(target != null)
			{
				DrawGUI.Active.PingObject(target);
			}
		}

		/// <inheritdoc />
		public override bool SelfPassesSearchFilter(SearchFilter filter)
		{
			if(!filter.HasFilterAffectingInspectedTargetContent)
			{
				lastPassedFilterTestType = FilterTestType.None;
				return true;
			}
			
			// Always return false when there's only one UnityObject shown in the inspector,
			// because it serves no purpose and can just lead to user confusion.
			if(!IsComponent && (UserSettings.MergedMultiEditMode || inspector.State.drawers.Length == 1))
			{
				return false;
			}

			if(filter.PassesFilter(this))
			{
				#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
				if(filter.HasFilter) { Debug.Log(ToString()+".PassesSearchFilter(\""+filter.RawInput+"\"): "+StringUtils.True); }
				#endif

				lastPassedFilterTestType = FilterTestType.Type;
				return true;
			}

			#if DEV_MODE && DEBUG_PASSES_SEARCH_FILTER
			if(filter.HasFilter) { Debug.Log(ToString()+".PassesSearchFilter(\""+filter.RawInput+"\"): "+StringUtils.False+" (default)"); }
			#endif

			return false;
		}

		/// <inheritdoc/>
		protected override void ViewInStackedMode()
		{
			Inspector.SetFilter("t:\"" + Type.FullName + "\"");
			UserSettings.MergedMultiEditMode = false;
			GUI.changed = true;
			ExitGUIUtility.ExitGUI();
		}

		/// <inheritdoc/>
		protected override void DoReset()
		{
			UndoHandler.RegisterUndoableAction(targets, "Reset");
			var resetMethod = Type.GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, CallingConventions.Any, ArrayPool<Type>.ZeroSizeArray, null);

			var type = Type;

			// Handle internal Unity Object types using presets if possible
			if(type.IsUnityObject() && !Types.MonoBehaviour.IsAssignableFrom(type) && !type.IsScriptableObject())
			{
				var defaultPresets = UnityEditor.Presets.Preset.GetDefaultPresetsForObject(Target);
				var presetsCount = defaultPresets.Length;
				var defaultPreset = presetsCount > 0 ? defaultPresets[presetsCount - 1] : null;

				if(defaultPreset == null)
				{
					if(IsComponent)
					{
						var tempGameObject = new GameObject("_TEMP");
						tempGameObject.SetActive(false);
						var tempInstance = tempGameObject.AddComponent(Type);
						defaultPreset = new UnityEditor.Presets.Preset(tempInstance);
						Object.DestroyImmediate(tempGameObject, false);
					}
					else if(Type.IsScriptableObject())
					{
						var tempInstance = ScriptableObject.CreateInstance(Type);
						defaultPreset = new UnityEditor.Presets.Preset(tempInstance);
						Object.DestroyImmediate(tempInstance, false);
					}
				}

				if(defaultPreset != null)
				{
					#if DEV_MODE
					Debug.Log("Resetting targets using Preset");
					#endif

					for(int n = targets.Length - 1; n >= 0; n--)
					{
						var target = targets[n];
						defaultPreset.ApplyTo(target);

						if(resetMethod != null)
						{
							resetMethod.Invoke(target, null);
						}
					}

					RebuildMembers();
					return;
				}
			}

			ResetAllFieldsAndProperties();
						
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				if(resetMethod != null)
				{
					resetMethod.Invoke(targets[n], null);
				}
			}
			RebuildMembers();
		}

		protected void ResetAllFieldsAndProperties()
		{
			#if DEV_MODE
			Debug.Log("Resetting all fields and properties manually");
			#endif

			var type = Type;
			object tempInstance;
			Object destroyWhenDone;
			if(IsComponent)
			{
				var tempGameObject = new GameObject("_TEMP");
				tempGameObject.SetActive(false);
				destroyWhenDone = tempGameObject;

				tempInstance = tempGameObject.AddComponent(type);
			}
			else if(type.IsScriptableObject())
			{
				destroyWhenDone = ScriptableObject.CreateInstance(type);
				tempInstance = destroyWhenDone;
			}
			else
			{
				try
				{
					tempInstance = type.CreateInstance();
				}
				#if DEV_MODE
				catch(Exception e)
				{
					Debug.LogError(e);
				#else
				catch
				{
				#endif
					SetAllFieldsToDefaultValues();
					SetAllAutoPropertiesToDefaultValues();
					return;
				}

				if(tempInstance == null)
				{
					SetAllFieldsToDefaultValues();
					SetAllAutoPropertiesToDefaultValues();
					return;
				}

				destroyWhenDone = null;
			}
			
			var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;
			for(; type != Types.MonoBehaviour && type != Types.ScriptableObject && type != Types.UnityObject && type != Types.Behaviour && type != null && type != Types.SystemObject; type = type.BaseType)
			{
				var fields = type.GetFields(flags);
				var properties = type.GetProperties(flags);

				for(int t = targets.Length - 1; t >= 0; t--)
				{
					var target = targets[t];

					for(int f = fields.Length - 1; f >= 0; f--)
					{
						var field = fields[f];
						if(!field.IsInitOnly)
						{
							#if DEV_MODE && DEBUG_RESET_STEPS
							Debug.Log("Resetting field "  +field.Name + " on type " + StringUtils.ToString(type));
							#endif

							field.SetValue(target, field.GetValue(tempInstance));
						}
					}
					
					for(int p = properties.Length - 1; p >= 0; p--)
					{
						var property = properties[p];
						if(property.IsAutoProperty() && property.CanWrite)
						{
							#if DEV_MODE && DEBUG_RESET_STEPS
							Debug.Log("Resetting property "  + property.Name + " on type " + StringUtils.ToString(type));
							#endif

							property.SetValue(target, property.GetValue(tempInstance, null), null);
						}
					}
				}
			}

			Object.DestroyImmediate(destroyWhenDone, false);

			if(Event.current != null)
			{
				ExitGUIUtility.ExitGUI();
			}
		}

		protected void SetAllFieldsToDefaultValues()
		{
			#if DEV_MODE
			Debug.Log("Setting all fields to default values");
			#endif

			var fields = Type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			for(int t = targets.Length - 1; t >= 0; t--)
			{
				var target = targets[t];
				for(int f = fields.Length - 1; f >= 0; f--)
				{
					var field = fields[f];
					if(!field.IsInitOnly)
					{
						field.SetValue(target, field.FieldType.DefaultValue());
					}
				}
			}
		}

		protected void SetAllAutoPropertiesToDefaultValues()
		{
			#if DEV_MODE
			Debug.Log("Setting all auto-properties to default values");
			#endif

			var properties = Type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			for(int t = targets.Length - 1; t >= 0; t--)
			{
				var target = targets[t];
				for(int p = properties.Length - 1; p >= 0; p--)
				{
					var property = properties[p];
					if(property.IsAutoProperty() && property.CanWrite)
					{
						property.SetValue(target, property.PropertyType.DefaultValue(), null);
					}
				}
			}
		}
		

		/// <summary> Query if any target has the given HideFlag. </summary>
		/// <param name="hideFlag"> The hide flag to test for. </param>
		/// <returns> True if any target has hide flag, false if not. </returns>
		protected virtual bool HasHideFlag(HideFlags hideFlag)
		{
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var target = targets[n];
				if(target != null && target.hideFlags.HasFlag(hideFlag))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary> Query if gameObject containing any of targets has the given HideFlag. </summary>
		/// <param name="hideFlag"> The hide flag to test for. </param>
		/// <returns> True if targets are components and any target GameObject has hide flag, otherwise false. </returns>
		protected virtual bool GameObjectHasFlag(HideFlags hideFlag)
		{
			if(IsComponent)
			{
				for(int n = targets.Length - 1; n >= 0; n--)
				{
					var component = targets[n] as Component;
					if(component != null && component.gameObject.hideFlags.HasFlag(hideFlag))
					{
						return true;
					}
				}
			}
			return false;
		}

		protected virtual bool IsAssetOpenForEdit()
		{
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var target = targets[n];

				if(target == null)
				{
					continue;
				}

				if(AssetDatabase.IsNativeAsset(target))
				{
					var assetPath = AssetDatabase.GetAssetPath(target);
					var statusOptions = EditorUserSettings.allowAsyncStatusUpdate ? StatusQueryOptions.UseCachedAsync : StatusQueryOptions.UseCachedIfPossible;
					if(!AssetDatabase.IsOpenForEdit(assetPath, statusOptions))
					{
						return false;
					}
				}
				else if(AssetDatabase.IsForeignAsset(target))
				{
					var statusOptions = EditorUserSettings.allowAsyncStatusUpdate ? StatusQueryOptions.UseCachedAsync : StatusQueryOptions.UseCachedIfPossible;
					if(!AssetDatabase.IsMetaFileOpenForEdit(target, statusOptions))
					{
						return false;
					}
				}
			}

			return true;
		}
	}
}