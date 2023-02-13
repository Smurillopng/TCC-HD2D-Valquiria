namespace Sisus
{
	public enum MethodVisibility
	{
		/// <summary>
		/// Don't show any methods unless specifically marked to be shown using attributes such as ShowInInspector or EditorBrowsable.
		/// </summary>
		AttributeExposedOnly = 0,

		/// <summary>
		/// In addition to ones specifically marked to be shown, also show all methods that have the ContextMenu attribute.
		/// </summary>
		ContextMenu = 1,

		/// <summary>
		/// All public methods will be shown by default, unless explicitly
		/// marked as not to be shown with Attributes like HideInInspector.
		/// </summary>
		AllPublic = 2
	}
}