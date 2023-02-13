//#define DEBUG_EDITING_MEMBERS

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Represents a generic vertically laid out collection of IDrawer.
	/// Used by Inspectors to hold drawers for the currently inspected targets.
	/// </summary>
	public sealed class DrawerGroup : ParentDrawer<object>, IReorderableParent
	{
		private readonly List<IParentDrawer> foldedBeforeFiltering = new List<IParentDrawer>();
		private IInspector inspector;
		private bool hasFilter;
		private bool nowEditingMembers;
		private bool wantsSearchBoxDisabled;

		/// <inheritdoc/>
		public override int AppendIndentLevel
		{
			get
			{
				return 0;
			}
		}

		/// <summary> Gets the first drawer in members, or null if has no members. </summary>
		/// <returns> IDrawer or null. </returns>
		[CanBeNull]
		public IDrawer First()
		{
			return members.Length > 0 ? members[0] : null;
		}

		[CanBeNull]
		public IDrawer FirstVisible()
		{
			return visibleMembers.Length > 0 ? visibleMembers[0] : null;
		}

		[CanBeNull]
		public IDrawer Last()
		{
			return members.Length > 0 ? members[members.Length - 1] : null;
		}

		/// <summary> Gets a value indicating whether the drawers would prefer it if there was no search box visible in the Inspector toolbar.. </summary>
		/// <value> True if wants search box disabled, false if not. </value>
		public bool WantsSearchBoxDisabled
		{
			get
			{
				return wantsSearchBoxDisabled;
			}
		}

		/// <inheritdoc/>
		public override IInspector Inspector
		{
			get
			{
				return inspector;
			}
		}

		/// <inheritdoc cref="IParentDrawer.HeaderHeight" />
		public override float HeaderHeight
		{
			get
			{
				return 0f;
			}
		}

		/// <inheritdoc />
		public Rect FirstReorderableDropTargetRect
		{
			get
			{
				//the spot is right below the header
				var dropRect = lastDrawPosition;
				dropRect.height = DrawGUI.SingleLineHeight;
				dropRect.y += HeaderHeight - DrawGUI.SingleLineHeight * 0.5f;
				return dropRect;
			}
		}

		/// <inheritdoc cref="IDrawer.Type" />
		public override Type Type
		{
			get
			{
				return typeof(object[]);
			}
		}

		/// <summary> Gets or sets the length of members array. </summary>
		/// <value> The length of members array. </value>
		public int Length
		{
			get
			{
				return members.Length;
			}

			set
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(nowEditingMembers, "DrawerGroup.Length set to "+StringUtils.ToColorizedString(value)+" with nowEditingMembers="+StringUtils.False+". Length="+StringUtils.ToColorizedString(Length));
				#endif

				int was = members.Length;
				if(value > was)
				{
					Array.Resize(ref members, value);
				}
				else if(value < was)
				{
					for(int n = was - 1; n >= value; n--)
					{
						DisposeMember(n);
					}
					Array.Resize(ref members, value);
				}
			}
		}

		/// <inheritdoc cref="IParentDrawer.Unfolded" />
		public override bool Unfolded
		{
			get
			{
				return true;
			}

			set
			{
				#if DEV_MODE
				Debug.LogWarning("InvalidOperation: Can't set Unfolded state of DrawerGroup; they are always unfolded.");
				#endif
			}
		}

		/// <inheritdoc cref="IDrawer.Selectable" />
		public override bool Selectable
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public int LastCollectionMemberCountOffset
		{
			get
			{
				return 1;
			}
		}

		/// <inheritdoc/>
		public int FirstCollectionMemberIndex
		{
			get
			{
				return members.Length == 0 ? -1 : 0;
			}
		}

		/// <inheritdoc/>
		public int LastCollectionMemberIndex
		{
			get
			{
				return members.Length - 1;
			}
		}

		/// <inheritdoc/>
		public int FirstVisibleCollectionMemberIndex
		{
			get
			{
				return visibleMembers.Length == 0 ? -1 : 0;
			}
		}

		/// <inheritdoc/>
		public int LastVisibleCollectionMemberIndex
		{
			get
			{
				return visibleMembers.Length - 1;
			}
		}
		
		public static DrawerGroup Create(IInspector inspector, IParentDrawer parent = null, GUIContent label = null)
		{
			DrawerGroup result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new DrawerGroup();
			}
			result.Setup(inspector, parent, label);
			result.LateSetup();
			return result;
		}

		private DrawerGroup() { }

		/// <inheritdoc/>
		protected sealed override void Setup(IParentDrawer setParent, GUIContent setLabel)
		{
			throw new NotSupportedException("Please use the other Setup method");
		}

		private void Setup([NotNull]IInspector setInspector, IParentDrawer setParent, GUIContent setLabel)
		{
			nowEditingMembers = true;
			inspector = setInspector;
			hasFilter = setInspector.State.filter.HasFilterAffectingInspectedTargetContent;
			base.Setup(setParent, setLabel);
			inspector.OnFilterChanging += OnFilterChanging;
		}

		/// <inheritdoc cref="IDrawer.LateSetup" />
		public override void LateSetup()
		{
			base.LateSetup();
			nowEditingMembers = false;
		}

		private void OnFilterChanging(SearchFilter filter)
		{
			if(filter.HasFilterAffectingInspectedTargetContent != hasFilter)
			{
				hasFilter = !hasFilter;
				if(hasFilter)
				{
					SetAllUnfoldedTemporarilyForFiltering();
				}
				else
				{
					RestoreFoldedStatesWhenFilteringEnding();
				}
			}
		}

		/// <summary>
		/// Disposes all members and sets all visible members to null.
		/// It can be useful to call this before rebuilding members, so that the previous instances will be pooled and can be reused.
		/// </summary>
		public void DisposeMembersAndClearVisibleMembers()
		{
			DrawerArrayPool.Dispose(ref visibleMembers, false);
			visibleMembers = ArrayPool<IDrawer>.ZeroSizeArray;
			assumeVisibleMembersChangedDuringNextUpdateVisibleMembers = true;			
			DrawerArrayPool.DisposeContent(ref members);
			updateCachedValuesFor.Clear();
		}

		/// <summary> Clears all members. </summary>
		public void Clear()
		{
			if(memberBuildState == MemberBuildState.MembersBuilt)
			{
				SetMembers(ArrayPool<IDrawer>.ZeroSizeArray, false);
			}
		}

		#if DEV_MODE && PI_ASSERTATIONS
		/// <inheritdoc/>
		public override void OnAfterMembersBuilt()
		{
			Debug.Assert(nowEditingMembers || inactive, "DrawerGroup.OnAfterMembersBuilt called with nowEditingMembers="+StringUtils.False+" and inactive="+StringUtils.False+". Length="+StringUtils.ToColorizedString(Length));
			base.OnAfterMembersBuilt();
		}
		#endif
		
		public void ApplyForEachControl(Action<IDrawer> action)
		{
			for(var n = members.Length - 1; n >= 0; n--)
			{
				members[n].ApplyInChildren(action);
			}
		}
		
		[CanBeNull]
		public IDrawer FindDrawer([NotNull]Object target)
		{
			if(target == null)
			{
				throw new NotSupportedException("FindDrawer was called with null UnityEngine.Object. Did you mean to use the Component variant?");
			}

			var component = target as Component;
			if(component != null)
			{
				return FindDrawer(component);
			}

			var inspected = Members;
			for(int n = inspected.Length - 1; n >= 0; n--)
			{
				var member = inspected[n];
				if(Array.IndexOf(member.UnityObjects, target) != -1)
				{
					return member;
				}
			}
			return null;
		}

		/// <summary>
		/// Tries to find Drawer for target in members
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public IComponentDrawer FindDrawer([CanBeNull]Component target)
		{
			if(target == null)
			{
				var gos = Members;
				for(int n = gos.Length - 1; n >= 0; n--)
				{
					var go = gos[n] as IGameObjectDrawer;
					if(go != null)
					{
						var comps = go.Members;
						for(int c = go.LastCollectionMemberIndex; c >= 0; c--)
						{
							var member = comps[c];
							if(member.UnityObject == null)
							{
								var asComponentDrawer = comps[c] as IComponentDrawer;
								if(asComponentDrawer != null)
								{
									return asComponentDrawer;
								}
							}
						}
					}
				}
			}
			else
			{
				var gameObject = target.gameObject;
				var gos = Members;
				for(int n = gos.Length - 1; n >= 0; n--)
				{
					var go = gos[n] as IGameObjectDrawer;
					if(go != null)
					{
						if(Array.IndexOf(go.GameObjects, gameObject) != -1)
						{
							var comps = go.Members;
							for(int c = go.LastCollectionMemberIndex; c >= 0; c--)
							{
								var comp = comps[c];
								if(comp.UnityObject == target)
								{
									return comp as IComponentDrawer;
								}
							}
						}
					}
				}
			}

			return null;
		}

		public IDrawer FindDrawer(LinkedMemberInfo memberInfo)
		{
			if(memberInfo == null)
			{
				return null;
			}

			var obj = memberInfo.UnityObject;
			if(obj == null)
			{
				return null;
			}

			var objDrawer = FindDrawer(obj);
			if(objDrawer != null)
			{
				return DrawerUtility.FindFieldDrawerInMembers(memberInfo, objDrawer);
			}
			return null;
		}

		/// <inheritdoc cref="IDrawer.AddPreviewWrappers" />
		public override void AddPreviewWrappers(ref List<IPreviewableWrapper> previews)
		{
			for(int n = members.Length - 1; n >= 0; n--)
			{
				var member = members[n];
				member.AddPreviewWrappers(ref previews);
			}
		}

		/// <inheritdoc/>
		public int GetDropTargetIndexAtPoint(Vector2 point)
		{
			return ReorderableParentDrawerUtility.GetDropTargetIndexAtPoint(this, point, true);
		}

		/// <inheritdoc/>
		public bool MemberIsReorderable(IReorderable member)
		{
			var values = member.GetValues();
			return values.Length > 0 && (values[0] as GameObject) != null;
		}

		/// <inheritdoc/>
		public bool SubjectIsReorderable(Object member)
		{
			return (member as GameObject) != null;
		}

		/// <inheritdoc/>
		public void DeleteMember(IDrawer delete)
		{
			int index = Array.IndexOf(members, delete);
			if(index == -1)
			{
				Debug.LogError(ToString()+ ".DeleteMemberValue - Unable to find item " + (delete == null ? "null" : delete.ToString())+" among members");
				return;
			}

			var setVisibleMembers = visibleMembers;
			int visibleMembersIndex = Array.IndexOf(visibleMembers, delete);
			if(visibleMembersIndex != -1)
			{
				DrawerArrayPool.RemoveAt(ref setVisibleMembers, visibleMembersIndex, false, false);
			}

			var setMembers = members;
			DrawerArrayPool.RemoveAt(ref setMembers, index, false, true);
			
			SetMembers(setMembers, setVisibleMembers, true);
			
			inspector.RefreshView();
		}

		/// <inheritdoc/>
		public void OnMemberReorderingStarted(IReorderable reordering) { }

		/// <inheritdoc/>
		public void OnMemberDrag(MouseDownInfo mouseDownInfo, Object[] draggedObjects)
		{
			int dragNDropIndex = mouseDownInfo.Reordering.MouseoveredDropTarget.MemberIndex;

			if(dragNDropIndex >= 0)
			{
				var memb = visibleMembers[dragNDropIndex] as IComponentDrawer;
				if(memb != null)
				{
					var reorderDropRect = memb.ReorderDropRect;
					reorderDropRect.y += Mathf.RoundToInt(reorderDropRect.height * 0.5f) - 1f;
					reorderDropRect.height = 3f;
					DrawGUI.Active.DragAndDropVisualMode = DragAndDropVisualMode.Link;
					GUI.DrawTexture(reorderDropRect, InspectorUtility.Preferences.graphics.ReorderDropTargetBg, ScaleMode.StretchToFill);
				}
			}
		}

		/// <inheritdoc/>
		public void OnSubjectOverDropTarget(MouseDownInfo mouseDownInfo, Object[] draggedObjects) { }

		/// <inheritdoc/>
		public void OnMemberDragNDrop(MouseDownInfo mouseDownInfo, Object[] draggedObjects)
		{
			var reordering = mouseDownInfo.Reordering;
			
			int dropIndex = reordering.MouseoveredDropTarget.MemberIndex;
			
			if(dropIndex >= 0)
			{
				var draggedGameObjectDrawer = reordering.Drawer;
				
				//if only raw GameObject references are dragged, e.g. from the hierarchy view
				if(draggedGameObjectDrawer == null)
				{
					var gameObjects = draggedObjects as GameObject[];
					if(gameObjects != null)
					{
						var inspector = Inspector;

						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(reordering.MouseoveredDropTarget.Inspector == inspector);
						#endif

						//create a new GameObjectGUIInstrutions to display the dragged GameObjects
						var gameObjectDrawer = inspector.DrawerProvider.GetForGameObjects(inspector, gameObjects, this);
						if(gameObjectDrawer != null)
						{
							var setMembers = Members;
							DrawerArrayPool.InsertAt(ref members, dropIndex, gameObjectDrawer, false);
							SetMembers(setMembers, true);
						}
					}
				}
				else
				{
					var sourceParent = reordering.Parent;
					int sourceIndex = reordering.MemberIndex;

					//if reordering GameObjects within the same DrawerGroup (e.g. stacked multi-editing mode)
					if(sourceParent == this)
					{
						if(dropIndex != sourceIndex)
						{
							inspector.State.ViewIsLocked = true;
							
							var setMembers = Members;
							DrawerArrayPool.RemoveAt(ref setMembers, sourceIndex, false, false);

							if(sourceIndex < dropIndex)
							{
								dropIndex--;
							}
							DrawerArrayPool.InsertAt(ref setMembers, dropIndex, draggedGameObjectDrawer, false);
							
							SetMembers(setMembers);
						}
					}
					//if cloning (or moving?) GameObjects from one DrawerGroup to another (e.g. between split views)
					else
					{
						var setMembers = Members;
						var clone = inspector.DrawerProvider.GetForGameObjects(reordering.MouseoveredDropTarget.Inspector, draggedGameObjectDrawer.GetValues() as GameObject[], this);
						DrawerArrayPool.InsertAt(ref setMembers, dropIndex, clone, false);
						SetMembers(setMembers);
					}
				}
			}
		}

		/// <inheritdoc/>
		public void OnMemberReorderingEnded(IReorderable reordering) { }

		/// <inheritdoc cref="IDrawer.Dispose" />
		public override void Dispose()
		{
			inspector.OnFilterChanging -= OnFilterChanging;
			if(hasFilter)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!inspector.State.filter.HasFilterAffectingInspectedTargetContent);
				#endif

				RestoreFoldedStatesWhenFilteringEnding();
			}
			hasFilter = false;
		
			nowEditingMembers = true;
			base.Dispose();
			nowEditingMembers = false;
			
			wantsSearchBoxDisabled = false;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inactive);
			#endif
		}

		public void DisposeMember(int index)
		{
			var dispose = members[index];
			if(dispose != null)
			{
				dispose.Dispose();
				members[index] = null;
			}
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList() { }

		/// <inheritdoc/>
		protected override void DoBuildMembers() { }

		/// <inheritdoc/>
		protected override Type GetMemberType(object memberBuildListItem)
		{
			return memberBuildListItem == null ? null : memberBuildListItem.GetType();
		}

		/// <inheritdoc/>
		protected override object GetMemberValue(object memberBuildListItem)
		{
			return memberBuildListItem == null ? null : memberBuildListItem;
		}

		/// <inheritdoc cref="IParentDrawer.SetMembers(IDrawer[])" />
		public override void SetMembers(IDrawer[] setMembers, bool sendVisibilityChangedEvents = true)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!sendVisibilityChangedEvents || setMembers.Length > 0);
			#endif

			var wasInactive = inactive;

			if(!nowEditingMembers)
			{
				nowEditingMembers = true;
				
				#if DEV_MODE && DEBUG_EDITING_MEMBERS
				Debug.Log("<color=green>StartEditingMembers</color> with members="+StringUtils.ToString(members)+", visibleMembers="+ StringUtils.ToString(visibleMembers));
				#endif

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(inspector.State.filter.HasFilterAffectingInspectedTargetContent == hasFilter, "inspector.State.filter.HasFilterAffectingInspectedTargetContent=" + StringUtils.ToColorizedString(inspector.State.filter.HasFilterAffectingInspectedTargetContent) +" != hasFilter="+StringUtils.ToColorizedString(hasFilter));
				#endif

				if(hasFilter)
				{
					RestoreFoldedStatesWhenFilteringEnding();
				}
			}
			#if DEV_MODE
			else { Debug.LogError("DrawerGroup.StartEditingMembers called but was already editing members"); }
			#endif

			inactive = true;

			base.SetMembers(setMembers, sendVisibilityChangedEvents);

			#if DEV_MODE && DEBUG_EDITING_MEMBERS
			Debug.Log("<color=red>StopEditingMembers</color> with members=" + StringUtils.ToString(members)+", visibleMembers="+ StringUtils.ToString(visibleMembers));
			#endif
			
			nowEditingMembers = false;
			inactive = wasInactive;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inspector.State.filter.HasFilterAffectingInspectedTargetContent == hasFilter, "inspector.State.filter.HasFilterAffectingInspectedTargetContent=" + StringUtils.ToColorizedString(inspector.State.filter.HasFilterAffectingInspectedTargetContent) +" != hasFilter="+StringUtils.ToColorizedString(hasFilter));
			#endif

			if(hasFilter)
			{
				SetAllUnfoldedTemporarilyForFiltering();
			}
		}

		private void SetAllUnfoldedTemporarilyForFiltering()
		{
			for(int n = members.Length - 1; n >= 0; n--)
			{
				var unfold = members[n] as IParentDrawer;
				if(unfold != null)
				{
					SetUnfoldedTemporarilyForFiltering(unfold);
				}
			}
		}

		public void SetUnfoldedTemporarilyForFiltering([NotNull]IParentDrawer subject)
		{
			if(!subject.Unfolded && subject.Foldable)
			{
				subject.SetUnfolded(true);
				foldedBeforeFiltering.AddIfDoesNotContain(subject);
			}
			
			var membs = subject.Members;
			for(int n = membs.Length - 1; n >= 0; n--)
			{
				var memb = membs[n] as IParentDrawer;
				if(memb != null)
				{
					SetUnfoldedTemporarilyForFiltering(memb);
				}
			}
		}
		
		private void RestoreFoldedStatesWhenFilteringEnding()
		{
			for(int n = foldedBeforeFiltering.Count - 1; n >= 0; n--)
			{
				var target = foldedBeforeFiltering[n];
				if(target != null && !target.Inactive && target.Unfolded)
				{
					target.SetUnfolded(false, false);
				}
			}
			foldedBeforeFiltering.Clear();
		}

		public void RestoreFoldedStateWhenDisposingDuringFiltering(IParentDrawer subject)
		{
			if(!hasFilter)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!inspector.State.filter.HasFilterAffectingInspectedTargetContent);
				#endif
				return;
			}

			if(foldedBeforeFiltering.Remove(subject) && subject.Unfolded)
			{
				var membs = subject.Members;
				for(int n = membs.Length - 1; n >= 0; n--)
				{
					var memb = membs[n] as IParentDrawer;
					if(memb != null)
					{
						RestoreFoldedStateWhenDisposingDuringFiltering(memb);
					}
				}

				subject.SetUnfolded(false, false);
			}
		}

		/// <inheritdoc/>
		public override void OnVisibleMembersChanged()
		{
			wantsSearchBoxDisabled = false;
			for(int n = visibleMembers.Length - 1; n >= 0; n--)
			{
				var root = visibleMembers[n] as IRootDrawer;
				if(root != null && root.WantsSearchBoxDisabled)
				{
					wantsSearchBoxDisabled = true;
					break;
				}
			}
			base.OnVisibleMembersChanged();
		}
	}
}