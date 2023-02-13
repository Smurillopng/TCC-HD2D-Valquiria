using UnityEditor;
using UnityEditor.Build;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

namespace Sisus
{
	
	
	#if UNITY_2018_1_OR_NEWER
	class PowerInspectorBuildPreprocessor : IPreprocessBuildWithReport
	#else
	class PowerInspectorBuildPreprocessor : IPreprocessBuild
	#endif
	{
		public int callbackOrder
		{
			get
			{
				return 0;
			}
		}

		#if UNITY_2018_1_OR_NEWER
		public void OnPreprocessBuild(BuildReport report)
		#else
		public void OnPreprocessBuild(BuildTarget target, string path)
		#endif
		{
			foreach(var inspector in InspectorManager.Instance().ActiveInstances)
			{
				var window = inspector.InspectorDrawer as PowerInspectorWindow;
				if(window != null)
				{
					window.editors.OnBeforeAssemblyReload();
				}
			}
		}
	}
}