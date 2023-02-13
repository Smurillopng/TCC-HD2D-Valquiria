//#define DEBUG_SET_MEMBER_INDEX
//#define DEBUG_SET_PARENT
//#define DEBUG_GET_MEMBER_INDEX
#define DEBUG_DRAG_STARTED
//#define DEBUG_TEST_FOR_DROP_TARGET
//#define DEBUG_UPDATE_DROP_TARGET

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public class ReorderDropTarget
	{
		private IReorderableParent parent;
		private IInspector inspector;
		private int memberIndex;
		
		/// <summary>
		/// Offset of the center of the top edge of the dragged control from the cursor position when drag started
		/// Useful for detecting whether the top center is currently residing inside a drop target rectangle.
		/// </summary>
		private Vector2 draggedControlTopCenterCursorPositionOffset;

		private List<IReorderableParent> dropTargetParentOptions = new List<IReorderableParent>(1);
		
		public IReorderableParent Parent
		{
			get
			{
				return parent;
			}
			
			set
			{
				#if DEV_MODE && DEBUG_SET_PARENT
				if(value != parent) { Debug.Log(StringUtils.ToColorizedString("ReorderDropTarget.Parent = ", value, " (was: ", parent, ") with Event=", Event.current, ", ObjectReferences=", DrawGUI.Active.DragAndDropObjectReferences)); }
				#endif

				parent = value;
			}
		}

		public IInspector Inspector
		{
			get
			{
				return inspector;
			}
		}

		/// <summary>
		/// Gets index of dragged IReorderable in the VisibleMembers array of parent
		/// </summary>
		/// <returns>
		/// The index of control in Members array of parent.
		/// </returns>
		public int MemberIndex
		{
			get
			{
				return memberIndex;
			}

			set
			{
				#if DEV_MODE && DEBUG_SET_MEMBER_INDEX
				if(value != memberIndex) { Debug.Log(StringUtils.ToColorizedString("ReorderDropTarget.memberIndex = ", value, " (was:", memberIndex,")")); }
				#endif

				memberIndex = value;
			}
		}

		/// <summary>
		/// Gets index of subject in the Members array of parent
		/// </summary>
		/// <param name="subject">
		/// The subject. </param>
		/// <returns>
		/// The index of subject in Members array of parent.
		/// </returns>
		public int GetIndexInParent(IDrawer subject)
		{
			#if DEV_MODE && DEBUG_GET_MEMBER_INDEX
			Debug.Log("ReorderDropTarget.GetIndexInParent("+StringUtils.ToString(subject)+ "): "+ Array.IndexOf(parent.Members, subject));
			#endif

			if(parent == null)
			{
				return -1;
			}

			return Array.IndexOf(parent.Members, subject);
		}

		public int GetIndexInParent(Object[] subjects)
		{
			//var members = parent.VisibleMembers;
			var members = parent.Members;
			for(int s = subjects.Length - 1; s >= 0; s--)
			{
				var subject = subjects[s];
				for(int n = members.Length - 1; n >= 0; n--)
				{
					var member = members[n];
					if(member.UnityObject == subject)
					{
						#if DEV_MODE && DEBUG_GET_MEMBER_INDEX
						Debug.Log("ReorderDropTarget.GetIndexInParent("+StringUtils.ToString(subject)+ "): "+ n);
						#endif
						return n;
					}
				}
			}
			return -1;
		}

		public void OnReorderableDragStarted([NotNull]IInspector containingInspector, [NotNull]IReorderable reordering)
		{
			#if DEV_MODE
			Debug.Assert(containingInspector != null);
			Debug.Assert(reordering != null);
			#endif

			inspector = containingInspector;
			UpdateReorderingOptions(reordering);
			UpdateDropTarget();
			
			#if DEV_MODE && DEBUG_DRAG_STARTED
			Debug.Log("OnReorderableDragStarted(" + reordering + "): inspector=" + StringUtils.ToString(inspector) +", parentOptions=" + StringUtils.ToString(dropTargetParentOptions));
			#endif
		}

		/// <summary>
		/// This should be called every time a new dragging of UnityEngine.Object references starts,
		/// or when the cursor moves over a new inspector during a drag.
		/// </summary>
		/// <param name="mouseoveredInspector"> The inspector over which the dragging is now taking place. </param>
		/// <param name="draggedObjects"> Dragged object references. </param>
		public void OnUnityObjectDragOverInspectorStarted([CanBeNull]IInspector mouseoveredInspector, [NotNull]Object[] draggedObjects)
		{
			#if DEV_MODE
			Debug.Assert(draggedObjects != null);
			#endif

			if(mouseoveredInspector == null)
			{
				return;
			}
			
			inspector = mouseoveredInspector;

			UpdateReorderingOptions(draggedObjects);
			UpdateDropTarget();

			#if DEV_MODE && DEBUG_DRAG_STARTED
			Debug.Log("OnUnityObjectDragOverInspectorStarted(" + StringUtils.ToString(draggedObjects) + "): inspector=" + StringUtils.ToString(inspector) + ", parentOptions=" + StringUtils.ToString(dropTargetParentOptions));
			#endif
		}

		/// <summary>
		/// Called when the inspector whose viewport is under the cursor is changed while an IReorderable or Objects are being dragged
		/// </summary>
		/// <param name="newlyMouseoveredInspector">
		/// The inspector whose viewport is now being mouseovered</param>
		/// <param name="reordering">
		/// The IReorderable currently being dragged. </param>
		/// <param name="draggedObjects">
		/// The Object references currently being dragged. </param>
		public void OnDropTargetInspectorChanged(IInspector newlyMouseoveredInspector, IReorderable reordering, Object[] draggedObjects)
		{
			inspector = newlyMouseoveredInspector;

			if(inspector == null)
			{
				Clear();
				return;
			}

			if(reordering != null)
			{
				UpdateReorderingOptions(reordering);
			}
			else
			{
				UpdateReorderingOptions(draggedObjects);
			}

			UpdateDropTarget();
			
			#if DEV_MODE
			//Debug.Log("OnDropTargetInspectorChanged(" + StringUtils.ToString(newlyMouseoveredInspector) + "): " + StringUtils.ToString(inspector) + " parentOptions: " + StringUtils.ToString(dropTargetParentOptions));
			#endif
		}

		public void OnCursorMovedOrInspectorLayoutChanged()
		{
			UpdateDropTarget();
		}
		
		public void Clear()
		{
			Parent = null;
			MemberIndex = -1;

			inspector = null;
			dropTargetParentOptions.Clear();
		}

		private void UpdateDropTarget()
		{
			#if DEV_MODE && DEBUG_UPDATE_DROP_TARGET
			Debug.Log(StringUtils.ToColorizedString("UpdateDropTarget with inspector=", StringUtils.ToString(inspector), ", Cursor.CanRequestLocalPosition=", Cursor.CanRequestLocalPosition, ", Drawer=", (inspector == null ? null : inspector.InspectorDrawer.Manager.MouseDownInfo.Reordering.Drawer), ", dropTargetParentOptions=", StringUtils.ToString(dropTargetParentOptions)));
			#endif

			if(!Cursor.CanRequestLocalPosition)
			{
				return;
			}
			
			if(inspector == null)
			{
				Parent = null;
				MemberIndex = -1;
				return;
			}

			var inspectorDrawer = inspector.InspectorDrawer;
			if(inspectorDrawer == null)
			{
				Parent = null;
				MemberIndex = -1;
				return;
			}

			var control = inspectorDrawer.Manager.MouseDownInfo.Reordering.Drawer;
			draggedControlTopCenterCursorPositionOffset = control != null ? control.MouseDownCursorTopLeftCornerOffset : Vector2.zero;
			inspector.State.OnNextLayoutForVisibleDrawers(3, TestForDropTarget);
		}

		private void TestForDropTarget([NotNull]IDrawer test)
		{
			for(int n = dropTargetParentOptions.Count - 1; n >= 0; n--)
			{
				if(dropTargetParentOptions[n] == test)
				{
					var reorderableParent = dropTargetParentOptions[n];
					var point = Cursor.LocalPosition + draggedControlTopCenterCursorPositionOffset;
					int index = reorderableParent.GetDropTargetIndexAtPoint(point);
					if(index != -1)
					{
						/*
						// UPDATE: Do a last-minute check for MemberIsReorderable / SubjectIsReorderable at this time,
						// because sometimes their result might be context-dependent (e.g. relate to current cursor position).
						var reordering = InspectorUtility.ActiveManager.MouseDownInfo.Reordering;
						var reorderedDrawer = reordering.Drawer;
						if(reorderedDrawer != null)
						{
							if(!reorderableParent.MemberIsReorderable(InspectorUtility.ActiveManager.MouseDownInfo.Reordering.Drawer))
							{
								#if DEV_MODE && DEBUG_TEST_FOR_DROP_TARGET
								Debug.Log("TestForDropTarget: found Parent "+reorderableParent+" with index "+index+" but ignoring because MemberIsReorderable returned false for dragged drawer at this time.");
								#endif
								return;
							}
						}
						else if(DrawGUI.Active.DragAndDropObjectReferences.Length < 1 || !reorderableParent.SubjectIsReorderable(DrawGUI.Active.DragAndDropObjectReferences[0]))
						{
							#if DEV_MODE && DEBUG_TEST_FOR_DROP_TARGET
							Debug.Log("TestForDropTarget: found Parent "+reorderableParent+" with index "+index+" but ignoring because SubjectIsReorderable returned false for DragAndDropObjectReferences[0] at this time.");
							#endif

							return;
						}
						*/

						#if DEV_MODE && DEBUG_TEST_FOR_DROP_TARGET
						Debug.Log("TestForDropTarget: found Parent "+reorderableParent+" with index "+index);
						#endif

						MemberIndex = index;
						Parent = reorderableParent;

						//once we've found the drop target, no need to check the rest
						InspectorUtility.ActiveInspector.State.ClearOnNextLayoutForVisibleDrawers(3);
						return;
					}
					
					if(test == parent)
					{
						#if DEV_MODE && DEBUG_TEST_FOR_DROP_TARGET
						if(parent != null) { Debug.Log(StringUtils.ToColorizedString("Clearing ReorderDropTarget.Parent=", parent, " because GetDropTargetIndexAtPoint @", point, " was ", -1, " with Event=", Event.current)); }
						#endif

						Parent = null;
						MemberIndex = -1;
					}
					return;
				}
			}
		}

		/// <summary>
		/// Finds the valid IReorderableParent drop targets in the currently mouseovered inspector
		/// that can receive the IReorderable and saves them in the dropTargetParentOptions list.
		/// </summary>
		/// <param name="reordering"> The IReorderable currently being dragged. </param>
		private void UpdateReorderingOptions([NotNull]IReorderable reordering)
		{
			dropTargetParentOptions.Clear();
			if(inspector != null)
			{
				GetReorderableParentOptionsForReorderable(reordering, inspector.State.drawers, ref dropTargetParentOptions);
			}
		}

		/// <summary>
		/// Finds the valid IReorderableParent drop targets in the currently mouseovered inspector
		/// that can receive the dragged Unity Objects and saves them in the "dropTargetParentOptions" list.
		/// </summary>
		/// <param name="draggedObjects"> The Objects currently being dragged. </param>
		private void UpdateReorderingOptions([NotNull]Object[] draggedObjects)
		{
			dropTargetParentOptions.Clear();
			if(inspector != null)
			{
				GetParentOptionsForDraggedUnityObjects(draggedObjects, inspector.State.drawers, ref dropTargetParentOptions);
			}
		}

		/// <summary>
		/// Searches the IParentDrawer for valid IReorderableParent options for the IReorderable and saves them in the "results" list.
		/// </summary>
		/// <param name="reorderable"> The reorderable for which to find valid parents. </param>
		/// <param name="searchInChildren"> Parent whose children we search for valid reorderable parents for the reorderable. </param>
		/// <param name="results"> [in,out] The results. </param>
		private static void GetReorderableParentOptionsForReorderable([NotNull]IReorderable reorderable, [NotNull]IParentDrawer searchInChildren, ref List<IReorderableParent> results)
		{
			var reorderableParent = searchInChildren as IReorderableParent;
			if(reorderableParent != null && reorderableParent.Unfolded && reorderableParent.MemberIsReorderable(reorderable))
			{
				results.Add(reorderableParent);

				/*
				//is nestedness of IReorderableParents possible? Can't figure out how that could be possible,
				//so won't search deeper after finding a valid target.
				return;
				UPDATE: It is possible! Can for example drag a Component into an array of Objects!
				*/
			}

			var members = searchInChildren.VisibleMembers;
			for(int n = members.Length - 1; n >= 0; n--)
			{
				var parent = members[n] as IParentDrawer;
				if(parent != null)
				{
					GetReorderableParentOptionsForReorderable(reorderable, parent, ref results);
				}
			}
		}

		/// <summary>
		/// Searches searchInChildren for IReorderableParent options for reorderable.
		/// </summary>
		/// <param name="draggedObjects"> The dragged objects for which to find valid drop target parents. </param>
		/// <param name="searchInChildren"> Parent whose children we search for valid reorderable parents for the reorderable. </param>
		/// <param name="results"> [in,out] The results. </param>
		private static void GetParentOptionsForDraggedUnityObjects(Object[] draggedObjects, [NotNull]IParentDrawer searchInChildren, ref List<IReorderableParent> results)
		{
			for(int draggedIndex = draggedObjects.Length - 1; draggedIndex >= 0; draggedIndex--)
			{
				var draggedObject = draggedObjects[draggedIndex];
				var reorderableParent = searchInChildren as IReorderableParent;
				if(reorderableParent != null && reorderableParent.SubjectIsReorderable(draggedObject))
				{
					results.Add(reorderableParent);

					/*
					//is nestedness of IReorderableParents possible? Can't figure out how that could be possible,
					//so won't search deeper after finding a valid target.
					return;
					UPDATE: It is possible! Can for example drag a Component into an array of Objects!
					*/
				}

				var members = searchInChildren.VisibleMembers;
				for(int memberIndex = members.Length - 1; memberIndex >= 0; memberIndex--)
				{
					var parent = members[memberIndex] as IParentDrawer;
					if(parent != null)
					{
						GetParentOptionsForDraggedUnityObjects(draggedObjects, parent, ref results);
					}
				}
			}
		}
	}
}