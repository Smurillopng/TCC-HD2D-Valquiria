namespace Sisus
{
	/// <summary> Parts of GameObjectDrawer header. </summary>
	public enum GameObjectHeaderPart
	{
		None = 0,

		Base = 1,

		ActiveFlag = 11,

		NameField = 200,
		StaticFlag = 205,
		DropDownArrow = 210,
		TagField = 215,
		LayerField = 220,

		#if UNITY_EDITOR

		#if !UNITY_2018_3_OR_NEWER
		SelectPrefab = 225,
		RevertPrefab = 226,  // removed with improved prefab workflow
		ApplyPrefab = 227,  // removed with improved prefab workflow
		#else
		OpenPrefab = 228, // introduced with improved prefab workflow
		SelectPrefab = 229,
		PrefabOverrides = 230 // introduced with improved prefab workflow
		#endif

		#endif
	}
}