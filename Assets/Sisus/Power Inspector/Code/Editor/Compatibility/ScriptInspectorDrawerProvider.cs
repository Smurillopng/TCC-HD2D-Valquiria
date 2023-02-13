#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Sisus.Compatibility
{
	public class ScriptInspectorDrawerProvider : DrawerFromPluginProvider
	{
		public static readonly Type DrawerType = typeof(ScriptInspectorDrawer);
		public static readonly Type DrawerTypeForTextAssets = typeof(ScriptInspectorForTextAssetsDrawer);
		private static Type scriptInspectorType;
		private static bool installationDetected;

		[CanBeNull]
		public static Type ScriptInspectorType
		{
			get
			{
				if(!installationDetected)
				{
					installationDetected = true;
					scriptInspectorType = TypeExtensions.GetType("ScriptInspector.ScriptInspector");
				}
				return scriptInspectorType;
			}
		}

		/// <inheritdoc/>
		public override bool IsActive
		{
			get
			{
				return ScriptInspectorType != null;
			}
		}
		
		/// <inheritdoc/>
		public override void AddAssetDrawer(Dictionary<Type, Type> assetDrawerByType, Dictionary<string, Type> assetDrawerByExtension)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(ScriptInspectorType != null);
			UnityEngine.Debug.Assert(DrawerType != null);
			UnityEngine.Debug.Assert(DrawerTypeForTextAssets != null);
			UnityEngine.Debug.Assert(IsActive);
			#endif

			assetDrawerByType[typeof(UnityEditor.MonoScript)] = DrawerType;

			assetDrawerByExtension[".ini"] = DrawerTypeForTextAssets;
			assetDrawerByExtension[".json"] = DrawerTypeForTextAssets;
			assetDrawerByExtension[".txt"] = DrawerTypeForTextAssets;
			assetDrawerByExtension[".yaml"] = DrawerTypeForTextAssets;
			assetDrawerByExtension[".shader"] = DrawerTypeForTextAssets;
		}
	}
}
#endif