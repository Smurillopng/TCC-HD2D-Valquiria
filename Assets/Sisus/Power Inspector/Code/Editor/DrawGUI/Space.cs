namespace Sisus
{
	public enum Space
	{
		/// <summary>
		/// Window local space. This is the coordinate system that is being used at the start of OnGUI, before drawing inspectors starts.
		/// </summary>
		Window,

		/// <summary>
		/// Inspector local space. E.g. the main view and split view of an inspector drawer have different global positions,
		/// but elements inside them are at the same position when in inspector local space.
		/// </summary>
		Inspector,
		
		/// <summary> Local space. For example in the Draw method of an IDrawer. </summary>
		Local,

		/// <summary> Screen space. Can be acquired using GUIUtility.GUIToScreenPoint. </summary>
		Screen
	}
}