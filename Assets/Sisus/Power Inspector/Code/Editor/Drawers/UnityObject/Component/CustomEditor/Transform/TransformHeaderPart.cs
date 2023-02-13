namespace Sisus
{
	public enum TransformHeaderPart
	{
		None = 0,						// PrefixedControlPart, HeaderPart, GameObjectHeaderPart

		#region Basic Header Parts		//10...19
		FoldoutArrow = 10,				// HeaderPart
		EnabledFlag = 11,				// HeaderPart
		ActiveFlag = 11,				// HeaderPart
		#endregion
	
		#region Header Toolbar Buttons	// 20...29
		ContextMenuIcon = 20,			// HeaderPart
		ReferenceIcon = 21,				// HeaderPart

		#if UNITY_2018_1_OR_NEWER
		PresetIcon = 22,				// HeaderPart
		#endif
	
		DebugModePlusButton = 30,		// HeaderPart
		MethodInvokerButton = 31,		// HeaderPart
		
		ToggleLocalSpaceButton = 50,	// TransformHeaderPart
		ToggleSnapToGridButton = 51,	// TransformHeaderPart
		Toggle2DModeButton = 52,		// TransformHeaderPart
		#endregion
	}
}