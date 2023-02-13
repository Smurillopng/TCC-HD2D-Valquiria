using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public interface IParentDrawer : IDrawer
	{
		/// <summary>
		/// Members belonging to this parent.
		/// Note that this array might be empty until the moment that the parent's
		/// members first become visible (e.g. when parent is first unfolded)
		/// </summary>
		/// <value>
		/// All members of the parent.
		/// </value>
		[NotNull]
		IDrawer[] Members { get; }

		/// <summary> Gets the members, but builds them first if they have not yet been built. </summary>
		/// <value> The members. </value>
		[NotNull]
		IDrawer[] MembersBuilt { get; }

		/// <summary>
		/// The portion of Members that are currently visible in the inspector,
		/// considering things like unfolded state and active filter.
		/// This is NOT culled based on whether the members are currently
		/// inside the view rect of the inspector.
		/// </summary>
		/// <value>
		/// Currently visible members of the parent.
		/// </value>
		[NotNull]
		IDrawer[] VisibleMembers { get; }

		/// <summary> Sets the member drawer of the parent drawer, and rebuilds the visible members. </summary>
		/// <param name="setMembers"> The new members. </param>
		/// <param name="sendVisibilityChangedEvents">
		/// True to broadcast events OnBecameInvisible and OnSelfOrParentBecameVisible events to applicable members.
		/// This should generally speaking be false during Setup and Dispose phases and otherwise true.
		/// </param>
		void SetMembers([NotNull]IDrawer[] setMembers, bool sendVisibilityChangedEvents = true);

		/// <summary> Sets the member drawer of the parent drawer, along with the visible ones. </summary>
		/// <param name="setMembers"> The new members. </param>
		/// <param name="setVisibleMembers"> The new visible members. </param>
		/// <param name="sendVisibilityChangedEvents">
		/// True to broadcast events OnBecameInvisible and OnSelfOrParentBecameVisible events to applicable members.
		/// This should generally speaking be false during Setup and Dispose phases and otherwise true.
		/// </param>
		void SetMembers([NotNull]IDrawer[] setMembers, [NotNull]IDrawer[] setVisibleMembers, bool sendVisibilityChangedEvents = true);

		/// <summary> Sets visible members. </summary>
		/// <param name="newVisibleMembers"> The new visible members. </param>
		/// <param name="sendVisibilityChangedEvents">
		/// True to broadcast events OnBecameInvisible and OnSelfOrParentBecameVisible events to applicable members.
		/// This should generally speaking be false during Setup and Dispose phases and otherwise true.
		/// </param>
		void SetVisibleMembers([NotNull]IDrawer[] newVisibleMembers, bool sendVisibilityChangedEvents = true);

		/// <summary>
		/// Called after members have just been rebuilt.
		/// This should make sure that visible members are updated so that they reflect possible changes in the members.
		/// </summary>
		void OnAfterMembersBuilt();

		/// <summary>
		/// Called after there has been changes to the visible members of the parent drawer.
		/// For example when the parent is folded, unfolded or filter in the search box has changed.
		/// </summary>
		void OnVisibleMembersChanged();

		/// <summary>
		/// Gets the height of the header portion of the drawer.
		/// </summary>
		/// <value>
		/// Header height.
		/// </value>
		float HeaderHeight { get; }

		/// <summary>
		/// False if the control has been collapsed via a foldout arrow control, true if not.
		/// When tween animations are used for unfolding, this will return the target value.
		/// So e.g. if an unfolded Component's foldout control was just clicked, it will visually
		/// still be fully unfolded, but Unfolded will still return false.
		/// </summary>
		/// <value>
		/// False if the control has been collapsed via a foldout arrow control, true if not.
		/// </value>
		bool Unfolded { get; set; }

		/// <summary>
		/// Value between 0 and 1 where 0 means parent is fully folded and 1 means it's full unfolded.
		/// When Unfolded is set to a value, unfoldedness will get tweened towards a matching value
		/// over a short period of time.
		/// </summary>
		/// <value> The unfoldedness. </value>
		float Unfoldedness { get; }

		/// <summary>
		/// Gets a value indicating whether or not the members (if any) of the parent are currently visible.
		/// 
		/// For the most part this is true when Unfoldedness is > 0f, and false when it's 0.
		/// However in rare cases some members might be visible even when folded, in which case this will still
		/// return true.
		/// 
		/// This does not consider whether or any parent has any members, meaning that it can return true
		/// even if member count is zero.
		/// </summary>
		bool MembersAreVisible { get; }

		/// <summary>
		/// Gets number indicating how many counts we should append the left indentation level
		/// before drawing the members of the parent.
		/// </summary>
		/// <value> The append indent level. </value>
		int AppendIndentLevel { get; }

		/// <summary>
		/// Gets the linked member hierarchy for target UnityEngine.Object(s)
		/// </summary>
		/// <value>
		/// The linked member hierarchy.
		/// </value>
		[NotNull]
		LinkedMemberHierarchy MemberHierarchy { get; }

		/// <summary>
		/// Rebuild the contents of the array containing all visible members of the drawer.
		/// 
		/// BuildMembers should always be called before this to, so that the array containing
		/// all members has been populated.
		/// </summary>
		void UpdateVisibleMembers();

		/// <summary>
		/// Are all members always drawn in a single row (instead of one on top of another)?
		/// </summary>
		/// <value>
		/// True if all members are always drawn in a single row, false if not.
		/// </value>
		bool DrawInSingleRow { get; }

		/// <summary>
		/// Can the target be unfolded for example using a foldout arrow control?
		/// </summary>
		/// <value>
		/// True if can fold the drawer, false if not.
		/// </value>
		bool Foldable { get; }

		/// <summary>
		/// Draws all members in a single row at position
		/// </summary>
		bool DrawBodySingleRow(Rect position);

		/// <summary>
		/// Draws all members one multipler rows, one on top of another,
		/// starting at (x,y) coordinates of position and using width
		/// of position (position.height is ignored).
		/// </summary>
		bool DrawBodyMultiRow(Rect position);

		/// <summary>
		/// When the value of a member is changed, this gets called recursively in all parents.
		/// 
		/// This can get called when user changes value through the inspector view, but also when external sources change
		/// the value, in which case the UpdateCachedValues method causes this to get called.
		/// </summary>
		/// <param name="memberIndex"> Zero-based index of the member whose value changed. </param>
		/// <param name="memberValue"> The new value of the member. </param>
		/// <param name="memberLinkedMemberInfo"> the linked member info of the changed member</param>
		void OnMemberValueChanged(int memberIndex, object memberValue, [CanBeNull]LinkedMemberInfo memberLinkedMemberInfo);

		/// <summary>
		/// Called whenever the layout of any child or grand-child has changed.
		/// </summary>
		void OnChildLayoutChanged();

		/// <summary>
		/// Sets the unfolded state of the drawer.
		/// </summary>
		/// <param name="setUnfolded"> True to unfold, false to fold. </param>
		void SetUnfolded(bool setUnfolded);

		/// <summary>
		/// Sets the unfolded state of the parent drawer, and optionally does the same recursively for its member drawer.
		/// </summary>
		/// <param name="setUnfolded"> True to unfold, false to fold. </param>
		/// <param name="setChildrenAlso"> True to also set unfolded state of members to setUnfolded value recursively. </param>
		void SetUnfolded(bool setUnfolded, bool setChildrenAlso);

		/// <summary>
		/// Gets zero-based index of member, starting from the left, amongst the all the
		/// visible member drawer drawn on the same row.
		/// If DrawInSingleRow is false, then this should always return zero.
		/// </summary>
		/// <param name="member"> The member whose index to get. </param>
		/// <returns> The member index on the row. </returns>
		int GetMemberRowIndex(IDrawer member);
		
		/// <summary>
		/// Gets type of first member.
		/// Returns null if has no members or is unable to get it.
		/// </summary>
		/// <returns>
		/// The type of first member.
		/// </returns>
		Type GetMemberType(int index);

		/// <summary> Gets value of member at given index. </summary>
		/// <param name="index"> Zero-based index of the member. </param>
		/// <returns> The value of the member. </returns>
		object GetMemberValue(int index);

		/// <summary> Clears the memberBuildList and builds it again from scratch, then rebuilds members with the new build list. </summary>
		void RebuildMemberBuildListAndMembers();

		/// <summary> Without considering any members, tests if this drawer by itself passes the search filter. </summary>
		/// <param name="filter"> The filter to test against. </param>
		/// <returns> True if passes filter check, false if fails. </returns>
		bool SelfPassesSearchFilter(SearchFilter filter);
	}
}