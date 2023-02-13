#if UNITY_EDITOR
using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Editor-only utility class that provides some methods usable by drawer
	/// representing assets and ones that implement IAssetDrawer.
	/// </summary>
	public static class AssetDrawerUtility
	{
		public static void SelectPreviousOfType(IAssetDrawer subject)
		{
			var subjectAssetPath = AssetDatabase.GetAssetPath(subject.UnityObject);
			var subjectGuid = AssetDatabase.AssetPathToGUID(subjectAssetPath);

			var type = subject.Type;
			var allAssetGuids = AssetDatabase.FindAssets(StringUtils.Concat("t:", type.Name));
			int assetCount = allAssetGuids.Length;
			if(assetCount > 1)
			{
				int index = Array.IndexOf(allAssetGuids, subjectGuid);
				if(index != -1)
				{
					if(index == 0)
					{
						index = allAssetGuids.Length - 1;
					}
					else
					{
						index--;
					}

					var assetPath = AssetDatabase.GUIDToAssetPath(allAssetGuids[index]);
					var select = AssetDatabase.LoadAssetAtPath(assetPath, type);

					var inspector = InspectorUtility.ActiveInspector;
					inspector.OnNextInspectedChanged(() => inspector.SelectAndShow(select, ReasonSelectionChanged.SelectPrevComponent));
					inspector.Select(select);
				}
			}
		}

		public static void SelectNextOfType(IAssetDrawer subject)
		{
			var subjectAssetPath = AssetDatabase.GetAssetPath(subject.UnityObject);
			var subjectGuid = AssetDatabase.AssetPathToGUID(subjectAssetPath);

			var type = subject.Type;
			var allAssetGuids = AssetDatabase.FindAssets(StringUtils.Concat("t:", type.Name));
			int assetCount = allAssetGuids.Length;
			if(assetCount > 1)
			{
				int index = Array.IndexOf(allAssetGuids, subjectGuid);
				if(index != -1)
				{
					index++;
					if(index > allAssetGuids.Length)
					{
						index = 0;
					}

					var assetPath = AssetDatabase.GUIDToAssetPath(allAssetGuids[index]);
					var select = AssetDatabase.LoadAssetAtPath(assetPath, type);

					var inspector = InspectorUtility.ActiveInspector;
					inspector.OnNextInspectedChanged(() => inspector.SelectAndShow(select, ReasonSelectionChanged.SelectPrevComponent));
					inspector.Select(select);
				}
			}
		}

		public static void Duplicate(Object[] targets)
		{
			#if UNITY_EDITOR
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				string sourcePath = AssetDatabase.GetAssetPath(targets[n]);
				string extension = System.IO.Path.GetExtension(sourcePath);
				string targetPath = sourcePath.Substring(0, sourcePath.Length - extension.Length);
				int pathLength = targetPath.Length;
				if(targetPath[pathLength - 1] == ')')
				{
					int index = targetPath.LastIndexOf('(');
					if(index != -1)
					{
						int num;
						if(int.TryParse(targetPath.Substring(index + 1, pathLength - index - 2), out num))
						{
							targetPath = StringUtils.Concat(targetPath.Substring(0, index), "(", num, ")", extension);
							AssetDatabase.CopyAsset(sourcePath, targetPath);
							continue;
						}
					}
				}
				targetPath = StringUtils.Concat(targetPath, "(1)", extension);
				AssetDatabase.CopyAsset(sourcePath, targetPath);
			}
			#else
			InspectorUtility.ActiveInspector.Message("Duplicating assets not supported at runtime", null, MessageType.Error);
			#endif
		}
	}
}
#endif