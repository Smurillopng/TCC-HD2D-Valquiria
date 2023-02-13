namespace Sisus
{
	public enum FilteringMethod
	{
		Any = 0,

		Label = 1,	// L
		Type = 2,	// T
		Value = 3,	// V
		Class = 10,	// C
		Scene = 20,	// S
		Asset = 30,	// A
		Window = 40,// W

		#if DEV_MODE
		GUIStyle = 60,   // G
		#endif

		Icon = 50   // I
	}
}