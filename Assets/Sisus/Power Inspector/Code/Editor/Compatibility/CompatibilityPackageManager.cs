//#define DEBUG_ENABLED

using System;
using JetBrains.Annotations;
using UnityEditor;
using System.Collections.Generic;

namespace Sisus
{
	/// <summary>
	/// Class responsible for determining when Power Inspector is uninstalled and handling removal of related defines in Scripting Define Symbols of Player Settings.
	/// </summary>
	[InitializeOnLoad]
	public sealed class CompatibilityPackageManager : UnityEditor.AssetModificationProcessor
	{
		private static PluginCompatibilityPackageInstaller[] packageInstallers = new PluginCompatibilityPackageInstaller[0];

		[UsedImplicitly]
		static CompatibilityPackageManager()
		{
			EditorApplication.delayCall += InstallPackages;
		}

		private static void InstallPackages()
		{
			if(!ApplicationUtility.IsReady())
			{
				EditorApplication.delayCall += InstallPackages;
				return;
			}

			var packageInstallerGuids = AssetDatabase.FindAssets("t:PluginCompatibilityPackageInstaller");
			int count = packageInstallerGuids.Length;

			#if DEV_MODE && DEBUG_ENABLED
			UnityEngine.Debug.Log("CompatibilityPackageManager found " + count + " PluginCompatibilityPackageInstallers");
			#endif

			packageInstallers = new PluginCompatibilityPackageInstaller[count];

			List<PluginCompatibilityPackageInstaller> installPackages = null;

			for(int n = count - 1; n >= 0; n--)
			{
				var packageInstaller = AssetDatabase.LoadAssetAtPath<PluginCompatibilityPackageInstaller>(AssetDatabase.GUIDToAssetPath(packageInstallerGuids[n]));
				packageInstallers[n] = packageInstaller;

				#if DEV_MODE
				UnityEngine.Debug.Log(StringUtils.ToColorizedString("PluginIsInstalled=", packageInstaller.PluginIsInstalled, ", CompatibilityPackageIsInstalled=", packageInstaller.CompatibilityPackageIsInstalled));
				#endif

				if(packageInstaller.autoInstallEnabled && packageInstaller.PluginIsInstalled && !packageInstaller.CompatibilityPackageIsInstalled)
				{
					if(installPackages == null)
					{
						installPackages = new List<PluginCompatibilityPackageInstaller>();
					}
					installPackages.Add(packageInstaller);
				}
			}

			if(installPackages != null)
			{
				AssetDatabase.StartAssetEditing();
				for(int n = installPackages.Count - 1; n >= 0; n--)
				{
					installPackages[n].Install();
				}
				AssetDatabase.StopAssetEditing();
			}
		}

		[UsedImplicitly]
		private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
		{
			#if DEV_MODE && DEBUG_ENABLED
			UnityEngine.Debug.Log("OnWillDeleteAsset: "+assetPath+" "+options);
			#endif

			if(!assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
			{
				if(!assetPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
				{
					return AssetDeleteResult.DidNotDelete;
				}

				var fullAssetPath = FileUtility.LocalToFullPath(assetPath);
				var assembly = System.Reflection.Assembly.LoadFile(fullAssetPath);
				if(assembly == null)
				{
					return AssetDeleteResult.DidNotDelete;
				}
				
				for(int n = packageInstallers.Length - 1; n >= 0; n--)
				{
					var targetType = packageInstallers[n].Type;
					if(targetType == null)
					{
						continue;
					}

					var packageInstaller = packageInstallers[n];

					if(targetType.Assembly == assembly && packageInstaller.autoInstallEnabled && packageInstaller.CompatibilityPackageIsInstalled)
					{
						packageInstaller.Uninstall();
						return AssetDeleteResult.DidNotDelete;
					}
				}

				return AssetDeleteResult.DidNotDelete;
			}

			var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
			if(script == null)
			{
				return AssetDeleteResult.DidNotDelete;
			}

			var type = script.GetClass();
			if(type == null)
			{
				return AssetDeleteResult.DidNotDelete;
			}

			for(int n = packageInstallers.Length - 1; n >= 0; n--)
			{				
				if(packageInstallers[n].Type == type && packageInstallers[n].CompatibilityPackageIsInstalled)
				{
					packageInstallers[n].Uninstall();
					return AssetDeleteResult.DidNotDelete;
				}
			}

			return AssetDeleteResult.DidNotDelete;
		}
	}
}