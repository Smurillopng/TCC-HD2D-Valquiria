#if UNITY_EDITOR
using System;
using System.Reflection;
using Object = UnityEngine.Object;

#if CSHARP_7_3_OR_NEWER
using Sisus.Vexe.FastReflection;
#endif

namespace Sisus
{
	/// <summary> Can be used for drawing the asset bundle GUI for target objects in the editor. </summary>
	public class AssetBundleGUIDrawer
	{
		private readonly object[] onAssetBundleNameGUIParams = new object[] { new Object[0] };
		private readonly object assetBundleNameGUI;
		#if CSHARP_7_3_OR_NEWER
		private readonly MethodCaller<object, object> onAssetBundleNameGUI;
		#else
		private readonly MethodInfo onAssetBundleNameGUI;
		#endif

		public AssetBundleGUIDrawer()
		{
			#if UNITY_EDITOR
			var type = Types.GetInternalEditorType("UnityEditor.AssetBundleNameGUI");
			assetBundleNameGUI = Activator.CreateInstance(type);
			#if CSHARP_7_3_OR_NEWER
			onAssetBundleNameGUI = type.GetMethod("OnAssetBundleNameGUI", BindingFlags.Public | BindingFlags.Instance).DelegateForCall();
			#else
			onAssetBundleNameGUI = type.GetMethod("OnAssetBundleNameGUI", BindingFlags.Public | BindingFlags.Instance);
			#endif
			#endif
		}
		
		public void ResetState()
		{
			onAssetBundleNameGUIParams[0] = ArrayPool<Object>.ZeroSizeArray;
		}

		public void SetAssets(Object[] targets)
		{
			onAssetBundleNameGUIParams[0] = targets;
		}

		public void Draw()
		{
			#if CSHARP_7_3_OR_NEWER
			onAssetBundleNameGUI(assetBundleNameGUI, onAssetBundleNameGUIParams);
			#else
			onAssetBundleNameGUI.Invoke(assetBundleNameGUI, onAssetBundleNameGUIParams);
			#endif
		}
	}
}
#endif