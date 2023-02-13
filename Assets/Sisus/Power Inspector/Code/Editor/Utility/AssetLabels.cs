#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// An utility class that handles fetchign asset labels for targets.
	/// </summary>
	public static class AssetLabels
	{
		public static Action<Object[]> OnAssetLabelsChanged;

		private static readonly List<string> TempLabelsFoundOnAll = new List<string>();
		private static readonly List<string> TempLabelsFoundOnSome = new List<string>();

		public static void Get(Object[] targets, ref GUIContent[] assetLabels, ref GUIContent[] assetLabelsOnlyOnSomeTargets)
		{
			var first = targets[0];

			if(first == null || !AssetDatabase.Contains(first))
			{
				ArrayPool<GUIContent>.Resize(ref assetLabels, 0);
				ArrayPool<GUIContent>.Resize(ref assetLabelsOnlyOnSomeTargets, 0);
				return;
			}

			var firstAssetLabels = AssetDatabase.GetLabels(first);
			int firstAssetLabelsCount = firstAssetLabels.Length;
			if(firstAssetLabelsCount == 0)
			{
				ArrayPool<GUIContent>.Resize(ref assetLabels, 0);
				ArrayPool<GUIContent>.Resize(ref assetLabelsOnlyOnSomeTargets, 0);
				return;
			}
			
			int targetCount = targets.Length;
			if(targetCount == 1)
			{
				if(assetLabels != null)
				{
					GUIContentArrayPool.Dispose(ref assetLabels);
				}
				assetLabels = GUIContentArrayPool.Create(firstAssetLabels);

				GUIContentArrayPool.Resize(ref assetLabelsOnlyOnSomeTargets, 0, true, false);
				return;
			}

			for(int l = 0; l < firstAssetLabelsCount; l++)
			{
				var testLabel = firstAssetLabels[l];
				
				bool foundOnAll = true;
				for(int t = targetCount - 1; t >= 1; t--)
				{
					if(Array.IndexOf(AssetDatabase.GetLabels(targets[t]), testLabel) == -1)
					{
						foundOnAll = false;
						break;
					}
				}

				if(foundOnAll)
				{
					TempLabelsFoundOnAll.Add(testLabel);
				}
				else
				{
					TempLabelsFoundOnSome.Add(testLabel);
				}
			}
			
			if(assetLabels != null)
			{
				GUIContentArrayPool.Dispose(ref assetLabels);
			}
			assetLabels = GUIContentArrayPool.Create(TempLabelsFoundOnAll);
			TempLabelsFoundOnAll.Clear();

			if(assetLabelsOnlyOnSomeTargets != null)
			{
				GUIContentArrayPool.Dispose(ref assetLabelsOnlyOnSomeTargets);
			}
			assetLabelsOnlyOnSomeTargets = GUIContentArrayPool.Create(TempLabelsFoundOnSome);
			TempLabelsFoundOnSome.Clear();
		}

		public static void Add(Object[] targets, string add)
		{
			#if DEV_MODE
			Debug.Log("AssetLabels.Add(" + StringUtils.ToString(targets) + ", \""+add+"\"");
			#endif

			bool changed = false;

			for(int t = targets.Length - 1; t >= 0; t--)
			{
				var target = targets[t];
				var targetLabels = AssetDatabase.GetLabels(target);
				if(Array.IndexOf(targetLabels, add) == -1)
				{
					TempLabelsFoundOnSome.AddRange(targetLabels);
					TempLabelsFoundOnSome.Add(add);
					AssetDatabase.SetLabels(target, TempLabelsFoundOnSome.ToArray());
					TempLabelsFoundOnSome.Clear();
					changed = true;
				}
			}

			if(changed)
			{
				OnAssetLabelsChanged(targets);
			}
		}

		public static void Remove(Object[] targets, string remove)
		{
			#if DEV_MODE
			Debug.Log("AssetLabels.Remove(" + StringUtils.ToString(targets) + ", \""+remove+"\"");
			#endif

			bool changed = false;
			
			for(int t = targets.Length - 1; t >= 0; t--)
			{
				var target = targets[t];
				var targetLabels = AssetDatabase.GetLabels(target);
				int foundAt = Array.IndexOf(targetLabels, remove);
				if(foundAt != -1)
				{
					TempLabelsFoundOnSome.AddRange(targetLabels);
					TempLabelsFoundOnSome.RemoveAt(foundAt);
					AssetDatabase.SetLabels(target, TempLabelsFoundOnSome.ToArray());
					changed = true;
					TempLabelsFoundOnSome.Clear();
				}
			}

			if(changed)
			{
				OnAssetLabelsChanged(targets);
			}
		}
	}
}
#endif