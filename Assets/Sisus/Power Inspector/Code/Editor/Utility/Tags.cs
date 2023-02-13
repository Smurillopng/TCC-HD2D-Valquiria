using System;

namespace Sisus
{
	public static class Tags
	{
		private static readonly string[] BuiltInTags =
		{
			"Untagged",
			"Respawn",
			"Finish",
			"EditorOnly",
			"MainCamera",
			"Player",
			"GameController"
		};

		public static bool TagExists(string test)
		{
			if(Array.IndexOf(BuiltInTags, test) != -1)
			{
				return true;
			}

			#if UNITY_EDITOR
			var findTagManagerAsset = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
			if(findTagManagerAsset.Length == 0)
			{
				#if DEV_MODE
				UnityEngine.Debug.LogError("Failed to find TagManager");
				#endif
				return false;
			}

			var tagManager = findTagManagerAsset[0];
			var tagsSerializedProperty = new UnityEditor.SerializedObject(tagManager).FindProperty("tags");
			for(int n = tagsSerializedProperty.arraySize - 1; n >= 0; n--)
			{
				var tag = tagsSerializedProperty.GetArrayElementAtIndex(n).stringValue;
				if(string.Equals(tag, test))
				{
					#if DEV_MODE && DEBUG_ENABLED
					UnityEngine.Debug.Log("CanPasteTag: YES (tag found in tag manager at index "+n+")");
					#endif
					return true;
				}
				#if DEV_MODE && DEBUG_ENABLED
				UnityEngine.Debug.Log("\""+test+"\" != \""+tag+"\"...");
				#endif
			}
			#if DEV_MODE && DEBUG_ENABLED
			UnityEngine.Debug.Log("CanPasteTag: NO (tag not found in tag manager's tag array of size "+tagsSerializedProperty.arraySize+")");
			#endif

			#endif

			return false;
		}
	}
}