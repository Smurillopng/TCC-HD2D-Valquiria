using System;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class MouseoverEffectSettings
	{
		[Tooltip("A rectangular graphic will be drawn around the header of GUI drawer that represent Unity Objects when mouseovered.")]
		public bool unityObjectHeader = false;
		[Tooltip("Text color of header of GUI drawer that represent Unity Objects will change when mouseovered.")]
		public bool unityObjectHeaderTint = true;

		[Tooltip("A rectangular graphic will be drawn around buttons on header toolbars when mouseovered.")]
		public bool headerButton = true;
		[Tooltip("A rectangular graphic will be drawn around prefix labels of interactive GUI drawer when mouseovered.")]
		public bool prefixLabel = true;
		[Tooltip("If true, prefix label color of clickable GUI drawer will change when mouseovered.")]
		public bool prefixLabelTint = true;
	}
}