#define ENABLE_UNFOLD_ANIMATIONS
#define DELAY_BUILDING_MEMBERS_UNTIL_UNFOLDED
#define CLONE_VALUE_BEFORE_SETTING

#define SAFE_MODE
//#define CHANGE_CURSOR_ON_MOUSEOVER

//#define DEBUG_ON_CLICK
//#define DEBUG_ON_MOUSE_UP

#define DEBUG_NULL_MEMBERS

//#define DEBUG_SET_NULL_VALUE
//#define DEBUG_ON_CACHED_VALUE_CHANGED
//#define DEBUG_SET_VALUE
//#define DEBUG_SET_VALUE_CLONE
//#define DEBUG_SET_UNFOLDED
//#define DEBUG_UPDATE_VISIBLE_MEMBERS
//#define DEBUG_ON_MEMBER_VALUE_CHANGED
//#define DEBUG_CACHED_VALUES_NEED_UPDATING
//#define DEBUG_VISUALIZE_BOUNDS

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using JetBrains.Annotations;
using Sisus.Newtonsoft.Json;

#if DEV_MODE && PI_ASSERTATIONS
using System.Linq;
#endif

namespace Sisus
{
	/// <summary>
	/// Drawer for a UnityEngine.Object member that has a value (field, property...)
	/// and members (not primitive, string...)
	/// </summary>
	/// <typeparam name="TValue"> Type of the value. </typeparam>
	[Serializable]
	public abstract class ParentFieldDrawer<TValue> : FieldDrawer<TValue>, IParentDrawer
	{
		protected const float foldoutArrowSize = 12f;

		/// <summary>
		/// State of the member build.
		/// </summary>
		protected MemberBuildState memberBuildState = MemberBuildState.Unstarted;

		/// <summary>
		/// List of member builds.
		/// </summary>
		protected List<LinkedMemberInfo> memberBuildList = new List<LinkedMemberInfo>();

		/// <summary>
		/// The update cached values for.
		/// </summary>
		private readonly List<IDrawer> updateCachedValuesFor = new List<IDrawer>();

		/// <summary>
		/// The label last draw position.
		/// </summary>
		protected Rect labelLastDrawPosition;

		/// <summary>
		/// The body last draw position.
		/// </summary>
		protected Rect bodyLastDrawPosition;

		/// <summary>
		/// True if fold arrow mouseovered.
		/// </summary>
		private bool foldArrowMouseovered;

		/// <summary>
		/// all members except for ones that are completely unlisted under current view preferences (e.g.
		/// nonserialized fields)
		/// </summary>
		protected IDrawer[] members = new IDrawer[0];

		/// <summary>
		/// all members that are currenly visible in the inspector
		/// i.e. passed all filters, on-screen etc.
		/// </summary>
		protected IDrawer[] visibleMembers = new IDrawer[0];

		/// <summary>
		/// True to cached values need updating.
		/// </summary>
		private bool cachedValuesNeedUpdating;

		/// <summary>
		/// True to was selected on click start.
		/// </summary>
		private bool wasSelectedOnClickStart;

		private readonly TweenedBool unfoldedness = new TweenedBool();

		/// <summary>
		/// Usully when UpdateVisibleMembers is called, it only invokes OnVisibleMembersChanged and OnChildLayoutChanged
		/// if the contents of the visible members array changed. This value can be used to force it to always invoke those
		/// methods no matter what during the next call.
		/// </summary>
		private bool assumeVisibleMembersChangedDuringNextUpdateVisibleMembers = true;

		/// <summary>
		/// Draw rects for members when members are drawn in a single row.
		/// </summary>
		protected Rect[] memberRects = new Rect[0];

		/// <inheritdoc cref="IDrawer.CachedValuesNeedUpdating" />
		public sealed override bool CachedValuesNeedUpdating
		{
			get
			{
				return cachedValuesNeedUpdating;
			}
		}

		/// <inheritdoc/>
		public virtual bool MembersAreVisible
		{
			get
			{
				return Unfoldedness > 0f;
			}
		}

		/// <inheritdoc/>
		public virtual float HeaderHeight
		{
			get
			{
				return DrawGUI.SingleLineHeight;
			}
		}

		/// <inheritdoc cref="IDrawer.ClickToSelectArea" />
		public override Rect ClickToSelectArea
		{
			get
			{
				if(DrawInSingleRow && members.Length == 0)
				{
					return lastDrawPosition;
				}
				return labelLastDrawPosition;
			}
		}

		/// <inheritdoc/>
		protected override Rect SelectionRect
		{
			get
			{
				var rect = base.SelectionRect;
				rect.x = DrawGUI.RightPadding;
				rect.width = DrawGUI.InspectorWidth - DrawGUI.RightPadding - DrawGUI.RightPadding;
				return rect;
			}
		}

		/// <inheritdoc/>
		protected override bool GetHasUnappliedChangesUpdated()
		{
			if(overrideHasUnappliedChanges != null)
			{
				return overrideHasUnappliedChanges();
			}

			if(memberBuildState != MemberBuildState.MembersBuilt)
			{
				return false;
			}

			for(int n = members.Length - 1; n >= 0; n--)
			{
				var memb = members[n];
				if(memb == null)
				{
					return HasUnappliedChanges;
				}

				if(memb.HasUnappliedChanges)
				{
					return true;
				}
			}
			return false;
		}

		/// <inheritdoc/>
		public virtual bool DrawInSingleRow
		{
			get
			{
				return members.Length == 0 && memberBuildList.Count == 0;
			}
		}
		
		/// <inheritdoc/>
		public virtual bool Foldable
		{
			get
			{
				return !DrawInSingleRow;
			}
		}

		/// <inheritdoc cref="IDrawer.PrefixResizingEnabledOverControl" />
		public override bool PrefixResizingEnabledOverControl
		{
			get
			{
				return PrefixLabelClippedToColumnWidth;
			}
		}

		/// <inheritdoc cref="IDrawer.DebugMode" />
		public override bool DebugMode
		{
			get
			{
				if(!base.DebugMode)
				{
					return false;
				}

				if(parent == null)
				{
					return true;
				}

				const int maxDepth = 10;

				int depth = 0;
				var type = Type;
				for(var testParent = parent; testParent != null; testParent = testParent.Parent)
				{
					// avoid infinite member building loops by disabling debug mode if parent type is exactly the same as this type.
					if(testParent.Type == type && testParent.GetType() == GetType())
					{
						return false;
					}
					depth++;
				}

				return depth <= maxDepth;
			}
		}

		/// <summary>
		/// Indexer to get or set items within this collection using array index syntax.
		/// </summary>
		/// <param name="index">
		/// Zero-based index of the entry to access. </param>
		/// <returns>
		/// The indexed item.
		/// </returns>
		[JsonIgnore]
		public IDrawer this[int index]
		{
			get
			{
				return members[index];
			}

			set
			{
				#if DEV_MODE
				if(value != null && value.Equals(members[index]))
				{
					Debug.LogError("this["+index+"] = "+value+"): member at index already matched value!");
					return;
				}
				#endif

				members[index] = value;
				if(value != null)
				{
					value.OnParentAssigned(this);
				}
			}
		}

		/// <summary>
		/// Gets the fold arrow position.
		/// </summary>
		/// <value>
		/// The fold arrow position.
		/// </value>
		private Rect FoldArrowPosition
		{
			get
			{
				var rect = labelLastDrawPosition;
				rect.width = 15f;
				return rect;
			}
		}

		/// <inheritdoc/>
		public sealed override Rect ControlPosition
		{
			get
			{
				return bodyLastDrawPosition;
			}
		}

		/// <inheritdoc cref="IDrawer.RightClickArea" />
		public sealed override Rect RightClickArea
		{
			get
			{
				return labelLastDrawPosition;
			}
		}

		/// <summary>
		/// Gets the append indent level.
		/// </summary>
		/// <value>
		/// The append indent level.
		/// </value>
		public virtual int AppendIndentLevel
		{
			get
			{
				return 1;
			}
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public IDrawer[] Members
		{
			get
			{
				return members;
			}

			protected set
			{
				if(members != value)
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(value != null);
					Debug.Assert(value != visibleMembers || value.Length == 0, ToString()+ " members array is same as visible members - This can lead to bugs!");
					Debug.Assert(!value.ContainsNullMembers());
					#endif

					//Don't dispose members, since usually will contain same members
					//as the members array
					DrawerArrayPool.Dispose(ref members, false);
					members = value;

					//should we clear visible members before invoking OnParentAssigned?
					#if DEV_MODE
					if(visibleMembers.ContainsNullMembers())
					{
						Debug.LogWarning(this+".Members value was set, but VisibleMembers contained null elements. This could lead to bugs when OnParentAssigned is called. You should use SetMembers instead, to set both Members and VisibleMembers simultaneously.");
					}
					#endif

					ParentDrawerUtility.SendOnParentAssignedEvents(this, members);
				}
				#if DEV_MODE
				else { Debug.LogWarning(ToString()+ ".Members set was called but already matched current members (="+StringUtils.ToString(members)+ "). Won't call OnParentAssigned."); }
				#endif
			}
		}
		
		/// <inheritdoc/>
		[JsonIgnore]
		public IDrawer[] MembersBuilt
		{
			get
			{
				if(memberBuildState != MemberBuildState.MembersBuilt)
				{
					if(memberBuildState == MemberBuildState.Unstarted)
					{
						#if DEV_MODE
						Debug.LogWarning(ToString()+".MembersBuilt - generating the member build list because it had not yet been done.");
						#endif
						GenerateMemberBuildList();
					}
					#if DEV_MODE
					Debug.LogWarning(ToString()+".MembersBuilt - building the members because it had not yet been done.");
					#endif
					BuildMembers();
				}
				return members;
			}
		}
		
		/// <inheritdoc/>
		[JsonIgnore]
		public IDrawer[] VisibleMembers
		{
			get
			{
				return visibleMembers;
			}
		}
		
		/// <inheritdoc cref="IDrawer.ShouldShowInInspector" />
		public override bool ShouldShowInInspector
		{
			get
			{
				return ParentDrawerUtility.ShouldShowInInspector(this, passedLastFilterCheck) && passedLastShowInInspectorIfTest;
			}
		}

		/// <inheritdoc/>
		public bool Unfolded
		{
			get
			{
				return unfoldedness;
			}

			set
			{
				bool was = Unfolded;

				#if DEV_MODE && DEBUG_SET_UNFOLDED
				Debug.Log(StringUtils.ToColorizedString(ToString(), ".Unfolded = ", value, " (was: ", was, ") with inactive=", inactive, ", MembersAreVisible=", MembersAreVisible, ", Foldable=", Foldable, ", DrawInSingleRow=", DrawInSingleRow, ", memberBuildState=", memberBuildState));
				#endif
				
				if(!inactive && memberInfo != null)
				{
					var sp = memberInfo.SerializedProperty;
					if(sp != null)
					{
						sp.isExpanded = value;
					}
				}
				
				if(value == was)
				{
					#if DEV_MODE && DEBUG_SET_UNFOLDED
					Debug.Log(StringUtils.ToColorizedString(ToString(), ".Unfolded = ", value, " aborting because Unfolded already matched value..."));
					#endif

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(value || unfoldedness <= 0f, StringUtils.ToColorizedString(ToString(), " Unfoldedness = ", value, " - Unfolded aready was ", value, ", but unfoldedness was ", (float)unfoldedness));
					Debug.Assert(!value || unfoldedness >= 1f, StringUtils.ToColorizedString(ToString(), " Unfoldedness = ", value, " - Unfolded aready was ", value, ", but unfoldedness was ", (float)unfoldedness));
					#endif
					return;
				}

				if(Foldable)
				{
					#if SAFE_MODE || DEV_MODE
					if(prefixLabelDrawer as FoldoutDrawer == null)
					{
						#if DEV_MODE
						Debug.LogError(ToString() + " has no FoldoutDrawer with Foldable=" + Foldable + ", DrawInSingleRow=" + DrawInSingleRow + ", memberBuildState=" + memberBuildState + ", MembersAreVisible=" + MembersAreVisible);
						#endif

						UpdatePrefixDrawer();
					}
					#endif

					prefixLabelDrawer.Unfolded = value;
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

		/// <summary>
		/// Gets a value indicating whether the prefix label width is clipped to prefix label column width,
		/// or if it can flow past it (the behaviour of most foldouts).
		/// </summary>
		/// <value> True if prefix label clipped to column width, false if not. </value>
		protected virtual bool PrefixLabelClippedToColumnWidth
		{
			get
			{
				return DrawInSingleRow || (Inspector.Preferences.enableTooltipIcons && label.tooltip.Length > 0);
			}
		}

		/// <summary>
		/// Called when drawer were fully folded (closedness was 0)
		/// and just now started unfolding (closedness is > 0), or when they
		/// were at least partially unfolded (closedness was > 0) and just became
		/// fully folded (closedness is 0).
		/// </summary>
		/// <param name="unfolded"> True if became partially unfolded, false if became fully folded. </param>
		private void OnFullClosednessChanged(bool unfolded)
		{
			if(inactive)
			{
				return;
			}

			if(MembersAreVisible && memberBuildState == MemberBuildState.BuildListGenerated)
			{
				// Handle situations like parent value having been set to null while members were not visible.
				RebuildMemberBuildList();

				BuildMembers();
			}
			else
			{
				UpdateVisibleMembers();
			}

			ParentDrawerUtility.OnMemberVisibilityChanged(this, unfolded);
		}

		/// <inheritdoc/>
		public float Unfoldedness
		{
			get
			{
				return unfoldedness;
			}
		}

		/// <inheritdoc cref="IDrawer.Height" />
		public override float Height
		{
			get
			{
				return ParentDrawerUtility.CalculateHeight(this);
			}
		}

		/// <inheritdoc/>
		public override TValue Value
		{
			set
			{
				#if DEV_MODE && DEBUG_SET_VALUE
				Debug.Log(ToString() + ".Value = " + StringUtils.ToString(value) +" (was : "+ StringUtils.ToString(Value) +")");
				#endif

				//copy the value that is being set to prevent issues with multiple fields referencing the same value etc.
				base.Value = GetCopyOfValue(value);
			}
		}
		
		protected bool ValueWasJustSet
		{
			get;
			private set;
		}

		/// <summary>
		/// Are members currently drawn underneath the parent.
		/// This is true if DrawInSingleRow is false and Unfolded is true.
		/// </summary>
		/// <value>
		/// True if members listed vertically, false if not.
		/// </value>
		private bool MembersListedVertically
		{
			get
			{
				return !DrawInSingleRow && Unfolded;
			}
		}

		/// <summary>
		/// Gets a value indicating whether rebuilding members is allowed.
		/// 
		/// If members are set manually by an external class instead of being
		/// built by the DoBuildMembers method, then this should return false.
		/// </summary>
		/// <value>
		/// True if rebuilding members is allowed, false if not.
		/// </value>
		protected virtual bool RebuildingMembersAllowed
		{
			get
			{
				#if DELAY_BUILDING_MEMBERS_UNTIL_UNFOLDED
				return MembersAreVisible || memberBuildState == MemberBuildState.MembersBuilt;
				#else
				return true;
				#endif
			}
		}

		/// <summary>
		/// Gets a value indicating whether the rebuild drawer if value was changed.
		/// </summary>
		/// <value>
		/// True if rebuild drawer if value changed, false if not.
		/// </value>
		protected virtual bool RebuildDrawersIfValueChanged
		{
			get
			{
				return memberInfo == null && RebuildingMembersAllowed;
			}
		}

		private Rect DragBarPosition
		{
			get
			{
				var rect = labelLastDrawPosition;
				rect.x -= 10f;
				rect.width = 6f;
				rect.height = 8f;
				rect.y += 5f;
				return rect;
			}
		}
		
		/// <inheritdoc />
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			#if DEV_MODE && DEBUG_ON_CACHED_VALUE_CHANGED
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".OnCachedValueChanged with applyToField=", applyToField, ", Value=", Value, ", updateMembers=", updateMembers, ", RebuildDrawerIfValueChanged=", RebuildDrawersIfValueChanged, ", updateCachedValuesFor=", StringUtils.ToString(updateCachedValuesFor), ", MembersAreVisible=", MembersAreVisible));
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			if(!Inspector.HasFilterAffectingInspectedTargetContent && MembersAreVisible && !updateCachedValuesFor.ContentsMatch(visibleMembers.Where(drawer => drawer.CachedValuesNeedUpdating).ToList(), false))
			{
				Debug.LogError(Msg(ToString(), " had ", members.Length, " members but updateCachedValuesFor was ", updateCachedValuesFor.Count, " with MembersAreVisible=", true, " and Inspector.HasFilter=", false));
			}
			#endif

			try
			{
				ValueWasJustSet = true;

				base.OnCachedValueChanged(applyToField, updateMembers);
			
				if(!updateMembers)
				{
					return;
				}

				if(RebuildDrawersIfValueChanged)
				{
					var inspector = Inspector;
					var selected = inspector.Manager.FocusedDrawer;
					var selectedPath = selected == null || !inspector.InspectorDrawer.HasFocus ? null : selected.GenerateMemberIndexPath(this);

					//Changing value could cause number of members to change (e.g. when the length of an array is changed)
					//because of this it is not enough to simply update visible children, we need to rebuild all children

					if(selectedPath != null)
					{
						SelectMemberAtIndexPath(selectedPath, ReasonSelectionChanged.Initialization);
					}
				}
				else
				{
					int n = updateCachedValuesFor.Count - 1;
					try
					{
						for(; n >= 0; n--)
						{
							updateCachedValuesFor[n].UpdateCachedValuesFromFieldsRecursively();
						}
					}
					catch(NullReferenceException)
					{
						#if DEV_MODE && DEBUG_NULL_MEMBERS
						Debug.LogWarning("UpdateCachedValuesFromFieldsRecursively() NullReferenceException " + ToString()+ " @ updateCachedValuesFor[" + n + "]");
						#endif
					}
				}
			}
			catch(Exception e)
			{
				Debug.LogError(e);
			}
			finally
			{
				ValueWasJustSet = false;
			}
		}
		
		/// <inheritdoc/>
		protected override void Setup(TValue setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
			GenerateMemberBuildList();
		}

		/// <summary>
		/// Populates memberBuildInfo list with LinkedMemberInfos for member drawer that should be built
		/// if/when the member drawer become visible in the inspector (usually when the parent is unfolded).
		/// This should be called only once between Create and Dispose being called, during the Setup phase.
		/// </summary>
		private void GenerateMemberBuildList()
		{
			#if DEV_MODE
			Debug.Assert(memberBuildState == MemberBuildState.Unstarted, ToString() + ".GenerateMemberBuildList was called but memberBuildState was " + memberBuildState);
			#endif

			DoGenerateMemberBuildList();

			memberBuildState = MemberBuildState.BuildListGenerated;
			OnAfterMemberBuildListGenerated();
		}

		/// <summary>
		/// The main body of the GenerateMemberBuildList method, actually handling building the member build list.
		/// This should never be called by inheriting classes directly, GenerateMemberBuildList should be used instead!
		/// </summary>
		protected abstract void DoGenerateMemberBuildList();

		/// <summary>
		/// Called after member build list has been generated
		/// </summary>
		protected virtual void OnAfterMemberBuildListGenerated()
		{
			UpdatePrefixDrawer();
		}

		/// <inheritdoc cref="IDrawer.LateSetup" />
		public override void LateSetup()
		{
			inactive = true;

			bool drawInSingleRowWas = DrawInSingleRow;

			// set unfolded state before building members, so that contents of visible members are built correctly
			// and before base.LateSetup is called, so that the prefix drawer is generated with correct unfolded state
			HandleInitialUnfolding();

			if(memberBuildState != MemberBuildState.MembersBuilt && MembersAreVisible)
			{
				BuildMembers();
			}

			base.LateSetup();
			
			// set inactive back to true, because calling base.LateSetup has set it to false
			inactive = true;

			// in some cases it's possible that the value of DrawInSingleRow changes between the
			// beginning of LateSetup and the end. If this happens, call HandleInitialUnfolding again
			// to make sure that Unfolded is true if Foldable is false and so forth.
			if(drawInSingleRowWas != DrawInSingleRow)
			{
				HandleInitialUnfolding();
			}
			
			UpdateCachedValuesNeedUpdating();

			inactive = false;
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!DrawInSingleRow || Unfoldedness >= 1f, StringUtils.ToColorizedString(ToString(), " Unfoldedness=", Unfoldedness, " with DrawInSingleRow=", true, "."));
			Debug.Assert(Foldable || Unfoldedness <= 0f || Unfoldedness >= 1f, StringUtils.ToColorizedString(ToString(), " was playing folding or unfolding animation even though Foldable was was ", false, "."));
			Debug.Assert(prefixLabelDrawer == null || prefixLabelDrawer.Unfolded == unfoldedness, StringUtils.ToColorizedString(ToString(),".prefixLabelDrawer.Unfolded ", (prefixLabelDrawer == null ? "null" : StringUtils.ToColorizedString(prefixLabelDrawer.Unfolded)), " != unfoldedness (", (bool)unfoldedness+" / "+ (float)unfoldedness, ")"));
			Debug.Assert(unfoldedness || !DrawInSingleRow, StringUtils.ToColorizedString(ToString(), " was folded even though DrawInSingleRow was", true, "!"));
			#endif
		}

		/// <summary>
		/// Figures out what the initial unfolded state of these drawer should be,
		/// and sets the drawer to said state.
		/// </summary>
		private void HandleInitialUnfolding()
		{
			if(DrawInSingleRow)
			{
				SetUnfoldedInstantly(true);
			}
			else if(Foldable && memberInfo != null)
			{
				var sp = memberInfo.SerializedProperty;
				if(sp != null && sp.isExpanded)
				{
					if(!Inspector.Preferences.animateInitialUnfolding)
					{
						SetUnfoldedInstantly(true);
					}
					else
					{
						SetUnfolded(true);
					}
				}
			}

			#if DEV_MODE && PI_ASSERTATIONS
			if(!Unfolded && DrawInSingleRow)
			{
				Debug.LogError(Msg(ToString()+".HandleInitialUnfolding Unfolded was ", false, " and DrawInSingleRow was ", DrawInSingleRow, " with Foldable=", Foldable, ", memberInfo=", memberInfo));
			}
			#endif
		}

		/// <summary>
		/// Clears the memberBuildList and builds it again from scratch.
		/// 
		/// This usually happens based on memberBuildList contents, so GenerateMemberBuildList should always be called before this method.
		/// 
		/// Note that this does NOT rebuild the member build list - RebuildMemberBuildListAndMembers can be used to achieve that.
		/// </summary>
		protected void RebuildMemberBuildList()
		{
			ClearMemberBuildList();
			memberBuildState = MemberBuildState.Unstarted;
			GenerateMemberBuildList();
		}

		/// <summary>
		/// Clears the member build list.
		/// </summary>
		private void ClearMemberBuildList()
		{
			//dispose now or later?
			var hierarchy = MemberHierarchy;
			for(int n = memberBuildList.Count - 1; n >= 0; n--)
			{
				var memberLinkedInfo = memberBuildList[n];

				if(memberLinkedInfo == null)
				{
					#if DEV_MODE
					Debug.LogWarning(ToString()+ ".memberBuildList["+n+"] was null. memberBuildList.Count="+memberBuildList.Count+", members.Length="+members.Length);
					#endif
					continue;
				}

				// IMPORTANT: Don't dispose member's LinkedMemberInfo if it is the same as this drawer's LinkedMemberInfo.
				// Otherwise the parent's LinkedMemberInfo.Data could go null.
				if(memberLinkedInfo == memberInfo)
				{
					continue;
				}

				hierarchy.Dispose(ref memberLinkedInfo);
			}
			memberBuildList.Clear();
		}

		/// <inheritdoc/>
		public virtual void RebuildMemberBuildListAndMembers()
		{
			if(!RebuildingMembersAllowed)
			{
				#if DEV_MODE
				Debug.LogError(ToString()+ ".RebuildMemberBuildListAndMembers called with RebuildingMembersAllowed=" + StringUtils.False+". Aborting.");
				#endif
				return;
			}

			DisposeMembers();
			RebuildMemberBuildList();
			BuildMembers();
		}

		/// <summary>
		/// Disposes all members and rebuilds them. This usually happens based on memberBuildList contents,
		/// so GenerateMemberBuildList should always be called before this method.
		/// This does NOT rebuild the member build list.
		/// </summary>
		protected virtual void RebuildMembers()
		{
			if(!RebuildingMembersAllowed)
			{
				#if DEV_MODE
				Debug.LogError(ToString()+ ".RebuildMembers called with RebuildingMembersAllowed=" + StringUtils.False+". Aborting.");
				#endif
				return;
			}

			DisposeMembers();
			BuildMembers();
		}

		/// <summary>
		/// Populates the members array with member drawer using information provided by the memberBuildList.
		/// Also should handle resizing the members array to the correct length, when necessary.
		/// GenerateMemberBuildList should always get called before this.
		/// </summary>
		protected void BuildMembers()
		{
			#if DEV_MODE
			Debug.Assert(memberBuildState == MemberBuildState.BuildListGenerated, ToString() + ".BuildMembers was called but memberBuildState was " + memberBuildState);
			Debug.Assert(!DrawInSingleRow || Unfolded);
			#endif
			
			memberBuildState = MemberBuildState.MembersBuilt;

			DoBuildMembers();

			ParentDrawerUtility.SendOnParentAssignedEvents(this);

			OnAfterMembersBuilt();
		}

		/// <summary>
		/// The main body of the BuildMembers method, actually handling building the member build list.
		/// This should never be called by inheriting classes directly, BuildMembers should be used instead!
		/// </summary>
		protected virtual void DoBuildMembers()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(!Selected && SelfOrMembersSelected())
			{
				Debug.LogWarning(ToString()+".DoBuildMembers was called while a member was selected. DrawGUI.EditingTextField="+ StringUtils.ToColorizedString(DrawGUI.EditingTextField));
			}
			#endif

			ParentDrawerUtility.BuildMembers(DrawerProvider, this, memberBuildList, ref members);
		}

		/// <inheritdoc/>
		public virtual void SetMembers(IDrawer[] setMembers, bool sendVisibilityChangedEvents = true)
		{
			ParentDrawerUtility.SetMembers(this, ref members, setMembers, ref visibleMembers, ref memberBuildState, ref assumeVisibleMembersChangedDuringNextUpdateVisibleMembers, updateCachedValuesFor, sendVisibilityChangedEvents);
		}

		/// <inheritdoc/>
		public virtual void SetMembers(IDrawer[] setMembers, IDrawer[] setVisibleMembers, bool sendVisibilityChangedEvents = true)
		{
			ParentDrawerUtility.SetMembers(this, ref members, setMembers, ref visibleMembers, setVisibleMembers, ref memberBuildState, updateCachedValuesFor, true);
		}

		/// <inheritdoc/>
		public virtual void SetVisibleMembers(IDrawer[] newVisibleMembers, bool sendVisibilityChangedEvents = true)
		{
			ParentDrawerUtility.SetVisibleMembers(this, ref visibleMembers, newVisibleMembers, updateCachedValuesFor, sendVisibilityChangedEvents, true);
		}

		/// <inheritdoc/>
		public virtual void OnAfterMembersBuilt()
		{
			UpdateVisibleMembers();

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Unfolded || !DrawInSingleRow, ToString() + " !Unfolded && DrawInSingleRow");
			#endif
		}

		/// <inheritdoc cref="IDrawer.OnFilterChanged" />
		public override void OnFilterChanged(SearchFilter filter)
		{
			ParentDrawerUtility.OnFilterChanged(this, filter, base.OnFilterChanged);
		}

		/// <inheritdoc/>
		public virtual void UpdateVisibleMembers()
		{
			if(ParentDrawerUtility.UpdateVisibleMembers(this) || assumeVisibleMembersChangedDuringNextUpdateVisibleMembers)
			{
				OnVisibleMembersChanged();
				OnChildLayoutChanged();
			}
			
			#if DEV_MODE && DEBUG_UPDATE_VISIBLE_MEMBERS
			Debug.Log(ToString()+".UpdateVisibleMembers() - "+visibleMembers.Length+" of " + members.Length + " now visible\nvisibleMembers=" + StringUtils.ToString(visibleMembers) + "\nmembers=" + StringUtils.ToString(members) + "\nUnfolded=" + Unfolded+ ", MembersAreVisible="+ MembersAreVisible);
			#endif
		}
		
		/// <summary>
		/// Called whenever the layout of any child or grand-child has changed.
		/// </summary>
		public virtual void OnChildLayoutChanged()
		{
			UpdateCachedValuesNeedUpdating();

			//don't send OnChildLayoutChanged events when this
			//member or its parent is still being built
			//(most probable reason for why they would be inactive)
			//the parent should obviously already know that the
			//child's layout is changing - it's being built!
			if(parent != null && !inactive && !parent.Inactive)
			{
				parent.OnChildLayoutChanged();
			}
		}

		/// <inheritdoc/>
		public virtual void OnVisibleMembersChanged()
		{
			#if DEV_MODE && DEBUG_UPDATE_VISIBLE_MEMBERS
			Debug.Log(ToString()+ ".OnVisibleMembersChanged - " + visibleMembers.Length+" of " + members.Length + " now visible\nvisibleMembers=" + StringUtils.ToString(visibleMembers) + "\nmembers=" + StringUtils.ToString(members) + "\nUnfolded=" + Unfolded+ ", MembersAreVisible="+ MembersAreVisible+", memberBuildState="+memberBuildState);
			#endif

			assumeVisibleMembersChangedDuringNextUpdateVisibleMembers = false;

			ArrayPool<Rect>.Resize(ref memberRects, DrawInSingleRow ? visibleMembers.Length : 0);
		}

		/// <inheritdoc/>
		public Type GetMemberType(int index)
		{
			try
			{
				if(memberBuildState == MemberBuildState.MembersBuilt)
				{
					if(members.Length > index)
					{
						return members[index].Type;
					}
				}
				else if(memberBuildState == MemberBuildState.BuildListGenerated)
				{
					if(memberBuildList.Count > index)
					{
						return memberBuildList[index] == null ? null : memberBuildList[index].Type;
					}
				}
			}
			catch(NullReferenceException)
			{
				#if DEV_MODE
				Debug.LogError(ToString()+ "GetTypeOfFirstMember() NullReferenceException with memberBuildState " + memberBuildState);
				#endif
			}

			return null;
		}

		/// <inheritdoc/>
		public object GetMemberValue(int index)
		{
			try
			{
				if(memberBuildState == MemberBuildState.MembersBuilt)
				{
					if(members.Length > index)
					{
						var memb = members[index];
						var memberMemberInfo = memb.MemberInfo;
						if(memberMemberInfo != null && memberMemberInfo.MixedContent)
						{
							return LinkedMemberInfo.MixedContentString;
						}
						return memb.GetValue();
					}
				}
				else if(memberBuildState == MemberBuildState.BuildListGenerated)
				{
					if(memberBuildList.Count > index)
					{
						var memberMemberInfo = memberBuildList[index];
						if(memberMemberInfo != null && memberMemberInfo.CanRead)
						{
							if(memberMemberInfo.MixedContent)
							{
								return LinkedMemberInfo.MixedContentString;
							}
							return memberMemberInfo.GetValue(0);
						}
					}
				}
			}
			catch(NullReferenceException)
			{
				#if DEV_MODE
				Debug.LogError(ToString()+ "GetTypeOfFirstMember() NullReferenceException with memberBuildState " + memberBuildState);
				#endif
			}

			return null;
		}

		/// <inheritdoc/>
		public virtual void OnMemberValueChanged(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			// temporary warning about this method being called when mixed content is true, because that use case is probably not yet perfectly handled everywhere
			AssertWarn(!MixedContent, this, ".OnMemberValueChanged was called but subject had mixed content.");
			AssertWarn(memberLinkedMemberInfo == null|| !memberLinkedMemberInfo.MixedContent, this, ".OnMemberValueChanged was called but memberLinkedMemberInfo had mixed content.");
			#endif

			#if DEV_MODE && DEBUG_ON_MEMBER_VALUE_CHANGED
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".OnMemberValueChanged(index=", StringUtils.ToString(memberIndex), ", value=", memberValue, ") with ValueWasJustSet=", ValueWasJustSet, ", inactive = ", inactive, ", MixedContent=", MixedContent, ", memberInfo.CanRead=" + (memberInfo == null ? StringUtils.Null : StringUtils.ToColorizedString(memberInfo.CanRead))+ ", memberLinkedMemberInfo.CanRead = " + (memberLinkedMemberInfo == null ? StringUtils.Null : StringUtils.ToColorizedString(memberLinkedMemberInfo.CanRead))));
			#endif
			
			if(ValueWasJustSet)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+".OnMemberValueChanged("+memberIndex+") ignored because valueWasJustSet is "+StringUtils.True+". This message can probably be removed.");
				#endif
				return;
			}

			if(inactive)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+".OnMemberValueChanged("+memberIndex+") ignored because inactive is "+StringUtils.True+". This message can probably be removed.");
				#endif
				return;
			}

			#if DEV_MODE
			if(ReadOnly)
			{
				Debug.LogWarning(ToString()+".OnMemberValueChanged was called for parent which was ReadOnly. This should not be possible - unless external scripts caused the value change.");
			}
			#endif
			
			bool tryToUpdateFromField;
			if(TryUpdateCachedValueFromFieldDuringOnMemberValueChanged() && memberLinkedMemberInfo != null && !memberLinkedMemberInfo.ParentChainIsBroken)
			{
				tryToUpdateFromField = true;
				memberInfo.GetHasMixedContentUpdated(); //make sure that MixedContent value is up-to-date, so value is guaranteed to get updated
			}
			else
			{
				tryToUpdateFromField = false;
			}

			if(tryToUpdateFromField && UpdateCachedValueFromField(false))
			{
				#if DEV_MODE && DEBUG_ON_MEMBER_VALUE_CHANGED
				Debug.Log(ToString()+ ".OnMemberValueChanged - successfully updated value through UpdateCachedValueFromField.");
				#endif
			}
			else if(TryToManuallyUpdateCachedValueFromMember(memberIndex, memberValue, memberLinkedMemberInfo))
			{
				#if DEV_MODE
				#if !DEBUG_ON_MEMBER_VALUE_CHANGED
				if(TryUpdateCachedValueFromFieldDuringOnMemberValueChanged())
				#endif
				{ Debug.LogWarning(ToString()+ ".OnMemberValueChanged - Could not update via LinkedMemberInfo but succesfully updated via TryToManuallyUpdateCachedValueFromMember."); }
				#endif
			}
			else
			{
				#if DEV_MODE
				Debug.LogError(ToString()+ ".OnMemberValueChanged - Failed to update cached value via fieldInfo or via TryToManuallyUpdateCachedValueFromMember");
				#endif
			}

			for(int n = members.Length - 1; n >= 0; n--)
			{
				if(n != memberIndex)
				{
					members[n].OnSiblingValueChanged(memberIndex, memberValue, memberLinkedMemberInfo);
				}
			}

			if(parent != null)
			{
				parent.OnMemberValueChanged(Array.IndexOf(parent.Members, this), Value, memberLinkedMemberInfo);
			}

			UpdateDataValidity(true);
			HasUnappliedChanges = GetHasUnappliedChangesUpdated();
		}

		/// <summary>
		/// Determines whether or not should try to update cached value from field during OnMemberValueChanged callback.
		/// </summary>
		/// <returns></returns>
		protected virtual bool TryUpdateCachedValueFromFieldDuringOnMemberValueChanged()
		{
			return memberInfo != null && memberInfo.CanRead;
		}

		/// <summary>
		/// Attempts to to manually update cached value from member at the given index.
		/// </summary>
		/// <param name="memberIndex"> Zero-based index of the member. </param>
		/// <param name="memberValue"> The member value. </param>
		/// <param name="memberLinkedMemberInfo">LinkedMemberInfo of changed member </param>
		/// <returns>
		/// True if it succeeds, false if it fails.
		/// </returns>
		protected virtual bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			var type = Type;
			bool memberHadLinkedMemberInfo = memberLinkedMemberInfo != null;

			// Sometimes there are instances where member value is the same as this value
			// e.g. for NullableDrawer, a member instruction handles drawing the field
			// and when its value changes, that should get passed up to the Nullable itself.
			var memberType = memberHadLinkedMemberInfo ? memberLinkedMemberInfo.Type : memberValue != null ? memberValue.GetType() : null;
			if(memberType == type)
			{
				// Value only needs to be applied to the field if member could not already do it (had no linked member info).
				// No need to update members, because changed member value is already up-to-date, and RebuildDrawerIfValueChanged is handled at the OnCachedValueChanged level.
				return DoSetValue((TValue)memberValue, !memberHadLinkedMemberInfo, false);
			}

			MemberInfo memberMemberInfo;
			if(memberLinkedMemberInfo != null)
			{
				memberMemberInfo = memberLinkedMemberInfo.MemberInfo;
			}
			else
			{
				memberMemberInfo = type.GetInspectorViewable(DebugMode, memberIndex);

				#if DEV_MODE && DEBUG_ON_MEMBER_VALUE_CHANGED
				Debug.Log(ToString()+ " Manually feched memberInfo "+StringUtils.ToString(memberMemberInfo)+" for members[" + memberIndex + "] "+ members[memberIndex] + " with memberValue " + StringUtils.ToString(memberValue)+ ", Type="+StringUtils.ToString(Type));
				#endif
			}
			
			// Possible issues if memberInfo is method, property with an indexer etc.?
			if(memberMemberInfo == null)
			{
				return false;
			}

			#if DEV_MODE && DEBUG_ON_MEMBER_VALUE_CHANGED
			Debug.Log(ToString()+ " Manually updating cached value from members["+ memberIndex + "] "+ members[memberIndex] + " value of "+StringUtils.ToString(memberValue)+ " via memberLinkedMemberInfo " + memberLinkedMemberInfo);
			#endif
			
			// Box a copy of value by casting it as object.
			// This way modifications done to it will stick even if it's a value type.
			object valueBoxed = Value;
			
			try
			{
				var field = memberMemberInfo as FieldInfo;
				if(field != null)
				{
					try
					{
						field.SetValue(valueBoxed, memberValue);
					}
					catch(ArgumentException e)
					{
						Debug.LogWarning(ToString()+ " of Type "+ StringUtils.ToString(Type) +" with memberField \""+field.Name+"\" of Type "+ StringUtils.ToString(field.FieldType)+" and valueBoxed "+StringUtils.ToString(valueBoxed)+" of type "+StringUtils.TypeToString(valueBoxed) + "\n" + e);
						return false;
					}
				}
				else
				{
					var property = memberMemberInfo as PropertyInfo;
					if(property != null && property.CanWrite)
					{
						if(property.GetIndexParameters().Length > 0)
						{
							#if DEV_MODE
							Debug.LogWarning(ToString()+ " failed to update via manually fetched property " + StringUtils.ToString(property) + " of members[" + memberIndex + "] " + members[memberIndex] + " with value " + StringUtils.ToString(memberValue)+" because property had index parameters "+StringUtils.ToString(property.GetIndexParameters()));
							#endif
							return false;
						}
						
						try
						{
							property.SetValue(valueBoxed, memberValue, null);
						}
						catch(ArgumentException e)
						{
							Debug.LogWarning(e);
							return false;
						}
					}
				}
			}
			catch(InvalidOperationException)
			{
				#if DEV_MODE
				Debug.Log(ToString()+ " failed to update via manually fetched memberInfo "+StringUtils.ToString(memberMemberInfo)+" of members[" + memberIndex + "] "+ members[memberIndex] + " with value "+StringUtils.ToString(memberValue));
				#endif
				return false;
			}

			// Value only needs to be applied to the field if member could not already do it (had no linked member info).
			// No need to update members, because changed member value is already up-to-date, and RebuildDrawerIfValueChanged is handled at the OnCachedValueChanged level.
			SetValue(valueBoxed, false, false);
			
			#if DEV_MODE //&& DEBUG_ON_MEMBER_VALUE_CHANGED
			Debug.Log(ToString()+".TryToManuallyUpdateCachedValueFromMember: valueBoxed=" + StringUtils.ToString(valueBoxed) + ", Value="+ StringUtils.ToString(Value));
			#endif
			return true;
		}

		/// <inheritdoc/>
		protected override void UpdatePrefixDrawer()
		{
			var selected = Selected;
			var mouseovered = Mouseovered;
			var unappliedChanges = HasUnappliedChanges;

			if(DrawInSingleRow)
			{
				if(prefixLabelDrawer != null)
				{
					prefixLabelDrawer.Dispose();
				}
				prefixLabelDrawer = PrefixDrawer.CreateLabel(label, selected, mouseovered, unappliedChanges);
			}
			else
			{
				var unfolded = Unfolded;
				var clipToColumnWidth = PrefixLabelClippedToColumnWidth;

				if(prefixLabelDrawer != null)
				{
					prefixLabelDrawer.Dispose();
				}
				prefixLabelDrawer = PrefixDrawer.CreateFoldout(label, selected, mouseovered, unappliedChanges, unfolded, clipToColumnWidth);
			}

			#if DEV_MODE
			Debug.Assert(prefixLabelDrawer != null, ToString()+ " prefixLabelDrawer null after UpdatePrefixDrawer!\nDrawInSingleRow="+ DrawInSingleRow+ ", Foldable="+ Foldable);
			#endif
		}

		/// <inheritdoc cref="IDrawer.UpdateCachedValuesFromFieldsRecursively" />
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			if(inactive)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+ ".UpdateCachedValuesFromFieldsRecursively called while inactive was true");
				for(var logParent = parent; logParent != null; logParent = logParent.Parent)
				{
					Debug.LogWarning("which is child of: "+logParent);
				}
				#endif
				return;
			}

			// Update own cached value from field.
			// NOTE: This should occur before UpdateMemberCachedValuesRecursively, so that if value have become null can dispose members.
			if(UpdateCachedValueFromField(RebuildDrawersIfValueChanged) && RebuildDrawersIfValueChanged)
			{
				return;
			}

			UpdateMemberCachedValuesRecursively();
		}
		
		/// <summary> Updates the cached values for member drawer recursively. </summary>
		protected virtual void UpdateMemberCachedValuesRecursively()
		{
			int n = updateCachedValuesFor.Count - 1;
			try
			{
				for(; n >= 0; n--)
				{
					updateCachedValuesFor[n].UpdateCachedValuesFromFieldsRecursively();
				}
			}
			#if DEV_MODE
			catch(NullReferenceException e)
			{
				Debug.LogWarning("UpdateCachedValuesFromFieldsRecursively() NullReferenceException " + ToString()+ " @ updateCachedValuesFor[" + n + "]\n" + e);
			#else
			catch(NullReferenceException)
			{
			#endif
			}
			#if DEV_MODE
			catch(IndexOutOfRangeException e)
			{
				Debug.LogWarning("UpdateCachedValuesFromFieldsRecursively() IndexOutOfRangeException " + ToString()+ " @ updateCachedValuesFor[" + n + "]\n" + e);
			#else
			catch(IndexOutOfRangeException)
			{
			#endif
			}
		}

		/// <summary>
		/// Folds or unfolds expandable parent drawer.
		/// </summary>
		/// <param name="setUnfolded"> True if should unfold, false if should fold. </param>
		public void SetUnfolded(bool setUnfolded)
		{
			SetUnfolded(setUnfolded, false);
		}

		/// <summary>
		/// Folds or unfolds expandable parent drawer.
		/// </summary>
		/// <param name="setUnfolded"> True if should unfold, false if should fold. </param>
		/// <param name="setChildrenAlso"> True to also fold/unfold children recursively. </param>
		public void SetUnfolded(bool setUnfolded, bool setChildrenAlso)
		{
			if((Unfolded != setUnfolded || setChildrenAlso) && (!DrawInSingleRow || setUnfolded))
			{
				#if DEV_MODE && DEBUG_SET_UNFOLDED
				if(GetType() == typeof(NullableDrawer))
				Debug.Log(StringUtils.ToColorizedString(ToString(), ".SetUnfolded = ", setUnfolded, " (was: ", Unfolded, ") with setChildrenAlso=", setChildrenAlso+ ", DrawInSingleRow=", DrawInSingleRow, ", Foldable=", Foldable));
				#endif

				ParentDrawerUtility.SetUnfolded(this, setUnfolded, setChildrenAlso);
			}
			#if DEV_MODE && DEBUG_SET_UNFOLDED
			else { Debug.Log(StringUtils.ToColorizedString(ToString(), " ignoring SetUnfolded = ", setUnfolded, " (was: ", Unfolded, ") with DrawInSingleRow=", DrawInSingleRow + ", Foldable=", Foldable)); }
			#endif
		}

		/// <summary>
		/// Folds or unfolds expandable parent drawer instantly without animating it over time.
		/// </summary>
		/// <param name="setUnfolded"> True if should unfold, false if should fold. </param>
		protected void SetUnfoldedInstantly(bool setUnfolded)
		{
			SetUnfolded(setUnfolded, false);
			unfoldedness.SetValueInstant(setUnfolded);

			#if DEV_MODE && PI_ASSERTATIONS
			if(Unfolded != setUnfolded)
			{
				Debug.LogError(Msg(ToString()+".SetUnfoldedInstantly(", setUnfolded, ") Unfolded still ", Unfolded, "! DrawInSingleRow=", DrawInSingleRow, ", Foldable=", Foldable, ", memberInfo=", memberInfo));
			}
			#endif
		}

		/// <inheritdoc cref="IDrawer.GetOptimalPrefixLabelWidth" />
		public override float GetOptimalPrefixLabelWidth(int indentLevel)
		{
			return ParentDrawerUtility.GetOptimalPrefixLabelWidth(this, indentLevel, !PrefixLabelClippedToColumnWidth);
		}

		/// <inheritdoc cref="IDrawer.ApplyInChildren" />
		public override void ApplyInChildren(Action<IDrawer> action)
		{
			int i = members.Length - 1;
			bool memberThrewException = false;
			do
			{
				try
				{
					while(i >= 0)
					{
						members[i]?.ApplyInChildren(action);
						i--;
					}

					memberThrewException = false;
				}
				#if DEV_MODE
				catch(Exception e)
				{			
					Debug.LogError(ToString() + " ApplyInChildren - members[" + i + "] " + e);
				#else
				catch(Exception)
                {
				#endif
					i = Mathf.Min(i - 1, members.Length - 1);
					memberThrewException = true;
				}
			}
			while(memberThrewException);

			action(this);
		}

		/// <inheritdoc cref="IDrawer.ApplyInVisibleChildren" />
		public override void ApplyInVisibleChildren(Action<IDrawer> action)
		{
			for(int n = visibleMembers.Length - 1; n >= 0; n--)
			{
				visibleMembers[n]?.ApplyInVisibleChildren(action);
			}

			action(this);
		}

		/// <inheritdoc/>
		protected override bool GetDataIsValidUpdated()
		{
			for(int n = members.Length - 1; n >= 0; n--)
			{
				var member = members[n];
				if(member != null && !member.DataIsValid)
				{
					return false;
				}
			}

			return true;
		}

		/// <inheritdoc cref="IParentDrawer.TestChildrenUntilTrue" />
		public override IDrawer TestChildrenUntilTrue(Func<IDrawer, bool> test)
		{
			var result = ParentDrawerUtility.TestChildrenUntilTrue(test, this);
			if(result == null)
			{
				if(test(this))
				{
					return this;
				}
			}
			return result;
		}
		
		/// <inheritdoc cref="IParentDrawer.TestVisibleChildrenUntilTrue" />
		public override IDrawer TestVisibleChildrenUntilTrue(Func<IDrawer, bool> test)
		{
			var result = ParentDrawerUtility.TestVisibleChildrenUntilTrue(test, this);
			if(result == null)
			{
				if(test(this))
				{
					return this;
				}
			}
			return result;
		}

		/// <inheritdoc/>
		protected override Rect PrefixLabelPosition
		{
			get
			{
				return labelLastDrawPosition;
			}
		}

		/// <inheritdoc/>
		public override bool PassesSearchFilter(SearchFilter filter)
		{
			return ParentDrawerUtility.PassesSearchFilter(this, filter, SelfPassesSearchFilter);
		}

		/// <inheritdoc/>
		public virtual bool SelfPassesSearchFilter(SearchFilter filter)
		{
			return base.PassesSearchFilter(filter);
		}

		/// <inheritdoc/>
		protected override void GetDrawPositions(Rect position)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(position.width > 0f, ToString()+".GetDrawPositions position width <= 0f: "+position);
			#endif

			lastDrawPosition = position;
			lastDrawPosition.height = HeaderHeight;

			if(DrawInSingleRow)
			{
				lastDrawPosition.GetLabelAndControlRects(label, out labelLastDrawPosition, out bodyLastDrawPosition);
				bodyLastDrawPosition.GetSingleRowControlRects(visibleMembers, ref memberRects);
			}
			else
			{
				labelLastDrawPosition = lastDrawPosition;

				bodyLastDrawPosition = lastDrawPosition;
				bodyLastDrawPosition.y += lastDrawPosition.height;

				DrawGUI.AddMarginsAndIndentation(ref labelLastDrawPosition);

				if(PrefixLabelClippedToColumnWidth)
				{
					Rect overrideLabelMaxX;
					Rect ignore;
					lastDrawPosition.GetLabelAndControlRects(label, out overrideLabelMaxX, out ignore);
					labelLastDrawPosition.xMax = overrideLabelMaxX.xMax;
				}

				labelLastDrawPosition.x -= foldoutArrowSize;
				labelLastDrawPosition.width += foldoutArrowSize;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			if(labelLastDrawPosition.width <= 0f)
			{
				if(DrawGUI.PrefixLabelWidth <= DrawGUI.LeftPadding)
				{
					Debug.LogWarning(ToString()+".GetDrawPositions labelLastDrawPosition width <= 0f: " + labelLastDrawPosition);
				}
				else
				{
					Debug.LogError(ToString()+".GetDrawPositions labelLastDrawPosition width <= 0f: " + labelLastDrawPosition);
				}
			}
			Debug.Assert(labelLastDrawPosition.height > 0f, ToString()+".GetDrawPositions labelLastDrawPosition height <= 0f: "+labelLastDrawPosition);
			Debug.Assert(bodyLastDrawPosition.width > 0f, ToString()+".GetDrawPositions bodyLastDrawPosition width <= 0f: "+bodyLastDrawPosition);
			Debug.Assert(bodyLastDrawPosition.height > 0f, ToString()+".GetDrawPositions bodyLastDrawPosition height <= 0f: "+bodyLastDrawPosition);
			#endif

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}

		/// <inheritdoc cref="IDrawer.Draw" />
		public override bool Draw(Rect position)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(position.width > 0f, position);
			#endif

			if(Event.current.type == EventType.Layout)
			{
				OnLayoutEvent(position);
			}
			else if(labelLastDrawPosition.height <= 0f)
			{
				GetDrawPositions(position);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(PrefixLabelPosition.width > 0f || DrawGUI.PrefixLabelWidth <= DrawGUI.LeftPadding + DrawGUI.IndentLevel * DrawGUI.IndentWidth, ToString()+".PrefixLabelPosition.width <= 0f: "+PrefixLabelPosition+ " with Event=" + Event.current.type);
			Debug.Assert(ControlPosition.width > 0f, ToString()+".ControlPosition.width <= 0f: "+ControlPosition+" with Event="+Event.current.type);
			#endif

			bool dirty = DrawPrefix(PrefixLabelPosition);

			if(DrawBody(ControlPosition))
			{
				dirty = true;
			}

			#if DEV_MODE && DEBUG_VISUALIZE_BOUNDS
			if(Event.current.control && Event.current.type == EventType.Repaint)
			{
				var color = Color.cyan;
				color.a = 0.25f;
				position = PrefixLabelPosition;
				position.x += 1f;
				position.y += 1f;
				position.width -= 2f;
				position.height -= 2f;
				DrawGUI.Active.ColorRect(position, color);

				//var color = Color.magenta;
				//color.a = 0.5f;
				//DrawGUI.Active.ColorRect(PrefixLabelPosition, color);

				//color = Color.cyan;
				//color.a = 0.5f;
				//DrawGUI.Active.ColorRect(ControlPosition, color);
			}
			#endif

			return dirty;
		}

		/// <inheritdoc/>
		public override bool DrawPrefix(Rect position)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(position.width > 0f || DrawGUI.PrefixLabelWidth <= DrawGUI.LeftPadding, ToString()+ ".DrawPrefix position.width <= 0f: " + position);
			#endif

			#if SAFE_MODE || DEV_MODE
			if(prefixLabelDrawer == null)
			{
				#if DEV_MODE
				Debug.Log(ToString() + ".DrawPrefix - prefixLabelDrawer under parent "+(parent == null ? "null" : parent.ToString())+" was null!");
				#endif
				return false;
			}
			#endif

			bool clipped = PrefixLabelClippedToColumnWidth;
			var prefixDrawRect = position;
			if(clipped)
			{
				// use BeginArea to prevent clipping with the column resizer
				var clipLabelInsideRect = prefixDrawRect;
				clipLabelInsideRect.width += clipLabelInsideRect.x;
				clipLabelInsideRect.x = 0f;
				prefixDrawRect.y = 0f;
				GUILayout.BeginArea(clipLabelInsideRect);
			}

			if(!DrawInSingleRow)
			{
				prefixDrawRect.x -= foldoutArrowSize;
				prefixDrawRect.width += foldoutArrowSize;
			}

			if(lastPassedFilterTestType != FilterTestType.None)
			{
				prefixLabelDrawer.Draw(prefixDrawRect, Inspector.State.filter, FullClassName, lastPassedFilterTestType);
			}
			else
			{
				prefixLabelDrawer.Draw(prefixDrawRect);
			}
			
			if(clipped)
            {
				GUILayout.EndArea();
            }

			if(!DrawInSingleRow && InspectorUtility.Preferences.enableTooltipIcons && label.tooltip.Length > 0)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(lastDrawPosition.width.Equals(DrawGUI.GetCurrentDrawAreaWidth()), "GetLabelAndControlRects : lastDrawPosition.width ("+lastDrawPosition.width+") != DrawGUI.GetCurrentDrawAreaWidth() ("+DrawGUI.GetCurrentDrawAreaWidth()+"). position="+position);
				#endif

				// NOTE: using lastDrawPosition instead of position intentionally to get correct results from GetLabelAndControlRects
				Rect ignoredLabelRect;
				Rect hintIconRect;
				lastDrawPosition.GetLabelAndControlRects(out ignoredLabelRect, out hintIconRect);
				hintIconRect.y = position.y;
				hintIconRect.width = DrawGUI.SingleLineHeight;
				DrawGUI.Active.HintIcon(hintIconRect, label.tooltip);
			}

			return false;
		}

		/// <inheritdoc/>
		public override bool DrawBody(Rect position)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(position.width > 0f, ToString()+".DrawBody called with position "+position);
			#endif
			return ParentDrawerUtility.DrawBody(this, position);
		}

		/// <inheritdoc/>
		public virtual bool DrawBodySingleRow(Rect position)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(position.width > 0f, ToString()+".DrawBodySingleRow called with position "+position);
			#endif

			DrawDragBarIfReorderable();

			return ParentDrawerUtility.DrawBodySingleRow(this, memberRects);
		}

		protected void DrawDragBarIfReorderable()
		{
			if(IsReorderable)
			{
				GUI.Label(DragBarPosition, GUIContent.none, InspectorPreferences.Styles.DragHandle);
			}
		}

		/// <inheritdoc/>
		public virtual bool DrawBodyMultiRow(Rect position)
		{
			return ParentDrawerUtility.DrawBodyMultiRow(this, position);
		}

		/// <inheritdoc cref="IDrawer.OnKeyboardInputGiven" />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			if(ParentDrawerUtility.HandleKeyboardInput(this, inputEvent, keys))
			{
				return true;
			}
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc cref="IDrawer.OnMouseover" />
		public override void OnMouseover()
		{
			base.OnMouseover();

			if(foldArrowMouseovered)
			{
				DrawGUI.Active.SetCursor(MouseCursor.Link);
			}
			else if(IsReorderable)
			{
				DrawGUI.Active.SetCursor(MouseCursor.MoveArrow);
			}
			
			#if CHANGE_CURSOR_ON_MOUSEOVER
			if(!DrawInSingleRow && (!IsReorderable || foldArrowMouseovered))
			{
				if(Selected || InspectorUtility.Preferences.changeFoldedStateOnFirstClick)
				{
					DrawGUI.Active.AddCursorRect(lastDrawPosition, MouseCursor.Link);
				}
				else
				{
					DrawGUI.Active.AddCursorRect(FoldArrowPosition, MouseCursor.Link);
				}
			}
			#endif
		}

		/// <inheritdoc/>
		protected override void OnLayoutEvent(Rect position)
		{
			base.OnLayoutEvent(position);
			foldArrowMouseovered = Foldable && Mouseovered && FoldArrowPosition.Contains(Event.current.mousePosition);
		}

		/// <inheritdoc cref="IDrawer.OnClick" />
		public override bool OnClick(Event inputEvent)
		{
			#if DEV_MODE && DEBUG_ON_CLICK
			Debug.Log(ToString()+".OnClick("+StringUtils.ToString(inputEvent) + ") with Foldable=" + StringUtils.ToColorizedString(Foldable) + ", foldArrowMouseovered=" + StringUtils.ToColorizedString(foldArrowMouseovered) + ", wasSelectedOnClickStart=" + StringUtils.ToString(wasSelectedOnClickStart));
			#endif

			wasSelectedOnClickStart = Selected;
			HandleOnClickSelection(inputEvent, ReasonSelectionChanged.PrefixClicked);
			return false;
		}

		/// <inheritdoc cref="IDrawer.OnMouseUpAfterDownOverControl" />
		public override void OnMouseUpAfterDownOverControl(Event inputEvent, bool isClick)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!isClick || Inspector.MouseoveredPart != InspectorPart.None);
			#endif

			#if DEV_MODE && DEBUG_ON_MOUSE_UP
			Debug.Log(StringUtils.ToColorizedString("isClick=", isClick,  ", Foldable=", Foldable, ", foldArrowMouseovered=", foldArrowMouseovered, ", wasSelectedOnClickStart=", wasSelectedOnClickStart, ", changeFoldedStateOnFirstClick=", InspectorUtility.Preferences.changeFoldedStateOnFirstClick, ", MouseDownEventWasUsed=", Inspector.Manager.MouseDownInfo.MouseDownEventWasUsed));
			#endif

			if(isClick && Foldable && !Inspector.Manager.MouseDownInfo.MouseDownEventWasUsed)
			{
				if(foldArrowMouseovered || wasSelectedOnClickStart || Inspector.Preferences.changeFoldedStateOnFirstClick)
				{
					DrawGUI.Use(inputEvent);
					SetUnfolded(!Unfolded, Event.current.alt);

					ExitGUIUtility.ExitGUI();
					return;
				}
			}

			base.OnMouseUpAfterDownOverControl(inputEvent, isClick);
		}

		/// <inheritdoc/>
		protected override bool ResetOnDoubleClick()
		{
			return base.ResetOnDoubleClick() && DrawInSingleRow;
		}

		/// <inheritdoc cref="IDrawer.OnDoubleClick" />
		public override bool OnDoubleClick(Event inputEvent)
		{
			if(ResetOnDoubleClick())
			{
				#if DEV_MODE
				if(!Event.current.control)
				{
					if(!DrawGUI.Active.DisplayDialog("Confirm Reset Field Value", "Do you want to reset the value of field \""+GetFieldNameForMessages()+"\"?\n\nHINT: You can hold down control when double clicking a field to skip seeing this message.", "Reset", "Cancel"))
					{
						return false;
					}
				}
				#endif

				Reset();
				
				DrawGUI.Use(inputEvent);
				return true;
			}
			return false;
		}

		/// <inheritdoc cref="IDrawer.GetNextSelectableDrawerUp" />
		public override IDrawer GetNextSelectableDrawerUp(int column, IDrawer requester)
		{
			return ParentDrawerUtility.GetNextSelectableDrawerUp(this, column, requester, MembersListedVertically);
		}

		/// <inheritdoc cref="IDrawer.GetNextSelectableDrawerDown" />
		public override IDrawer GetNextSelectableDrawerDown(int column, IDrawer requester)
		{
			return ParentDrawerUtility.GetNextSelectableDrawerDown(this, column, requester, MembersListedVertically);
		}

		/// <inheritdoc cref="IDrawer.GetNextSelectableDrawerLeft" />
		public override IDrawer GetNextSelectableDrawerLeft(bool moveToNextControlAfterReachingEnd, IDrawer requester)
		{
			return ParentDrawerUtility.GetNextSelectableDrawerLeft(this, moveToNextControlAfterReachingEnd, requester, MembersListedVertically);
		}

		/// <inheritdoc cref="IDrawer.GetNextSelectableDrawerRight" />
		public override IDrawer GetNextSelectableDrawerRight(bool moveToNextControlAfterReachingEnd, IDrawer requester)
		{
			return ParentDrawerUtility.GetNextSelectableDrawerRight(this, moveToNextControlAfterReachingEnd, requester, MembersListedVertically);
		}

		/// <inheritdoc cref="IDrawer.OnBecameInvisible" />
		public override void OnBecameInvisible()
		{
			labelLastDrawPosition.width = 0f;
			bodyLastDrawPosition.width = 0f;
			base.OnBecameInvisible();
		}

		/// <inheritdoc cref="IDrawer.OnSelfOrParentBecameVisible" />
		public override void OnSelfOrParentBecameVisible()
		{
			if(memberBuildState != MemberBuildState.MembersBuilt && MembersAreVisible)
			{
				if(memberBuildState == MemberBuildState.Unstarted)
				{
					GenerateMemberBuildList();
				}
				BuildMembers();
			}
		}
		
		#if DEV_MODE
		/// <inheritdoc/>
		protected override void AddDevModeDebuggingEntriesToRightClickMenu(ref Menu menu)
		{
			base.AddDevModeDebuggingEntriesToRightClickMenu(ref menu);

			menu.Add("Debugging/List Members", DebugListMembers);
			menu.Add("Debugging/List Member Values", DebugListMemberValues);
			menu.Add("Debugging/Rebuild Members", RebuildMembers);

			foreach(var member in members)
			{
				menu.Add("Debugging/Print Member Dev Info/"+member.ToString(), ()=>typeof(BaseDrawer).GetMethod("PrintDevInfo", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(member, null));
			}

			foreach(var member in members)
			{
				menu.Add("Debugging/Print Member Full State/" + member.ToString(), () => typeof(BaseDrawer).GetMethod("PrintFullStateForDevs", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(member, null));
			}
		}

		/// <inheritdoc/>
		protected override object[] GetDevInfo()
		{
			return base.GetDevInfo().Add(", Unfolded=", Unfolded, ", MembersAreVisible=", MembersAreVisible, ", DrawInSingleRow=", DrawInSingleRow, ", memberBuildState=", memberBuildState);
		}

		/// <summary>
		/// Debug list members.
		/// </summary>
		protected void DebugListMembers()
		{
			Debug.Log("Members: "+StringUtils.ToString(members));
			Debug.Log("Visible Members: "+StringUtils.ToString(visibleMembers));
		}

		/// <summary>
		/// Debug list member values.
		/// </summary>
		private void DebugListMemberValues()
		{
			string s = "Member Values: ";
			int i = 0;
			foreach(var member in members)
			{
				s += "\n"+i+" : "+StringUtils.ToString(member.GetValue());
				i++;
			}
			Debug.Log(s);
		}
		#endif

		/// <param name="mousePosition"></param>
		/// <inheritdoc cref="IDrawer.DetectMouseover" />
		public override bool DetectMouseover(Vector2 mousePosition)
		{
			return ParentDrawerUtility.DetectMouseover(this);
		}

		/// <param name="mousePosition"></param>
		/// <inheritdoc cref="IDrawer.DetectMouseoverForSelfAndChildren" />
		public override bool DetectMouseoverForSelfAndChildren(Vector2 mousePosition)
		{
			return ParentDrawerUtility.DetectMouseoverForSubjectAndChildren(this, mousePosition);
		}
		
		/// <inheritdoc/>
		public int GetMemberRowIndex(IDrawer member)
		{
			return ParentDrawerUtility.GetMemberRowIndex(this, member);
		}

		/// <inheritdoc cref="IDrawer.GetRowSelectableCount" />
		public override int GetRowSelectableCount()
		{
			return DrawInSingleRow ? visibleMembers.Length : 1;
		}

		/// <summary>
		/// Updates value of cachedValuesNeedUpdating based on whether or not cached values need updating.
		/// </summary>
		private void UpdateCachedValuesNeedUpdating()
		{
			UpdateCachedValuesForList();
			cachedValuesNeedUpdating = updateCachedValuesFor.Count > 0 || ShouldConstantlyUpdateCachedValues();
			
			#if DEV_MODE && DEBUG_CACHED_VALUES_NEED_UPDATING
			if(memberInfo == null)
			{
				Debug.Log(Msg(ToString(), ".cachedValuesNeedUpdating = ", cachedValuesNeedUpdating, ", with updateCachedValuesFor.Count=", updateCachedValuesFor.Count, ", ShouldConstantlyUpdateCachedValues()=", ShouldConstantlyUpdateCachedValues(), ", memberInfo=", null));
			}
			else
			{
				Debug.Log(Msg(ToString(), ".cachedValuesNeedUpdating = ", cachedValuesNeedUpdating, ", with updateCachedValuesFor.Count=", updateCachedValuesFor.Count, ", ShouldConstantlyUpdateCachedValues()=", ShouldConstantlyUpdateCachedValues(), ", memberInfo.CanRead=", memberInfo.CanRead,", memberInfo.ParentChainIsBroken=", memberInfo.ParentChainIsBroken));
			}
			#endif
		}

		/// <summary>
		/// Updates the cached values for list.
		/// </summary>
		private void UpdateCachedValuesForList()
		{
			updateCachedValuesFor.Clear();
			for(int n = visibleMembers.Length - 1; n >= 0; n--)
			{
				var member = visibleMembers[n];
				if(member.CachedValuesNeedUpdating)
				{
					updateCachedValuesFor.Add(member);
				}
			}
		}

		/// <summary>
		/// Determines if these drawer or any of its direct visible members
		/// are selected. Nested members will not be checked.
		/// </summary>
		/// <returns> True if self or any direct member is select, false if not. </returns>
		protected bool SelfOrMembersSelected()
		{
			if(inactive)
			{
				return false;
			}

			if(Selected)
			{
				return true;
			}

			for(int n = visibleMembers.Length - 1; n >= 0; n--)
			{
				var member = visibleMembers[n];
				if(member != null)
				{
					if(member.Selected)
					{
						return true;
					}
				}
				#if DEV_MODE && DEBUG_NULL_MEMBERS
				else {Debug.LogError(GetType().Name + " / \"" + Name + "\".SelfOrMembersSelected() - visibleMembers[" + n + "] was null with inactive="+inactive); }
				#endif
			}
			return false;
		}

		/// <inheritdoc cref="IDrawer.Randomize" />
		protected override void DoRandomize()
		{
			for(int n = MembersBuilt.Length - 1; n >= 0; n--)
			{
				members[n].Randomize(false);
			}
		}

		/// <inheritdoc/>
		protected override TValue GetRandomValue()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!ReadOnly);
			#endif

			#if DEV_MODE
			Debug.LogError("Randomize is not supported for " + ToString());
			#endif			

			return Value;
		}

		/// <inheritdoc cref="IDrawer.Dispose" />
		public override void Dispose()
		{
			inactive = true;
			
			ParentDrawerUtility.OnDisposing(Inspector, this);

			unfoldedness.SetValueInstant(false);
			ValueWasJustSet = false;
			
			cachedValuesNeedUpdating = false;
			updateCachedValuesFor.Clear();

			if(memberBuildState == MemberBuildState.MembersBuilt)
			{
				DisposeMembers();
			}
			if(memberBuildState == MemberBuildState.BuildListGenerated)
			{
				memberBuildList.Clear();
			}

			DrawerArrayPool.Resize(ref members, 0);
			DrawerArrayPool.Resize(ref visibleMembers, 0);

			ArrayPool<Rect>.Resize(ref memberRects, 0);

			memberBuildState = MemberBuildState.Unstarted;
			
			assumeVisibleMembersChangedDuringNextUpdateVisibleMembers = true;

			labelLastDrawPosition.y = -100f;
			labelLastDrawPosition.width = 0f;
			labelLastDrawPosition.height = 0f;
			bodyLastDrawPosition.y = -100f;
			bodyLastDrawPosition.width = 0f;
			bodyLastDrawPosition.height = 0f;

			base.Dispose();

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberBuildState == MemberBuildState.Unstarted);
			#endif
		}

		/// <summary>
		/// Disposes all members' drawer.
		/// Will not resize members or visibleMembers arrays.
		/// </summary>
		protected void DisposeMembers()
		{
			if(memberBuildState == MemberBuildState.MembersBuilt)
			{
				for(int n = members.Length - 1; n >= 0; n--)
				{
					if(members[n] != null)
					{
						members[n].Dispose();
						members[n] = null;
					}
				}
				memberBuildState = MemberBuildState.BuildListGenerated;
				cachedValuesNeedUpdating = false;
				updateCachedValuesFor.Clear();
				assumeVisibleMembersChangedDuringNextUpdateVisibleMembers = true;
			}
			#if DEV_MODE
			else
			{
				Debug.LogWarning(ToString()+".DisposeMembers called with memberBuildState "+memberBuildState);
			}
			#endif
		}

		/// <summary>
		/// Adds a menu items from attributes.
		/// </summary>
		/// <param name="menu">
		/// [in,out] The menu. </param>
		protected override void AddMenuItemsFromAttributes(ref Menu menu)
		{
			if(CanReadFromFieldWithoutSideEffects)
			{
				ParentDrawerUtility.AddMenuItemsFromContextMenuAttribute(GetValues(), ref menu);
			}
			base.AddMenuItemsFromAttributes(ref menu);
		}

		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(!DrawInSingleRow)
			{
				menu.AddSeparatorIfNotRedundant();

				bool unfolded = Unfolded;
				if(unfolded)
				{
					menu.Add("Unfolding/Unfold", () => SetUnfolded(true, false));
				}
				else
				{
					menu.Add("Unfolding/Fold", () => SetUnfolded(false, false));
				}
				
				bool childrenUnfoldable = false;
				if(memberBuildState == MemberBuildState.MembersBuilt)
				{
					for(int n = members.Length - 1; n >= 0; n--)
					{
						var memberParent = members[n] as IParentDrawer;
						if(memberParent != null && !memberParent.DrawInSingleRow)
						{
							childrenUnfoldable = true;
							break;
						}
					}
				}
				else
				{
					for(int n = memberBuildList.Count - 1; n >= 0; n--)
					{
						var memberInfo = memberBuildList[n];
						if(DrawerUtility.CanDrawInSingleRow(memberInfo.Type, DebugMode))
						{
							childrenUnfoldable = true;
							break;
						}
					}
				}

				if(childrenUnfoldable)
				{
					menu.Add("Unfolding/Unfold Children", () =>
					{
						SetUnfolded(true, true);
						SetUnfoldedInstantly(unfolded);
					});
					menu.Add("Unfolding/Fold Children", () =>
					{
						SetUnfolded(false, true);
						SetUnfoldedInstantly(unfolded);
					});
				}
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}


		/// <inheritdoc />
		protected override bool ClearKeyboardControlWhenPrefixClicked()
		{
			return true;
		}

		/// <inheritdoc />
		protected override bool ClearKeyboardControlWhenThisClicked()
		{
			return true;
		}

		/// <summary> Sets the values of all members. If members have not been built yet, builds them first. </summary>
		/// <param name="value"> The value to set for all members. </param>
		protected void SetMemberValues(object value)
		{
			var setMembers = MembersBuilt;
			for(int n = setMembers.Length - 1; n >= 0; n--)
			{
				setMembers[n].SetValue(value, true, true);
			}
		}

		/// <summary> Sets the values of members. If members have not been built yet, builds them first. </summary>
		/// <param name="values"> The values for members. </param>
		protected void SetMemberValues([NotNull]object[] values)
		{
			var setMembers = MembersBuilt;
			for(int n = setMembers.Length - 1; n >= 0; n--)
			{
				setMembers[n].SetValue(values[n], true, true);
			}
		}
	}
}