namespace Sisus
{
	public interface IDraggablePrefix<TValue> : IDraggablePrefix
	{
		/// <summary> Gets or sets the value of the drawer. </summary>
		/// <value> The value. </value>
		TValue Value { get; set; }


		/// <summary> Called every frame during which the target's prefix is being dragged. </summary>
		/// <param name="value"> [in,out] The value of the drawer. This value will be changed based on the mouseDownValues and mouse get changed during the drag. </param>
		/// <param name="valueDuringMouseDown"> The value of the drawer during the moment that the mouse was pressed down at the beginning of the dragging. </param>
		/// <param name="mouseDelta"> The distance that the mouse has moved from the cursor mouse down position. </param>
		void OnPrefixDragged(ref TValue value, TValue valueDuringMouseDown, float mouseDelta);
	}
}