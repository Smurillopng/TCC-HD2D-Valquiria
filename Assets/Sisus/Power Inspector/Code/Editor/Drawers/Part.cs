namespace Sisus
{
	/// <summary> Parts of something (GameObjectDrawer header, UnityObjectDrawer header, Toolbar...) </summary>
	public enum Part
	{
		None = 0,						// PrefixedControlPart, HeaderPart, GameObjectHeaderPart
	
		#region Basic					//1...9
		Base = 1,						// HeaderPart, GameObjectHeaderPart
		Prefix = 1,						// PrefixedControlPart
		Label = 1,
		Control = 2,					// PrefixedControlPart
		Body = 2,
		#endregion
	
		#region Basic Header Parts		//10...19
		FoldoutArrow = 10,				// HeaderPart
		EnabledFlag = 11,				// HeaderPart
		ActiveFlag = 11,				// HeaderPart
		#endregion
	
		#region Header Toolbar Buttons	// 20...99
		ContextMenuIcon = 20,			// HeaderPart
		ReferenceIcon = 21,				// HeaderPart

		#if UNITY_2018_1_OR_NEWER
		PresetIcon = 22,				// HeaderPart
		#endif
	
		DebugModePlusButton = 30,		// HeaderPart
		MethodInvokerButton = 31,		// HeaderPart

		CustomHeaderButton1 = 51,
		CustomHeaderButton2 = 52,
		CustomHeaderButton3 = 53,
		CustomHeaderButton4 = 54,
		CustomHeaderButton5 = 55,
		#endregion
	
		#region Toolbar					 //100...199
		BackButton	= 100,				// ToolbarPart
		ForwardButton = 101,			// ToolbarPart
		ViewMenu = 110,					// ToolbarPart
		SearchBox = 120,				// ToolbarPart
		ClearSearchBoxButton = 121,		// ToolbarPart
		LockButton = 130,				// ToolbarPart
		SplitViewButton = 140,			// ToolbarPart
		#endregion
	
		#region GameObjectDrawer		// 200..299
		NameField = 200,				// GameObjectHeaderPart
		StaticFlag = 205,				// GameObjectHeaderPart
		DropDownArrow = 210,			// GameObjectHeaderPart
		TagField = 215,					// GameObjectHeaderPart
		LayerField = 220,				// GameObjectHeaderPart

		#if UNITY_EDITOR
		#if !UNITY_2018_3_OR_NEWER		// removed with improved prefab workflow in Unity 2018.3
		SelectPrefab = 225,				// GameObjectHeaderPart
		RevertPrefab = 226,				// GameObjectHeaderPart 
		ApplyPrefab = 227,				// GameObjectHeaderPart 
		#else							// introduced with improved prefab workflow in Unity 2018.3
		OpenPrefab = 228,				// GameObjectHeaderPart 
		SelectPrefab = 229,
		PrefabOverrides = 230,
		#endif
		#endif

		#endregion

		Button = 300,					// MethodDrawer
		Picker = 310,                   // ColorBaseDrawer
		Eyedropper = 320,               // ColorBaseDrawer, ObjectReferenceDrawer

		LineNumber = 400,				// FormattedTextAssetDrawer
		Line = 401,						// FormattedTextAssetDrawer
		SaveChangesButton = 410,		// FormattedTextAssetDrawer
		DiscardChangesButton = 411,		// FormattedTextAssetDrawer
		UnappliedChangesButton = 412,	// FormattedTextAssetDrawer

		AssetLabelsButton = 500,		// PreviewDrawer
		AssetLabel = 501,				// PreviewDrawer

		TextField = 600					// FilterField
	}
}