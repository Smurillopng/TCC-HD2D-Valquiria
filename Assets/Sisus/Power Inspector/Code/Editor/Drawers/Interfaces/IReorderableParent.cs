using UnityEngine;

namespace Sisus
{
	public interface IReorderableParent : IParentDrawer
	{
		/// <summary>
		/// Gets the drop target rectangle where a reorderable can be dropped
		/// to have it be placed as the first member of the collection.
		/// E.g. for an array field this would usually be below the resize field,
		/// while for a GameObject this would be right right below the header
		/// (even if reordering something above the Transform isn't allow, that
		/// is still the first drop target rect)
		/// </summary>
		/// <value>
		/// The first reorderable drop target rectangle.
		/// </value>
		Rect FirstReorderableDropTargetRect { get; }
	
		/// <summary>
		/// how much do you need to subtract from members.Length to get the
		/// index of the last collection element
		/// </summary>
		/// <value>
		/// difference between IDrawer member count and last collection element index
		/// </value>
		int LastCollectionMemberCountOffset { get; }

		/// <summary>
		/// Gets the zero-based index of the first IDrawer in Members
		/// array which represents a value of an element in the collection
		/// (instead of representing an auxilary control, like a resize field)
		/// </summary>
		/// <value> index of first member representing an element in collection, -1 if there are none </value>
		int FirstCollectionMemberIndex { get; }
		
		/// <summary>
		/// Gets the zero-based index of the last IDrawer in Members
		/// array which represents a value of an element in the collection
		/// (instead of representing an auxilary control, like an add component button)
		/// </summary>
		/// <value> index of last member representing an element in collection, -1 if there are none </value>
		int LastCollectionMemberIndex { get; }

		/// <summary>
		/// Gets the zero-based index of the first IDrawer in VisibleMembers
		/// array which represents a value of an element in the collection
		/// (instead of representing an auxilary control, like a resize field)
		/// </summary>
		/// <value> index of first visible member representing an element in collection, -1 if there are none </value>
		int FirstVisibleCollectionMemberIndex { get; }

		/// <summary>
		/// Gets the zero-based index of the last IDrawer in VisibleMembers
		/// array which represents a value of an element in the collection
		/// (instead of representing an auxilary control, like an add component button)
		/// </summary>
		/// <value> index of last visible member representing an element in collection, -1 if there are none </value>
		int LastVisibleCollectionMemberIndex { get; }

		/// <summary>
		/// Called after left mouse button has been pressed down over a reorderable member.
		/// </summary>
		/// <param name="reordering"> The member that is being reordered. </param>
		void OnMemberReorderingStarted(IReorderable reordering);

		/// <summary>
		/// Called every frame that a Reorderable member of the parent is being dragged
		/// </summary>
		/// <param name="mouseDownInfo"> Contains all kinds of information about what was under the cursor when left mouse button was pressed down. </param>
		/// <param name="draggedObjects"> The dragged objects. </param>
		void OnMemberDrag(MouseDownInfo mouseDownInfo, Object[] draggedObjects);

		/// <summary>
		/// Called every frame that a Reorderable or an UnityObject that is being dragged is hovering over a valid drop target of the parent.
		/// </summary>
		/// <param name="mouseDownInfo"> Contains all kinds of information about what was under the cursor when left mouse button was pressed down. </param>
		/// <param name="draggedObjects"> The dragged objects. </param>
		void OnSubjectOverDropTarget(MouseDownInfo mouseDownInfo, Object[] draggedObjects);

		/// <summary>
		/// Handles a new member being drag n dropped on the parent.
		/// </summary>
		/// <param name="sourceParent">
		/// Source parent. </param>
		/// <param name="draggedDrawer">
		/// The dragged member. </param>
		void OnMemberDragNDrop(MouseDownInfo mouseDownInfo, Object[] draggedObjects);

		/// <summary>
		/// Called after left mouse button has been pressed down over a reorderable member and then released.
		/// </summary>
		/// <param name="reordering"> The member that was being reordered. </param>
		void OnMemberReorderingEnded(IReorderable reordering);

		/// <summary>
		/// Gets zero-based index in visible members over which given Rect currently resides.
		/// To be precices, this means the seams between members, where 0 means the slot before
		/// the first member, 1 means the slot before the second member, and so on.
		/// </summary>
		/// <returns>
		/// zero-based index in visible members if over a valid drop target, or -1 if not
		/// </returns>
		int GetDropTargetIndexAtPoint(Vector2 point);

		/// <summary>
		/// Returns false if member in question is not reorderable or a valid drag N drop subject for this parent.
		/// 
		/// For example in the case of an int array returns false for the resize field but true for all IntDrawers.
		/// </summary>
		/// <param name="member">
		/// The reorderable being dragged. </param>
		/// <returns>
		/// False if member is not reorderable as a special case
		/// </returns>
		bool MemberIsReorderable(IReorderable member);

		/// <summary>
		/// Returns false if Object reference in question is not a valid drag N drop subject for this parent.
		/// 
		/// For example in the case of a GameObjectDrawer a MonoScript is a valid drag N drop subject.
		/// </summary>
		/// <param name="subject">
		/// The reorderable being dragged. </param>
		/// <returns>
		/// False if member is not reorderable as a special case
		/// </returns>
		bool SubjectIsReorderable(Object subject);

		/// <summary>
		/// Deletes the member drawer and the value
		/// that it represents in the reorderable parent.
		/// </summary>
		/// <param name="delete"> The member to delete. </param>
		void DeleteMember(IDrawer delete);
	}
}