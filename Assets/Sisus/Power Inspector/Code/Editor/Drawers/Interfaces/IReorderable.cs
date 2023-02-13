//using UnityEngine;

using UnityEngine;

namespace Sisus
{
	public interface IReorderable : IDrawer
	{
		/// <summary>
		/// Offset from cursor position to top-left corner of lastDrawPosition
		/// at the moment when left mouse button was last pressed down. This can
		/// be used to have the reorderable follow the cursor around visually
		/// when being reordered.
		/// </summary>
		/// <value>
		/// cursor to top-left corner offset
		/// </value>
		Vector2 MouseDownCursorTopLeftCornerOffset { get; }

		/// <summary>
		/// in the context where the left mouse button was pressed down with the cursor over this control,
		/// gets a value indicating if the cursor was over a portion of the control which would make it
		/// possible to start dragging the control
		/// </summary>
		/// <value>
		/// True if mouse was over draggable area when cursor was pressed down over the control
		/// </value>
		bool MouseDownOverReorderArea { get; }

		/// <summary>
		/// Called every frame when the target is being reordered
		/// </summary>
		/// <param name="yOffset">y-axis offset to use for all drawing.</param>
		void OnBeingReordered(float yOffset);
	}
}