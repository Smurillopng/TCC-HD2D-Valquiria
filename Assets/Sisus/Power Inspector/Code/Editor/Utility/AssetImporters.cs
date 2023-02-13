#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	public static class AssetImporters
	{
		private static Type nativeFormatImporter;
		private static Type NativeFormatImporter
		{
			get
			{
				if(nativeFormatImporter == null)
				{
					nativeFormatImporter = Types.GetInternalType("UnityEngine.NativeFormatImporter");
				}
				return nativeFormatImporter;
			}
		}

		/// <summary>
		/// If targets have asset importers, sets importers array to be the same size as targets array, and sets it to
		/// contain asset importers for the targets, otherwise sets importers value to null.
		/// </summary>
		/// <param name="targets"> The target assets whose asset importers to fetch. </param>
		/// <param name="importers"> [in,out] The asset importers for targets, or null if targets have no asset importers. </param>
		/// <returns> True if asset importers were found for targets, false if not. </returns>
		public static bool TryGet([NotNull]Object[] targets, [CanBeNull]ref Object[] importers)
		{
			var firstTarget = targets[0];

			if(!AssetDatabase.IsMainAsset(firstTarget))
			{
				importers = null;
				return false;
			}

			var firstImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(firstTarget));
			if(firstImporter == null)
			{
				importers = null;
				return false;
			}

			var assetImporterType = firstImporter.GetType();
			if(assetImporterType == NativeFormatImporter)
			{
				#if DEV_MODE
				UnityEngine.Debug.LogWarning("Won't return asset importers for targets of type "+firstTarget.GetType().FullName+" because importer type was "+ assetImporterType.FullName);
				#endif
				importers = null;
				return false;
			}
			
			int count = targets.Length;
			if(importers == targets)
			{
				importers = ArrayPool<Object>.Create(count);
			}
			else
			{
				ArrayPool<Object>.Resize(ref importers, count);
			}
			
			importers[0] = firstImporter;
			for(int n = count - 1; n >= 1; n--)
			{
				importers[n] = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(targets[n]));
			}
			return true;
		}
	}
}
#endif