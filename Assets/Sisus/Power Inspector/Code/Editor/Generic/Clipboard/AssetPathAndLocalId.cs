#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable]
	public struct AssetPathAndLocalId
	{
		[SerializeField]
		private string assetPath;

		#if UNITY_2018_1_OR_NEWER
		[SerializeField]
		#if UNITY_2018_2_OR_NEWER
		private long localId;
		#else
		private int localId;
		#endif
		#endif

		public string AssetPath
		{
			get
			{
				return assetPath;
			}
		}

		#if UNITY_2018_1_OR_NEWER
		#if UNITY_2018_2_OR_NEWER
		public long LocalId
		#else
		public int LocalId
		#endif
		{
			get
			{
				return localId;
			}
		}
		#endif

		public static bool TryParse(string text, out AssetPathAndLocalId result)
		{
			#if UNITY_2018_1_OR_NEWER
			int i = text.IndexOf('|');
			if(i != -1)
			{
				string assetPath = text.Substring(0, i);
				if(AssetDatabase.AssetPathToGUID(assetPath).Length == 0)
				{
					result = default(AssetPathAndLocalId);
					return false;
				}

				string localIdString = text.Substring(i + 1);
				
				#if UNITY_2018_2_OR_NEWER
				long localId;
				if(long.TryParse(localIdString, out localId))
				#else
				int localId;
				if(int.TryParse(localIdString, out localId))
				#endif
				{
					result = new AssetPathAndLocalId(assetPath, localId);
					return true;
				}

				result = new AssetPathAndLocalId(assetPath);
				return true;
			}
			#endif

			if(AssetDatabase.AssetPathToGUID(text).Length == 0)
			{
				result = default(AssetPathAndLocalId);
				return false;
			}

			result = new AssetPathAndLocalId(text);
			return true;
		}

		public AssetPathAndLocalId(Object target)
		{
			#if UNITY_2018_1_OR_NEWER
			string guid;
			if(target != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out guid, out localId))
			{
				assetPath = AssetDatabase.GUIDToAssetPath(guid);
				if(string.Equals(assetPath, "Library/unity editor resources"))
				{
					assetPath = FileUtility.GetAssetPath(target);
				}
			}
			else
			{
				assetPath = "";
				#if UNITY_2018_2_OR_NEWER
				localId = 0L;
				#else
				localId = 0;
				#endif
			}
			#else
			assetPath = FileUtility.GetAssetPath(target);
			#endif
		}

		#if UNITY_2018_2_OR_NEWER
		public AssetPathAndLocalId(string setAssetPath, long setLocalId)
		{
			assetPath = setAssetPath;
			localId = setLocalId;
		}
		#elif UNITY_2018_1_OR_NEWER
		public AssetPathAndLocalId(string setAssetPath, int setLocalId)
		{
			assetPath = setAssetPath;
			localId = setLocalId;
		}
		#endif

		public AssetPathAndLocalId(string setAssetPath)
		{
			assetPath = setAssetPath;
			#if UNITY_2018_1_OR_NEWER
			localId = 0;
			#endif
		}

		public bool HasPath()
		{
			return assetPath.Length > 0;
		}

		public bool Exists()
		{
			return Load() != null;
		}

		[CanBeNull]
		public Object Load()
		{
			if(assetPath.Length == 0)
			{
				return null;
			}

			#if UNITY_2018_1_OR_NEWER
			var assetsAtPath = AssetDatabase.LoadAllAssetsAtPath(assetPath);
			for(int n = assetsAtPath.Length - 1; n >= 0; n--)
			{
				var assetAtPath = assetsAtPath[n];

				#if UNITY_2018_2_OR_NEWER
				long id;
				#else
				int id;
				#endif

				string guid;
				if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(assetAtPath, out guid, out id) && id == localId)
				{
					return assetAtPath;
				}
			}
			return null;
			#else
			return AssetDatabase.LoadAssetAtPath<Object>(assetPath);
			#endif
		}

		public string Serialize()
		{
			#if UNITY_2018_1_OR_NEWER
			if(localId > 0)
			{
				return assetPath + '|' + StringUtils.ToString(localId);
			}
			#endif
			
			return assetPath;
		}
	}
}
#endif