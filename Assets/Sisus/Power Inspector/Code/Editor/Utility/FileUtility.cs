using System;
using UnityEngine;
using System.IO;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	public static class FileUtility
	{
		private const char Null = (char)0;
		
		public static bool IsPackageAsset(string localPath)
		{
			return localPath.StartsWith("Packages", StringComparison.OrdinalIgnoreCase);
		}

		#if UNITY_EDITOR
		public static string GUIDToAssetPath(string guid)
		{
			return AssetDatabase.GUIDToAssetPath(guid);
		}
		#endif

		public static string LocalToFullPath(string localPath)
		{
			return LocalToFullPath(localPath, IsPackageAsset(localPath));
		}

		public static string LocalToFullPath(string localPath, bool isPackage)
		{
			#if UNITY_2017_2_OR_NEWER
			return isPackage ? GetPackageFullPath(localPath) : LocalAssetsPathToFullPath(localPath);
			#else
			return LocalAssetsPathToFullPath(localPath);
			#endif
		}

		#if UNITY_2017_2_OR_NEWER
		public static string GetPackageFullPath(string localPath)
		{
			#if DEV_MODE
			Debug.Assert(localPath.StartsWith("Packages", StringComparison.OrdinalIgnoreCase), "GetPackagePath: localPath " + StringUtils.ToString(localPath) + " did not start with \"Packages\"");
			#endif

			return Path.GetFullPath(localPath);
		}
		#endif

		public static string LocalAssetsPathToFullPath(string localPath)
		{
			if(localPath.Length <= 7)
			{
				#if DEV_MODE
				Debug.LogWarning("LocalAssetsPathToFullPath: localPath " + StringUtils.ToString(localPath) + " too short to be a valid assets path.");
				#endif
				return Path.GetFullPath(localPath);
			}
			
			if(localPath.StartsWith("Library/", StringComparison.Ordinal))
			{
				#if DEV_MODE
				Debug.LogWarning("LocalAssetsPathToFullPath was called for "+localPath);
				#endif
				return localPath;
			}

			#if UNITY_EDITOR
			if(string.Equals(localPath, "Resources/unity_builtin_extra"))
			{
				var assetsPath = Application.dataPath;
				var projectRoot = Path.GetDirectoryName(assetsPath);
				string platformFolder = EditorUserBuildSettings.activeBuildTarget.ToString();
				if(platformFolder.StartsWith("Standalone", StringComparison.Ordinal))
				{
					platformFolder = platformFolder.Substring(10);
					platformFolder = platformFolder.Replace("Windows", "Win");
				}
				if(platformFolder.EndsWith("Universal", StringComparison.Ordinal))
				{
					platformFolder = platformFolder.Substring(platformFolder.Length - 9);
				}
				return projectRoot + "/Library/PlayerDataCache/" + platformFolder + "/Data/Resources/unity_builtin_extra";
			}
			#endif

			return Path.GetFullPath(localPath);
		}

		public static bool IsBinaryFile(string filePath)
		{
			int lastNullCharacter = -100;
			using(var streamReader = new StreamReader(filePath))
			{
				for(int n = 0; n < 1000; n++)
				{
					int character = streamReader.Read();

					if(character == Null)
					{
						//two consecutive nulls: looks like a binary file!
						if(lastNullCharacter == n - 1)
						{
							return true;
						}
						lastNullCharacter = n;
					}
				}
			}
			return false;
		}

		#if UNITY_EDITOR
		public static void Disable(Object[] targets, string extension)
		{
			int count = targets.Length;
			if(count > 1)
			{
				if(DrawGUI.Active.DisplayDialog("Disable " + count + " Targets?", "Do you want to disable the " + count + " targets by adding the supernumerary extension \"" + extension + "\" to their filenames?", "Disable", "Cancel"))
				{
					for(int n = count - 1; n >= 0; n--)
					{
						var target = targets[n];
						string localPath = AssetDatabase.GetAssetPath(target);
						FileUtil.MoveFileOrDirectory(localPath, localPath + extension);
					}
					AssetDatabase.Refresh();
				}
			}
			else
			{
				var target = targets[0];
				string localPath = AssetDatabase.GetAssetPath(target);
				string filename = Path.GetFileName(localPath);
				if(!string.IsNullOrEmpty(filename))
				{
					if(DrawGUI.Active.DisplayDialog("Disable Target?", "Do you want to disable the target \"" + filename + "\" by adding the supernumerary extension \"" + extension + "\" to its filename?", "Disable", "Cancel"))
					{
						FileUtil.MoveFileOrDirectory(localPath, localPath + extension);
						AssetDatabase.Refresh();
					}
				}
			}
		}
		#endif

		#if UNITY_EDITOR
		public static void Undisable(Object[] targets)
		{
			int count = targets.Length;
			if(count > 1)
			{
				if(DrawGUI.Active.DisplayDialog("Undisable " + count+" Targets?", "Do you want to undisable the " + count + " targets by removing supernumerary extensions from their filenames?", "Undisable", "Cancel"))
				{
					for(int n = count - 1; n >= 0; n--)
					{
						var target = targets[n];
						string path = AssetDatabase.GetAssetPath(target);
						string filename = Path.GetFileName(path);
						if(!string.IsNullOrEmpty(filename))
						{
							int dot = filename.LastIndexOf('.');
							int secondDot = filename.LastIndexOf('.', dot - 1);
							if(dot == -1 || secondDot == -1)
							{
								Debug.LogError("Can't undisable target \"" + filename + "\" because it doesn't have a double extension.");
								continue;
							}

							int lastExtensionStartsAt = path.Length - filename.Length + dot;
							FileUtil.MoveFileOrDirectory(path, path.Substring(0, lastExtensionStartsAt));
						}
					}
					AssetDatabase.Refresh();
				}
			}
			else
			{
				var target = targets[0];
				string path = AssetDatabase.GetAssetPath(target);
				string filename = Path.GetFileName(path);
				if(!string.IsNullOrEmpty(filename))
				{
					int dot = filename.LastIndexOf('.');
					int secondDot = filename.LastIndexOf('.', dot - 1);
					if(dot == -1 || secondDot == -1)
					{
						Debug.LogError("Can't undisable target \"" + filename + "\" because it doesn't have a double extension.");
						return;
					}

					if(DrawGUI.Active.DisplayDialog("Undisable Targets?", "Do you want to undisable the target \"" + filename + "\" by removing the supernumerary extension \"" + Path.GetExtension(path) +"\" from it's filename?", "Undisable", "Cancel"))
					{
						int lastExtensionStartsAt =  path.Length - filename.Length + dot;
						FileUtil.MoveFileOrDirectory(path, path.Substring(0, lastExtensionStartsAt));
						AssetDatabase.Refresh();
					}
				}
			}
		}
		#endif

		public static string FilenameFromType([NotNull]Type classType)
		{
			string name = classType.Name;

			// If it's a generic type, parse out generic type information from name
			if(classType.IsGenericType)
			{
				int i = name.IndexOf('`');
				if(i != -1)
				{
					name = name.Substring(0, i);
				}
			}

			return name;
		}

		#if UNITY_EDITOR
		[NotNull]
		public static string FindAssetByName([NotNull]string name, bool useFullPath)
		{
			var guids = AssetDatabase.FindAssets(name);
			int count = guids.Length;
			if(count == 0)
			{
				return "";
			}

			for(int n = count - 1; n >= 0; n--)
			{
				var guid = guids[n];
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var filename = Path.GetFileNameWithoutExtension(path);
				if(string.Equals(filename, name, StringComparison.OrdinalIgnoreCase))
				{
					return useFullPath ? LocalToFullPath(path) : path;
				}
			}

			return "";
		}
		#endif

		#if UNITY_EDITOR
		[NotNull]
		public static bool FindAssetsByName([NotNull]string name, [NotNull]ref System.Collections.Generic.List<string> addToList, bool useFullPaths)
		{
			var guids = AssetDatabase.FindAssets(name);
			int count = guids.Length;
			if(count == 0)
			{
				return false;
			}
			
			bool found = false;

			for(int n = count - 1; n >= 0; n--)
			{
				var guid = guids[n];
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var filename = Path.GetFileNameWithoutExtension(path);
				if(string.Equals(filename, name, StringComparison.OrdinalIgnoreCase))
				{
					addToList.Add(useFullPaths ? LocalToFullPath(path) : path);
					found = true;
				}
			}

			return found;
		}
		#endif

		#if UNITY_EDITOR
		[CanBeNull]
		public static T LoadAssetByName<T>([NotNull]string name) where T : Object
		{
			var guids = AssetDatabase.FindAssets(name);
			int count = guids.Length;
			if(count == 0)
			{
				return null;
			}

			for(int n = count - 1; n >= 0; n--)
			{
				var guid = guids[n];
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var filename = Path.GetFileNameWithoutExtension(path);
				if(string.Equals(filename, name, StringComparison.OrdinalIgnoreCase))
				{
					var asset = AssetDatabase.LoadAssetAtPath<T>(path);
					if(asset != null)
					{
						return asset;
					}
				}
			}

			return null;
		}
		#endif

		#if UNITY_EDITOR
		[CanBeNull]
		public static MonoScript FindScriptFile([NotNull]Type classType)
		{
			if(TypeExtensions.IsUnityAssemblyThreadSafe(classType.Assembly))
			{
				#if DEV_MODE
				Debug.LogWarning("FindScriptFile("+StringUtils.ToString(classType)+ ") returning null because classType.Assembly \"" + classType.Assembly.GetName().Name + "\" is an Unity assembly.");
				#endif
				return null;
			}

			string name = classType.Name;

			if(classType.IsGenericType)
			{
				// Parse out generic type information from generic type name
				int i = name.IndexOf('`');
				if(i != -1)
				{
					name = name.Substring(0, i);
				}

				// Additionally, convert generic types to their generic type defitions.
				// E.g. List<string> to List<>.
				if(!classType.IsGenericTypeDefinition)
				{
					classType = classType.GetGenericTypeDefinition();
				}
			}

			var guids = AssetDatabase.FindAssets(name + " t:MonoScript");

			int count = guids.Length;
			if(count == 0)
			{
				return null;
			}

			MonoScript fallback = null;

			for(int n = count - 1; n >= 0; n--)
			{
				var guid = guids[n];
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var filename = Path.GetFileNameWithoutExtension(path);
				if(string.Equals(filename, name, StringComparison.OrdinalIgnoreCase))
				{
					var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
					var scriptClassType = scriptAsset.GetClass();
					if(scriptClassType == classType)
					{
						return scriptAsset;
					}

					if(scriptClassType == null)
					{
						fallback = scriptAsset;
					}
					
					#if DEV_MODE
					Debug.LogWarning("FindScriptFile("+StringUtils.ToString(classType)+") ignoring file @ \""+path+"\" because MonoScript.GetClass() result "+StringUtils.ToStringSansNamespace(scriptClassType)+" did not match classType.");
					#endif
				}
			}

			// Second pass: test files where filename is only a partial match for class name.
			// E.g. class Header could be defined in file HeaderAttribute.cs.
			if(count > 1)
			{
				for(int n = count - 1; n >= 0; n--)
				{
					var guid = guids[n];
					var path = AssetDatabase.GUIDToAssetPath(guid);
					var filename = Path.GetFileNameWithoutExtension(path);
					if(filename.Length != name.Length) // skip testing exact matches a second time
					{
						var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
						var scriptClassType = scriptAsset.GetClass();
						if(scriptClassType == classType)
						{
							return scriptAsset;
						}

						#if DEV_MODE
						Debug.LogWarning("FindScriptFile("+StringUtils.ToString(classType)+") second pass: ignoring partial match @ \""+path+"\" because MonoScript.GetClass() result "+StringUtils.ToStringSansNamespace(scriptClassType)+" did not match classType.");
						#endif
					}
				}
			}

			// If was unable to verify correct script class type using MonoScript.GetClass()
			// but there was a probable match whose GetClass() returned null (seems to happen
			// with all generic types), then return that.
			if(fallback != null)
			{
				#if DEV_MODE
				Debug.LogWarning("FindScriptFile("+StringUtils.ToString(classType)+") returning fallback result @ \""+AssetDatabase.GetAssetPath(fallback)+"\".");
				#endif
				return fallback;
			}

			#if DEV_MODE
			Debug.LogWarning("FindScriptFile("+StringUtils.ToString(classType)+") failed to find MonoScript for classType "+StringUtils.ToString(classType)+" AssetDatabase.FindAssets(\""+name + " t:MonoScript\") returned "+count+" results: "+StringUtils.ToString(GuidsToAssetPaths(guids)));
			#endif

			return null;
		}
		#endif

		#if UNITY_EDITOR
		public static Object[] LoadAssetsAtPath(string[] paths)
		{
			int count = paths.Length;
			var result = ArrayPool<Object>.Create(count);
			for(int n = count - 1; n >= 0; n--)
			{
				result[n] = AssetDatabase.LoadAssetAtPath(paths[n], Types.UnityObject);
			}
			return result;
		}

		public static Object[] LoadAssetsByGuids(string[] guids)
		{
			int count = guids.Length;
			var result = ArrayPool<Object>.Create(count);
			for(int n = count - 1; n >= 0; n--)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[n]);
				result[n] = AssetDatabase.LoadAssetAtPath(path, Types.UnityObject);
			}
			return result;
		}

		public static string[] GuidsToAssetPaths(string[] guids)
		{
			int count = guids.Length;
			var result = ArrayPool<string>.Create(count);
			for(int n = count - 1; n >= 0; n--)
			{
				result[n] = AssetDatabase.GUIDToAssetPath(guids[n]);
			}
			return result;
		}
		#endif

		#if UNITY_EDITOR
		public static string GetHumanReadableNameForAsset(Type type, string path)
		{
			if(type == null)
			{
				return "";
			}

			if(type == Types.DefaultAsset)
			{
				string extension = Path.GetExtension(path);
				if(!string.IsNullOrEmpty(extension))
				{
					switch(extension.ToLowerInvariant())
					{
						case ".exe":
							return "Executable";
						default:
							var result = StringUtils.SplitPascalCaseToWords(extension.Substring(1));
							return !result.EndsWith("Asset") ? string.Concat(result, " Asset") : result;
					}
					
				}
				return "Default Asset";
			}

			return StringUtils.SplitPascalCaseToWords(StringUtils.ToStringSansNamespace(type));
		}
		#endif

		#if UNITY_EDITOR
		public static string GetAssetPath([NotNull]Object target)
		{
			var path = AssetDatabase.GetAssetPath(target);
			if(string.Equals(path, "Library/unity editor resources", StringComparison.Ordinal))
			{
				var type = target.GetType();
				if(type == Types.Texture || type == Types.Texture2D)
				{
					return "icons/" + target.name + ".png";
				}
				return target.name;
			}
			
			return path;
		}
		#endif

		#if UNITY_EDITOR
		public static Object LoadAssetAtPath(string path)
		{
			if(path.StartsWith("icons/", StringComparison.OrdinalIgnoreCase))
			{
				return EditorGUIUtility.Load(path.Substring(6));
			}
			return AssetDatabase.LoadAssetAtPath<Object>(path);
		}
		#endif

		/// <summary>
		/// Returns parent directory of given full or local path.
		/// If has no parent directory, returns an empty string.
		/// </summary>
		/// <param name="path"> Full or local directory path. </param>
		/// <returns> Parent directory path or an empty string. </returns>
		[NotNull]
		public static string GetParentDirectory(string path)
		{
			int lastIndex1 = path.LastIndexOf('/');
			int lastIndex2 = path.LastIndexOf('\\');

			int lastIndex;
			if(lastIndex1 != -1)
			{
				if(lastIndex2 != -1)
				{
					lastIndex = Mathf.Min(lastIndex1, lastIndex2);
				}
				else
				{
					lastIndex = lastIndex1;
				}
			}
			else if(lastIndex2 != -1)
			{
				lastIndex = lastIndex2;
			}
			else
			{
				return "";
			}

			return path.Substring(0, lastIndex);
		}

		public static string GetChildDirectory(string path, string childDirectory)
		{
			if(path.EndsWith("/") || path.EndsWith("\\"))
			{
				return path + childDirectory;
			}
			return path + "/" + childDirectory;
		}

		/// <summary>
		/// Returns true if path contains a directory named "Editor".
		/// </summary>
		/// <param name="path"> Full or local path to check. </param>
		/// <returns> True if is editor path, otherwise false. </returns>
		public static bool IsEditorPath(string path)
		{
			int i = path.IndexOf("editor", StringComparison.OrdinalIgnoreCase);
			if(i == -1)
			{
				return false;
			}

			int charCount = path.Length;
			if(charCount == 6)
			{
				return true;
			}

			if(i == 0)
			{
				var c = path[6];
				if(c == '/' || c == '\\')
				{
					return true;
				}
			}

			do
			{
				var c = path[i - 1];
				if(c != '/' && c != '\\')
				{
					continue;
				}

				if(i == charCount - 6)
				{
					return true;
				}

				c = path[i + 6];
				if(c == '/' || c == '\\')
				{
					return true;
				}

				i = path.IndexOf("editor", i + 1, StringComparison.OrdinalIgnoreCase);
			}
			while(i != -1);

			return false;
		}

		/// <summary>
		/// Given a full or local path returns same path execpt with all directories named "Editor" included in the path removed.
		/// </summary>
		/// <param name="path"> Full or local path to modify. </param>
		/// <returns> Path with "Editor" folders removed. </returns>
		public static string MakeNonEditorPath(string path)
		{
			int i = path.IndexOf("editor", StringComparison.OrdinalIgnoreCase);
			if(i == -1)
			{
				return path;
			}

			int charCount = path.Length;
			if(charCount == 6)
			{
				return "";
			}

			if(i == 0)
			{
				var c = path[6];
				if(c == '/' || c == '\\')
				{
					return MakeNonEditorPath(path.Substring(6));
				}
			}

			do
			{
				var c = path[i - 1];
				if(c != '/' && c != '\\')
				{
					continue;
				}
				
				if(i == charCount - 6)
				{
					return path.Substring(0, charCount - 7);
				}

				c = path[i + 6];
				if(c == '/' || c == '\\')
				{
					path = path.Substring(0, i) + path.Substring(i + 7);
					i--;
				}

				i = path.IndexOf("editor", i + 1, StringComparison.OrdinalIgnoreCase);
			}
			while(i != -1);

			return path;
		}
	}
}