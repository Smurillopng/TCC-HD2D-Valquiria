#define PI_ENABLE_UI_SUPPORT
#define USE_IL_FOR_GET_AND_SET
#define UNFOLD_ON_SELECT_IN_SINGLE_ACTIVE_MODE
#define DRAW_EXECUTE_METHOD_BUTTON
#define DRAW_DEBUG_BUTTON
#define USE_DEFAULT_MARGINS
#define USE_FOCUS_CONTROLLER

// E.g. for enum fields in Odin Inspector KeyboardControl rect is (0,0,0,0), even though they still seem to be keyboard focusable internally.
// Caluclating scores based on rect does not work with zero rect controls, we can only make decisions based on their id alone.
//#define SUPPORT_KEYBOARD_FOCUS_FOR_ZERO_RECT_CONTROLS

//#define DEBUG_DRAW_GREYED_OUT

//#define ENABLE_MOUSEOVER_EFFECTS

#define DEBUG_GET_EDITOR
//#define DEBUG_HEIGHT
//#define DEBUG_HEIGHT_TWEENING
//#define DEBUG_CREATE
//#define DEBUG_GET_OPTIMAL_PREFIX_LABEL_WIDTH
//#define DEBUG_VISUALIZE_EDITOR_BOUNDS_AND_FOCUSED_CONTROL
//#define DEBUG_SELECT_NEXT_FIELD
//#define DEBUG_MEMBER_CONTROL

//#define DEBUG_SKIP_OUT_OF_BOUNDS
//#define DEBUG_SKIP_PHANTOM_CONTROL
//#define DEBUG_SKIP_CAN_NOT_HAVE_KEYBOARD_CONTROL

//#define DEBUG_GET_NEXT_CONTROL
//#define DEBUG_GET_NEXT_CONTROL_Y
//#define DEBUG_KEYBOARD_INPUT

using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using System.Linq;

#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
using Sisus.Vexe.FastReflection;
#endif

#if UNITY_2019_1_OR_NEWER // UI Toolkit doesn't exist in older versions
using UnityEngine.UIElements;
#endif

namespace Sisus
{
	[Serializable]
	public abstract class CustomEditorBaseDrawer<TSelf, TTarget> : UnityObjectDrawer<TSelf, TTarget>, ICustomEditorDrawer where TSelf : CustomEditorBaseDrawer<TSelf, TTarget> where TTarget : Object
	{
		#if DEV_MODE
		private const bool DebugSelectNextControl = true;
		#endif
		private const int GetNextFieldMaxOutOfBoundsCount = 100;

		private const int OffsetNextControlId = 0;

		protected static readonly Dictionary<Type, float> lastUnfoldedHeightsByType = new Dictionary<Type, float>();
		private static readonly EditorKeyboardFocusController keyboardFocusController = new EditorKeyboardFocusController();
		private static FieldInfo hideInInspectorField;

		protected bool allSameType = true;
		protected bool canHaveEditor = true;
		
		/// <summary>
		/// Type of the main Editor used for drawing the body of the drawer.
		/// This is used when building the main Editor. For AssetImporter targets,
		/// this is the type of the AssetImporterEditor.
		/// </summary>
		[CanBeNull]
		protected Type editorType;

		private float unfoldedHeight;
		protected bool hideInInspector;
		private bool isFirstInspectedEditor;

		#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
		private MethodCaller<object,object> onSceneGUI;
		#else
		private MethodInfo onSceneGUI;
		#endif

		#if UNITY_2019_1_OR_NEWER
		protected VisualElement element;
		#endif

		#if UNITY_2019_1_OR_NEWER
		public override PrefixResizer PrefixResizer
		{
			get
			{
				// Prefix resizing doesn't support VisualElements yet, so disable the resizer for now.
				if(element != null)
				{
					return PrefixResizer.Disabled;
				}
				return base.PrefixResizer;
			}
		}
		#endif

		/// <summary> Offset of items inside Editor from the bottom of the header. </summary>
		protected virtual float ControlsTopMargin
		{
			get
			{
				return DrawGUI.TopPadding;
			}
		}

		/// <summary> Offset of items inside Editor from the left edge of the inspector. </summary>
		protected virtual float ControlsLeftMargin
		{
			get
			{
				return 13f;
			}
		}

		/// <summary> Offset of items inside Editor from the right edge of the inspector. </summary>
		protected virtual float ControlsRightMargin
		{
			get
			{
				return 4f;
			}
		}

		/// <summary> Gets the controller responsible for changing focused keyboard control. </summary>
		/// <value> The keyboard focus controller. </value>
		protected virtual EditorKeyboardFocusController KeyboardFocusController
		{
			get
			{
				return keyboardFocusController;
			}
		}

		private const float ControlsBottomMargin = DrawGUI.BottomPadding;

		/// <summary> Offset of controls from the end of the prefix column. </summary>
		private const float PrefixColumnEndToControlOffset = 6f;

		/// <summary> The horizontal offset between multiple controls drawn on a single row. </summary>
		private const float ControlsOffset = 2f;
		
		private int endControlId;
		
		private bool usingInitialEstimatedHeight;
		
		private bool memberBuildListActuallyPopulated;
		
		private int focusFirstField;

		/// <inheritdoc cref="IDrawer.RequiresConstantRepaint" />
		public override bool RequiresConstantRepaint
		{
			get
			{
				return editor != null && editor.RequiresConstantRepaint();
			}
		}

		/// <inheritdoc/>
		public override bool ShouldShowInInspector
		{
			get
			{
				return !hideInInspector && base.ShouldShowInInspector;
			}
		}

		/// <inheritdoc cref="IDrawer.Height" />
		public override float Height
		{
			get
			{
				#if UNITY_2019_1_OR_NEWER
				if(element != null)
				{
					UpdateHeightEstimate();
				}
				#endif

				if(NowTweeningUnfoldedness)
				{
					float headerHeight = HeaderHeight;
					return headerHeight + (unfoldedHeight - headerHeight) * Unfoldedness;
				}

				if(!Unfolded)
				{
					return HeaderHeight + afterComponentHeaderGUIHeight;
				}

				return unfoldedHeight;
			}
		}

		/// <inheritdoc/>
		protected override bool UsesEditorForDrawingBody
		{
			get
			{
				try
				{
					return !DebugMode && !inspector.HasFilterAffectingInspectedTargetContent;
				}
				#if DEV_MODE
				catch(NullReferenceException e) // happened once during OnFilterChanging callback for some reason
				{
					Debug.LogError(ToString()+ " NullReferenceException with inspector="+(inspector == null ? "null" : inspector.ToString())+", inactive="+inactive + ", parent="+(parent == null ? "null" : parent.ToString())+"\n" + e);
				#else
				catch(NullReferenceException) // happened once during OnFilterChanging callback for some reason
				{
				#endif
					return !DebugMode;
				}
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

		/// <summary>
		/// The main Editor used for drawing the body of the drawer.
		/// For AssetImporter targets, this is the AssetImporterEditor.
		///
		/// If targets are of mismatching types, returns null.
		/// </summary>
		/// <value> The editor. Can be null. </value>
		[CanBeNull]
		protected virtual Editor Editor
		{
			get
			{
				//needed?
				if(editor == null)
				{
					UpdateEditor();
				}
				else if(Editors.DisposeIfInvalid(ref editor))
				{
					UpdateEditor();
				}
				return editor;
			}
		}

		protected virtual int AppendLastCheckedId
		{
			get
			{
				return 0;
			}
		}

		/// <summary> What height is a single row in the body of the drawer? </summary>
		/// <value> Row height in pixels. </value>
		protected virtual float ControlsRowHeight
		{
			get
			{
				return DrawGUI.SingleLineHeight;
			}
		}

		protected int EndControlId
		{
			get
			{
				return endControlId;
			}

			set
			{
				if(value > 0 && parent != null && Event.current != null && Event.current.type != EventType.Repaint)
				{
					var membs = parent.VisibleMembers;
					int myIndex = Array.IndexOf(membs, this);
					if(myIndex != -1)
					{
						for(int n = myIndex + 1, count = membs.Length; n < count; n++)
						{
							var member = membs[n] as TSelf;
							if(member != null)
							{
								var id = member.controlId;
								if(id > 0)
								{
									if(id < controlId)
									{
										#if DEV_MODE
										Debug.Log(ToString()+" - huh, next editor "+ member+ " id " + id + " was < my id " + controlId+" with Event.type="+Event.current.type);
										#endif
									}
									else
									{
										#if DEV_MODE
										if(label.text == "Light" && endControlId != id - 1)Debug.Log(ToString() + " _endControlID = "+(id - 1) + "(Dynamic)");
										#endif

										endControlId = id - 1;
										return;
									}
									break;
								}
							}
						}
					}
				}
				#if DEV_MODE
				if(label.text == "Light" && endControlId != value)Debug.Log(ToString() + " _endControlID = " + endControlId + " (Manual)");
				#endif
				endControlId = value;
			}
		}

		/// <inheritdoc cref="IDrawer.GetOptimalPrefixLabelWidth" />
		public sealed override float GetOptimalPrefixLabelWidth(int indentLevel)
		{
			var type = Type;
			float savedWidth;
			if(type != null && Inspector.InspectorDrawer.PrefixColumnWidths.TryGet(type, out savedWidth))
			{
				#if DEV_MODE && DEBUG_GET_OPTIMAL_PREFIX_LABEL_WIDTH
				Debug.Log("GetOptimalPrefixLabelWidth - savedWidth: "+savedWidth);
				#endif

				return savedWidth;
			}

			return UsesEditorForDrawingBody ? GetOptimalPrefixLabelWidthForEditor(indentLevel) : GetOptimalPrefixLabelWidthWhenNotUsingEditor(indentLevel);
		}

		/// <summary> Gets optimal prefix label width when not using the Editor for drawing. </summary>
		/// <param name="indentLevel"> The current indent level. </param>
		/// <returns> The optimal prefix label width. </returns>
		protected virtual float GetOptimalPrefixLabelWidthWhenNotUsingEditor(int indentLevel)
		{
			return base.GetOptimalPrefixLabelWidth(indentLevel);
		}

		/// <summary> Gets optimal prefix label width for the Editor that draws these drawer. </summary>
		/// <param name="indentLevel"> The current indent level. </param>
		/// <returns> The optimal prefix label width for the Editor. </returns>
		protected virtual float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return GetOptimalPrefixLabelWidthForEditor(editor, indentLevel);
		}

		protected float GetOptimalPrefixLabelWidthForEditor(Editor editor, int indentLevel)
		{
			if(editor == null)
			{
				#if DEV_MODE
				Debug.LogWarning("GetOptimalPrefixLabelWidthForEditor called with null editor");
				#endif
				return 0f;
			}

			var serializedObject = editor.GetSerializedObject();
			if(serializedObject == null)
			{
				#if DEV_MODE
				Debug.LogWarning("GetOptimalPrefixLabelWidthForEditor failed to get serializedObject for editor."); // I think that this can sometimes happen when editor has no serialized fields at all?
				#endif
				return 0f;
			}

			var property = serializedObject.GetIterator();

			// Get first property, which is the script reference, and skip past it (this is usually not shown in custom editors).
			if(!property.NextVisible(true) || !property.NextVisible(true))
			{
				#if DEV_MODE
				Debug.Log("GetOptimalPrefixLabelWidthForEditor(" + (editor.target?.GetType()?.Name ?? editor.GetType().Name) + " Editor serializedObject had no serialized properties.");
				#endif
				return 0f;
			}

			var tempLabel = GUIContentPool.Empty();
			float widestPrefixWidth = 0f;
			do
			{
				tempLabel.text = property.displayName;

				float width = DrawerUtility.GetOptimalPrefixLabelWidth(indentLevel + property.depth, tempLabel);
				if(width > widestPrefixWidth)
				{
					#if DEV_MODE && DEBUG_GET_OPTIMAL_PREFIX_LABEL_WIDTH
					Debug.Log("GetOptimalPrefixLabelWidth widest="+width+" from label "+StringUtils.ToString(tempLabel) + " with indentLevel " + indentLevel);
					#endif
					widestPrefixWidth = width;
				}
			}
			while(property.NextVisible(true));

			return widestPrefixWidth;
		}

		/// <summary>
		/// Initial estimated height to use for height calculations of the drawer
		/// before OnGUI has been called during a layout event to calculate the real height.
		/// </summary>
		/// <value> The esimated height of the drawer. </value>
		protected virtual float EstimatedUnfoldedHeight
		{
			get
			{
				float lastUnfoldedHeight;
				if(lastUnfoldedHeightsByType.TryGetValue(Type, out lastUnfoldedHeight))
                {
					return lastUnfoldedHeight;
				}

				if(!UsesEditorForDrawingBody)
                {
					return base.Height;
                }

				var editor = Editor;
				if(editor != null)
				{
					#if DEV_MODE && DEBUG_HEIGHT
					Inspector.OnNextLayout(()=>
					{
						Debug.Log(ToString()+" estimated height: "+editor.GetUnfoldedHeight()+" vs real height: " + Height);
					});
					#endif

					return editor.GetUnfoldedHeight();
				}
				return HeaderHeight;
			}
		}
		
		/// <inheritdoc/>
		protected sealed override void Setup([NotNull]TTarget[] setTargets, [CanBeNull]IParentDrawer setParent, [CanBeNull]GUIContent setLabel, [NotNull]IInspector setInspector)
		{
			throw new NotSupportedException("Please use the other Setup method of CustomEditorBaseDrawer.");
		}

		/// <summary> Sets up an instance of the drawer for usage. </summary>
		/// <param name="setTargets"> The targets that the drawer represent. Can not be null. </param>
		/// <param name="setParent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="setInspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <param name="setEditorType"> The type of the custom editor. Can be null. </param>
		protected virtual void Setup([NotNull]TTarget[] setTargets, [CanBeNull]IParentDrawer setParent, [NotNull]IInspector setInspector, [CanBeNull]Type setEditorType)
		{
			editorType = setEditorType;
			targets = setTargets;
			allSameType = setTargets.AllSameType();
			canHaveEditor = true;

			inspector = setInspector;
			UpdateEditor();

			if(editor == null)
			{
				#if DEV_MODE
				Debug.LogWarning("Failed to get editor of type "+StringUtils.ToString(editorType)+" for targets of type: "+StringUtils.TypesToString(setTargets));
				#endif
				canHaveEditor = false;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inactive);
			#endif

			base.Setup(setTargets, setParent, null, setInspector);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inactive);
			#endif

			if(!memberBuildListActuallyPopulated)
			{
				RebuildMemberBuildList();
			}
			UpdateHeightEstimate();
			inspector.OnFilterChanging += OnFilterChanging;

			#if DEV_MODE && DEBUG_CREATE
			Debug.Log("Created "+GetType().Name+" for Component "+label.text+" with editorType "+(editorType == null ? "null" : editorType.Name));
			#endif

			if(canHaveEditor)
			{
				#if CSHARP_7_3_OR_NEWER && USE_IL_FOR_GET_AND_SET
				MethodInfo onSceneGUIInfo = editor.GetType().GetMethod("OnSceneGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				onSceneGUI = onSceneGUIInfo == null ? null : onSceneGUIInfo.DelegateForCall();
				#else
				onSceneGUI = editor.GetType().GetMethod("OnSceneGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				#endif

				#if UNITY_2019_1_OR_NEWER
				SceneView.duringSceneGui += OnSceneGUI;
				#else
				SceneView.onSceneGUIDelegate += OnSceneGUI;
				#endif
			}
		}

		/// <inheritdoc/>
		protected override void OnDebugModeChanged(bool nowEnabled)
		{
			base.OnDebugModeChanged(nowEnabled);
			UpdateHeightEstimate();
		}

		private void UpdateHeightEstimate()
		{
			if(!UsesEditorForDrawingBody)
			{
				usingInitialEstimatedHeight = false;
				if(memberBuildState == MemberBuildState.MembersBuilt)
				{
					unfoldedHeight = UnityObjectDrawerUtility.CalculateHeight(this) + afterComponentHeaderGUIHeight;
				}
				else
				{
					unfoldedHeight = HeaderHeight + afterComponentHeaderGUIHeight;
				}
			}
			else if(!canHaveEditor)
			{
				unfoldedHeight = HeaderHeight + afterComponentHeaderGUIHeight;
				usingInitialEstimatedHeight = false;
			}
			#if UNITY_2019_1_OR_NEWER
			else if(element != null && !float.IsNaN(element.resolvedStyle.height))
			{
				unfoldedHeight = HeaderHeight + afterComponentHeaderGUIHeight + PrefixResizerDragHandleHeight + ControlsTopMargin + element.resolvedStyle.height + ControlsBottomMargin;
				usingInitialEstimatedHeight = false;
			}
			#endif
			else
			{
				unfoldedHeight = EstimatedUnfoldedHeight + afterComponentHeaderGUIHeight;
				usingInitialEstimatedHeight = true;
			}

			#if DEV_MODE && DEBUG_HEIGHT
			Debug.Log("UpdateHeightEstimate unfoldedHeight="+ unfoldedHeight+ ", usingInitialEstimatedHeight="+ usingInitialEstimatedHeight+", Unfoldedness="+Unfoldedness);
			#endif
		}

		public void SetIsFirstInspectedEditor(bool value)
        {
			if(isFirstInspectedEditor == value)
            {
				return;
            }

			if(editor == null)
            {
				if(!value)
                {
					return;
                }

				if(Editor == null)
				{
					#if DEV_MODE
					Debug.LogWarning(this + ".SetIsFirstInspectedEditor("+value+") was called but Editor returned null");
					#endif
					return;
				}				
            }

			editor.SetIsFirstInspectedEditor(value);

			isFirstInspectedEditor = value;
		}

		/// <summary>
		/// Updates the main custom Editor used for drawing the body.
		/// 
		/// If canHaveEditor is false this does nothing.
		/// 
		/// If canHaveEditor is true and a null Editor is fetched,
		/// then canHaveEditor is set to false.
		/// 
		/// Requires targets, editorType and allSameType to be setup before being called.
		/// </summary>
		protected void UpdateEditor()
		{
			#if DEV_MODE && DEBUG_GET_EDITOR
			Debug.Log(ToString()+".UpdateEditor called with canHaveEditor="+StringUtils.ToColorizedString(canHaveEditor)+" with targets="+StringUtils.ToString(targets)+", editorType="+ StringUtils.ToStringSansNamespace(editorType)+", allSameType="+StringUtils.ToColorizedString(allSameType));
			#endif

			if(canHaveEditor)
			{
				var targetsForEditor = GetTargetsForEditor();

				if(targetsForEditor.ContainsNullObjects())
                {
					canHaveEditor = false;

					if(editor != null)
                    {
						Editors.Dispose(ref editor, true);
					}
					inspector.RebuildDrawers(true);
					GUIUtility.ExitGUI();
					return;
                }

				#if DEV_MODE
				Debug.Assert(targetsForEditor.Length > 0);
				Debug.Assert(targetsForEditor[0] != null);
				#endif

				var was = editor;

				inspector.InspectorDrawer.Editors.GetEditorInternal(ref editor, targetsForEditor, editorType, allSameType);
				canHaveEditor = editor != null;

				#if DEV_MODE && DEBUG_GET_EDITOR
				Debug.Log("editor.GetType(): "+editor.GetType().FullName +", editor.targets: "+StringUtils.ToString(editor.targets) + ", editor.targets.Types: "+StringUtils.TypesToString(editor.targets));
				#endif

				#if DEV_MODE
				if(!canHaveEditor) { Debug.LogWarning(ToString()+"canHaveEditor="+StringUtils.False+" with targets="+StringUtils.ToString(targets)+", editorType="+ StringUtils.ToStringSansNamespace(editorType)+", allSameType="+StringUtils.ToColorizedString(allSameType)); }
				#endif

				if(was != editor)
				{
					OnEditorUpdated();
				}
			}
		}

		/// <summary>
		/// Gets targets used when building the main editor.
		/// </summary>
		/// <returns></returns>
		protected virtual TTarget[] GetTargetsForEditor()
		{
			return targets;
		}

		/// <summary>
		/// Called when the value of the editor field has been updated.
		/// This usually occurs during the Setup phase.
		/// 
		/// If some other Setup functionality requires having access to the editor,
		/// or related fields (canHaveEditor, editorType, allSameType, targets)
		/// it should be safe to do so here.
		/// </summary>
		protected virtual void OnEditorUpdated()
		{
			if(editor == null)
			{
				hideInInspector = false;

				#if UNITY_2019_1_OR_NEWER
				if(element != null)
				{
					inspector.InspectorDrawer.RemoveElement(element, this);
					element = null;
				}
				#endif
				return;
			}

			#if UNITY_2019_1_OR_NEWER && (DEV_MODE || PI_ENABLE_UI_SUPPORT)
			element = editor.CreateInspectorGUI();
			if(element != null)
			{
				inspector.InspectorDrawer.AddElement(element, this);
			}
			#if DEV_MODE
			Debug.Log(ToString()+" elements: "+(element == null ? StringUtils.Null : element.childCount.ToString()));
			#endif
			#endif

			if(hideInInspectorField == null)
			{
				hideInInspectorField = typeof(Editor).GetField("hideInspector", BindingFlags.Instance | BindingFlags.NonPublic);
				if(hideInInspectorField == null)
				{
					#if DEV_MODE
					Debug.LogWarning("Editor.hideInInspector field not found!");
					#endif
					hideInInspector = false;
					return;
				}
			}
			hideInInspector = (bool)hideInInspectorField.GetValue(editor);
		}

		private void OnFilterChanging(SearchFilter filter)
		{
			if(filter.HasFilterAffectingInspectedTargetContent && !memberBuildListActuallyPopulated)
			{
				RebuildMemberBuildListAndMembers();
			}
			else
			{
				// NEW TEST
				UpdateVisibleMembers();
			}

			UpdateHeightEstimate();
		}

		/// <inheritdoc/>
		public override void UpdateVisibleMembers()
		{
			// NEW TEST
			if(UsesEditorForDrawingBody)
			{
				if(visibleMembers.Length > 0)
				{
					SetVisibleMembers(ArrayPool<IDrawer>.ZeroSizeArray, true);
					OnVisibleMembersChanged();
					OnChildLayoutChanged();
				}
				return;
			}

			base.UpdateVisibleMembers();
		}

		/// <inheritdoc/>
		public override void OnVisibleMembersChanged()
		{
			base.OnVisibleMembersChanged();
			UpdateHeightEstimate();
		}

		/// <inheritdoc cref="IDrawer.Draw" />
		public override bool Draw(Rect position)
		{
			#if DEV_MODE || PI_PROFILE
			Profiler.BeginSample("CustomEditorBaseDrawer.Draw");
			#endif

			for(int n = targets.Length - 1; n >= 0; n--)
			{
				if(targets[n] == null)
				{
					#if DEV_MODE
					Debug.LogWarning(this+".Draw() - target "+(n+1)+"/"+(targets.Length + 1)+" was null, rebuilding");
					#endif
					inspector.RebuildDrawersIfTargetsChanged();

					#if DEV_MODE || PI_PROFILE
					Profiler.EndSample();
					#endif
					return false;
				}
			}
			
			bool dirty = false;
			var e = Event.current;
			var eventType = e.type;

			if(eventType == EventType.Layout)
			{
				OnLayoutEvent(position);
			}
			else if(TweenedBool.AnyTweening) // this helps make animations a little bit smoother when unfoldedness is being tweened
			{
				GetDrawPositions(position);
			}
			
			//this should help make the default inspector look more similar to my own controls
			EditorGUIUtility.wideMode = true;
			
			var guiColorWas = GUI.color;
			if(DrawGreyedOut)
			{
				#if DEV_MODE && DEBUG_DRAW_GREYED_OUT
				Debug.Log(Msg(ToString(), " DrawGreyedOut with Enabled=", Enabled, ", GuiEnabled= ", GuiEnabled, ", Target=", Target, ", HasFlag(HideInInspector)=", Target == null ? false : Target.hideFlags.HasFlag(HideFlags.HideInInspector), ", HasFlag(NotEditable)=", Target == null ? false : Target.hideFlags.HasFlag(HideFlags.NotEditable)));
				#endif

				var color = GUI.color;
				color.a = 0.5f;
				GUI.color = color;
			}

			float setAfterComponentHeaderGUIHeight = 0f;
			var headerPosition = position;
			float headerHeight = HeaderHeight;
			headerPosition.height = headerHeight;
			if(!HeadlessMode)
			{
				if(DrawPrefix(headerPosition))
				{
					dirty = true;
				}

				if(IsComponent)
				{
					setAfterComponentHeaderGUIHeight = EditorGUIDrawer.InvokeAfterComponentHeaderGUI(headerPosition, targets, SelectedHeaderPart);
				}
			}

			var bodyPosition = headerPosition;
			bodyPosition.y += headerPosition.height;
			float dragHandleHeight = PrefixResizerDragHandleHeight;
			bodyPosition.y += dragHandleHeight + ControlsTopMargin;
			
			GUI.color = guiColorWas;

			if(setAfterComponentHeaderGUIHeight != afterComponentHeaderGUIHeight)
			{
				dirty = true;
				GUI.changed = true;

				if(Event.current.type == EventType.Layout)
				{
					#if DEV_MODE
					Debug.Log(ToString() + "afterComponentHeaderGUIHeight = " + setAfterComponentHeaderGUIHeight);
					#endif
					afterComponentHeaderGUIHeight = setAfterComponentHeaderGUIHeight;
				}
			}

			bodyPosition.y += afterComponentHeaderGUIHeight;

			if(MembersAreVisible)
			{
				//needed if HandleResizing is placed after DrawBody
				DrawGUI.PrefixLabelWidth = PrefixLabelWidth;

				//height is set to use HeaderHeight in during the setup phase
				//if it still has its initial value, then set the height
				//to a very large value for the DrawBody phase, since it uses
				//BeginArea, and it will look better on the first draw cycle
				//if the rect is not too small
				bodyPosition.height = usingInitialEstimatedHeight ? EstimatedUnfoldedHeight : unfoldedHeight - headerHeight - dragHandleHeight - ControlsTopMargin;

				#if !USE_DEFAULT_MARGINS
				float leftIndent = DrawGUI.IndentWidth * DrawGUI.IndentLevel + DrawGUI.LeftPadding;
				pos.x += leftIndent;
				pos.width -= leftIndent;
				#endif

				if(PrefixResizer != PrefixResizer.Disabled)
				{
					DrawGUI.LayoutSpace(dragHandleHeight + ControlsTopMargin);
				}

				if(DrawBody(bodyPosition))
				{
					dirty = true;
				}

				if(PrefixResizer != PrefixResizer.Disabled)
				{
					HandlePrefixColumnResizing();
					int labelRightPadding = (int)(DrawGUI.MiddlePadding + DrawGUI.MiddlePadding);
					EditorStyles.label.padding.right = labelRightPadding;
				}
			}

			EditorStyles.label.padding.right = 2;

			#if DEV_MODE && DEBUG_VISUALIZE_EDITOR_BOUNDS_AND_FOCUSED_CONTROL
			if(Event.current.control) { KeyboardFocusController.DrawDebugVisualization(this, Event.current.alt, beforeHeaderControlId, EndControlId); }
			#endif

			#if DEV_MODE || PI_PROFILE
			Profiler.EndSample();
			#endif

			return dirty;
		}

		/// <inheritdoc/>
		public override bool DrawPrefix(Rect position)
		{
			bool dirty = base.DrawPrefix(position);

			#if DEV_MODE && DEBUG_KEYBOARD_RECT
			if(Selected && MouseIsOverHeader && this is IComponentDrawer)
			{
				string text =
				"\ncontrolID...endID: " + controlId + "..." + endControlId +
				"\nbeforeHeaderControlID: " + beforeHeaderControlId +
				"\nPrefix Width: "+PrefixLabelWidth +
				"\nWidth/Height: "+Width + "x" + Height +
				"\nControlRectBounds: " + StringUtils.ToString(ControlRectBounds()) +
				"\nKeyboardControl: " + KeyboardControlUtility.KeyboardControl +
				"\nKeyboardRect: " + StringUtils.ToString(KeyboardControlUtility.Info.KeyboardRect);
			
				var drawPos = position;
				float boxHeight = 7f * 14f;
				drawPos.y = Mathf.Max(0f, drawPos.y - boxHeight - inspector.State.ScrollPos.y + 14f);
				drawPos.height = boxHeight;
				InspectorUtility.SetActiveTooltip(drawPos, text);
			}
			#endif

			DrawGUI.LayoutSpace(position.height);

			return dirty;
		}

		/// <inheritdoc />
		public override bool DrawBody(Rect position)
		{
			#if UNITY_2019_1_OR_NEWER
			if(element != null)
			{
				//DrawGUI.LayoutSpace(afterComponentHeaderGUIHeight + position.height);
				return false;
			}
			#endif

			bool dirty;

			if(!UsesEditorForDrawingBody)
			{
				usingInitialEstimatedHeight = false;
				unfoldedHeight = UnityObjectDrawerUtility.CalculateHeight(this) + afterComponentHeaderGUIHeight;
				dirty = ParentDrawerUtility.DrawBodyMultiRow(this, position);

				//DrawGUI.LayoutSpace(afterComponentHeaderGUIHeight + position.height);
				return dirty;
			}

			#if DEV_MODE || PI_PROFILE
			Profiler.BeginSample("CustomEditorBaseDrawer.DrawBody");
			#endif

			//DrawGUI.LayoutSpace(afterComponentHeaderGUIHeight);

			float unfoldedness = Unfoldedness;
			if(unfoldedness >= 1f)
			{
				dirty = DrawMembers(position);
			}
			else
			{
				using(new MemberScaler(position.min, unfoldedness))
				{
					dirty = DrawMembers(position);
				}
			}

			#if DEV_MODE || PI_PROFILE
			Profiler.EndSample();
			#endif
			
			return dirty;
		}

		/// <summary>
		/// Get style that determines margins when drawing the body.
		/// </summary>
		/// <returns></returns>
		protected virtual GUIStyle GetMarginsStyle()
		{
			var bodyEditor = Editor;
			return bodyEditor != null && (!bodyEditor.UseDefaultMargins()) ? GUIStyle.none : EditorStyles.inspectorDefaultMargins;
		}

		protected virtual bool DrawMembers(Rect position)
		{
			bool dirty = false;

			var e = Event.current;
			var eventType = e.type;

			GUILayout.BeginHorizontal();
			{
				var bodyEditor = Editor;
				var style = GetMarginsStyle();
				GUILayout.BeginVertical(style, GUILayout.Width(Width));
				{
					EditorGUI.BeginChangeCheck();
					{
						OnInspectorGUI(bodyEditor);
						DrawImportedObjectGUI();
					}
					if(EditorGUI.EndChangeCheck())
					{
						// new test
						if(bodyEditor.serializedObject != null)
						{
							bodyEditor.serializedObject.ApplyModifiedProperties();
						}

						#if DEV_MODE
						Debug.Log(ToString()+ " GUI change detected");
						#endif
						dirty = true;

						if(IsComponent)
						{
							for(int n = targets.Length - 1; n >= 0; n--)
							{
								var component = targets[n] as Component;
								if(component != null)
								{
									ComponentModifiedCallbackUtility.OnComponentModified(component);
								}
							}
						}
					}

					EndControlId = KeyboardControlUtility.Info.LastControlID;

					//add a gap of several units to the ControlID. This makes it
					//easier to figure out which ControlIDs belong under which
					//CustomEditors, which is important for selection changing logic
					//UPDATE: Had to increase this from 100 to make things work with CameraInspector
					//at least I had to increase end offset in GetNextDown(), not sure if this is 100% necessary
					for(int n = OffsetNextControlId; n > 0; n--)
					{
						GUIUtility.GetControlID(FocusType.Passive);
					}
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();

			// Making sure GUILayout.GetLastRect works even if OnInspectorGUI had GUILayout.Space at the end.
			GUILayoutEmpty();

			if(eventType == EventType.Repaint)
			{
				var rectEnd = GUILayoutUtility.GetLastRect();
				float yEnd = rectEnd.y - rectEnd.height;
				float bodyHeight = yEnd - position.y;
				float setHeight = HeaderHeight + PrefixResizerDragHandleHeight + afterComponentHeaderGUIHeight + bodyHeight;
				if(!setHeight.Equals(unfoldedHeight))
				{
					// Do not recalculate unfolded height is being animated for more stable animations.
					if(TweenedBool.AnyTweening && !usingInitialEstimatedHeight)
					{
						return dirty;
					}

					unfoldedHeight = setHeight;
					dirty = true;
					GUI.changed = true;
					lastUnfoldedHeightsByType[Type] = setHeight;
				}
				usingInitialEstimatedHeight = false;
			}
			else if(usingInitialEstimatedHeight || NowTweeningUnfoldedness)
			{
				#if DEV_MODE
				Debug.Log(ToString()+ " GUI changed = true because usingInitialEstimatedHeight || NowUnfolding with Event=" + StringUtils.ToString(Event.current) + ", usingInitialEstimatedHeight=" + usingInitialEstimatedHeight +", NowUnfolding=" + NowTweeningUnfoldedness);
				#endif

				GUI.changed = true;
				dirty = true;
			}

			return dirty;
		}
		
		protected virtual void DrawImportedObjectGUI() { }

		protected void OnInspectorGUI(Editor bodyEditor)
		{
			if(inspector == null)
			{
				return;
			}

			DrawerUtility.HandleFieldFocus(controlId, ref focusFirstField);
			
			//UPDATE: This padding.right of 2 makes prefix resizer look good
			//but breaks field-embedded prefixes, like Rect.x,y,z,w, so can't use it...
			EditorStyles.label.padding.right = 0;

			if(bodyEditor != null)
			{
				if(ReadOnly)
				{
					GUI.enabled = false;
				}

				bool editingTextFieldWas;
				EventType eventType;
				KeyCode keyCode;
				CustomEditorUtility.BeginEditor(out editingTextFieldWas, out eventType, out keyCode);
				{
					// fix needed or foldouts inside custom property drawers will be drawn at incorrect positions
					var leftMarginWas = EditorStyles.foldout.margin.left;
					if(!EditorGUIDrawer.EnableFoldoutFix)
					{
						EditorStyles.foldout.margin.left = -12;
					}

					try
					{
						DrawOnInspectorGUI(bodyEditor);
					}
					catch(Exception e)
					{
						if(ExitGUIUtility.ShouldRethrowException(e))
						{
							EditorStyles.foldout.margin.left = leftMarginWas;
							throw;
						}
						#if DEV_MODE
						Debug.LogWarning(ToString()+" "+e);
						#endif
						// todo: should we rebuild drawers whenever there are errors?
					}
					EditorStyles.foldout.margin.left = leftMarginWas;
				}
				CustomEditorUtility.EndEditor(editingTextFieldWas, eventType, keyCode);

				GUI.enabled = true;
			}

			EditorStyles.label.padding.right = 2;

			if(NowTweeningUnfoldedness)
			{
				// We actually add negative GUILayout.Space when animating unfoldedness to offset the effects
				// of matrix scaling, since Editor.OnInspectorGUI won't handle it automatically.
				float removeHeight = (unfoldedHeight - HeaderHeight) * (Unfoldedness - 1f);
				GUILayout.Space(removeHeight);

				#if DEV_MODE && PI_ASSERTATIONS
				if(removeHeight > 0f) { Debug.LogError(ToString() + " removeHeight ("+ removeHeight + ") > 0f"); }
				if(!Mathf.Approximately(unfoldedHeight + removeHeight, Height)) { Debug.LogError(StringUtils.ToColorizedString("unfoldedHeight (", unfoldedHeight, ") + removeHeight (", removeHeight, ") != Height (", Height, ") with diff=", unfoldedHeight + removeHeight - Height, ", Unfoldedness=", Unfoldedness, ", HeaderHeight=", HeaderHeight)); }
				#endif

				#if DEV_MODE && DEBUG_HEIGHT_TWEENING
				Debug.Log(StringUtils.ToColorizedString("removeHeight=", removeHeight, ", Unfoldedness=", Unfoldedness, ", unfoldedHeight=", unfoldedHeight, ", HeaderHeight=", HeaderHeight, ", PrefixResizerDragHandleHeight=", PrefixResizerDragHandleHeight, ", Height=", Height));
				#endif
			}
		}

		protected virtual void OnSceneGUI(SceneView sceneView)
		{
			if(onSceneGUI == null)
			{
				return;
			}

			var editor = Editor;
			if(editor == null)
            {
				return;
            }

			onSceneGUI.Invoke(editor, null);
		}

		/// <summary>
		/// Just draws the body using the Editor without doing anything else.
		/// </summary>
		/// <param name="bodyEditor"></param>
		protected virtual void DrawOnInspectorGUI([NotNull]Editor bodyEditor)
		{
			#if DEV_MODE || PI_PROFILE
			Profiler.BeginSample("CustomEditorBaseDrawer.DrawBody");
			#endif

			Application.logMessageReceived += OnOnInspectorGUILogMessage;

			bodyEditor.OnInspectorGUI();

			Application.logMessageReceived -= OnOnInspectorGUILogMessage;

			#if DEV_MODE || PI_PROFILE
			Profiler.EndSample();
			#endif
		}

		private void OnOnInspectorGUILogMessage(string condition, string stackTrace, LogType type)
		{
			if(type != LogType.Exception && type != LogType.Error)
			{
				return;
			}

			// Should we rebuild drawers with any errors or just specific ones that we know to be show-stoppers?
			if(!string.Equals(condition, "SerializedObject target has been destroyed."))
			{
				return;
			}

			#if DEV_MODE
			Debug.LogWarning("Rebuilding all drawers because detected error from Editor OnInspectorGUI: \"" + condition+"\".");
			#endif

			if(editor != null)
			{
				Editors.Dispose(ref editor, true);
			}
			
			Inspector.ForceRebuildDrawers();
			return;
		}

		/// <inheritdoc cref="IDrawer.DrawSelectionRect" />
		public override void DrawSelectionRect()
		{
			if(!HeaderIsSelected)
			{
				if(Height > HeaderHeight && KeyboardControlUtility.KeyboardControl > controlId)
				{
					var rect = KeyboardControlUtility.Info.KeyboardRect;
					if(rect.x < 16f)
					{
						DrawGUI.DrawSelectionRect(rect, localDrawAreaOffset);
					}
				}
			}
			else
			{
				base.DrawSelectionRect();
			}
		}

		#if ENABLE_MOUSEOVER_EFFECTS
		/// <summary>
		/// new test
		/// </summary>
		public override void OnMouseover()
		{
			if(InspectorUtility.Preferences.visualizeClickToSelectAreas && Unfolded && Mouseovered)
			{
				//NEW TEST
				Rect rect;
				Vector2 mousePos = Cursor.LocalPosition;
				mousePos.y -= bodyLastDrawPosition.y;
				if(TryGetRectOfControlAtPosition(mousePos, out rect))
				{
					Debug.Log("Found control under rect: "+rect);
					rect.y += bodyLastDrawPosition.y;
					//Rect prefixRect;
					//Rect controlRect;
					//DrawGUI.GetLabelAndControlRects(rect, out prefixRect, out controlRect);
					//DrawGUI.DrawLeftClickAreaMouseoverEffect(prefixRect);

					
					//TEMP DISABLED FOR EASIER TESTING!
					//if(rect.height > DrawGUI.singleLineHeight)
					//{
					//	rect.height = DrawGUI.singleLineHeight;
					//}
					//if(rect.x == 14f)
					//{
					//	rect.width = PrefixLabelWidth - DrawGUI.leftPadding - DrawGUI.IndentLevel * DrawGUI.indentWidth - DrawGUI.middlePadding;
					//}

					DrawGUI.DrawLeftClickAreaMouseoverEffect(rect);
				}
			}

			base.OnMouseover();
		}

		/// <summary>
		/// TO DO: make static to reduce how much data it contains
		/// Handle dispose, click, keyboard input etc. possibly changing this value
		/// </summary>
		private bool controlUnderCursorResultCached;
		private Rect controlUnderCursorRectCached;
		private Vector2 controlUnderCursorLastCheckPos;
		
		//TO DO: only moves downwards, skips prefixes on the right side,
		//like Rect member prefixes
		public bool TryGetRectOfControlAtPosition(Vector2 position, out Rect result)
		{
			if(controlUnderCursorLastCheckPos == position)
			{
				result = controlUnderCursorRectCached;
				return controlUnderCursorResultCached;
			}
			controlUnderCursorLastCheckPos = position;
			
			int idWas = GUIControlUtility.KeyboardControl;
			int stop = endControlID + AppendLastCheckedId;
			GUIControlUtility.KeyboardControl = 0;
			int lastSelected = idWas;
			for(int id = controlId + 1; id < stop; id++)
			{
				SelectNextControlInsideEditorDown(false, false);

				if(GUIControlUtility.KeyboardControl == lastSelected)
				{
					break;
				}
				lastSelected = GUIControlUtility.KeyboardControl;

				var rect = GUIControlUtility.Info.KeyboardRect;
				if(rect.Contains(position))
				{
					//Debug.Log("id " + lastSelected + " rect " + rect + " CONTAINED mousePos " + position + "!!!");
					result = rect;
					GUIControlUtility.KeyboardControl = idWas;
					controlUnderCursorRectCached = result;
					controlUnderCursorResultCached = true;
					return true;
				}
			}

			result = default(Rect);
			GUIControlUtility.KeyboardControl = idWas;
			controlUnderCursorResultCached = false;
			return false;
		}
		#endif
		
		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList()
		{
			if(!UsesEditorForDrawingBody)
			{
				memberBuildListActuallyPopulated = true;
			
				if(debugModeDisplaySettings == null)
				{
					debugModeDisplaySettings = new DebugModeDisplaySettings();
				}

				ParentDrawerUtility.GetMemberBuildList(this, linkedMemberHierarchy, ref memberBuildList, debugModeDisplaySettings);
			}
		}

		/// <inheritdoc />
		protected override void DoBuildMembers()
		{
			if(!UsesEditorForDrawingBody)
			{
				base.DoBuildMembers();
				return;
			}

			// members could contains null elements after leaving debug mode
			DrawerArrayPool.Resize(ref members, 0);

			// Added to fix issue where first control would get selected after changing debug mode.
			if(!ObjectPicker.IsOpen && GUIUtility.hotControl == 0)
			{
				KeyboardControlUtility.KeyboardControl = 0;
			}
		}

		/// <inheritdoc/>
		protected sealed override bool OverrideFieldFocusing()
		{
			if(!UsesEditorForDrawingBody)
			{
				return true;
			}

			switch(inspector.Preferences.overrideCustomEditorFieldFocusing)
			{
				case Sisus.OverrideFieldFocusing.Never:
					return false;
				case Sisus.OverrideFieldFocusing.Dynamic:
					return WantsToOverrideFieldFocusing();
				case Sisus.OverrideFieldFocusing.Always:
					return true;
				default:
					throw new IndexOutOfRangeException();
			}
		}

		/// <summary>
		/// Determines if prefers field focusing system to be overridden inside the Custom Editor,
		/// or if prefers to allow Custom Editor to handle it internally.
		/// </summary>
		/// <returns> True if overriding is preferred, false if using internal system is preferred. </returns>
		protected virtual bool WantsToOverrideFieldFocusing()
		{
			return true;
		}

		/// <inheritdoc/>
		protected override void SelectFirstField()
		{
			#if DEV_MODE
			if(!OverrideFieldFocusing()) { Debug.LogWarning(Msg("SelectFirstField called with OverrideFieldFocusing=", false, " and KeyboardControl=", KeyboardControlUtility.KeyboardControl)); }
			#endif

			SelectHeaderPart(HeaderPart.Base, false);
			
			#if USE_FOCUS_CONTROLLER
			KeyboardFocusController.SelectFirstField(this, beforeHeaderControlId, EndControlId, SelectHeaderPart, GetNextSelectableDrawerDown, !OverrideFieldFocusing());
			#else
			KeyboardControlUtility.KeyboardControl = beforeHeaderControlId;
			SelectNextFieldDown(0);
			#endif
		}

		private void SelectLastField()
		{
			#if USE_FOCUS_CONTROLLER
			KeyboardFocusController.SelectLastField(this, EndControlId, SelectHeaderPart);
			#else
			var getHeight = Height;
			if(getHeight <= HeaderHeight)
			{
				#if DEV_MODE
				Debug.LogWarning(Msg("SelectLastField selecting HeaderPart.Base because Height (", getHeight, ") <= HeaderHeight (", HeaderHeight, ")"));
				#endif
				SelectHeaderPart(headerParts.Base);
				ScrollToShow();
				return;
			}

			var rectWas = new Rect(ControlsLeftMargin, bodyLastDrawPosition.y + getHeight - HeaderHeight, DrawGUI.InspectorWidth - 18f, ControlsRowHeight);

			//first try finding only controls that are smaller than endControlID +1
			//only if that fails try values larger than that
			KeyboardControlUtility.KeyboardControl = EndControlId + AppendLastCheckedId + 1;

			#if DEV_MODE
			Debug.Log("-------"+ToString() + ".SelectLastField - rectWas=" + rectWas+", KeyboardControl="+KeyboardControlUtility.KeyboardControl+", controlId="+controlId+ ", endControlID="+ EndControlId);
			#endif
			
			var setId = GetNextEditorControlUp(rectWas);

			#if DEV_MODE
			Debug.Log("SelectLastField GetNextEditorControlUp result: " + StringUtils.ToColorizedString(setId));
			#endif

			if(setId == -1)
			{
				SelectHeaderPart(headerParts.Base);
				ScrollToShow();
				return;
			}

			if(setId == 0)
			{
				KeyboardControlUtility.KeyboardControl = KeyboardControlUtility.KeyboardControl + 200;
				setId = GetNextEditorControlUp(rectWas);
				if(setId == -1 || setId == 0)
				{
					SelectHeaderPart(headerParts.Base);
					ScrollToShow();
					return;
				}
			}

			KeyboardControlUtility.SetKeyboardControl(setId, 3);
			ScrollToShow();
			#endif
		}
		
		/// <summary>
		/// Bounds inside which controls should be
		/// if they belong to this Editor
		/// </summary>
		/// <returns></returns>
		protected virtual Rect ControlRectBounds()
		{
			float totalHeight = Height;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(lastDrawPosition.height.Equals(totalHeight), "lastDrawPosition.height ("+lastDrawPosition.height+") != totalHeight ("+totalHeight+")");
			#endif

			// if unolded or no controls inside body, return bounds with zero height.
			if(totalHeight <= HeaderHeight)
			{
				return Rect.zero;
			}

			float headerHeight = HeaderHeight;
			
			var bounds = lastDrawPosition;
			float topOffset = headerHeight + PrefixResizerDragHandleHeight + ControlsTopMargin;
			bounds.y += topOffset;
			bounds.height = totalHeight - topOffset - ControlsBottomMargin;
			bounds.x = ControlsLeftMargin;
			bounds.width = Width - ControlsLeftMargin - ControlsRightMargin;
			return bounds;
		}

		/// <inheritdoc/>
		protected override void OnSelectedInternal(ReasonSelectionChanged reason, IDrawer previous, bool isMultiSelection)
		{
			#if DEV_MODE
			//Debug.Log(StringUtils.ToColorizedString(ToString(), " gaining focus from ", previous, " (reason=", reason, ")"));
			#endif

			switch(reason)
			{
				case ReasonSelectionChanged.SelectControlUp:
					var getHeight = Height;
					if(getHeight <= HeaderHeight)
					{
						SelectHeaderPart(headerParts.Base);
						return;
					}
					SelectLastField();
					return;
				case ReasonSelectionChanged.SelectPrevControl:
					unfoldedHeight = Height;
					// if has no fields (height equals height of only header) select last header part
					if(unfoldedHeight <= HeaderHeight)
					{
						SelectHeaderPart(headerParts.LastSelectable);
						return;
					}
					// otherwise select last field
					SelectLastField();
					return;
			}
			
			base.OnSelectedInternal(reason, previous, isMultiSelection);
		}
		
		/// <inheritdoc/>
		protected override void ScrollToShow()
		{
			if(Selected && !HeaderIsSelected)
			{
				inspector.ScrollToShow(KeyboardControlUtility.Info.KeyboardRect);
			}
			else
			{
				inspector.ScrollToShow(PrefixLabelPosition);
			}
		}

		/// <summary> Move focus to next control inside Editor above currently focused control. </summary>
		/// <param name="rectWas"> Position and dimensions of the currenly focused control. </param>
		/// <returns> ControlID of next control above currently focused one within this Editor, or -1 if none found. </returns>
		private int GetNextEditorControlUp(Rect rectWas)
		{
			#if DEV_MODE && ENABLE_ASSERTATIONS
			Debug.Assert(height > HeaderHeight);
			#endif
			
			int idWas = KeyboardControlUtility.KeyboardControl;

			#if DEV_MODE
			Debug.Log(ToString() + ".GetNextEditorControlUp(rectWas=" + rectWas + ") with editorEnd=" + (bodyLastDrawPosition.y + Height - 24f)+", idWas="+ idWas+", controlId="+controlId);
			#endif

			if(idWas <= controlId) //probably zero
			{
				if(controlId != 0) { Debug.Log("GetNextEditorControlUp: -1 because idWas ("+idWas+") <= controlId ("+controlId+")"); }
				return -1;
			}

			//is something consuming this event? Why does it not change the selection automatically?
			int bestId = 0;

			var bounds = ControlRectBounds();
			float editorEnd = bounds.yMax;

			// if last rect is above bounds bottom then move bounds bottom
			// to start at last rect top
			if(rectWas.y < bounds.yMax)
			{
				bounds.yMax = rectWas.y;
			}
			
			//float preferredMaxY = Mathf.Min(editorEnd, rectWas.y - 2f);
			float optimalY = Mathf.Min(editorEnd, rectWas.y - 2f);

			float bestMatch = Mathf.Infinity;
			var rect = KeyboardControlUtility.Info.KeyboardRect;
			int prevMember3Index = Get3MemberControlIndex(rectWas);

			for(int id = idWas - 1; id > controlId; id--) //Should this start from something like GUIControlUtility.KeyboardControl + 100 instead?
			{
				KeyboardControlUtility.KeyboardControl = id;

				var nextRect = KeyboardControlUtility.Info.KeyboardRect;
				bool outOfBounds;
				if(GetShouldSkipControl(rect, nextRect, bounds, id, out outOfBounds))
				{
					rect = nextRect;
					continue;
				}
				rect = nextRect;
				
				float diffY = Mathf.Abs(optimalY - rect.yMax);
				
				int diffId = GetMatchPreviousID(idWas, id);
				float diffX = Mathf.Abs(rectWas.x - rect.x);
				float matchBase = 100f * diffY + 5f * diffX + diffId; //UPDATE had to greatly reduce x multiplier, or x position differences could cause valid fields to get skipped
				
				float addID = GetMatchIDScore(diffId);
				float addY = GetUpDownYMatchScore(rect, diffY);
				float addX = GetUpDownXMatchScore(rectWas, rect, diffX, prevMember3Index);
				float addHeight = GetUpDownHeightMatchScore(rect);
				float addWidth = GetUpDownWidthMatchScore(rectWas.width, rect);

				float match = matchBase + addID + addY + addX + addHeight + addWidth;

				if(match <= bestMatch)
				{
					bestMatch = match;
					bestId = id;
					#if DEV_MODE && DEBUG_GET_NEXT_CONTROL
					Debug.Log("<color=green>BEST MATCH! id=" + id + ", rect=" + rect + "\nmatch=" + match + ", (base="+ matchBase+", diffY=" + diffY + ", diffX=" + diffX + ", diffID=" + diffId + ", addX="+ addX+", addY="+ addY+", addHeight="+addHeight+", addWidth="+addWidth+ ", addID=" + addID +")</color>");
					#endif
				}
				#if DEV_MODE && DEBUG_GET_NEXT_CONTROL
				else { Debug.Log("(not best) id=" + id + ", rect=" + rect + "\nmatch=" + match + ", (base=" + matchBase + ", diffY=" + diffY + ", diffX=" + diffX + ", diffID=" + diffId + ", addX=" + addX + ", addY=" + addY + ", addHeight=" + addHeight + ", addWidth=" + addWidth + ", addID=" + addID + ")"); }
				#endif
			}

			return bestId;
		}
		
		private int GetNextEditorControlDown(bool secondAttempt = false)
		{
			int idWas = KeyboardControlUtility.KeyboardControl;
			
			var bounds = ControlRectBounds();
			
			float optimalY;
			Rect rectWas;
			if(idWas == 0 || HeaderIsSelected)
			{
				rectWas = bounds;
				rectWas.y -= ControlsRowHeight;
				rectWas.height = ControlsRowHeight;
				
				optimalY = bounds.y;
			}
			else
			{
				rectWas = KeyboardControlUtility.Info.KeyboardRect;
				optimalY = rectWas.yMax + 2f; // 2f is offset between controls (margin x 2)

				// if bottom of last selected control is below bounds top
				// then adjust bounds so that resulting control cannot be above
				// previously selected control
				if(rectWas.yMax > bounds.y)
				{
					float removeFromTop = rectWas.yMax - bounds.y;
				
					#if DEV_MODE && DEBUG_SELECT_NEXT_FIELD
					Debug.Log("Removing "+removeFromTop+" from bounds top because rectWas.yMax ("+rectWas.yMax+") > bounds.y ("+bounds.y+")\nbounds="+bounds+", rectWas="+rectWas);
					#endif

					bounds.y += removeFromTop;
					bounds.height -= removeFromTop;
				}
			}

			int bestId = 0;
			int outOfBoundsCounter = 0;
			
			float preferredMaxY = Height - 31f;
			float bestMatch = Mathf.Infinity;
			
			int prevMember3Index = Get3MemberControlIndex(rectWas);

			//sometimes control IDs are higher than endControlID, it's not a very reliable method, unfortunately
			int start = HeaderIsSelected || idWas <= controlId ? controlId + 1 : idWas + 1;
			if(start == idWas)
			{
				start++;
			}
			
			int stop = Mathf.Max(EndControlId, idWas) + AppendLastCheckedId;
			//UPDATE: had to add this to make things work with CameraInspector
			if(secondAttempt)
			{
				stop += 200;
			}

			#if DEV_MODE && DEBUG_GET_NEXT_CONTROL
			Debug.Log("<color=blue>GetNextEditorControlDown: idWas=" + idWas + ", controlId=" + controlId+", start="+start+", stop="+stop+"</color>\r\nrectWas="+rectWas+", bounds="+bounds+"\r\nprevMember3Index="+ prevMember3Index+ ", preferredMaxY="+ preferredMaxY+ ", secondAttempt="+ secondAttempt +"\n" + ToString());
			#endif

			float optimalX = rectWas.x;

			var rect = rectWas;
			for(int id = start; id < stop; id++)
			{
				KeyboardControlUtility.KeyboardControl = id;

				var nextRect = KeyboardControlUtility.Info.KeyboardRect;
				bool outOfBounds;
				if(GetShouldSkipControl(rect, nextRect, bounds, id, out outOfBounds))
				{
					if(outOfBounds)
					{
						outOfBoundsCounter++;
						if(outOfBoundsCounter >= GetNextFieldMaxOutOfBoundsCount) //In NetworkTransform this was 41 before a valid field was found UPDATE: Was 50 for CameraDrawer
						{
							#if DEV_MODE && DEBUG_GET_NEXT_CONTROL
							Debug.Log("Giving up because outOfBoundsCounter >= "+ GetNextFieldMaxOutOfBoundsCount);
							#endif
							break;
						}
					}
					rect = nextRect;
					continue;
				}
				rect = nextRect;

				float diffY = Mathf.Abs(rect.y - optimalY);
				int diffId = GetMatchNextID(idWas, id);

				float diffX = Mathf.Abs(rect.x - optimalX);
				float match = 100f * diffY + 5f * diffX + diffId;

				if(rect.yMax > preferredMaxY)
				{
					match += 10000f;
				}

				float addID = GetMatchIDScore(diffId);
				float addY = GetUpDownYMatchScore(rect, diffY); 
				float addX = GetUpDownXMatchScore(rectWas, rect, diffX, prevMember3Index);
				float addHeight = GetUpDownHeightMatchScore(rect);
				float addWidth = GetUpDownWidthMatchScore(rectWas.width, rect);

				match = match + addID + addY + addX + addHeight + addWidth;

				if(match <= bestMatch)
				{
					bestMatch = match;
					bestId = id;
					#if DEV_MODE && DEBUG_GET_NEXT_CONTROL
					Debug.Log("<color=green>BEST MATCH! id=" + id + "</color>\r\nrect=" + rect + "\r\nmatch=" + match + " (addID="+addID+", addY="+addY+", addX="+addX+", addHeight="+addHeight+ ", addWidth="+ addWidth+", diffY=" + diffY + ", diffX=" + diffX + ", diffID=" + diffId + ")");
					#endif
				}
				#if DEV_MODE && DEBUG_GET_NEXT_CONTROL
				else { Debug.Log("(not best) id=" + id + "\r\nrect=" + rect + "\r\nmatch=" + match + " (addID=" + addID + ", addY=" + addY + ", addX=" + addX + ", addHeight=" + addHeight + ", addWidth=" + addWidth + ", diffY=" + diffY + ", diffX=" + diffX + ", diffID=" + diffId + ")"); }
				#endif
			}

			if(bestId == 0)
			{
				if(!secondAttempt && AppendLastCheckedId == 0)
				{
					KeyboardControlUtility.KeyboardControl = idWas;
					return GetNextEditorControlDown(true);
				}
			}

			return bestId;
		}

		/// <summary> When getting next Editor control in a direction, determines whether or not should skip this control. </summary>
		/// <param name="prevRect"> The rect of previously selected control. </param>
		/// <param name="rect"> The rect of control to test. </param>
		/// <param name="bounds"> The bounds inside which all control are found. </param>
		/// <param name="id"> ID of control to test. </param>
		/// <param name="wasOutOfBounds"> [out] Set to true if control was out of bounds. </param>
		/// <returns> True if should skip control, false if not. </returns>
		protected virtual bool GetShouldSkipControl(Rect prevRect, Rect rect, Rect bounds, int id, out bool wasOutOfBounds)
		{
			if(!KeyboardControlUtility.Info.CanHaveKeyboardFocus(id))
			{
				#if DEV_MODE && DEBUG_SKIP_CAN_NOT_HAVE_KEYBOARD_CONTROL
				Debug.Log("!!!!! control " + id + " CanHaveKeyboardFocus was false!");
				#endif
				wasOutOfBounds = false;
				return true;
			}

			// enum fields in Odin Inspector can be like this
			if(rect.IsZero())
			{
				wasOutOfBounds = false;
				#if SUPPORT_KEYBOARD_FOCUS_FOR_ZERO_RECT_CONTROLS
				return false;
				#else
				return true;
				#endif
			}
			

			if(rect == prevRect)
			{
				#if DEV_MODE && DEBUG_SKIP_CAN_NOT_HAVE_KEYBOARD_CONTROL
				Debug.Log("!!!!! control " + id + " rect == prevRect ("+prevRect+"), ignoring");
				#endif
				wasOutOfBounds = false;
				return true;
			}

			return GetShouldSkipControl(rect, bounds, id, out wasOutOfBounds);
		}

		/// <summary> When getting next Editor control in a direction, determines whether or not should skip this control. </summary>
		/// <param name="rect"> The rect of control to test. </param>
		/// <param name="bounds"> The bounds inside which all control are found. </param>
		/// <param name="id"> ID of control to test. </param>
		/// <param name="wasOutOfBounds"> [out] Set to true if control was out of bounds. </param>
		/// <returns> True if should skip control, false if not. </returns>
		private bool GetShouldSkipControl(Rect rect, Rect bounds, int id, out bool wasOutOfBounds)
		{
			wasOutOfBounds = false;

			if(GetIsKeyboardSelectedControlInvalid(rect, id))
			{
				return true;
			}

			// enum fields in Odin Inspector can be like this
			if(rect.IsZero())
			{
				wasOutOfBounds = false;
				#if SUPPORT_KEYBOARD_FOCUS_FOR_ZERO_RECT_CONTROLS
				return false;
				#else
				return true;
				#endif
			}

			if(IsOutOfVerticalBounds(rect, bounds))
			{
				#if DEV_MODE && DEBUG_SKIP_OUT_OF_BOUNDS
				Debug.Log("Skipping control " + id + " because y component (" + rect.y + "..." + rect.yMax + ") outside bounds (" + bounds.y + "..." + bounds.yMax + ")...");
				#endif
				wasOutOfBounds = true;
				return true;
			}

			if(IsOutOfHorizontalBounds(rect, bounds))
			{
				#if DEV_MODE && DEBUG_SKIP_OUT_OF_BOUNDS
				Debug.Log("Skipping control " + id + " because x component (" + rect.x + "..." + rect.xMax + ") outside bounds (" + bounds.x + "..." + bounds.xMax + ")...");
				#endif
				wasOutOfBounds = true;
				return true;
			}

			return false;
		}

		private bool IsOutOfBounds(Rect rect, Rect bounds)
		{
			return IsOutOfHorizontalBounds(rect, bounds) || IsOutOfVerticalBounds(rect, bounds);
		}

		private bool IsOutOfHorizontalBounds(Rect rect, Rect bounds)
		{
			return rect.x < bounds.x || rect.xMax > bounds.xMax;
		}

		private bool IsOutOfVerticalBounds(Rect rect, Rect bounds)
		{
			return rect.y < bounds.y || rect.yMax > bounds.yMax;
		}

		private bool GetIsKeyboardSelectedControlInvalid(Rect rect, int id)
		{
			float inspectorWidthDiff = Width - rect.width;

			if(rect.x.Equals(ControlsLeftMargin))
			{
				switch((int)inspectorWidthDiff)
				{
					case 210:
					case 156:
					case 33:
					case 54:
						#if DEV_MODE && DEBUG_SKIP_PHANTOM_CONTROL
						Debug.Log("Skipping control " + id + " because inspectorWidthDiff is " + inspectorWidthDiff + ". This is probably a phantom control.");
						#endif
						return true;
				}
			}
			#if DEV_MODE && PI_ASSERTATIONS
			else { Debug.Assert(Mathf.Abs(rect.x - ControlsLeftMargin) > 0.99f, "rect.x="+rect.x+", ControlsLeftMargin="+ControlsLeftMargin); }
			#endif

			return false;
		}

		private static int GetMatchPreviousID(int idWas, int id)
		{
			int optimalId = idWas - 1;
			return Mathf.Abs(id - optimalId);
		}

		private static int GetMatchNextID(int idWas, int id)
		{
			int optimalId = idWas + 1;
			return Mathf.Abs(id - optimalId);
		}

		/// <summary>
		/// Gets a score based on how close the ID is to being a perfect match
		/// in the context of figuring out the next field to gain focus when moving keyboard focus.
		/// </summary>
		/// <param name="diffID"> Identifier offset from perfect match. </param>
		/// <returns> Score. Smaller is better. </returns>
		protected virtual float GetMatchIDScore(int diffID)
		{
			if(diffID >= 40)
			{
				return 0f;
			}

			if(diffID >= 20)
			{
				// UPDATE: also changed from -2500f to improve Light component results
				// (actually problems are caused by above changes, since wrong id with 17 diff was beating right id with 37 diff - so could also merge these two checks)
				return -1500f;
			}

			if(diffID > 0)
			{
				// UPDATE: changed from -2500f to improve Light component results
				return -3000f;
			}

			// was -100f before, because resulted in some false positive earlier? But does that still happen?
			return -5000f;
		}

		
		/// <summary>
		/// Gets a score based on how close the control y position is to being a perfect match
		/// in the context of figuring out the next field to gain focus when moving keyboard focus up or down.
		/// </summary>
		/// <param name="diffY"> y position offset from perfect match. </param>
		/// <returns> Score. Smaller is better. </returns>
		protected virtual float GetUpDownYMatchScore(Rect rect, float diffY)
		{
			int diffYInt = (int)diffY;
			float result = GetYMatchScoreBase(rect);

			if(!((float)diffYInt).Equals(diffY))
			{
				#if DEV_MODE && DEBUG_GET_NEXT_CONTROL_Y
				Debug.Log("rect "+rect+" returning "+result+" + 21000f because diffYInt ("+diffYInt+") != diffY ("+diffY+") with difference="+Mathf.Abs(diffYInt-diffY));
				#endif
				return result + 21000f;
			}

			switch(diffYInt)
			{
				case 0:
					//next row, with 1px margin for both fields
					return result - 10000f;
				case 8:
					//next row with a small gap between (e.g. RectTransform, AudioSource)
					return result - 8000f;
				case 14:
					//two row difference
					return result - 6000f;
				/*
				case 45:
					//two fields with a help box between them (e.g. MeshRenderer)
					return result - 7000f;
				*/
				default:
					#if DEV_MODE && DEBUG_GET_NEXT_CONTROL_Y
					Debug.Log("rect "+rect+" returning "+result+" + 20000f because diffYInt ("+diffYInt+") was not a value we like");
					#endif
					return result + 20000f;
			}
		}

		protected virtual float GetYMatchScoreBase(Rect rect)
		{
			//not 100% sure about this. Can a HelpBox e.g. change y value to be not divisible by 2?
			if(!(rect.y % 2f).Equals(0f))
			{
				return 1000f;
			}
			return 0f;
		}

		/// <summary>
		/// Gets a score based on how close the control x position is to being a perfect match
		/// in the context of figuring out the next field to gain focus when moving keyboard focus up or down.
		/// </summary>
		/// <param name="prevRect"> Rectangle for the previously selected control. </param>
		/// <param name="rect"> Rectangle for the control that is being tested. </param>
		/// <param name="diffX"> x position offset from perfect match. </param>
		/// <param name="prevMember3Index"> Zero-based index of control being tested in row that has three controls on it. -1 if not applicable. </param>
		/// <returns> Score. Smaller is better. </returns>
		protected virtual float GetUpDownXMatchScore(Rect prevRect, Rect rect, float diffX, int prevMember3Index)
		{
			bool startsFromLeftEdge = rect.x.Equals(ControlsLeftMargin);

			float result = GetXMatchScoreBase(rect);
			if(diffX.Equals(0f))
			{
				if(startsFromLeftEdge)
				{
					result -= 12000f;
				}
				else
				{
					result -= 11000f;
				}
			}

			float fullWidth = Width;
			float prefixWidth = PrefixLabelWidth;
			float fullWidthDiff = fullWidth - rect.width - ControlsLeftMargin - ControlsRightMargin;

			if(startsFromLeftEdge)
			{
				// if the control starts from the left edge of the control, then it's width needs to basically match
				// the width of the inspector, or it's probably a "phantom" control (from another window maybe?).
				if(fullWidthDiff > 1f || fullWidthDiff < 0f)
				{
					#if DEV_MODE
					Debug.Log("rect "+rect+" fullWidthDiff ("+fullWidthDiff+") != "+(ControlsLeftMargin + ControlsRightMargin)+" with Width="+Width);
					#endif
					return result + 50000f;
				}

				if(prevRect.x.Equals(prefixWidth + 5f))
				{
					return result - 11000f;
				}
				
				return result - 5000f;
			}

			//test if the field width is one third of the control column width
			//E.g. Vector3's x, y and z fields have this width
			int member3Index = Get3MemberControlIndex(rect);
			if(member3Index != -1)
			{
				float vertDiff = MathUtils.VerticalDifference(rect, prevRect);
				bool vertDiffMatch = vertDiff == 0f || vertDiff == 10f; //10 is found in RectTransform at least
				float vertMult = vertDiffMatch ? 10f : 1f;

				switch(member3Index)
				{
					case 0:
						switch(prevMember3Index)
						{
							case 0:
								return result - 2500f * vertMult;
							case 1:
								return result - 1500f * vertMult;
							case 2:
								return result - 1250f * vertMult;
							default:
								if(prevRect.x.Equals(ControlsLeftMargin))
								{
									return result - 2500f * vertMult;
								}
								return result - 1000f * vertMult;
						}
					case 1:
						switch(prevMember3Index)
						{
							case 1:
								return result - 2500f * vertMult;
							case 0:
							case 2:
								return result - 1500f * vertMult;
							default:
								return result - 1000f * vertMult;
						}
					case 2:
						switch(prevMember3Index)
						{
							case 2:
								return result - 2500f * vertMult;
							case 1:
								return result - 1500f * vertMult;
							case 0:
								return result - 1250f * vertMult;
							default:
								return result - 1000f * vertMult;
						}
				}
			}

			return result;
		}

		protected virtual float GetUpDownHeightMatchScore(Rect rect)
		{
			int heightWholeNumber = (int)rect.height;
			if(!rect.height.Equals(heightWholeNumber))
			{
				return 5000f;
			}
			
			switch(heightWholeNumber)
			{
				case 16:
					//most common control height
					return -5f;
				case 32:
					//two rows is quite common
					return -3f;
				case 42:
					//text area
					return -1f;
				default:
					if((rect.height % ControlsRowHeight).Equals(0f))
					{
						#if DEV_MODE
						Debug.Log("GetUpDownHeightMatchScore("+rect+"): -1 because divisible by "+ControlsRowHeight);
						#endif

						return -1f;
					}

					#if DEV_MODE
					Debug.Log("GetUpDownHeightMatchScore("+rect+"): 0 because not divisible by "+ControlsRowHeight);
					#endif

					return 0f;
			}
		}

		/// <summary>
		/// Gets the zero-based index of control in row where three members are drawn.
		/// If three members are not drawn on same row with control, returns -1.
		/// </summary>
		/// <param name="rect"> The rectangle. </param>
		/// <returns> Zero-based index, or -1 if not part of one. </returns>
		private int Get3MemberControlIndex(Rect rect)
		{
			//RectTransform fields have double height
			if(rect.height.Equals(16f) || rect.height.Equals(32f))
			{
				float afterPrefix = PrefixLabelWidth + PrefixColumnEndToControlOffset;
				if(rect.x >= afterPrefix)
				{
					float third = Vector3MemberControlWidth();
					//width matches!
					if(MathUtils.Approximately(rect.width, third))
					{
						float localX = rect.x - afterPrefix;
						if(Mathf.Approximately(localX, 0f))
						{
							#if DEV_MODE && DEBUG_MEMBER_CONTROL
							Debug.Log("Get3MemberControlIndex: 0");
							#endif
							return 0;
						}
						if(Mathf.Approximately(localX, third + ControlsOffset))
						{
							#if DEV_MODE && DEBUG_MEMBER_CONTROL
							Debug.Log("Get3MemberControlIndex: 1");
							#endif
							return 1;
						}
						if(Mathf.Approximately(localX, third + third + ControlsOffset + ControlsOffset))
						{
							#if DEV_MODE && DEBUG_MEMBER_CONTROL
							Debug.Log("Get3MemberControlIndex: 2");
							#endif
							return 2;
						}
						#if DEV_MODE && DEBUG_MEMBER_CONTROL
						Debug.Log("Mathf.Abs(rect.width - third) was < 0.001 but localX "+localX+ " was not equal to 0, "+ (third + ControlsOffset)+ " or "+(third + third + ControlsOffset + ControlsOffset));
						#endif
					}
				}
			}

			return -1;
		}

		protected float GetLeftXMatchScore(Rect prevRect, Rect rect, float matchX)
		{
			float result = GetXMatchScoreBase(rect);
			if(matchX == 0f)
			{
				result -= 11000f;
			}

			if(rect.x == ControlsLeftMargin)
			{
				if(Get3MemberControlIndex(prevRect) == 0)
				{
					return result - 50000f;
				}
				return result - 1000f;
			}

			//test if the field width is one third of the control column width
			//E.g. Vector3's x, y and z fields have this width
			int member3Index = Get3MemberControlIndex(rect);
			if(member3Index != -1)
			{
				switch(member3Index)
				{
					case 0:
					case 1:
						if(Get3MemberControlIndex(prevRect) == member3Index - 1)
						{
							return result - 50000f;
						}
						return result + 50000f;
				}
			}
			return result;
		}

		private float GetXMatchScoreBase(Rect rect)
		{
			// this is valid for toggle fields, though!
			// rect.x = 154, Prefix = 198
			// fullWidth = 365
			// fullWidth - Prefix = 167
			// 167 - 154 = 13 (Controls Left Margin)
			if(rect.x > 18f && rect.x < PrefixLabelWidth)
			{
				// new: support toggle field, where rect doesn't contain prefix portion
				if(rect.x.Equals(Width - ControlsLeftMargin - PrefixLabelWidth))
				{
					return 0f;
				}

				#if DEV_MODE && DEBUG_GET_NEXT_CONTROL_Y
				Debug.Log("XBase: rect "+rect+" returning "+50000f+" because rect.x ("+rect.x+") > 18 and not equal to "+(Width - ControlsLeftMargin - PrefixLabelWidth));
				#endif

				return 50000f;
			}
			return 0f;
		}

		protected float GetRightXMatchScore(Rect prevRect, Rect rect, float matchX)
		{
			float result = GetXMatchScoreBase(rect);
			if(matchX == 0f)
			{
				result -= 11000f;
			}

			//test if the field width is one third of the control column width
			//E.g. Vector3's x, y and z fields have this width
			int member3Index = Get3MemberControlIndex(rect);
			if(member3Index != -1)
			{
				switch(member3Index)
				{
					case 0:
						if(prevRect.x.Equals(ControlsLeftMargin))
						{
							return result - 50000f;
						}
						return result + 50000f;
					default:
						if(Get3MemberControlIndex(prevRect) == member3Index + 1)
						{
							return result - 50000f;
						}
						return result + 50000f;
				}
			}
			return result;
		}

		private float GetControlWidth(int memberCount)
		{
			float controlsColumnWidth = Width - PrefixLabelWidth - PrefixColumnEndToControlOffset - ControlsRightMargin;

			//test if the field width is one third of the control column width
			//E.g. Vector3's x, y and z fields have this width
			//return ((controlsColumnWidth - ControlsOffset * ((memberCount - 1)) / memberCount));
			return (controlsColumnWidth - ControlsOffset * (memberCount - 1)) / memberCount;
		}

		private float Vector3MemberControlWidth()
		{
			return GetControlWidth(3);
		}

		/// <summary>
		/// Gets a score based on how close the control rect width is to being a perfect match
		/// in the context of figuring out the next field to gain focus when moving keyboard focus up or down.
		/// </summary>
		/// <param name="prevControlWidth"> Width of previously selected control. </param>
		/// <param name="rect"> Bounds of control to test. </param>
		/// <returns> Score. Smaller is better. </returns>
		protected virtual float GetUpDownWidthMatchScore(float prevControlWidth, Rect rect)
		{
			if(prevControlWidth.Equals(rect.width))
			{
				return -100f; //too much?
			}

			if(rect.x.Equals(ControlsLeftMargin))
			{
				float widthWithoutLeftMargin = Width - ControlsLeftMargin;
				float fullWidthDiff = widthWithoutLeftMargin - rect.width - ControlsRightMargin;
				//if(!rect.width.Equals(Width - 18f))
				if(fullWidthDiff > 1f || fullWidthDiff < 0f)
				{
					float widthWithoutPrefixLabel = widthWithoutLeftMargin - PrefixLabelWidth;

					// new: support toggle field, where rect doesn't contain prefix portion
					if(rect.x.Equals(widthWithoutPrefixLabel))
					{
						return 0f;
					}

					#if DEV_MODE
					Debug.LogWarning("Phantom control?: "+rect+"\r\nWidth="+Width+", PrefixLabelWidth="+PrefixLabelWidth+"\r\nControlsLeftMargin="+ControlsLeftMargin+", ControlsRightMargin="+ControlsRightMargin+"\r\nfullWidthDiff="+fullWidthDiff+"\r\nwidthWithoutPrefixLabel="+widthWithoutPrefixLabel+"\r\nwidthWithoutPrefixLabelDiff="+(rect.x - widthWithoutPrefixLabel));
					#endif
					return 10000f;
				}
			}

			return 0f;
		}
		
		private void SelectNextEditorControlRight()
		{
			int idWas = KeyboardControlUtility.KeyboardControl;

			if(idWas == 0)
			{
				SelectNextHeaderPartRight(false);
				ScrollToShow();
				return;
			}

			var rectWas = KeyboardControlUtility.Info.KeyboardRect;
			
			int bestID = idWas;

			int outOfBoundsCounter = 0;

			var bounds = ControlRectBounds();
			bounds.y = rectWas.y;
			bounds.yMax = rectWas.yMax;
			bounds.x = rectWas.x;
			
			float preferredMinX = Width - rectWas.width <= 18f ? PrefixLabelWidth + 4f : rectWas.xMax + 2f;
			float bestMatch = Mathf.Infinity;
			Rect rect = rectWas;

			//sometimes control IDs are higher than endControlID, it's not very reliable unfortunately :(
			int stop = Mathf.Max(EndControlId, KeyboardControlUtility.KeyboardControl + 100);
			int start = KeyboardControlUtility.KeyboardControl <= controlId ? controlId + 1 : KeyboardControlUtility.KeyboardControl + 1;

			#if DEV_MODE
			Debug.Log("<color=blue>SelectNextEditorControlRight: idWas=" + idWas + ", controlId=" + controlId+", rect="+rect+ ", bounds="+ bounds+", start="+start+", stop="+stop+ "</color>\n" + ToString());
			#endif
			
			for(int id = start; id < stop; id++)
			{
				KeyboardControlUtility.KeyboardControl = id;

				var nextRect = KeyboardControlUtility.Info.KeyboardRect;
				bool outOfBounds;
				if(GetShouldSkipControl(rect, nextRect, bounds, id, out outOfBounds))
				{
					if(outOfBounds)
					{
						outOfBoundsCounter++;
						if(outOfBoundsCounter >= GetNextFieldMaxOutOfBoundsCount)
						{
							break;
						}
					}
					rect = nextRect;
					continue;
				}
				rect = nextRect;

				float diffY = Mathf.Abs(bounds.y - rect.y);

				#if DEV_MODE
				if(DebugSelectNextControl) { Debug.Log("KeyboardControl " + id + " diffY=" + diffY + " (rect=" + rect + ")"); }
				#endif

				int matchY = (int)diffY;
				int matchID = GetMatchNextID(idWas, id);
				float matchX = Mathf.Abs(rect.x - preferredMinX);
				float match = 100f * matchY + 50f * matchX + matchID;

				float addID = GetMatchIDScore(matchID);
				float addY = GetYMatchScoreBase(rect);
				float addX = GetRightXMatchScore(rectWas, rect, matchX);
				float addHeight = GetUpDownHeightMatchScore(rect);
				float addWidth = GetUpDownWidthMatchScore(rectWas.width, rect);

				match = match + addID + addY + addX + addHeight + addWidth;

				if(rect.x < preferredMinX)
				{
					match += 10000f;
				}

				if(match <= bestMatch)
				{
					bestMatch = match;
					bestID = id;
					#if DEV_MODE
					if(DebugSelectNextControl) { Debug.Log("<color=green>BEST MATCH! id=" + id + ", rect=" + rect + ", match=" + match + ", (addID=" + addID + ", addY=" + addY + ", addX=" + addX + ", addHeight=" + addHeight + ", addWidth=" + addWidth + ", y=" + matchY + ", x=" + matchX + ", id=" + matchID + ")</color>"); }
					#endif
				}
				#if DEV_MODE
				else if(DebugSelectNextControl) { Debug.Log("(not best) id=" + id + ", rect=" + rect + ", match=" + match + ", (addID=" + addID + ", addY=" + addY + ", addX=" + addX + ", addHeight=" + addHeight + ", addWidth=" + addWidth + ", y=" + matchY + ", x=" + matchX + ", id=" + matchID + ")"); }
				#endif
			}

			KeyboardControlUtility.KeyboardControl = bestID;

			if(bestID < idWas)
			{
				KeyboardControlUtility.KeyboardControl = 0;
				inspector.Select(GetNextSelectableDrawerRight(true, this), ReasonSelectionChanged.SelectNextControl);
			}
			ScrollToShow();
		}
		
		private bool IsKeyboardControlOutOfBodyBounds()
		{
			var id = KeyboardControlUtility.KeyboardControl;

			if(id == 0)
			{
				return false;
			}

			var rect = KeyboardControlUtility.Info.KeyboardRect;
			
			// For some fields like enum fields in Odin Inspector, KeyboardRect can be (0,0,0,0).
			// In this case there's no way to detect if the field is out of bounds.
			if(rect.IsZero())
			{
				return false;
			}

			var bounds = ControlRectBounds();
			
			if(rect.y < bounds.y || rect.yMax > bounds.yMax)
			{
				#if DEV_MODE
				Debug.Log(ToString()+" Control " + id + " is out of bounds because y component (" + rect.y + "..." + rect.yMax + ") outside bounds (" + bounds.y + "..." + bounds.yMax + ")...");
				#endif
				return true;
			}

			if(rect.x < bounds.x || rect.xMax > bounds.xMax)
			{
				#if DEV_MODE
				Debug.Log(ToString() + " Control " + id + " is out of bounds because x component (" + rect.x + "..." + rect.xMax + ") outside bounds (" + bounds.x + "..." + bounds.xMax + ")...");
				#endif
				return true;
			}

			return false;
		}

		/// <inheritdoc cref="IDrawer.OnKeyboardInputGiven" />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			#if DEV_MODE && DEBUG_KEYBOARD_INPUT
			Debug.Log(ToString() + ".OnKeyboardInputGiven(" + inputEvent.keyCode + ") char='"+StringUtils.ToString(inputEvent.character) + ", mods=" + inputEvent.modifiers + "', keyboardControl=" + KeyboardControlUtility.KeyboardControl + ", KeyboardRect=" + KeyboardControlUtility.Info.KeyboardRect+", EditingTextField="+DrawGUI.EditingTextField+", SelectedHeaderPart="+SelectedHeaderPart+ ", OverrideFieldFocusing()="+OverrideFieldFocusing());
			#endif

			#if UNITY_EDITOR && DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(EditorGUIUtility.editingTextField == DrawGUI.EditingTextField, "DrawGUI.EditingTextField "+DrawGUI.EditingTextField+" != EditorGUIUtility.editingTextField");
			#endif

			#if UNITY_EDITOR
			// Usually DrawGUI.EditingTextField is kept as a separate value from Unity's built-in
			// EditorGUIUtility.editingTextField, since in later Unity versions the value is constantly
			// set to true when a field is selected. Here however we want to sync those two values
			// so that we'll know when to ignore certain shortcut combinations etc.
			if(EditorGUIUtility.editingTextField)
			{
				if(!DrawGUI.EditingTextField)
				{
					DrawGUI.EditingTextField = true;
				}
			}
			#endif
			
			if(DrawGUI.EditingTextField)
			{
				var textFieldType = KeyboardControlUtility.Info.KeyboardRect.height.Equals(42f) ? TextFieldType.TextArea : TextFieldType.TextRow;
				if(keys.DetectTextFieldReservedInput(inputEvent, textFieldType))
				{
					#if DEV_MODE
					Debug.Log("Ignoring input because EditingTextField && DetectTextFieldReservedInput");
					#endif
					return true;
				}
			}
			
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc/>
		protected override void SelectNextFieldUp(int column, bool additive = false)
		{
			if(!UsesEditorForDrawingBody)
			{
				base.SelectNextFieldUp(column, additive);
				return;
			}

			if(Height <= HeaderHeight || HeaderIsSelected)
			{
				KeyboardControlUtility.KeyboardControl = 0;
				inspector.Select(GetNextSelectableDrawerUp(0, this), ReasonSelectionChanged.SelectControlUp);	
				return;
			}

			var previousKeyboardRect = KeyboardControlUtility.Info.KeyboardRect;

			if(OverrideFieldFocusing())
			{
				int idWas = KeyboardControlUtility.KeyboardControl;
				if(idWas == 0)
				{
					SelectLastField();
					ScrollToShow();
					return;
				}

				var setId = unfoldedHeight <= HeaderHeight ? 0 : GetNextEditorControlUp(previousKeyboardRect);

				if(setId <= 0)
				{
					SelectHeaderPart(headerParts.Base);
					ScrollToShow();
				}
				else
				{
					KeyboardControlUtility.SetKeyboardControl(setId, 3);
					SelectHeaderPart(HeaderPart.None);
					ScrollToShow();
				}
			}
			else
			{
				var previousKeyboardControl = KeyboardControlUtility.KeyboardControl;
				SelectHeaderPart(HeaderPart.None, false);
				HandleSelectNextFieldUpLeavingBounds(previousKeyboardControl, previousKeyboardRect);
				OnNextLayout(ScrollToShow);
			}
		}

		private void HandleSelectNextFieldUpLeavingBounds(int previousKeyboardControl, Rect previousKeyboardRect)
		{
			HandleSelectNextFieldLeavingBounds(()=>
			{
				#if DEV_MODE && DEBUG_SKIP_OUT_OF_BOUNDS
				Debug.LogWarning(ToString()+ ".HandleSelectNextFieldUpLeavingBounds - control out of body bounds! previousKeyboardControl=" + previousKeyboardControl + ",  keyboardControl=" + KeyboardControlUtility.KeyboardControl + ", keyboardRect=" + KeyboardControlUtility.Info.KeyboardRect + ", previousKeyboardRect=" + previousKeyboardRect + ", outOfBounds=" + IsKeyboardControlOutOfBodyBounds() + ", controlId=" + controlId + ", endControlId=" + EndControlId);
				#endif

				SelectHeaderPart(HeaderPart.Base);
			});
		}

		/// <inheritdoc/>
		protected override void SelectNextFieldDown(int column, bool additive = false)
		{
			if(!UsesEditorForDrawingBody)
			{
				base.SelectNextFieldDown(column, additive);
				return;
			}

			if(Height <= HeaderHeight)
			{
				KeyboardControlUtility.KeyboardControl = 0;
				inspector.Select(GetNextSelectableDrawerDown(0, this), ReasonSelectionChanged.SelectControlDown);	
				return;
			}

			if(OverrideFieldFocusing())
			{
				var setId = Unfoldedness <= 0f ? 0 : GetNextEditorControlDown();
			
				if(setId == 0)
				{
					KeyboardControlUtility.KeyboardControl = 0;
					inspector.Select(GetNextSelectableDrawerDown(0, this), ReasonSelectionChanged.SelectControlDown);
					return;
				}

				KeyboardControlUtility.SetKeyboardControl(setId, 3);
				SelectHeaderPart(HeaderPart.None);
				ScrollToShow();
			}
			else
			{
				var previousKeyboardControl = KeyboardControlUtility.KeyboardControl;
				var previousKeyboardRect = KeyboardControlUtility.Info.KeyboardRect;
				SelectHeaderPart(HeaderPart.None, false);
				HandleSelectNextFieldDownLeavingBounds(previousKeyboardControl, previousKeyboardRect);
				OnNextLayout(ScrollToShow);
			}
		}

		private void HandleSelectNextFieldDownLeavingBounds(int previousKeyboardControl, Rect previousKeyboardRect)
		{
			HandleSelectNextFieldLeavingBounds(()=>
			{
				#if DEV_MODE && DEBUG_SKIP_OUT_OF_BOUNDS
				Debug.LogWarning(ToString()+ ".HandleSelectNextFieldDownLeavingBounds - control out of body bounds! previousKeyboardControl=" + previousKeyboardControl + ",  keyboardControl=" + KeyboardControlUtility.KeyboardControl + ", keyboardRect=" + KeyboardControlUtility.Info.KeyboardRect + ", previousKeyboardRect=" + previousKeyboardRect + ", outOfBounds=" + IsKeyboardControlOutOfBodyBounds() + ", controlId=" + controlId + ", endControlId=" + EndControlId);
				#endif

				KeyboardControlUtility.KeyboardControl = beforeHeaderControlId;
				inspector.Select(GetNextSelectableDrawerDown(0, this), ReasonSelectionChanged.SelectControlDown);
			});
		}
		
		/// <inheritdoc />
		protected override void SelectNextFieldLeft(bool moveToNextControlAfterReachingEnd, bool additive = false)
		{
			#if DEV_MODE && DEBUG_SELECT_NEXT_FIELD
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".SelectNextFieldLeft(", moveToNextControlAfterReachingEnd, ") SelectedHeaderPart=", SelectedHeaderPart));
			#endif

			if(HeaderIsSelected || Unfoldedness <= 0f || Height <= HeaderHeight)
			{
				SelectNextHeaderPartLeft(moveToNextControlAfterReachingEnd);
				ScrollToShow();
			}
			// new test
			else if(IsFirstFieldSelected())
			{
				if(moveToNextControlAfterReachingEnd)
				{
					SelectNextHeaderPartLeft(moveToNextControlAfterReachingEnd);
					ScrollToShow();
				}
				else if(Event.current != null && Event.current.isMouse && Event.current.type != EventType.Used)
				{
					DrawGUI.Use(Event.current);
				}
			}
			// If CustomEditor custom selection logic is enabled select next field to the left.
			// Also if this is a shift+tab event, use internal focusing system, since that functions
			// identically to Power Inspector's logic.
			//else if((OverrideFieldFocusing() && !moveToNextControlAfterReachingEnd) || IsFirstFieldSelected())
			else if(OverrideFieldFocusing() && !moveToNextControlAfterReachingEnd)
			{
				SelectNextEditorControlLeft();
			}
			// When relying on Unity's internal selection logic for switching between fieldsm
			// the only thing we need to do is detect when the selection has moved out
			// of the bounds of the Editor and react accordingly (select header or move to next component).
			else
			{
				HandleSelectNextFieldLeftLeavingBounds(moveToNextControlAfterReachingEnd, inspector.State.previousKeyboardControl, inspector.State.previousKeyboardControlRect);
				ScrollToShow();
			}
		}

		private void SelectNextEditorControlLeft()
		{
			int idWas = KeyboardControlUtility.KeyboardControl;

			if(idWas == 0 || idWas == beforeHeaderControlId)
			{
				return;
			}

			var rectWas = KeyboardControlUtility.Info.KeyboardRect;
			if(rectWas.x <= 24f)
			{
				return;
			}
			
			int bestID = idWas;

			int outOfBoundsCounter = 0;

			var bounds = ControlRectBounds();

			//UPDATE: when going left, support case like Rect where
			//parent prefix is higher up than current member field.
			//Not sure if we should support even higher differences?
			//Maybe for something like array?
			//bounds.y = rectWas.y;
			bounds.y = rectWas.y - 16f;

			//UPDATE: also added +16 here so that can e.g. move from
			//x member of Rect field to Rect prefix
			bounds.yMax = rectWas.yMax + 16f;
			//is any control ever without any offset? should I split the -2f to preferredMaxX ?
			float preferredMinY = rectWas.y;
			float preferredMaxX = rectWas.x - 2f;

			float bestMatch = Mathf.Infinity;
			Rect rect = rectWas;

			#if DEV_MODE
			Debug.Log("<color=blue>SelectNextEditorControlLeft: idWas=" + idWas + ", controlId=" + controlId+", rect="+rect+ ", bounds="+ bounds+", start="+(KeyboardControlUtility.KeyboardControl - 1) +", stop="+ controlId + "</color>\n" + ToString());
			#endif

			for(int id = KeyboardControlUtility.KeyboardControl - 1; id > controlId; id--)
			{
				KeyboardControlUtility.KeyboardControl = id;

				var nextRect = KeyboardControlUtility.Info.KeyboardRect;
				bool outOfBounds;
				if(GetShouldSkipControl(rect, nextRect, bounds, id, out outOfBounds))
				{
					if(outOfBounds)
					{
						outOfBoundsCounter++;
						if(outOfBoundsCounter >= GetNextFieldMaxOutOfBoundsCount)
						{
							break;
						}
					}
					rect = nextRect;
					continue;
				}
				rect = nextRect;

				float diffY = Mathf.Abs(preferredMinY - rect.y);

				#if DEV_MODE
				if(DebugSelectNextControl) { Debug.Log("KeyboardControl " + id + " diffY=" + diffY + " (rect=" + rect + ")"); }
				#endif

				int matchY = (int)diffY;
				int matchID = GetMatchPreviousID(idWas, id);
				float matchX = Mathf.Abs(rect.xMax - preferredMaxX);
				float match = 100f * matchY + 50f * matchX + matchID;
				
				float addID = GetMatchIDScore(matchID);
				float addY = GetYMatchScoreBase(rect);
				float addX = GetLeftXMatchScore(rectWas, rect, matchX);
				float addHeight = GetUpDownHeightMatchScore(rect);
				float addWidth = GetUpDownWidthMatchScore(rectWas.width, rect);

				match = match + addID + addY + addX + addHeight + addWidth;

				if(rect.x > preferredMaxX)
				{
					match += 10000f;
				}

				if(match <= bestMatch)
				{
					bestMatch = match;
					bestID = id;
					#if DEV_MODE
					if(DebugSelectNextControl) { Debug.Log("<color=green>BEST MATCH! id=" + id + ", rect=" + rect + ", match=" + match + ", (addID=" + addID + ", addY=" + addY + ", addX=" + addX + ", addHeight=" + addHeight + ", addWidth=" + addWidth + ", y=" + matchY + ", x=" + matchX + ", id=" + matchID + ")</color>"); }
					#endif
				}
				#if DEV_MODE
				else if(DebugSelectNextControl) { Debug.Log("(not best) id=" + id + ", rect=" + rect + ", match=" + match + ", (addID=" + addID + ", addY=" + addY + ", addX=" + addX + ", addHeight=" + addHeight + ", addWidth=" + addWidth + ", y=" + matchY + ", x=" + matchX + ", id=" + matchID + ")"); }
				#endif
			}

			KeyboardControlUtility.KeyboardControl = bestID;
			ScrollToShow();
		}

		private void HandleSelectNextFieldLeftLeavingBounds(bool moveToNextControlAfterReachingEnd, int previousKeyboardControl, Rect previousKeyboardRect)
		{
			HandleSelectNextFieldLeavingBounds(()=>
			{
				#if DEV_MODE && DEBUG_SKIP_OUT_OF_BOUNDS
				Debug.LogWarning(ToString()+ ".HandleSelectNextFieldLeftLeavingBounds - control out of body bounds! previousKeyboardControl=" + previousKeyboardControl + ",  keyboardControl=" + KeyboardControlUtility.KeyboardControl + ", keyboardRect=" + KeyboardControlUtility.Info.KeyboardRect + ", previousKeyboardRect=" + previousKeyboardRect + ", outOfBounds=" + IsKeyboardControlOutOfBodyBounds() + ", controlId=" + controlId + ", endControlId=" + EndControlId);
				#endif

				if(moveToNextControlAfterReachingEnd)
				{
					SelectHeaderPart(HeaderPart.ContextMenuIcon);
				}
				else
				{
					KeyboardControlUtility.KeyboardControl = previousKeyboardControl;
				}
			});
		}
		
		private bool IsLastFieldSelected()
		{
			return keyboardFocusController.IsLastFieldSelected(this);
		}

		private bool IsFirstFieldSelected()
		{
			return keyboardFocusController.IsFirstFieldSelected(this);
		}

		/// <inheritdoc />
		protected override void SelectNextFieldRight(bool moveToNextControlAfterReachingEnd, bool additive = false)
		{
			#if DEV_MODE && DEBUG_SELECT_NEXT_FIELD
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".SelectNextFieldRight(", moveToNextControlAfterReachingEnd, ") SelectedHeaderPart=", SelectedHeaderPart, ", HeaderIsSelected=", HeaderIsSelected, ", Expandable=", Expandable, ", Unfolded=", Unfolded, ", Unfoldedness=", Unfoldedness, ", Height=", Height, ", HeaderHeight=", HeaderHeight, ", OverrideFieldFocusing=", OverrideFieldFocusing()));
			#endif

			if(HeaderIsSelected || Unfoldedness <= 0f || Height <= HeaderHeight)
			{
				SelectNextHeaderPartRight(moveToNextControlAfterReachingEnd);
				ScrollToShow();
			}
			// new test
			else if(IsLastFieldSelected())
			{
				if(moveToNextControlAfterReachingEnd)
				{
					int index = Array.IndexOf(parent.VisibleMembers, this);
					if(index == parent.VisibleMembers.Length - 1)
					{
						var first = parent.VisibleMembers[0];
						if(first == this)
						{
							KeyboardControlUtility.KeyboardControl = 0;
							SelectNextHeaderPartRight(moveToNextControlAfterReachingEnd);
						}
						else
						{
							first.Select(ReasonSelectionChanged.SelectNextControl);
						}
					}
					else
					{
						parent.VisibleMembers[index + 1].Select(ReasonSelectionChanged.SelectNextControl);
					}
					/*
					base.SelectNextFieldRight(true);
					
					if(Selected)
					{
						SelectNextHeaderPartRight(moveToNextControlAfterReachingEnd);
					}

					//parent.GetNextSelectableDrawerRight(true, this);
					//SelectNextHeaderPartRight(moveToNextControlAfterReachingEnd);
					*/
					ScrollToShow();
				}
				else if(Event.current != null && Event.current.isMouse && Event.current.type != EventType.Used)
				{
					DrawGUI.Use(Event.current);
				}
			}
			//if CustomEditor custom selection logic is enabled
			//and this is an arrow right action, not tab action
			//select next field to the right
			//else if(OverrideFieldFocusing() && (!moveToNextControlAfterReachingEnd || IsLastFieldSelected()))
			else if(OverrideFieldFocusing() && !moveToNextControlAfterReachingEnd)
			{
				SelectNextEditorControlRight();
			}
			//rely on Unity's internal selection logic for switching between fields
			//the only thing we need to do is detect when the selection has moved out
			//of the bounds of the Editor and react accordingly (move to next component)
			else
			{
				//it can this many frames before Unitys internal systems changed the focused control
				inspector.OnNextLayout(() => inspector.OnNextLayout(() => inspector.OnNextLayout(() =>
				{
					int previousKeyboardControl = inspector.State.previousKeyboardControl;
					var previousKeyboardRect = inspector.State.previousKeyboardControlRect;

					var keyboardRect = KeyboardControlUtility.Info.KeyboardRect;

					// For some fields like enum fields in Odin Inspector, KeyboardRect can be (0,0,0,0).
					// In this case there's no way to detect if the field is out of bounds, and it is possible and valid that previousKeyboardRect equals keyboardRect (if there are two enum fields in a row).
					if(!keyboardRect.IsZero() && (IsKeyboardControlOutOfBodyBounds() || previousKeyboardRect == keyboardRect))
					{
						#if DEV_MODE && DEBUG_SELECT_NEXT_FIELD
						Debug.LogWarning(ToString() + ".SelectNextFieldRight - control out of body bounds! previousKeyboardControl=" + previousKeyboardControl + ",  keyboardControl=" + KeyboardControlUtility.KeyboardControl + ", keyboardRect=" + KeyboardControlUtility.Info.KeyboardRect + ", previousKeyboardRect=" + previousKeyboardRect + ", outOfBounds=" + IsKeyboardControlOutOfBodyBounds() + ", controlId=" + controlId + ", endControlId=" + EndControlId);
						#endif

						if(moveToNextControlAfterReachingEnd)
						{
							inspector.Select(GetNextSelectableDrawerRight(moveToNextControlAfterReachingEnd, this), ReasonSelectionChanged.SelectNextControl);
						}
						else
						{
							KeyboardControlUtility.KeyboardControl = previousKeyboardControl;
						}
					}
					#if DEV_MODE && DEBUG_SELECT_NEXT_FIELD
					else { Debug.Log(ToString() + ".SelectNextFieldRight - control within body bounds. previousKeyboardControl=" + previousKeyboardControl + ",  keyboardControl=" + KeyboardControlUtility.KeyboardControl + ", keyboardRect=" + KeyboardControlUtility.Info.KeyboardRect + ", previousKeyboardRect=" + previousKeyboardRect + ", outOfBounds=" + IsKeyboardControlOutOfBodyBounds() + ", controlId=" + controlId + ", endControlId=" + EndControlId); }
					#endif
				})));
			}
		}

		private void HandleSelectNextFieldLeavingBounds([NotNull]Action invokeIfLeftBounds, int repeatTimes = 3)
		{
			if(IsKeyboardControlOutOfBodyBounds())
			{
				#if DEV_MODE && DEBUG_SELECT_NEXT_FIELD
				Debug.LogWarning(ToString()+ ".HandleSelectNextFieldLeavingBounds - control out of body bounds!");
				#endif

				invokeIfLeftBounds();
			}
			//it can take multiple frames before Unitys internal systems changed the focused control
			else if(repeatTimes > 0)
			{
				repeatTimes--;
				OnNextLayout(() => HandleSelectNextFieldLeavingBounds(invokeIfLeftBounds, repeatTimes));
			}
			#if DEV_MODE && DEBUG_SELECT_NEXT_FIELD
			else { Debug.Log(ToString() + ".HandleSelectNextFieldLeavingBounds - control within body bounds."); }
			#endif
		}

		private void HandleSelectNextFieldRightLeavingBounds(bool moveToNextControlAfterReachingEnd, int previousKeyboardControl, Rect previousKeyboardRect)
		{
			HandleSelectNextFieldLeavingBounds(()=>
			{
				#if DEV_MODE && DEBUG_SELECT_NEXT_FIELD
				Debug.LogWarning(ToString()+ ".HandleSelectNextFieldRightLeavingBounds - control out of body bounds! previousKeyboardControl=" + previousKeyboardControl + ",  keyboardControl=" + KeyboardControlUtility.KeyboardControl + ", keyboardRect=" + KeyboardControlUtility.Info.KeyboardRect + ", previousKeyboardRect=" + previousKeyboardRect + ", outOfBounds=" + IsKeyboardControlOutOfBodyBounds() + ", controlId=" + controlId + ", endControlId=" + EndControlId);
				#endif

				if(moveToNextControlAfterReachingEnd)
				{
					SelectHeaderPart(HeaderPart.ContextMenuIcon);
				}
				else
				{
					KeyboardControlUtility.KeyboardControl = previousKeyboardControl;
				}
			});
		}

		/// <inheritdoc cref="IDrawer.SelectPreviousComponent" />
		public override void SelectPreviousComponent()
		{
			if(KeyboardControlUtility.KeyboardControl == 0)
			{
				base.SelectPreviousComponent();
			}
			else
			{
				KeyboardControlUtility.KeyboardControl = 0;
			}
		}

		/// <inheritdoc cref="IDrawer.AddPreviewWrappers" />
		public override void AddPreviewWrappers(ref List<IPreviewableWrapper> previews)
		{
			#if UNITY_EDITOR
			if(editor != null)
			{
				Previews.GetPreviews(editor, targets, ref previews);
			}
			#endif
		}

		/// <inheritdoc cref="IDrawer.Dispose" />
		public override void Dispose()
		{
			//all cached layout data should be set to zero size
			//so that if for whatever reason any MouseIsOver checks or anything
			//should happen for an item that is no longer active, it will return false
			unfoldedHeight = 0f;
			endControlId = 0;
			memberBuildListActuallyPopulated = false;
			EditorStyles.label.padding.right = 2;
			canHaveEditor = true;
			hideInInspector = false;
			Application.logMessageReceived -= OnOnInspectorGUILogMessage;

			#if UNITY_2019_1_OR_NEWER
			SceneView.duringSceneGui -= OnSceneGUI;
			#else
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			#endif
			onSceneGUI = null;

			#if UNITY_2019_1_OR_NEWER
			if(element != null)
			{
				inspector.InspectorDrawer.RemoveElement(element, this);
				element = null;
			}
			#endif

			if(!ReferenceEquals(editor, null))
			{
				if(isFirstInspectedEditor)
				{
					SetIsFirstInspectedEditor(false);
				}
				Editors.Dispose(ref editor);
			}
			else
            {
				isFirstInspectedEditor = false;
            }

			editorType = null;

			inspector.OnFilterChanging -= OnFilterChanging;

			base.Dispose();
		}


		/// <inheritdoc cref="IDrawer.OnMouseUpAfterDownOverControl" />
		public override void OnMouseUpAfterDownOverControl(Event inputEvent, bool isClick)
		{
			if(isClick && MouseoveredHeaderPart == HeaderPart.None && KeyboardControlUtility.KeyboardControl == 0 && !CanBeSelectedWithoutHeaderBeingSelected)
			{
				SelectHeaderPart(headerParts.Base);
			}
			base.OnMouseUpAfterDownOverControl(inputEvent, isClick);
		}
		
		private void GUILayoutEmpty()
		{
			GUILayout.Label(" ", GUILayout.Height(0f));
		}

		/// <inheritdoc/>
		protected override bool HasHideFlag(HideFlags hideFlag)
		{
			// new test: fix issue where assets with AssetImporters have
			// hide flag NotEditable true, which results things being
			// drawn greyed out
			var editorTargets = Editor.targets;
			for(int n = editorTargets.Length - 1; n >= 0; n--)
			{
				if(editorTargets[n].hideFlags.HasFlag(hideFlag))
				{
					return true;
				}
			}
			return false;
		}

		#if DEV_MODE
		/// <inheritdoc/>
		protected override object[] GetDevInfo()
		{
			return base.GetDevInfo().Add(", Editor=", Editor);
		}
		#endif
	}
}