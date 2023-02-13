//#define USE_TRANSFORM_HAS_CHANGED
//#define SET_TRANSFORM_HAS_CHANGED_TO_FALSE

using System;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Handles drawing the Transform component inside the inspector view.
	/// </summary>
	[Serializable, DrawerForComponent(typeof(Transform), false, true)]
	public class TransformDrawer : ComponentDrawer, ITransformDrawer
	{
		private static bool fieldInfosGenerated;

		private static PropertyInfo propertyInfoPositionLocalSpace;
		private static PropertyInfo propertyInfoRotationLocalSpace;
		private static PropertyInfo propertyInfoScaleLocalSpace;
		private static PropertyInfo propertyInfoPositionWorldSpace;
		private static PropertyInfo propertyInfoRotationWorldSpace;
		private static PropertyInfo propertyInfoScaleWorldSpace;

		#if USE_TRANSFORM_HAS_CHANGED
		protected override bool ShouldConstantlyUpdateCachedValues()
		{
			if(Target.hasChanged)Debug.Log("\""+Target.name+"\" hasChanged was true");
			return targets.Length > 1 || Target.hasChanged;
		}
		#endif

		private float height;
		
		/// <inheritdoc/>
		protected override float PrefixResizerMaxHeight
		{
			get
			{
				// This is needed so that if the floating point precision warning box is shown
				// the resize control won't be drawn behind it in an ugly manner.
				return DrawGUI.SingleLineHeight * 3f - 10f;
			}
		}

		protected bool UsingLocalSpace
		{
			get
			{
				return Inspector.State.usingLocalSpace;
			}

			set
			{
				Inspector.State.usingLocalSpace = value;
			}
		}

		private static bool UsingSnapping
		{
			get
			{
				return UserSettings.Snapping.Enabled;
			}
		}
		
		/// <inheritdoc />
		protected override bool HasExecuteMethodIcon
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc />
		public override float Height
		{
			get
			{
				return height;
			}
		}

		/// <inheritdoc />
		public override bool Unfolded
		{
			set
			{
				base.Unfolded = value;

				// UpdateHeight is only called when UpdateMemberVisibility is called
				// once the folding has completely finished, so we need to call it
				// to start it updating here at the beginning of the unfolding animation too
				if(!value && MembersAreVisible)
				{
					UpdateHeight();
				}
			}
		}

		#if USE_TRANSFORM_HAS_CHANGED && SET_TRANSFORM_HAS_CHANGED_TO_FALSE
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			base.UpdateCachedValuesFromFieldsRecursively();
			Target.hasChanged = false;
		}
		#endif

		/// <inheritdoc/>
		public override Transform[] Transforms
		{
			get
			{
				return ArrayPool<Component>.Cast<Transform>(targets);
			}
		}

		/// <inheritdoc/>
		public override string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetDrawerInfoUrl("transform-drawer");
			}
		}

		/// <inheritdoc/>
		protected override string OverrideDocumentationUrl(out string documentationTitle)
		{
			documentationTitle = "Transform Drawer";
			return "https://docs.sisus.co/power-inspector/enhanced-gui-drawers/transform-drawer/";
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawers represent. </param>
		/// <param name="parent"> The parent drawers of the created drawers. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public new static TransformDrawer Create([NotNull]Component[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inspector != null, "Transform.Create inspector was null for targets "+StringUtils.ToString(targets));
			#endif

			TransformDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new TransformDrawer();
			}
			result.Setup(targets, parent, null, inspector);
			result.LateSetup();
			return result;
		}
		
		/// <summary>
		/// Generates PropertyInfos for members.
		/// </summary>
		private static void GenerateMemberInfos()
		{
			if(!fieldInfosGenerated)
			{
				fieldInfosGenerated = true;
				propertyInfoPositionLocalSpace = Types.Transform.GetProperty("localPosition", BindingFlags.Instance | BindingFlags.Public);
				propertyInfoRotationLocalSpace = Types.Transform.GetProperty("localEulerAngles", BindingFlags.Instance | BindingFlags.Public);
				propertyInfoScaleLocalSpace = Types.Transform.GetProperty("localScale", BindingFlags.Instance | BindingFlags.Public);
				propertyInfoPositionWorldSpace = Types.Transform.GetProperty("position", BindingFlags.Instance | BindingFlags.Public);
				propertyInfoRotationWorldSpace = Types.Transform.GetProperty("eulerAngles", BindingFlags.Instance | BindingFlags.Public);
				propertyInfoScaleWorldSpace = Types.Transform.GetProperty("lossyScale", BindingFlags.Instance | BindingFlags.Public);
			}
		}

		/// <inheritdoc/>
		public override void LateSetup()
		{
			#if USE_TRANSFORM_HAS_CHANGED && SET_TRANSFORM_HAS_CHANGED_TO_FALSE
			Target.hasChanged = false;
			#endif

			base.LateSetup();
			UpdateHeight();
			OptimizePrefixLabelWidth();
		}

		private void UpdateHeight()
		{
			// base.Height already handles multiplying by Unfoldedness
			height = base.Height;
			
			if(MembersAreVisible && (!UsingLocalSpace || UsingSnapping))
			{
				height += 23f * Unfoldedness;
			}

			// warning box is always visible even if Unfolded
			// and is not affected by Unfoldedness scaling
			if(!DataIsValid)
			{
				height += 58f;
			}

			if(NowTweeningUnfoldedness)
			{
				OnNextLayout(UpdateHeight);
			}
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			//add debug mode fields additively, or instead of the default fields?
			if(DebugMode)
			{
				base.DoGenerateMemberBuildList();
				return;
			}

			GenerateMemberInfos();

			if(UsingLocalSpace)
			{
				memberBuildList.Add(linkedMemberHierarchy.Get(null, propertyInfoPositionLocalSpace, LinkedMemberParent.UnityObject, "m_LocalPosition"));
				memberBuildList.Add(linkedMemberHierarchy.Get(null, propertyInfoRotationLocalSpace, LinkedMemberParent.UnityObject, "m_LocalRotation"));
				memberBuildList.Add(linkedMemberHierarchy.Get(null, propertyInfoScaleLocalSpace, LinkedMemberParent.UnityObject, "m_LocalScale"));
			}
			else
			{
				memberBuildList.Add(linkedMemberHierarchy.Get(null, propertyInfoPositionWorldSpace, LinkedMemberParent.UnityObject));
				memberBuildList.Add(linkedMemberHierarchy.Get(null, propertyInfoRotationWorldSpace, LinkedMemberParent.UnityObject));
				
				// Don't add PropertyInfo for lossy scale field, because then it would become read-only, as the property is read.only.
				memberBuildList.Add(linkedMemberHierarchy.Get(null, propertyInfoScaleWorldSpace, LinkedMemberParent.UnityObject));
			}
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			if(DebugMode)
			{
				base.DoBuildMembers();
				return;
			}

			DrawerArrayPool.Resize(ref members, 3);
			
			bool usingLocalSpace = UsingLocalSpace;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberBuildList.Count == (usingLocalSpace ? 3 : 2));
			#endif

			var labels = Preferences.labels;
			var firstTransform = (Transform)Target;
			members[0] = PositionDrawer.Create(usingLocalSpace ? firstTransform.localPosition : firstTransform.position, memberBuildList[0], this, labels.Position, ReadOnly);
			members[1] = RotationDrawer.Create(usingLocalSpace ? firstTransform.localEulerAngles : firstTransform.eulerAngles, memberBuildList[1], this, labels.Rotation, ReadOnly);
			var scaleMember = ScaleDrawer.Create(usingLocalSpace ? firstTransform.localScale : firstTransform.lossyScale, memberBuildList[2], this, labels.Scale, ReadOnly);
			members[2] = scaleMember;
		}
		
		/// <inheritdoc />
		public override bool Draw(Rect position)
		{
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				if(targets[n] == null)
				{
					#if DEV_MODE
					Debug.LogWarning(this + ".Draw() - target was null, rebuilding");
					#endif
					inspector.RebuildDrawersIfTargetsChanged();
					return false;
				}
			}

			if(DebugMode)
			{
				if(Event.current.type == EventType.Layout)
				{
					UpdateHeight();
				}
				return base.Draw(position);
			}
			
			bool dirty = false;

			if(Event.current.type == EventType.Layout)
			{
				OnLayoutEvent(position);
			}
			else if(lastDrawPosition.height <= 0f)
			{
				GetDrawPositions(position);
			}
			
			var guiColorWas = GUI.color;
			if(DrawGreyedOut)
			{
				var color = GUI.color;
				color.a = 0.5f;
				GUI.color = color;
			}

			if(!HeadlessMode && DrawPrefix(labelLastDrawPosition))
			{
				dirty = true;

				if(DebugMode)
				{
					ExitGUIUtility.ExitGUI();
				}
			}
			
			float unfoldedness = Unfoldedness;

			if(unfoldedness > 0f)
			{
				var pos = position;
				pos.y += HeaderHeight;
				pos.height = Height - HeaderHeight;

				HandlePrefixColumnResizing();

				pos.height = DrawGUI.SingleLineHeight;

				if(unfoldedness >= 1f)
				{
					DrawFoldableContent(pos);
				}
				else
				{
					#if DEV_MODE && PI_ASSERTATIONS
					var assertColor = GUI.color;
					#endif

					using(new MemberScaler(bodyLastDrawPosition.min, unfoldedness))
					{
						DrawFoldableContent(pos);
					}

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(GUI.color == assertColor, ToString() + " - After DrawFoldableContent");
					#endif
				}
			}

			GUI.color = guiColorWas;

			if(!DataIsValid)
			{
				var pos = position;
				pos.y += Height - 57f;
				pos.height = 50f;
				DrawGUI.Active.HelpBox(pos, "Due to floating-point precision limitations, it is recommended to bring the world coordinates of the GameObject within a smaller range.", MessageType.Warning);
				pos.y += pos.height;
			}

			DrawGUI.LayoutSpace(Height);

			return dirty;
		}

		protected bool DrawFoldableContent(Rect position)
		{
			bool dirty = false;

			for(int n = 0; n < 3; n++)
			{
				var draw = members[n];
				if(draw.ShouldShowInInspector)
				{
					if(draw.Draw(position))
					{
						dirty = true;
					}
					DrawGUI.NextLine(ref position);
				}
			}

			if(!UsingLocalSpace)
			{
				position.y += 3f;
				position.height = 18f;

				if(UsingSnapping)
				{
					position.width = 90f;
					position.x = (DrawGUI.InspectorWidth - (90f + 138f + 5f)) * 0.5f;
					DrawGUI.Active.Label(position, GUIContentPool.Temp("snapping", "You can open snap settings using menu item Edit/Snap Settings..."), "WarningOverlay");

					position.x += 95f;
					position.width = 138f;
					DrawGUI.Active.Label(position, GUIContentPool.Temp("using world space", "Position, rotation and scale are listed using world space instead of local space."), "WarningOverlay");
				}
				else
				{
					position.width = 138f;
					position.x = (DrawGUI.InspectorWidth - position.width) * 0.5f;
							
					DrawGUI.Active.Label(position, GUIContentPool.Temp("using world space", "Position, rotation and scale are listed using world space instead of local space."), "WarningOverlay");
				}
			}
			else if(UsingSnapping)
			{
				position.y += 3f;
				position.height = 18f;
				position.width = 90f;
				position.x = (DrawGUI.InspectorWidth - position.width) * 0.5f;
				DrawGUI.Active.Label(position, GUIContentPool.Temp("snapping", "You can open snap settings using menu item Edit/Snap Settings..."), "WarningOverlay");
			}

			return dirty;
		}


		/// <inheritdoc/>
		public override void UpdateVisibleMembers()
		{
			base.UpdateVisibleMembers();
			UpdateHeight();
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(!ReadOnly && !IsPrefab)
			{
				menu.AddSeparatorIfNotRedundant();
				menu.Add("Align With NavMesh", AlignWithNavMesh);
				menu.Add("Align With Ground", AlignWithGround);
			}

			menu.AddSeparatorIfNotRedundant();
			menu.Add("Snapping", ()=>SetSnapToGrid(!UsingSnapping), UsingSnapping);
			menu.Add("Edit Snap Settings...", ()=>DrawGUI.ExecuteMenuItem("Edit/Snap Settings..."));
			menu.Add("Use World Space", () => SetUsingLocalSpace(!UsingLocalSpace), !UsingLocalSpace);

			if(extendedMenu && !ReadOnly)
			{
				menu.Add("Convert to RectTransform", ConvertToRectTransform);
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			var resetIndex = menu.IndexOf("Reset");
			if(resetIndex != -1)
			{
				menu.Insert(resetIndex + 1, "Reset Without Affecting Children", ResetWithoutAffectingChildren);
			}
		}

		private void ConvertToRectTransform()
		{
			var wasLocked = Inspector.State.ViewIsLocked;
			Inspector.State.ViewIsLocked = true;
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				targets[n].gameObject.AddComponent<RectTransform>();
			}
			Inspector.State.ViewIsLocked = wasLocked;
		}

		private void ResetWithoutAffectingChildren()
		{
			var transform = Transform;
			
			var temp = (new GameObject()).transform;
			temp.parent = transform.parent;
			temp.localPosition = transform.localPosition;
			temp.localEulerAngles = transform.localEulerAngles;
			temp.localScale = transform.localScale;

			int childCount = transform.childCount;
			var children = ArrayPool<Transform>.Create(childCount);
			for(int n = childCount - 1; n >= 0; n--)
			{
				var child = transform.GetChild(n);
				children[n] = transform.GetChild(n);
				child.parent = temp;
			}

			DoReset();
			
			for(int n = childCount - 1; n >= 0; n--)
			{
				var child = temp.GetChild(n);
				child.parent = transform;
			}

			ArrayPool<Transform>.Dispose(ref children);

			Platform.Active.Destroy(temp.gameObject);
		}

		private void AlignWithGround()
		{
			((PositionDrawer)members[0]).Ground();
			((RotationDrawer)members[1]).AlignWithGround();
		}

		private void AlignWithNavMesh()
		{
			((PositionDrawer)members[0]).AlignWithNavMesh();
		}

		/// <inheritdoc/>
		protected override bool GetDataIsValidUpdated()
		{
			var firstTransform = Target as Transform;
			if(firstTransform != null)
			{
				var p = firstTransform.position;
				var min = -100000f;
				var max = 100000f;
				if(p.x < min || p.x > max || p.y < min || p.y > max || p.z < min || p.z > max)
				{
					return false;
				}
			}
			return true;
		}

		/// <inheritdoc/>
		protected override void DoReset()
		{
			if(memberBuildState == MemberBuildState.MembersBuilt)
			{
				members[0].SetValue(Vector3.zero);
				members[1].SetValue(Vector3.zero);
				members[2].SetValue(Vector3.one);
			}
			else if(memberBuildState == MemberBuildState.BuildListGenerated)
			{
				memberBuildList[0].SetValue(Vector3.zero);
				memberBuildList[1].SetValue(Vector3.zero);
				memberBuildList[2].SetValue(Vector3.one);
			}
			else
			{
				Debug.LogWarning(ToString()+ ".DoReset was called but memberBuildState was "+ memberBuildState);
			}
		}

		/// <inheritdoc />
		protected override void DoBuildHeaderToolbar()
		{
			base.DoBuildHeaderToolbar();

			var graphics = inspector.Preferences.graphics;
			var toggleLocalSpaceButton = HeaderPartDrawer.Create((HeaderPart)TransformHeaderPart.ToggleLocalSpaceButton, true, true, UsingLocalSpace ? graphics.LocalSpaceIcon : graphics.WorldSpaceIcon, UsingLocalSpace ? "Display transform values in world space." : "Display transform values in local space relative to parent.", OnToggleLocalSpaceIconClicked);
			AddHeaderToolbarItem(toggleLocalSpaceButton);
			
			var toggleSnappingButton = HeaderPartDrawer.Create((HeaderPart)TransformHeaderPart.ToggleSnapToGridButton, true, true, UsingSnapping ? graphics.SnappingOnIcon : graphics.SnappingOffIcon, UsingSnapping ? "Hide snapping controls for transform values." : "Show snapping controls for transform values.", OnToggleSnapToGridIconClicked, OnToggleSnapToGridIconRightClicked);
			#if UNITY_2019_3_OR_NEWER
			if(!UsingSnapping)
			{
				var guiColor = GUI.color;
				guiColor.a = 0.5f;
				toggleSnappingButton.SetGUIColor(guiColor);
			}
			#endif
			AddHeaderToolbarItem(toggleSnappingButton);
		}

		private void OnToggleLocalSpaceIconClicked(IUnityObjectDrawer containingDrawer, Rect buttonRect, Event inputEvent)
		{
			DrawGUI.Use(inputEvent);
			SetUsingLocalSpace(!UsingLocalSpace);
			GUI.changed = true;
		}

		private void OnToggleSnapToGridIconClicked(IUnityObjectDrawer containingDrawer, Rect buttonRect, Event inputEvent)
		{
			DrawGUI.Use(inputEvent);
			if(inputEvent.control)
			{
				DrawGUI.ExecuteMenuItem("Edit/Snap Settings...");
			}
			else
			{
				SetSnapToGrid(!UsingSnapping);
			}
			GUI.changed = true;
		}

		private void OnToggleSnapToGridIconRightClicked(IUnityObjectDrawer containingDrawer, Rect buttonRect, Event inputEvent)
		{
			var menu = Menu.Create();
			menu.Insert(0, "Edit Snap Settings...", () => DrawGUI.ExecuteMenuItem("Edit/Snap Settings..."));
			ContextMenuUtility.OpenAt(menu, buttonRect, this, (Part)TransformHeaderPart.ToggleSnapToGridButton);
		}
		
		private void SetUsingLocalSpace(bool value)
		{
			if(UsingLocalSpace != value)
			{
				UsingLocalSpace = value;
				fieldInfosGenerated = false;
				
				switch(memberBuildState)
				{
					case MemberBuildState.Unstarted:
						break;
					case MemberBuildState.BuildListGenerated:
						RebuildMemberBuildList();
						break;
					case MemberBuildState.MembersBuilt:
						RebuildMemberBuildListAndMembers();
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				RebuildHeaderToolbar();
				UpdateHeight();
			}
		}

		/// <inheritdoc/>
		public override void OnVisibleMembersChanged()
		{
			base.OnVisibleMembersChanged();
			UpdateHeight();
		}

		/// <inheritdoc/>
		public override void OnChildLayoutChanged()
		{
			base.OnChildLayoutChanged();
			UpdateHeight();
		}

		private void SetSnapToGrid(bool value)
		{
			if(UsingSnapping != value)
			{
				UserSettings.Snapping.Enabled = value;

				RebuildMemberBuildListAndMembers();
				RebuildHeaderToolbar();
			}
		}
				
		/// <inheritdoc />
		public override void Duplicate()
		{
			for(int n = 0, count = targets.Length; n < count; n++)
			{
				var source = (Transform)targets[n];
				var clone = new GameObject(source.name).transform;
				clone.position = source.position;
				clone.rotation = source.rotation;
				clone.localScale = source.localScale;
				clone.tag = source.tag;
				clone.gameObject.layer = source.gameObject.layer;
				clone.gameObject.SetActive(source.gameObject.activeSelf);
				clone.gameObject.isStatic = source.gameObject.isStatic;
			}
		}

		public RaycastHit?[] RaycastGround()
		{
			return GroundUtility.RaycastGround(Transforms);
		}

		/// <inheritdoc/>
		public override void AddItemsToOpeningViewMenu(ref Menu menu)
		{
			menu.Add("Help/Transform Drawer", PowerInspectorDocumentation.ShowDrawerInfo, "transform-gui-drawers");
			base.AddItemsToOpeningViewMenu(ref menu);
		}
	}
}