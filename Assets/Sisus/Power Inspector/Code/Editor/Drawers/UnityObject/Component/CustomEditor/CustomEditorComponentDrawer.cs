#define UNFOLD_ON_SELECT_IN_SINGLE_ACTIVE_MODE

using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable]
	public class CustomEditorComponentDrawer : CustomEditorBaseDrawer<CustomEditorComponentDrawer, Component>, ICustomEditorComponentDrawer, IReorderable
	{
		/// <summary>
		/// True if Component has an enabled flag, false if not.
		/// </summary>
		private bool hasEnabledFlag;

		/// <summary>
		/// True if should draw custom enabled flag (Unity doesn't draw one unless certain
		/// methods are found in the code, which is not always desired behaviour).
		/// </summary>
		private bool createCustomEnabledFlag;

		private bool hasExecuteMethodItems;
		private bool expandable;

		public Vector2 MouseDownCursorTopLeftCornerOffset
		{
			get
			{
				return Vector2.zero;
			}
		}

		/// <inheritdoc cref="IDrawer.Height" />
		public override float Height
		{
			get
			{
				return base.Height + PrefixResizerDragHandleHeight;
			}
		}

		public Component Component
		{
			get
			{
				return Target;
			}
		}

		public Component[] Components
		{
			get
			{
				return targets;
			}
		}

		/// <inheritdoc cref="IParentDrawer.Foldable" />
		public override bool Foldable
		{
			get
			{
				return expandable;
			}
		}

		/// <inheritdoc/>
		protected override MonoScript MonoScript
		{
			get
			{
				var target = Target as MonoBehaviour;
				return target == null ? null : MonoScript.FromMonoBehaviour(target);
			}
		}

		/// <inheritdoc/>
		protected override bool Enabled
		{
			get
			{
				return !HasEnabledFlag || targets.AllEnabled();
			}

			set
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(HasEnabledFlag, this+".Enabled = "+StringUtils.ToColorizedString(value)+" was called, but HasEnabledFlag was "+StringUtils.False+".");
				#endif
				
				targets.SetEnabled(value);
			}
		}

		/// <inheritdoc/>
		protected override float ToolbarIconsTopOffset
		{
			get
			{
				return ComponentToolbarIconsTopOffset;
			}
		}

		/// <inheritdoc/>
		protected sealed override float HeaderToolbarIconWidth
		{
			get
			{
				return ComponentHeaderToolbarIconWidth;
			}
		}

		/// <inheritdoc/>
		protected sealed override float HeaderToolbarIconHeight
		{
			get
			{
				return ComponentHeaderToolbarIconHeight;
			}
		}

		/// <inheritdoc />
		protected sealed override float HeaderToolbarIconsRightOffset
		{
			get
			{
				return ComponentHeaderToolbarIconsRightOffset;
			}
		}

		/// <inheritdoc />
		protected sealed override float HeaderToolbarIconsOffset
		{
			get
			{
				return ComponentHeaderToolbarIconsOffset;
			}
		}

		/// <inheritdoc/>
		protected sealed override Color PrefixBackgroundColor
		{
			get
			{
				return HeaderMouseovered ? Preferences.theme.ComponentMouseoveredHeaderBackground : Preferences.theme.ComponentHeaderBackground;
			}
		}

		/// <inheritdoc />
		protected override bool IsAsset
		{
			get
			{
				return Target.IsPrefab();
			}
		}

		/// <inheritdoc/>
		protected override bool HasEnabledFlag
		{
			get
			{
				return hasEnabledFlag;
			}
		}

		/// <inheritdoc/>
		protected override bool IsComponent
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc/>
		protected override bool HasExecuteMethodIcon
		{
			get
			{
				return hasExecuteMethodItems;
			}
		}

		/// <inheritdoc/>
		public GameObject gameObject
		{
			get
			{
				var target = Target;
				if(target != null)
				{
					return target.gameObject;
				}

				if(parent != null)
				{
					return parent.UnityObject as GameObject;
				}

				return null;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawers represent. </param>
		/// <param name="parent"> The parent drawers of the created drawers. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <param name="editorType"> The type of for the custom editor. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static CustomEditorComponentDrawer Create(Component[] targets, IParentDrawer parent, [NotNull]IInspector inspector, Type editorType = null)
		{
			CustomEditorComponentDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new CustomEditorComponentDrawer();
			}
			result.Setup(targets, parent, inspector, editorType);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc cref="ICustomEditorComponentDrawer.SetupInterface" />
		public virtual void SetupInterface(Type setEditorType, Component[] setTargets, IParentDrawer setParent, IInspector setInspector)
		{
			Setup(setTargets, setParent, setInspector, setEditorType);
		}

		/// <inheritdoc />
		protected override void Setup(Component[] setTargets, IParentDrawer setParent, IInspector setInspector, Type setEditorType)
		{
			inspector = setInspector;
			hasExecuteMethodItems = base.HasExecuteMethodIcon && HasExecuteMethodMenuItems();
			var firstTarget = setTargets[0];
			if(!HeadlessMode && firstTarget != null && firstTarget.HasEnabledProperty())
			{
				hasEnabledFlag = true;
				var monoBehaviour = firstTarget as MonoBehaviour;
				if(monoBehaviour != null)
				{
					//Unity doesn't show the enabled flag in the inspector unless
					//the Behaviour contains certain methods. However, it can be
					//useful to have it be there anyways for various reasons.
					createCustomEnabledFlag = !monoBehaviour.HasEnabledFlagInEditor();
					#if DEV_MODE && DEBUG_CUSTOM_ENABLED_FLAG
					if(createCustomEnabledFlag) { Debug.Log(ToString()+" - Creating custom enabled flag for "+firstTarget.GetType()); }
					#endif
				}
			}

			base.Setup(setTargets, setParent, setInspector, setEditorType);

			#if DEV_MODE && DEBUG_ENABLED_FLAG
			Debug.Log(ToString()+" hasEnabledFlag: "+hasEnabledFlag);
			#endif
		}

		/// <inheritdoc/>
		protected override void OnAfterMemberBuildListGenerated()
		{
			UpdateIsExpandable();
			base.OnAfterMemberBuildListGenerated();
		}

		private void UpdateIsExpandable()
		{
			bool was = expandable;

			#if DEV_MODE
			Debug.Assert(memberBuildState != MemberBuildState.Unstarted);
			#endif
			
			if(DebugMode && (memberBuildState == MemberBuildState.Unstarted || memberBuildList.Count > 0 || members.Length > 0))
			{
				expandable = true;
			}
			else
			{
				if(!canHaveEditor)
				{
					#if DEV_MODE
					Debug.Assert(Editor == null);
					#endif

					expandable = false;
				}
				else
				{
					var editor = Editor;
					if(editor == null)
                    {
						#if DEV_MODE
						Debug.LogError(ToString()+ ".UpdateIsExpandable called but Editor returned null.");
						#endif
						expandable = false;
						return;
                    }

					expandable = editor.CanBeExpandedViaAFoldout(); // && editor.HasVisibleProperties();
				}
			}

			if(expandable != was && headerParts.Count > 0)
			{
				RebuildHeaderToolbar();
			}
		}

		/// <inheritdoc />
		protected override void NameByType()
		{
			ComponentDrawerUtility.NameByType(this);
		}

		/// <inheritdoc/>
		protected override void FindReferencesInScene()
		{
			DrawGUI.ExecuteMenuItem("CONTEXT/Component/Find References In Scene");
		}

		/// <inheritdoc />
		public void OnBeingReordered(float yOffset)
		{
			var rect = SelectionRect;
			rect.y += yOffset;
			DrawGUI.DrawMouseoverEffect(rect, localDrawAreaOffset);
		}

		/// <inheritdoc />
		protected override void OnSelectedInternal(ReasonSelectionChanged reason, IDrawer previous, bool isMultiSelection)
		{
			bool isClick = false;
			switch(reason)
			{
				case ReasonSelectionChanged.PrefixClicked:
					isClick = true;
					if(DrawGUI.LastInputEvent().control)
					{
						ComponentDrawerUtility.singleInspectedInstance = this;
					}
					break;
				case ReasonSelectionChanged.ControlClicked:
					if(DrawGUI.LastInputEvent().control)
					{
						ComponentDrawerUtility.singleInspectedInstance = this;
					}
					return;
			}

			#if UNFOLD_ON_SELECT_IN_SINGLE_ACTIVE_MODE
			if(UserSettings.EditComponentsOneAtATime && !InspectorUtility.ActiveInspector.HasFilterAffectingInspectedTargetContent)
			{
				if(ComponentDrawerUtility.singleInspectedInstance != null && ComponentDrawerUtility.singleInspectedInstance != this)
				{
					ComponentDrawerUtility.singleInspectedInstance.SetUnfolded(false);
				}
				ComponentDrawerUtility.singleInspectedInstance = this;

				// if Component was clicked, let the click event handle the unfolding
				if(!Unfolded && (!isClick || (MouseoveredHeaderPart != HeaderPart.FoldoutArrow && !inspector.Preferences.changeFoldedStateOnFirstClick)))
				{
					SetUnfolded(true, Event.current.alt);
				}
			}
			#endif

			base.OnSelectedInternal(reason, previous, isMultiSelection);
		}

		/// <inheritdoc />
		protected override Object[] FindObjectsOfType()
		{
			#if UNITY_2023_1_OR_NEWER
			return Object.FindObjectsByType(Type, FindObjectsInactive.Include, FindObjectsSortMode.None);
			#else
			return Object.FindObjectsOfType(Type);
			#endif
		}
		
		/// <summary>
		/// called when the left mouse button is released 
		/// if it was pressed down while being over this
		/// </summary>
		public override void OnMouseUpAfterDownOverControl(Event inputEvent, bool isClick)
		{
			if(isClick && HasEnabledFlag && MouseoveredHeaderPart == HeaderPart.EnabledFlag)
			{
				SelectHeaderPart(MouseoveredHeaderPart);

				if(!Selected)
				{
					Select(HeaderMouseovered ? ReasonSelectionChanged.PrefixClicked : ReasonSelectionChanged.ControlClicked);
				}
				
				if(createCustomEnabledFlag)
				{
					ComponentDrawerUtility.OnCustomEnabledControlClicked(this, inputEvent);

					OnValidateHandler.CallForTargets(UnityObjects);

					if(OnValueChanged != null)
					{
						OnValueChanged(this, Target);
					}

					if(parent != null)
					{
						parent.OnMemberValueChanged(Array.IndexOf(parent.Members, this), Target, null);
					}
				}
				else
				{
					OnNextLayout(()=>
					{
						OnValidateHandler.CallForTargets(UnityObjects);

						if(OnValueChanged != null)
						{
							OnValueChanged(this, Target);
						}

						if(parent != null)
						{
							parent.OnMemberValueChanged(Array.IndexOf(parent.Members, this), Target, null);
						}
					});
				}

				//if using built in enabled flag
				//don't consume click event so that it will cause
				//the enabled state of the component to change
				return;
			}
			
			base.OnMouseUpAfterDownOverControl(inputEvent, isClick);
		}

		/// <inheritdoc cref="IDrawer.OnMouseoverDuringDrag" />
		public override void OnMouseoverDuringDrag(MouseDownInfo mouseDownInfo, Object[] dragAndDropObjectReferences)
		{
			if(mouseDownInfo.MouseDownOverDrawer == this || mouseDownInfo.MouseDownOverDrawer == null || mouseDownInfo.Reordering.MouseoveredDropTarget.MemberIndex == -1)
			{
				// Don't reject drag n drop if cursor is over editor body, because it could accept drag n drops (e.g. if it contains Object reference fields).
				if(HeaderMouseovered)
				{
					DrawGUI.Active.DragAndDropVisualMode = DragAndDropVisualMode.Rejected;
				}
			}
		}

		/// <inheritdoc/>
		protected override void OnDebugModeChanged(bool nowEnabled)
		{
			base.OnDebugModeChanged(nowEnabled);

			bool setHasExecuteMethodItems = base.HasExecuteMethodIcon && HasExecuteMethodMenuItems();
			if(setHasExecuteMethodItems != hasExecuteMethodItems)
			{
				hasExecuteMethodItems = setHasExecuteMethodItems;
				RebuildHeaderToolbar();
			}
		}

		/// <inheritdoc cref="IDrawer.SelectPreviousComponent" />
		public override void SelectPreviousComponent()
		{
			ComponentDrawerUtility.SelectPreviousVisibleComponent(this);
		}

		/// <inheritdoc cref="IDrawer.SelectNextComponent" />
		public override void SelectNextComponent()
		{
			ComponentDrawerUtility.SelectNextVisibleComponent(this);
		}

		private void GetFocusedControlAndLocalRect(out int focusedControl, out Rect focusedControlLocalRect)
		{
			focusedControl = KeyboardControlUtility.KeyboardControl;
			if(focusedControl <= 0)
			{
				focusedControlLocalRect = default(Rect);
			}
			else
			{
				focusedControlLocalRect = KeyboardControlUtility.Info.KeyboardRect;
				focusedControlLocalRect.x -= lastDrawPosition.x;
				focusedControlLocalRect.y -= lastDrawPosition.y;
			}
		}

		/// <inheritdoc cref="IDrawer.SelectPreviousOfType" />
		public override void SelectPreviousOfType()
		{
			// Get rect of focused control to try and preserve focused control.
			int focusedControlWas;
			Rect focusedControlLocalRect;
			GetFocusedControlAndLocalRect(out focusedControlWas, out focusedControlLocalRect);
			
			Component component;
			if(!HierarchyUtility.TryGetPreviousOfType(Component, out component))
			{
				if(component == null)
				{
					inspector.Message("No instances to select found in scene.");
				}
				else
				{
					inspector.Message("No additional instances found in scene.");
				}
				return;
			}
			
			SelectShowAndFocusControl(component, focusedControlWas, focusedControlLocalRect);
		}

		/// <inheritdoc cref="IDrawer.SelectNextOfType" />
		public override void SelectNextOfType()
		{
			// Get rect of focused control to try and preserve focused control.
			int focusedControlWas;
			Rect focusedControlLocalRect;
			GetFocusedControlAndLocalRect(out focusedControlWas, out focusedControlLocalRect);
			
			Component component;
			if(!HierarchyUtility.TryGetNextOfType(Component, out component))
			{
				if(component == null)
				{
					inspector.Message("No instances to select found in scene.");
				}
				else
				{
					inspector.Message("No additional instances found in scene.");
				}
				return;
			}
			
			SelectShowAndFocusControl(component, focusedControlWas, focusedControlLocalRect);
		}

		private void SelectShowAndFocusControl(Component component, int focusedControlWas, Rect focusedControlAtRect)
		{
			if(component.gameObject != gameObject)
			{
				inspector.OnNextInspectedChanged(()=>SelectShowAndFocusControlInInspectedComponent(component, focusedControlWas, focusedControlAtRect));
				inspector.Select(component);
			}
			else
			{
				SelectShowAndFocusControlInInspectedComponent(component, focusedControlWas, focusedControlAtRect);
			}
		}

		private void SelectShowAndFocusControlInInspectedComponent(Component component, int focusedControlWas, Rect focusedControlLocalRect)
		{
			var selectDrawer = inspector.State.drawers.FindDrawer(component) as CustomEditorComponentDrawer;
			if(selectDrawer != null)
			{
				selectDrawer.Select(ReasonSelectionChanged.SelectPrevOfType);
				inspector.ScrollToShow(selectDrawer);

				if(focusedControlWas > 0)
				{
					selectDrawer.OnNextLayout(()=>
					{
						int selectControl = selectDrawer.KeyboardFocusController.GetEditorControlAtRect(selectDrawer, focusedControlLocalRect, selectDrawer.beforeHeaderControlId, selectDrawer.EndControlId);
						if(selectControl > 0)
						{
							#if DEV_MODE
							Debug.LogWarning("Restoring focused control @ "+focusedControlLocalRect+": "+selectControl, component);
							#endif
							KeyboardControlUtility.KeyboardControl = selectControl;
						}
						#if DEV_MODE
						else { Debug.LogWarning("HandleRestoreFocusedControlForComponentShownInInspector - failed to restore focused control @ "+focusedControlLocalRect, component); }
						#endif
					});
				}
			}
			#if DEV_MODE
			else { Debug.LogWarning("HandleRestoreFocusedControl called for Component "+component.GetType().Name+" on GameObject \""+component.gameObject.name+"\" but drawers not found among inspected: "+StringUtils.ToString(inspector.State.drawers.VisibleMembers), component); }
			#endif
		}

		/// <inheritdoc cref="IDrawer.Duplicate" />
		public override void Duplicate()
		{
			ComponentDrawerUtility.Duplicate(targets);
		}

		/// <inheritdoc cref="IComponentDrawer.AddItemsToOpeningViewMenu(ref Menu)" />
		public override void AddItemsToOpeningViewMenu(ref Menu menu)
		{
			
		}

		/// <inheritdoc/>
		protected override void DrawHeaderParts()
		{
			if(createCustomEnabledFlag)
			{
				var behaviour = Target as Behaviour;
				if(behaviour != null)
				{
					ComponentDrawerUtility.DrawCustomEnabledField(this, EnabledFlagPosition);
				}
			}
			base.DrawHeaderParts();
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			base.Dispose();

			createCustomEnabledFlag = false;
			hasEnabledFlag = false;
		}

		/// <inheritdoc/>
		protected override bool IsAssetOpenForEdit()
		{
			return true;
		}
	}
}