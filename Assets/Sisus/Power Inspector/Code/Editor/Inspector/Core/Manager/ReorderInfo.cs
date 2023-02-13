//#define DEBUG_DRAG_STARTED

using JetBrains.Annotations;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public class ReorderInfo
	{
		private IReorderable drawer;
		private IReorderableParent parent;
		private int controlIndexInParent = -1;
		private bool isUnityObjectHeaderDrag;

		private readonly ReorderDropTarget mouseoveredDropTarget = new ReorderDropTarget();

		
		public IReorderable Drawer
		{
			get
			{
				return drawer;
			}
		}

		public IReorderableParent Parent
		{
			get
			{
				return parent;
			}
		}

		public ReorderDropTarget MouseoveredDropTarget
		{
			get
			{
				return mouseoveredDropTarget;
			}
		}

		public int MemberIndex
		{
			get
			{
				return controlIndexInParent;
			}
		}

		/// <summary>
		/// Determines whether or not currently reordering a drawer that represents an Object (not counting Object reference fields).
		/// 
		/// Can be useful in differentiating
		/// </summary>
		public bool IsUnityObjectHeaderDrag
		{
			get
			{
				return isUnityObjectHeaderDrag;
			}
		}
		
		public void OnReorderableDragStarted([NotNull]IReorderable reorderedControl, [NotNull]IReorderableParent reorderedControlParent, [NotNull]IInspector inspector)
		{
			#if DEV_MODE && DEBUG_DRAG_STARTED
			Debug.Log("OnReorderableDragStarted(control=" + StringUtils.ToString(reorderedControl) +", parent="+ StringUtils.ToString(reorderedControlParent) +")");
			#endif

			drawer = reorderedControl;
			parent = reorderedControlParent;
			controlIndexInParent = Array.IndexOf(parent.Members, drawer);

			mouseoveredDropTarget.OnReorderableDragStarted(inspector, drawer);
			
			reorderedControlParent.OnMemberReorderingStarted(reorderedControl);

			isUnityObjectHeaderDrag = reorderedControl is IUnityObjectDrawer || reorderedControl is IAssetDrawer;
		}

		/// <summary>
		/// This should be called every time a new dragging of UnityEngine.Object references starts,
		/// or when the cursor moves over a new inspector during a drag.
		/// </summary>
		/// <param name="mouseoveredInspector"> The inspector over which the dragging is now taking place. </param>
		/// <param name="draggedObjects"> Dragged object references. </param>
		public void OnUnityObjectDragOverInspectorStarted([CanBeNull]IInspector mouseoveredInspector, [NotNull]Object[] draggedObjects)
		{
			#if DEV_MODE && DEBUG_DRAG_STARTED
			Debug.Log("OnUnityObjectDragOverInspectorStarted(inspector="+ StringUtils.ToString(mouseoveredInspector) + ", dragged=" + StringUtils.ToString(draggedObjects) + ")");
			#endif
			
			#if DEV_MODE
			Debug.Assert(mouseoveredInspector != null || (drawer == null && parent == null));
			#endif

			mouseoveredDropTarget.OnUnityObjectDragOverInspectorStarted(mouseoveredInspector, draggedObjects);

			isUnityObjectHeaderDrag = false;
		}
		
		public void OnMouseoveredInspectorChanged(IInspector newlyMouseoveredInspector)
		{
			mouseoveredDropTarget.OnDropTargetInspectorChanged(newlyMouseoveredInspector, drawer, DrawGUI.Active.DragAndDropObjectReferences);
		}

		public void OnCursorMovedOrInspectorLayoutChanged()
		{
			mouseoveredDropTarget.OnCursorMovedOrInspectorLayoutChanged();
		}

		public void Clear()
		{
			if(drawer != null)
			{
				parent.OnMemberReorderingEnded(drawer);
				drawer = null;
				parent = null;
				controlIndexInParent = -1;
				mouseoveredDropTarget.Clear();
				isUnityObjectHeaderDrag = false;
			}
		}
	}
}