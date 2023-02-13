using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Interface implemented by drawers whose values can sometimes be adjusted by dragging their prefix horizontally.
	/// </summary>
	public interface IDraggablePrefix : IDrawer
	{
		/// <summary> Gets the position and dimensions of the control. </summary>
		/// <value> The control position. </value>
		Rect ControlPosition { get; }

		/// <summary>
		/// Determines whether or not the mouse button was pressed down over the prefix of the drawer which can be dragged to adjust its value.
		/// Also should return true if parent prefix is being dragged and it affects the value of this drawer.
		/// </summary>
		bool DraggingPrefix { get; }

		/// <summary> Called during the first frame that the drawer' prefix started being dragged. </summary>
		/// <param name="inputEvent"> Information about the drag event. </param>
		void OnPrefixDragStart(Event inputEvent);

		/// <summary> Called every frame during which the drawer's prefix is being dragged. </summary>
		/// <param name="inputEvent"> Information about the drag event. </param>
		void OnPrefixDragged(Event inputEvent);
	}
}