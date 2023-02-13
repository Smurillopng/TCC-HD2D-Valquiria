using System;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Defines preferences that can be used to decide whether some Power Inspector enhancements should also spread out to the default inspector.
	/// </summary>
	[Serializable]
	public class DefaultInspectorEnhancements
	{
		[HideInInspector] // TO DO: implement this
		public bool overrideGameObjectEditor = false;
		[HideInInspector] // TO DO: implement this
		public bool overrideComponentEditors = false;
		[HideInInspector] // TO DO: implement this
		public bool overrideAssetEditors = false;

		public FieldContextMenuItems enhanceFieldContextMenu = FieldContextMenuItems.None;

		public ObjectContextMenuItems enhanceUnityObjectContextMenu = ObjectContextMenuItems.ViewInPowerInspector | ObjectContextMenuItems.PeekInPowerInspector;
	}

	[Flags]
	public enum FieldContextMenuItems
	{
		None = (1 << 0),
		Reset = (1 << 5),
		CopyPaste = (1 << 10),
		InspectStaticMembers = (1 << 15),
		Peek = (1 << 20)
	}

	[Flags]
	public enum ObjectContextMenuItems
	{
		None = (1 << 0),
		ViewInPowerInspector = (1 << 5),
		PeekInPowerInspector = (1 << 10)
	//	AutoName = (1 << 15),
	//	HideFlags = (1 << 20),
	//	InspectStaticMembers = (1 << 25),
	//	CollapseAll = (1 << 30)
	}
}