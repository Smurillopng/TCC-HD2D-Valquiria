namespace Sisus
{
	public enum FieldVisibility
	{
		/// <summary>
		/// Only show fields that are serialized by Unity or explicitly exposed with
		/// attributes like ShowInInspector.
		/// </summary>
		SerializedOnly = 0,

		/// <summary>
		/// All public fields are shown, even if not serialized by Unity,
		/// unless explicitly hidden with Attributes like HideInInspector or NonSerialized.
		/// </summary>
		AllPublic = 1,

		/// <summary>
		/// All fields are shown, including private ones, unless explicitly hidden
		/// with Attributes like HideInInspector or NonSerialized.
		/// </summary>
		AllExceptHidden = 2
	}
}