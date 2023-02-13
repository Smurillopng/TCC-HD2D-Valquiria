using System;
using UnityEngine;
using Sisus.Attributes;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	/// <summary>
	/// Class responsible for determining when Power Inspector is uninstalled and handling removal of related defines in Scripting Define Symbols of Player Settings.
	/// </summary>
	#if DEV_MODE
	[CreateAssetMenu]
	#endif
	public class PluginCompatibilityPackageInstaller : ScriptableObject
	{
		[NotNullOrEmpty, Tooltip("Full name of a type that is found in the plugin.\nWe detect whether or not type by name exists to determine if the plugin is currently installed or not, and install the compatibility package if so.")]
		public string fullTypeName;

		#if UNITY_EDITOR
		private bool typeFetched = false;
		private Type type = null;

		public bool autoInstallEnabled = true;

		[ShowInInspector]
		public Type Type
		{
			get
			{
				if(!typeFetched)
				{
					typeFetched = true;
					type = TypeExtensions.GetType(fullTypeName);
				}
				return type;
			}
		}

		public string UnityPackagePath
		{
			get
			{
				return GetDirectoryPath() + "/package.unitypackage";
			}
		}

		public string InstallPath
		{
			get
			{
				return GetDirectoryPath() + "/package";
			}
		}

		[ShowInInspector]
		public bool PluginIsInstalled
		{
			get
			{
				return Type != null;
			}
		}

		[ShowInInspector]
		public bool CompatibilityPackageIsInstalled
		{
			get
			{
				return AssetDatabase.IsValidFolder(InstallPath);
			}
		}

		[PSpace(3f)]
		[Button("Install"), ShowIf("CompatibilityPackageIsInstalled", false)]
		[ContextMenu("Install")]
		private void InstallManually()
		{
			autoInstallEnabled = false;
			Install();
		}

		public void Install()
		{
			if(CompatibilityPackageIsInstalled)
			{
				Debug.LogWarning("Can't install " + GetDirectoryPath() + " because it is already installed.");
				return;
			}

			Debug.Log("Installing Compatibility Package "+ GetDirectoryPath()+"...", this);

			AssetDatabase.ImportPackage(UnityPackagePath, false);
		}

		[Button("Uninstall"), ShowIf("CompatibilityPackageIsInstalled", true)]
		[ContextMenu("Uninstall")]
		private void UninstallManually()
		{
			autoInstallEnabled = false;
			AssetDatabase.StartAssetEditing();
			Uninstall();
			AssetDatabase.StopAssetEditing();
			AssetDatabase.Refresh();
		}

		public void Uninstall()
		{
			if(!CompatibilityPackageIsInstalled)
			{
				Debug.LogWarning("Can't uninstall " + GetDirectoryPath() + " because it is not installed.");
				return;
			}

			Debug.Log("Uninstalling Compatibility Package "+ GetDirectoryPath()+"...", this);

			FileUtil.DeleteFileOrDirectory(InstallPath);
		}

		private string GetDirectoryPath()
		{
			var scriptAssetPath = AssetDatabase.GetAssetPath(this);
			var directory = Path.GetDirectoryName(scriptAssetPath);
			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("scriptAssetPath="+scriptAssetPath+", directory="+directory);
			#endif
			return directory;
		}
		#endif
	}
}