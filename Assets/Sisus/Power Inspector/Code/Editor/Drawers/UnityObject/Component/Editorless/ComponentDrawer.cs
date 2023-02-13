#define UNFOLD_ON_SELECT_IN_SINGLE_ACTIVE_MODE

//#define DEBUG_ON_CLICK
//#define DEBUG_SET_UNFOLDED
//#define DEBUG_CUSTOM_ENABLED_FLAG
//#define DEBUG_ON_DESELECTED
///define DEBUG_ON_SELECTED

#define SAFE_MODE

using System;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Class for drawer that represent Components, being able to list all of their exposed fields, properties and methods.
	/// This class can be used as is, or extended for additional functionality.
	/// </summary>
	[Serializable]
	public class ComponentDrawer : UnityObjectDrawer<ComponentDrawer, Component>, IEditorlessComponentDrawer, IReorderable
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

		/// <summary>
		/// True if this Component has members and can be expanded
		/// </summary>
		private bool expandable;

		/// <inheritdoc/>
		public Vector2 MouseDownCursorTopLeftCornerOffset
		{
			get
			{
				return Vector2.zero;
			}
		}

		/// <inheritdoc />
		public override bool Foldable
		{
			get
			{
				return expandable;
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

		/// <inheritdoc />
		protected override bool IsAsset
		{
			get
			{
				var target = Target;
				return target != null && target.IsPrefab();
			}
		}

		/// <inheritdoc />
		public Component Component
		{
			get
			{
				return Target;
			}
		}

		/// <inheritdoc />
		public Component[] Components
		{
			get
			{
				return targets;
			}
		}

		/// <inheritdoc />
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
					return parent.UnityObject.GameObject();
				}

				return null;
			}
		}

		/// <inheritdoc/>
		protected sealed override float ToolbarIconsTopOffset
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

		/// <inheritdoc />
		protected sealed override float HeaderToolbarIconsOffset
		{
			get
			{
				return ComponentHeaderToolbarIconsOffset;
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

		/// <inheritdoc/>
		protected override Color PrefixBackgroundColor
		{
			get
			{
				return HeaderMouseovered ? Preferences.theme.ComponentMouseoveredHeaderBackground : Preferences.theme.ComponentHeaderBackground;
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
				if(HasEnabledFlag)
				{
					var firstTarget = Target;
					var behaviour = firstTarget as Behaviour;
					if(behaviour != null)
					{
						return behaviour.enabled;
					}

					// NOTE: RendererDrawer and ColliderDrawer both override the Enabled property so no need to check for them here.

					#if DEV_MODE
					Debug.LogError(Msg(ToString(), " HasEnabledFlag was ", true, " but "+ StringUtils.TypeToString(firstTarget) + " was not a Behaviour!"));
					#endif
				}
				return true;
			}

			set
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(HasEnabledFlag, "Assigned to "+this+".Enabled but HasEnabledFlag was "+StringUtils.False+".");
				#endif
				
				var behaviour = Target as Behaviour;
				if(behaviour != null)
				{
					behaviour.enabled = value;
					return;
				}
				
				var collider = Target as Collider;
				if(collider != null)
				{
					collider.enabled = value;
					return;
				}

				#if DEV_MODE
				Debug.LogError(Msg(ToString(), ".Enabled = ", value, " called, but Component was not Behaviour or Collider!"));
				#endif
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
		
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ComponentDrawer Create(Component[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			ComponentDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result =  new ComponentDrawer();
			}

			result.Setup(targets, parent, null, inspector);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		public virtual void SetupInterface(Component[] setTargets, IParentDrawer setParent, IInspector setInspector)
		{
			Setup(setTargets, setParent, null, setInspector);
		}

		/// <inheritdoc/>
		protected override void Setup(Component[] setTargets, IParentDrawer setParent, GUIContent setLabel, IInspector setInspector)
		{
			var firstTarget = setTargets.Length > 0 ? setTargets[0] : null;

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

			base.Setup(setTargets, setParent, setLabel, setInspector);
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
		public void OnBeingReordered(float yOffset)
		{
			var rect = SelectionRect;
			rect.y += yOffset;

			if(Manager.MouseDownInfo.CursorMovedAfterMouseDown)
			{
				DrawGUI.DrawEdgeSelectionIndicator(SelectionRect);
			}
		}

		/// <inheritdoc cref="IDrawer.OnMouseUpAfterDownOverControl" />
		public override void OnMouseUpAfterDownOverControl(Event inputEvent, bool isClick)
		{
			#if DEV_MODE && DEBUG_ON_CLICK
			Debug.Log(StringUtils.ToColorizedString(GetType(), ".OnMouseUpAfterDownOverControl(isClick=", isClick,") with mouseOverHeaderPart=", MouseoveredHeaderPart, ", HeaderMouseovered=", HeaderMouseovered, ", Selectable=", Selectable, ", event=", StringUtils.ToString(inputEvent), ", mousePos=", inputEvent.mousePosition));
			#endif

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

		/// <inheritdoc cref="IDrawer.SelectPreviousOfType" />
		public override void SelectPreviousOfType()
		{
			ComponentDrawerUtility.SelectPreviousOfType(this);
		}

		/// <inheritdoc cref="IDrawer.SelectNextOfType" />
		public override void SelectNextOfType()
		{
			ComponentDrawerUtility.SelectNextOfType(this);
		}

		/// <inheritdoc/>
		protected override Object[] FindObjectsOfType()
		{
			#if UNITY_2023_1_OR_NEWER
			return Object.FindObjectsByType(Type, FindObjectsInactive.Include, FindObjectsSortMode.None);
			#else
			return Object.FindObjectsOfType(Type);
			#endif
		}

		/// <inheritdoc/>
		protected override void NameByType()
		{
			ComponentDrawerUtility.NameByType(this);
		}

		/// <inheritdoc/>
		protected override void FindReferencesInScene()
		{
			DrawGUI.ExecuteMenuItem("CONTEXT/Component/Find References In Scene", ArrayPool<Component>.Cast<Object>(targets));
		}

		/// <inheritdoc/>
		protected override void OnSelectedInternal(ReasonSelectionChanged reason, IDrawer previous, bool isMultiSelection)
		{
			#if DEV_MODE && DEBUG_ON_SELECTED
			Debug.Log(ToString()+".OnSelectedInternal("+ reason + ","+ StringUtils.ToString(previous)+")");
			#endif

			bool wasUnfolded = false;

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
				case ReasonSelectionChanged.ThisClicked:
					isClick = true;
					break;
			}

			#if UNFOLD_ON_SELECT_IN_SINGLE_ACTIVE_MODE
			if(UserSettings.EditComponentsOneAtATime && !inspector.HasFilterAffectingInspectedTargetContent)
			{
				if(ComponentDrawerUtility.singleInspectedInstance != null && ComponentDrawerUtility.singleInspectedInstance != this)
				{
					ComponentDrawerUtility.singleInspectedInstance.SetUnfolded(false);
				}
				ComponentDrawerUtility.singleInspectedInstance = this;

				// if Component was clicked, let the click event handle the unfolding
				if(!Unfolded && (!isClick || (MouseoveredHeaderPart != HeaderPart.FoldoutArrow && !inspector.Preferences.changeFoldedStateOnFirstClick)))
				{
					wasUnfolded = true;
					// allow bypassing EditComponentsOneAtATime functionality by holding down control when clicking the Component header
					bool collapseAllOthers = !Event.current.control || reason != ReasonSelectionChanged.PrefixClicked;
					SetUnfolded(true, collapseAllOthers, false);
				}
			}
			#endif

			base.OnSelectedInternal(reason, previous, isMultiSelection);

			if(wasUnfolded)
			{
				ExitGUIUtility.ExitGUI();
			}
		}

		/// <inheritdoc/>
		protected override void OnDeselectedInternal(ReasonSelectionChanged reason, IDrawer losingFocusTo)
		{
			#if DEV_MODE && DEBUG_ON_DESELECTED
			Debug.Log(ToString()+ ".OnDeselectedInternal(" + reason + ","+ StringUtils.ToString(losingFocusTo) +")");
			#endif

			bool unfoldIfLostFocus = UserSettings.EditComponentsOneAtATime;
			switch(reason)
			{
				case ReasonSelectionChanged.PrefixClicked:
				case ReasonSelectionChanged.ControlClicked:
					if(DrawGUI.LastInputEvent().control)
					{
						unfoldIfLostFocus = false;
					}
					break;
			}

			if(unfoldIfLostFocus)
			{
				var test = losingFocusTo;
				while(test != null)
				{
					var component = test as IComponentDrawer;
					if(component != null)
					{
						if(component != this)
						{
							SetUnfolded(false, false);
						}
						break;
					}
					test = test.Parent;
				}
			}

			base.OnDeselectedInternal(reason, losingFocusTo);
		}

		/// <inheritdoc cref="IParentDrawer.SetUnfolded(bool, bool)" />
		public override void SetUnfolded(bool setUnfolded, bool setChildrenAlso)
		{
			bool collapseAllOthers = Event.current == null ? false : (Event.current.control && !UserSettings.EditComponentsOneAtATime) || (!Event.current.control && setUnfolded && UserSettings.EditComponentsOneAtATime);
			SetUnfolded(setUnfolded, collapseAllOthers, setChildrenAlso);
		}

		/// <inheritdoc cref="IComponentDrawer.SetUnfolded(bool, bool, bool)" />
		public override void SetUnfolded(bool setUnfolded, bool collapseAllOthers, bool setChildrenAlso)
		{
			if((Unfolded != setUnfolded || setChildrenAlso) && (!UnityObjectDrawerUtility.HeadlessMode || setUnfolded))
			{
				#if DEV_MODE && DEBUG_SET_UNFOLDED
				Debug.Log(StringUtils.ToColorizedString(ToString(), " - SetUnfolded(setUnfolded=", setUnfolded, ", collapseAllOthers=", collapseAllOthers, ", setChildrenAlso=", setChildrenAlso, ")"), Target);
				#endif

				base.SetUnfolded(setUnfolded, collapseAllOthers, setChildrenAlso);

				if(UserSettings.EditComponentsOneAtATime)
				{
					if(setUnfolded)
					{
						if(ComponentDrawerUtility.singleInspectedInstance != this && ComponentDrawerUtility.singleInspectedInstance != null)
						{
							ComponentDrawerUtility.singleInspectedInstance.SetUnfolded(false);
						}
						ComponentDrawerUtility.singleInspectedInstance = this;
					}
					else if(ComponentDrawerUtility.singleInspectedInstance == this)
					{
						ComponentDrawerUtility.singleInspectedInstance = null;
					}
				}
			}
		}

		/// <inheritdoc/>
		protected override void OnAfterMemberBuildListGenerated()
		{
			expandable = memberBuildList.Count > 0 || (Inspector.Preferences.drawScriptReferenceFields && Target is MonoBehaviour);
			base.OnAfterMemberBuildListGenerated();
		}

		/// <inheritdoc/>
		public override void OnAfterMembersBuilt()
		{
			expandable = members.Length > 0;
			base.OnAfterMembersBuilt();
		}

		/// <inheritdoc cref="IDrawer.OnMouseoverDuringDrag" />
		public override void OnMouseoverDuringDrag(MouseDownInfo mouseDownInfo, Object[] dragAndDropObjectReferences)
		{
			if(mouseDownInfo.MouseDownOverDrawer == this || mouseDownInfo.MouseDownOverDrawer == null || mouseDownInfo.Reordering.MouseoveredDropTarget.MemberIndex == -1)
			{
				DrawGUI.Active.DragAndDropVisualMode = DragAndDropVisualMode.Rejected;
			}
		}
		
		/// <inheritdoc cref="IDrawer.Duplicate" />
		public override void Duplicate()
		{
			ComponentDrawerUtility.Duplicate(targets);
		}

		/// <inheritdoc cref="IDrawer.Dispose" />
		public override void Dispose()
		{
			expandable = true; // imporant to set this to true as a default value so that Components return true for Foldable during the Setup phase
			hasEnabledFlag = false;
			createCustomEnabledFlag = false;

			if(ComponentDrawerUtility.singleInspectedInstance == this)
			{
				ComponentDrawerUtility.singleInspectedInstance = null;
			}

			base.Dispose();
		}

		/// <inheritdoc cref="IComponentDrawer.AddItemsToOpeningViewMenu(ref Menu)" />
		public override void AddItemsToOpeningViewMenu(ref Menu menu)
		{
			
		}

		/// <inheritdoc/>
		protected override bool IsAssetOpenForEdit()
		{
			return true;
		}
	}
}