#define DEBUG_ENABLED

using System;
using JetBrains.Annotations;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Class responsible for determining when Power Inspector is uninstalled and handling removal of related defines in Scripting Define Symbols of Player Settings.
	/// </summary>
	[UsedImplicitly]
	public class PowerInspectorUninstaller : UnityEditor.AssetModificationProcessor
	{
		[UsedImplicitly]
		private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
		{
			#if DEV_MODE && DEBUG_ENABLED
			UnityEngine.Debug.Log("OnWillDeleteAsset: "+assetPath+" "+options);
			#endif
			
			if(assetPath.EndsWith("Inspector/PowerInspector/PowerInspector.cs", StringComparison.Ordinal))
			{
				UnityEngine.Debug.Log("Detected uninstall of Power Inspector. Removing POWER_INSPECTOR define from Scripting Define Symbols in Player Settings.");

				ScriptingDefines.Remove("POWER_INSPECTOR");
				ScriptingDefines.Remove("DEV_MODE");

				var hierarchyFolderDrawer = FileUtility.FindAssetByName("HierarchyFolderDrawer", false);
				if(hierarchyFolderDrawer != null)
				{
					FileUtil.DeleteFileOrDirectory(hierarchyFolderDrawer);
				}
			}
			
			return AssetDeleteResult.DidNotDelete;
		}
	}
}