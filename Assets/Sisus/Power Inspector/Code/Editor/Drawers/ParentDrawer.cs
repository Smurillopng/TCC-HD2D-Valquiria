//#define ENABLE_SPREADING
#define DELAY_BUILDING_MEMBERS_UNTIL_UNFOLDED
#define DEBUG_NULL_MEMBERS
//#define DEBUG_UPDATE_CACHED_VALUES
//#define DEBUG_ON_MEMBER_VALUE_CHANGED
#define DEBUG_SET_VISIBLE_MEMBERS

using System;
using System.Collections.Generic;
using UnityEngine;
using Sisus.Newtonsoft.Json;

namespace Sisus
{
	/// <summary>
	/// Drawer with member IDrawer.
	/// </summary>
	/// <typeparam name="TMemberBuildList">
	/// Type of the member build list. </typeparam>
	[Serializable]
	public abstract class ParentDrawer<TMemberBuildList> : BaseDrawer, IParentDrawer
	{
		/// <summary>
		/// Information about member build state
		/// </summary>
		protected MemberBuildState memberBuildState = MemberBuildState.Unstarted;

		/// <summary>
		/// List of members to build once they become visible
		/// </summary>
		protected List<TMemberBuildList> memberBuildList = new List<TMemberBuildList>();
		
		/// <summary>
		/// All member drawers of the parent.
		/// </summary>
		protected IDrawer[] members = new IDrawer[0];

		/// <summary>
		/// The portion of members that are currenly visible in the inspector
		/// (passed search filter, parent is unfolded etc.)
		/// </summary>
		protected IDrawer[] visibleMembers = new IDrawer[0];

		/// <summary>
		/// Last position where prefix was drawn.
		/// </summary>
		protected Rect labelLastDrawPosition;

		/// <summary>
		/// Last position where the paren't body was drawn.
		/// </summary>
		protected Rect bodyLastDrawPosition;

		/// <summary>
		/// Targets whose cached values should be updated continuously
		/// </summary>
		protected readonly List<IDrawer> updateCachedValuesFor = new List<IDrawer>();

		/// <summary>
		/// True if cached values should be updated continuously for this or any of its members
		/// </summary>
		private bool cachedValuesNeedUpdating;

		/// <summary>
		/// Usully when UpdateVisibleMembers is called, it only invokes OnVisibleMembersChanged and OnChildLayoutChanged
		/// if the contents of the visible members array changed. This value can be used to force it to always invoke those
		/// methods no matter what during the next call.
		/// </summary>
		protected bool assumeVisibleMembersChangedDuringNextUpdateVisibleMembers = true;
		
		#if ENABLE_SPREADING
		private int lastUpdateCachedValuesMemberIndex = -1;
		#endif

		/// <inheritdoc/>
		public override Rect ControlPosition
		{
			get
			{
				return bodyLastDrawPosition;
			}
		}

		/// <inheritdoc cref="IDrawer.CachedValuesNeedUpdating" />
		public sealed override bool CachedValuesNeedUpdating
		{
			get
			{
				return cachedValuesNeedUpdating;
			}
		}

		/// <inheritdoc/>
		public bool MembersAreVisible
		{
			get
			{
				return Unfoldedness > 0f;
			}
		}

		/// <inheritdoc cref="IDrawer.RequiresConstantRepaint" />
		public override bool RequiresConstantRepaint
		{
			get
			{
				for(int n = visibleMembers.Length - 1; n >= 0; n--)
				{
					try
					{
						if(visibleMembers[n].RequiresConstantRepaint)
						{
							return true;
						}
					}
					catch(NullReferenceException)
					{
						#if DEV_MODE
						Debug.LogWarning(ToString()+".RequiresConstantRepaint NullReferenceException @ visibleMembers["+n+"]: "+StringUtils.ToString(visibleMembers[n]));
						#endif
						return false;
					}
				}
				return false;
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

		/// <inheritdoc cref="IDrawer.ClickToSelectArea" />
		public override Rect ClickToSelectArea
		{
			get
			{
				return labelLastDrawPosition;
			}
		}

		/// <inheritdoc cref="IDrawer.RightClickArea" />
		public override Rect RightClickArea
		{
			get
			{
				return labelLastDrawPosition;
			}
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
		public virtual int AppendIndentLevel
		{
			get
			{
				return 1;
			}
		}

		/// <inheritdoc/>
		public virtual bool DrawInSingleRow
		{
			get { return false; }
		}

		/// <summary>
		/// Indexer to get or set items within this collection using array index syntax.
		/// </summary>
		/// <param name="index">
		/// Zero-based index of the entry to access. </param>
		/// <returns>
		/// The indexed item.
		/// </returns>
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
				
				if(members[index] != null && !members[index].Inactive)
				{
					Debug.LogError(ToString()+"["+index+"] = "+StringUtils.ToString(value)+" called but member at index wasn't inactive!");
				}
				#endif
				
				members[index] = value;
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

			private set
			{
				if(members != value)
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(value != null);
					Debug.Assert(value != visibleMembers || value == null || value.Length == 0, ToString()+ " members array is same as visible members - This can lead to bugs!");
					Debug.Assert(!value.ContainsNullMembers());
					#endif

					//Don't dispose contents, since might contain same members as value, or visibleMembers
					DrawerArrayPool.Dispose(ref members, false);

					members = value;

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

		/// <inheritdoc/>
		public virtual float HeaderHeight
		{
			get
			{
				return DrawGUI.SingleLineHeight;
			}
		}

		/// <inheritdoc cref="IDrawer.Height" />
		public override float Height
		{
			get
			{
				return UnityObjectDrawerUtility.CalculateHeight(this);
			}
		}

		/// <inheritdoc/>
		public sealed override bool PassesSearchFilter(SearchFilter filter)
		{
			return ParentDrawerUtility.PassesSearchFilter(this, filter, SelfPassesSearchFilter);
		}

		/// <inheritdoc/>
		public virtual bool SelfPassesSearchFilter(SearchFilter filter)
		{
			return base.PassesSearchFilter(filter);
		}

		/// <inheritdoc cref="IDrawer.HasUnappliedChanges" />
		public override bool HasUnappliedChanges
		{
			get
			{
				if(memberBuildState != MemberBuildState.MembersBuilt)
				{
					return false;
				}

				for(int n = MembersBuilt.Length - 1; n >= 0; n--)
				{
					var memb = members[n];
					if(memb == null)
					{
						return base.HasUnappliedChanges;
					}

					if(memb.HasUnappliedChanges)
					{
						return true;
					}
				}
				return false;
			}
		}

		/// <inheritdoc cref="IDrawer.ShouldShowInInspector" />
		public override bool ShouldShowInInspector
		{
			get
			{
				return ParentDrawerUtility.ShouldShowInInspector(this, passedLastFilterCheck);
			}
		}

		/// <summary>
		/// Gets a value indicating whether rebuilding members is allowed.
		/// </summary>
		/// <value>
		/// True if rebuilding members allowed, false if not.
		/// </value>
		protected virtual bool RebuildingMembersAllowed
		{
			get
			{
				#if DELAY_BUILDING_MEMBERS_UNTIL_UNFOLDED
				return MembersAreVisible;
				#else
				return true;
				#endif
			}
		}

		/// <summary>
		/// True if unfoldedness can be controlled by clicking a foldout control.
		/// </summary>
		public virtual bool Foldable
		{
			get
			{
				return !DrawInSingleRow;
			}
		}

		/// <inheritdoc/>
		public abstract bool Unfolded
		{
			get;
			set;
		}

		/// <inheritdoc/>
		public virtual float Unfoldedness
		{
			get
			{
				return Unfolded ? 1f : 0f;
			}
		}

		/// <inheritdoc/>
		protected override void Setup(IParentDrawer setParent, GUIContent setLabel)
		{
			base.Setup(setParent, setLabel);
			GenerateMemberBuildList();
		}

		/// <summary>
		/// Populates memberBuildInfo list with LinkedMemberInfos for member drawers that should be built
		/// if/when the member drawers become visible in the inspector (usually when the parent is unfolded).
		/// This should be called only once between Create and Dispose being called, during the Setup phase.
		/// </summary>
		private void GenerateMemberBuildList()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(memberBuildState == MemberBuildState.BuildListGenerated)
			{
				Debug.LogError(Msg(ToString(), ".GenerateMemberBuildList was called but memberBuildState was ", memberBuildState));
			}
			#endif

			DoGenerateMemberBuildList();

			if(memberBuildState == MemberBuildState.Unstarted)
			{
				memberBuildState = MemberBuildState.BuildListGenerated;
			}

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
		protected virtual void OnAfterMemberBuildListGenerated() { }

		/// <inheritdoc cref="IDrawer.LateSetup" />
		public override void LateSetup()
		{
			if(RebuildingMembersAllowed && memberBuildState != MemberBuildState.MembersBuilt)
			{
				BuildMembers();
			}

			base.LateSetup();
		}

		/// <summary>
		/// Populates the members array with member drawers using information provided by the memberBuildList.
		/// Also should handle resizing the members array to the correct length, when necessary.
		/// GenerateMemberBuildList should always get called before this.
		/// </summary>
		protected void BuildMembers()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(memberBuildState == MemberBuildState.BuildListGenerated, ToString() + ".BuildMembers was called but memberBuildState was " + memberBuildState);
			if(memberBuildList.ContainsNullMembers())
			{
				Debug.LogError(ToString()+" memberBuildList had empty elements: "+StringUtils.ToString(memberBuildList));
			}
			#endif
			
			DoBuildMembers();

			#if DEV_MODE && PI_ASSERTATIONS
			if(Members.ContainsNullMembers())
			{
				Debug.LogError("buildState="+ memberBuildState+ ", DebugMode=" + DebugMode+", members:\n"+StringUtils.ToString(members,"\n"));
			}
			#endif

			memberBuildState = MemberBuildState.MembersBuilt;
			ParentDrawerUtility.SendOnParentAssignedEvents(this);
			OnAfterMembersBuilt();
		}

		/// <summary>
		/// The main body of the BuildMembers method, actually handling building the member build list.
		/// This should never be called by inheriting classes directly, BuildMembers should be used instead!
		/// </summary>
		protected abstract void DoBuildMembers();

		/// <inheritdoc/>
		public virtual void SetMembers(IDrawer[] setMembers, bool sendVisibilityChangedEvents = true)
		{
			ParentDrawerUtility.SetMembers(this, ref members, setMembers, ref visibleMembers, ref memberBuildState, ref assumeVisibleMembersChangedDuringNextUpdateVisibleMembers, updateCachedValuesFor, sendVisibilityChangedEvents);
		}
		
		/// <inheritdoc/>
		public void SetMembers(IDrawer[] setMembers, IDrawer[] setVisibleMembers, bool sendVisibilityChangedEvents = true)
		{
			ParentDrawerUtility.SetMembers(this, ref members, setMembers, ref visibleMembers, setVisibleMembers, ref memberBuildState, updateCachedValuesFor, sendVisibilityChangedEvents);
		}
		
		/// <inheritdoc/>
		public void SetVisibleMembers(IDrawer[] newVisibleMembers, bool sendVisibilityChangedEvents = true)
		{
			ParentDrawerUtility.SetVisibleMembers(this, ref visibleMembers, newVisibleMembers, updateCachedValuesFor, sendVisibilityChangedEvents, true);
		}

		/// <inheritdoc/>
		public virtual void OnAfterMembersBuilt()
		{
			UpdateVisibleMembers();
		}

		/// <inheritdoc/>
		public virtual void UpdateVisibleMembers()
		{
			#if DEV_MODE && DEBUG_UPDATE_VISIBLE_MEMBERS
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".UpdateVisibleMembers with inactive=", inactive));
			#endif

			if(ParentDrawerUtility.UpdateVisibleMembers(this) || assumeVisibleMembersChangedDuringNextUpdateVisibleMembers)
			{
				OnVisibleMembersChanged();
				OnChildLayoutChanged();
			}
		}

		/// <inheritdoc/>
		public virtual void OnVisibleMembersChanged()
		{
			assumeVisibleMembersChangedDuringNextUpdateVisibleMembers = false;
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
						return memberBuildList[index] == null ? null : GetMemberType(memberBuildList[index]);
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
						var memberInfo = memb.MemberInfo;
						if(memberInfo != null && memberInfo.MixedContent)
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
						return memberBuildList[index] == null ? null : GetMemberValue(memberBuildList[index]);
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
		
		/// <summary>
		/// Gets member type.
		/// </summary>
		/// <param name="memberBuildListItem">
		/// The member build list item. </param>
		/// <returns>
		/// The member type.
		/// </returns>
		protected abstract Type GetMemberType(TMemberBuildList memberBuildListItem);

		/// <summary>
		/// Gets value of member with the given memberBuildList element value.
		/// </summary>
		/// <param name="memberBuildListItem"> Element from the memberBuildList. </param>
		/// <returns>
		/// The value of the member representing the item.
		/// </returns>
		protected abstract object GetMemberValue(TMemberBuildList memberBuildListItem);

		/// <inheritdoc/>
		public virtual void OnChildLayoutChanged()
		{
			UpdateCachedValuesNeedUpdating();

			// Don't send OnChildLayoutChanged events when this member or its parent is currently being built
			// (the most probable reason for why they would be inactive). OnChildLayoutChanged should already
			// get called via BuildMembers / OnAfterMembersBuilt anyways.
			if(parent != null && !inactive && !parent.Inactive)
			{
				parent.OnChildLayoutChanged();
			}
		}

		/// <inheritdoc cref="IDrawer.OnFilterChanged" />
		public override void OnFilterChanged(SearchFilter filter)
		{
			ParentDrawerUtility.OnFilterChanged(this, filter, base.OnFilterChanged);
		}

		/// <summary>
		/// Updates value of cachedValuesNeedUpdating based on whether or not cached values need updating.
		/// </summary>
		private void UpdateCachedValuesNeedUpdating()
		{
			UpdateCachedValuesForList();
			cachedValuesNeedUpdating = updateCachedValuesFor.Count > 0 || ShouldConstantlyUpdateCachedValues();

			#if DEV_MODE && DEBUG_UPDATE_CACHED_VALUES
			Debug.Log(ToString() + " updateCachedValuesFor now: " + StringUtils.ToString(updateCachedValuesFor)+"\nvisibleMembers.Length="+visibleMembers.Length);
			#endif
		}

		/// <summary>
		/// Determine if we should constantly update cached values for these drawers.
		/// </summary>
		/// <returns>
		/// True if cached values need to be updated continously, false if external sources can't alter them.
		/// </returns>
		protected override bool ShouldConstantlyUpdateCachedValues()
		{
			return updateCachedValuesFor.Count > 0;
		}

		/// <summary>
		/// Updates contents of updateCachedValuesFor list with visible members whose CachedValuesNeedUpdating is true.
		/// </summary>
		protected virtual void UpdateCachedValuesForList()
		{
			updateCachedValuesFor.Clear();
			for(int n = visibleMembers.Length - 1; n >= 0; n--)
			{
				var member = visibleMembers[n];

				#if DEV_MODE && PI_ASSERTATIONS
				if(member.Inactive) { Debug.LogError(Msg(ToString(), " visibleMembers["+n+"] ", member, " inactive !!!!! parent=", Parent)); }
				#endif

				if(member.CachedValuesNeedUpdating)
				{
					updateCachedValuesFor.Add(member);
				}
			}
		}

		/// <inheritdoc cref="IDrawer.UpdateCachedValuesFromFieldsRecursively" />
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(ShouldConstantlyUpdateCachedValues() != cachedValuesNeedUpdating)
			{
				Debug.LogError(Msg(ToString(), ".UpdateCachedValuesFromFieldsRecursively with inactive=", inactive, ", ShouldConstantlyUpdateCachedValues()=", ShouldConstantlyUpdateCachedValues(), " != cachedValuesNeedUpdating=", cachedValuesNeedUpdating));
			}
			#endif
			
			int count = updateCachedValuesFor.Count;
			#if ENABLE_SPREADING
			lastUpdateCachedValuesMemberIndex++;
			if(lastUpdateCachedValuesMemberIndex >= count)
			{
				if(count == 0)
				{
					#if DEV_MODE
					Debug.LogError("UpdateCachedValuesFromFieldsRecursively was called for "+ToString()+ " but updateCachedValuesFor.Count was zero");
					#endif
					return;
				}
				lastUpdateCachedValuesMemberIndex = 0;
			}
			int updateCachedValuesIndex = lastUpdateCachedValuesMemberIndex;
			try
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!updateCachedValuesFor[lastUpdateCachedValuesMemberIndex].Inactive, ToString()+".updateCachedValuesIndex["+updateCachedValuesIndex+"] was inactive!!!");
				#endif
				updateCachedValuesFor[updateCachedValuesIndex].UpdateCachedValuesFromFieldsRecursively();
			}
			#else
			int updateCachedValuesIndex = count - 1;
			try
			{
				for(; updateCachedValuesIndex >= 0; updateCachedValuesIndex--)
				{
					#if DEV_MODE && PI_ASSERTATIONS
					if(updateCachedValuesFor[updateCachedValuesIndex].Inactive)
					{
						Debug.LogError(Msg(ToString(), ".updateCachedValuesIndex[" + updateCachedValuesIndex + "] ", updateCachedValuesFor[updateCachedValuesIndex], " was inactive!!!"));
					}
					#endif

					updateCachedValuesFor[updateCachedValuesIndex].UpdateCachedValuesFromFieldsRecursively();
				}
			}
			#endif
			catch(NullReferenceException)
			{
				#if DEV_MODE
				Debug.LogWarning("UpdateCachedValuesFromFieldsRecursively() NullReferenceException " + ToString()+ " @ updateCachedValuesFor[" + updateCachedValuesIndex +"]");
				#endif
			}
			catch(ArgumentOutOfRangeException)
			{
				#if DEV_MODE
				Debug.LogWarning("UpdateCachedValuesFromFieldsRecursively() IndexOutOfRangeException " + ToString()+ " @ updateCachedValuesFor[" + updateCachedValuesIndex + "]");
				#endif
			}
		}

		/// <inheritdoc cref="IDrawer.GetOptimalPrefixLabelWidth" />
		public override float GetOptimalPrefixLabelWidth(int indentLevel)
		{
			return ParentDrawerUtility.GetOptimalPrefixLabelWidth(this, indentLevel);
		}

		/// <inheritdoc cref="IDrawer.ApplyInChildren" />
		public override void ApplyInChildren(Action<IDrawer> action)
		{
			for(int n = MembersBuilt.Length - 1; n >= 0; n--)
			{
				try
				{
					members[n].ApplyInChildren(action);
				}
				catch(Exception e)
				{
					#if DEV_MODE
					Debug.LogError(ToString() + " ApplyInChildren - members[" + n + "] " + e);
					#endif

					if(ExitGUIUtility.ShouldRethrowException(e))
					{
						throw;
					}
				}
			}

			action(this);
		}

		/// <inheritdoc cref="IDrawer.ApplyInVisibleChildren" />
		public override void ApplyInVisibleChildren(Action<IDrawer> action)
		{
			for(int n = visibleMembers.Length - 1; n >= 0; n--)
			{
				try
				{
					visibleMembers[n].ApplyInVisibleChildren(action);
				}
				catch(Exception e)
				{
					#if DEV_MODE
					Debug.LogError(ToString() + " ApplyInVisibleChildren - visibleMembers[" + n + "] " + e);
					#endif

					if(ExitGUIUtility.ShouldRethrowException(e))
					{
						throw;
					}
				}
			}

			action(this);
		}

		/// <inheritdoc cref="IDrawer.TestChildrenUntilTrue" />
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

		/// <inheritdoc cref="IDrawer.TestVisibleChildrenUntilTrue" />
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
		protected override bool GetDataIsValidUpdated()
		{
			try
			{
				for(int n = members.Length - 1; n >= 0; n--)
				{
					if(!members[n].DataIsValid)
					{
						return false;
					}
				}
			}
			catch(NullReferenceException){ }

			return true;
		}

		/// <inheritdoc cref="IDrawer.Draw" />
		public override bool Draw(Rect position)
		{
			if(Event.current.type == EventType.Layout)
			{
				OnLayoutEvent(position);
			}
			else if(labelLastDrawPosition.height <= 0f)
			{
				GetDrawPositions(position);
			}

			bool dirty = DrawPrefix(PrefixLabelPosition);
			if(DrawBody(ControlPosition))
			{
				dirty = true;
			}
			return dirty;
		}

		/// <inheritdoc/>
		protected override void GetDrawPositions(Rect position)
		{
			lastDrawPosition = position;
			float totalHeight = Height;
			lastDrawPosition.height = totalHeight;

			lastDrawPosition = position;

			if(DrawInSingleRow)
			{
				lastDrawPosition.GetLabelAndControlRects(label, out labelLastDrawPosition, out bodyLastDrawPosition);
			}
			else
			{
				labelLastDrawPosition = position;
				float headerHeight = HeaderHeight;
				labelLastDrawPosition.height = headerHeight;

				bodyLastDrawPosition = position;
				bodyLastDrawPosition.y += headerHeight;
				bodyLastDrawPosition.height = totalHeight - headerHeight;
			}

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}

		/// <inheritdoc/>
		public override bool DrawBody(Rect position)
		{
			return ParentDrawerUtility.DrawBody(this, position);
		}

		/// <inheritdoc/>
		public virtual bool DrawBodySingleRow(Rect position)
		{
			return ParentDrawerUtility.DrawBodySingleRow(this, position);
		}

		/// <inheritdoc/>
		public virtual bool DrawBodyMultiRow(Rect position)
		{
			return ParentDrawerUtility.DrawBodyMultiRow(this, position);
		}

		/// <inheritdoc cref="IDrawer.DrawSelectionRect" />
		public override void DrawSelectionRect()
		{
			DrawGUI.DrawSelectionRect(SelectionRect, localDrawAreaOffset);
		}

		/// <inheritdoc/>
		public virtual void SetUnfolded(bool setUnfolded)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Foldable, ToString()+".SetUnfolded("+setUnfolded+") called but Foldable was false!");
			#endif

			SetUnfolded(setUnfolded, false);
		}

		/// <inheritdoc/>
		public virtual void SetUnfolded(bool setUnfolded, bool setChildrenAlso)
		{
			ParentDrawerUtility.SetUnfolded(this, setUnfolded, setChildrenAlso);
		}

		/// <inheritdoc />
		public virtual void OnMemberValueChanged(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			#if DEV_MODE && DEBUG_ON_MEMBER_VALUE_CHANGED
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".OnMemberValueChanged(index=", StringUtils.ToString(memberIndex), ", value=", memberValue, ") with inactive=", inactive, ", memberLinkedMemberInfo.CanRead = " + (memberLinkedMemberInfo == null ? StringUtils.Null : StringUtils.ToColorizedString(memberLinkedMemberInfo.CanRead))));
			#endif

			UpdateDataValidity(true);

			if(OnValueChanged != null)
			{
				OnValueChanged(this, ReadOnly ? null : GetValue(0));
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
				parent.OnMemberValueChanged(Array.IndexOf(parent.Members, this), GetValue(0), memberLinkedMemberInfo);
			}
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

		/// <inheritdoc cref="IDrawer.GetNextSelectableDrawerUp" />
		public override IDrawer GetNextSelectableDrawerUp(int column, IDrawer requester)
		{
			return ParentDrawerUtility.GetNextSelectableDrawerUp(this, column, requester, !DrawInSingleRow);
		}

		/// <inheritdoc cref="IDrawer.GetNextSelectableDrawerDown" />
		public override IDrawer GetNextSelectableDrawerDown(int column, IDrawer requester)
		{
			return ParentDrawerUtility.GetNextSelectableDrawerDown(this, column, requester, !DrawInSingleRow);
		}

		/// <inheritdoc cref="IDrawer.GetNextSelectableDrawerLeft" />
		public override IDrawer GetNextSelectableDrawerLeft(bool moveToNextControlAfterReachingEnd, IDrawer requester)
		{
			return ParentDrawerUtility.GetNextSelectableDrawerLeft(this, moveToNextControlAfterReachingEnd, requester, !DrawInSingleRow);
		}

		/// <inheritdoc cref="IDrawer.GetNextSelectableDrawerRight" />
		public override IDrawer GetNextSelectableDrawerRight(bool moveToNextControlAfterReachingEnd, IDrawer requester)
		{
			return ParentDrawerUtility.GetNextSelectableDrawerRight(this, moveToNextControlAfterReachingEnd, requester, !DrawInSingleRow);
		}

		/// <inheritdoc/>
		protected override void DoReset()
		{
			for(int n = MembersBuilt.Length - 1; n >= 0; n--)
			{
				members[n].Reset(false);
			}
		}

		/// <inheritdoc cref="IDrawer.Dispose" />
		public override void Dispose()
		{
			inactive = true;

			ParentDrawerUtility.OnDisposing(Inspector, this);

			cachedValuesNeedUpdating = false;

			#if ENABLE_SPREADING
			lastUpdateCachedValuesMemberIndex = -1;
			#endif
			
			if(memberBuildState == MemberBuildState.MembersBuilt)
			{
				DisposeMembers();
			}

			DrawerArrayPool.Resize(ref members, 0);
			DrawerArrayPool.Resize(ref visibleMembers, 0);
			updateCachedValuesFor.Clear();

			memberBuildState = MemberBuildState.Unstarted;

			memberBuildList.Clear();

			assumeVisibleMembersChangedDuringNextUpdateVisibleMembers = true;

			labelLastDrawPosition.y = -100f;
			labelLastDrawPosition.width = 0f;
			labelLastDrawPosition.height = 0f;
			bodyLastDrawPosition.y = -100f;
			bodyLastDrawPosition.width = 0f;
			bodyLastDrawPosition.height = 0f;

			base.Dispose();
		}

		/// <summary>
		/// Calls Dispose for all member drawers and sets the element at their index in the members array to null.
		/// Should only be called with memberBuildState at MembersBuilt, and once finshed, this will revert memberBuildState
		/// to BuildListGenerated.
		/// </summary>
		protected void DisposeMembers()
		{
			if(memberBuildState == MemberBuildState.MembersBuilt)
			{
				#if DEV_MODE || SAFE_MODE
				if(members == null)
				{
					#if DEV_MODE
					Debug.Log("DisposeChildren called with null members for "+ToString());
					#endif
					return;
				}
				#endif

				for(int n = members.Length - 1; n >= 0; n--)
				{
					if(members[n] != null)
					{
						members[n].Dispose();
						members[n] = null;
					}
				}

				memberBuildState = MemberBuildState.BuildListGenerated;
			}
			#if DEV_MODE
			else
			{
				Debug.LogWarning("DisposeMembers called with memberBuildState "+memberBuildState);
			}
			#endif
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
			if(inactive)
			{
				return;
			}

			if(memberBuildState != MemberBuildState.MembersBuilt && MembersAreVisible)
			{
				BuildMembers();
			}
		}

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

		/// <inheritdoc cref="IParentDrawer.GetRowSelectableCount" />
		public override int GetRowSelectableCount()
		{
			return DrawInSingleRow ? visibleMembers.Length : 1;
		}

		/// <inheritdoc/>
		public void RebuildMemberBuildListAndMembers()
		{
			DisposeMembers();
			RebuildMemberBuildList();
			BuildMembers();
		}

		/// <summary>
		/// Clears the memberBuildList and builds it again from scratch.
		/// 
		/// This usually happens based on memberBuildList contents, so GenerateMemberBuildList should always be called before this method.
		/// 
		/// Note that this does NOT rebuild the member build list - RebuildMemberBuildListAndMembers can be used to achieve that.
		/// </summary>
		protected virtual void RebuildMemberBuildList()
		{
			memberBuildList.Clear();
			if(memberBuildState == MemberBuildState.BuildListGenerated)
			{
				memberBuildState = MemberBuildState.Unstarted;
			}

			GenerateMemberBuildList();
		}

		/// <summary>
		/// Disposes all members and rebuilds them. This usually happens based on memberBuildList contents,
		/// so GenerateMemberBuildList should always be called before this method.
		/// This does NOT rebuild the member build list.
		/// </summary>
		protected void RebuildMembers()
		{
			DisposeMembers();
			BuildMembers();
		}

		/// <inheritdoc/>
		protected override void DoRandomize()
		{
			for(int n = MembersBuilt.Length - 1; n >= 0; n--)
			{
				members[n].Randomize(false);
			}
		}

		#if DEV_MODE
		/// <inheritdoc/>
		protected override object[] GetDevInfo()
		{
			return base.GetDevInfo().Add(", Unfolded=", Unfolded, ", MembersAreVisible=", MembersAreVisible, ", DrawInSingleRow=", DrawInSingleRow, ", memberBuildState=", memberBuildState);
		}
		#endif
	}
}