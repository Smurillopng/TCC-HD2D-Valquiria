namespace Sisus
{
	public enum ShowPreviewArea
	{
		/// <summary>
		/// The preview area is always minimized when the inspected targets change.
		/// </summary>
		Minimized = 0,

		/// <summary>
		/// The preview area is automatically minimized or expanded based on whether or
		/// not the previews offer information that users will likely find useful - such
		/// as a visual preview of a texture or a model.
		/// </summary>
		Dynamic = 1,

		/// <summary>
		/// The preview area remains in its minimized or expanded state unless manually
		/// changed by the user.
		/// </summary>
		Manual = 3
	}
}