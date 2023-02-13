//#define DEBUG_ENABLED

#if !UNITY_2023_1_OR_NEWER
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Utility class for checking or modifying Scripting Define Symbols in Player Settings.
	/// </summary>
	public static class ScriptingDefines
	{
		public static bool Contains(string define)
		{
			var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
			if(buildTarget == BuildTargetGroup.Unknown)
			{
				buildTarget = BuildTargetGroup.Standalone;
			}
			return Contains(PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget), define);
		}

		public static bool Contains(BuildTargetGroup buildTarget, string define)
		{
			if(buildTarget == BuildTargetGroup.Unknown)
			{
				buildTarget = BuildTargetGroup.Standalone;
			}
			return Contains(PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget), define);
		}
		
		public static bool Add(string define)
		{
			var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
			if(buildTarget == BuildTargetGroup.Unknown)
			{
				buildTarget = BuildTargetGroup.Standalone;
			}

			var scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);
			if(Contains(scriptingDefineSymbols, define))
			{
				return false;
			}

			PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, define+";"+scriptingDefineSymbols);
			return true;
		}

		public static bool Add(string define1, string define2)
		{
			var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
			if(buildTarget == BuildTargetGroup.Unknown)
			{
				buildTarget = BuildTargetGroup.Standalone;
			}

			var scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);
			bool changed = false;

			if(!Contains(scriptingDefineSymbols, define2))
			{
				changed = true;
				scriptingDefineSymbols = define2+";"+scriptingDefineSymbols;
			}

			if(!Contains(scriptingDefineSymbols, define1))
			{
				changed = true;
				scriptingDefineSymbols = define1+";"+scriptingDefineSymbols;
			}

			if(changed)
			{
				PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, scriptingDefineSymbols);
				return true;
			}
			return false;
		}

		public static bool Remove(string define)
		{
			var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
			if(buildTarget == BuildTargetGroup.Unknown)
			{
				buildTarget = BuildTargetGroup.Standalone;
			}

			var scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);
			if(!Remove(ref scriptingDefineSymbols, define))
			{
				return false;
			}
			PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, scriptingDefineSymbols);
			return true;
		}

		public static bool Remove(string define1, string define2)
		{
			var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
			if(buildTarget == BuildTargetGroup.Unknown)
			{
				buildTarget = BuildTargetGroup.Standalone;
			}

			var scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);

			if(Remove(ref scriptingDefineSymbols, define1) | Remove(ref scriptingDefineSymbols, define2))
			{
				PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, scriptingDefineSymbols);
				return true;
			}
			return false;
		}

		private static bool Contains(string scriptingDefineSymbols, string define)
		{
			return IndexOf(scriptingDefineSymbols, define) != -1;
		}

		private static bool Remove(ref string scriptingDefineSymbols, string define)
		{
			int foundAt = IndexOf(scriptingDefineSymbols, define);	
			
			// define not found in list
			if(foundAt == -1)
			{
				return false;
			}

			int totalLength = scriptingDefineSymbols.Length;
			int defineLength = define.Length;

			// list consists of only this one define
			if(totalLength == defineLength)
			{
				scriptingDefineSymbols = "";
			}
			// define at beginning of list
			else if(foundAt == 0)
			{
				scriptingDefineSymbols = scriptingDefineSymbols.Substring(defineLength + 1);
			}
			// define at end of list
			else if(foundAt == totalLength - defineLength)
			{
				scriptingDefineSymbols = scriptingDefineSymbols.Substring(0, foundAt - 1);
			}
			// define in the middle of list
			else
			{
				scriptingDefineSymbols = scriptingDefineSymbols.Substring(0, foundAt - 1) + scriptingDefineSymbols.Substring(foundAt + defineLength + 1);
			}
			return true;
		}

		private static int IndexOf(string scriptingDefineSymbols, string define)
		{
			int count = scriptingDefineSymbols.Length;
			if(count == 0)
			{
				return -1;
			}
			int start = 0;
			int end = scriptingDefineSymbols.IndexOf(';');
			
			while(end != -1)
			{
				#if DEV_MODE && DEBUG_ENABLED
				UnityEngine.Debug.Log("ScriptingDefines: Checking part "+scriptingDefineSymbols.Substring(start, end - start)+"...");
				#endif
				if(string.Equals(scriptingDefineSymbols.Substring(start, end - start), define))
				{
					return start;
				}

				start = end + 1;
				if(start >= count)
				{
					return -1;
				}
				end = scriptingDefineSymbols.IndexOf(';', start);
			}

			if(string.Equals(scriptingDefineSymbols.Substring(start), define))
			{
				return start;
			}

			return -1;
		}
	}
}
#endif