using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class AddComponentMenuGroupConfig
	{
		public string label;
		
		[ContextMenuItem("UnityEngine Group", "SetPreviewForUnityEngineGroup"),
		ContextMenuItem("UnityEditor Group", "SetPreviewForUnityEditorGroup")]
		public Texture customIcon;
		
		public AddComponentMenuGroupConfig()
		{

		}
		
		public AddComponentMenuGroupConfig(string setLabel, Texture setIcon = null)
		{
			label = setLabel;
			if(setIcon != null)
			{
				if(setIcon != InspectorUtility.Preferences.graphics.DirectoryIcon)
				{
					customIcon = setIcon;
				}
			}
		}

		[UsedImplicitly]
		private void SetPreviewForUnityEngineGroup()
		{
			customIcon = InspectorUtility.Preferences.graphics.DirectoryIconUnity;
		}

		[UsedImplicitly]
		private void SetPreviewForUnityEditorGroup()
		{
			customIcon = InspectorUtility.Preferences.graphics.DirectoryIconUnityEditor;
		}
		
		public override string ToString()
		{
			return label;
		}
	}
}