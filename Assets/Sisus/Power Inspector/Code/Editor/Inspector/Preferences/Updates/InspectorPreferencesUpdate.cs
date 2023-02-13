#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Sisus.Attributes;
using JetBrains.Annotations;

namespace Sisus
{
	/// <summary>
	/// A class that responsible for updating InspectorPreferences from one version to another.
	/// 
	/// The idea is to only modify the minimum required amount of items, while avoiding overriding user settings.
	/// </summary>
	public abstract class InspectorPreferencesUpdate : ScriptableObject, IComparable<InspectorPreferencesUpdate>
	{
		/// <summary>
		/// Version number int which user's InspectorPreferences will be updated to.
		/// </summary>
		public abstract int ToVersion { get; }
		
		/// <summary>
		/// Gets all updates for InspectorPreferences.
		/// Update files should be located in the same directory as the PreferencesAsset inside
		/// a folder named "Updates"
		/// </summary>
		/// <param name="preferences"></param>
		/// <returns></returns>
		public static InspectorPreferencesUpdate[] GetAllUpdates([NotNull]InspectorPreferences preferences)
		{
			var preferencesPath = AssetDatabase.GetAssetPath(preferences);
			var directoryPath = FileUtility.GetParentDirectory(preferencesPath);
			directoryPath = FileUtility.GetChildDirectory(directoryPath, "Updates");

			if(!AssetDatabase.IsValidFolder(directoryPath))
			{
				#if DEV_MODE
				Debug.LogWarning(preferences.name+" had no Updates folder next to it.");
				#endif
				return ArrayPool<InspectorPreferencesUpdate>.ZeroSizeArray;
			}

			var updateGuids = AssetDatabase.FindAssets("t:InspectorPreferencesUpdate", ArrayExtensions.TempStringArray(directoryPath));
			int count = updateGuids.Length;
			var results = new InspectorPreferencesUpdate[count];

			for(int n = updateGuids.Length - 1; n >= 0; n--)
			{
				var updatePath = AssetDatabase.GUIDToAssetPath(updateGuids[n]);
				results[n] = AssetDatabase.LoadAssetAtPath<InspectorPreferencesUpdate>(updatePath);
			}

			return results;
		}

		/// <summary>
		/// Given the current preferences version, determines whether or not this update should be applied next.
		/// </summary>
		/// <param name="currentVersion"> Current preferences version. This might be smaller than Version.Current, since it's only updated when patches are applied. </param>
		/// <returns></returns>
		public abstract bool ShouldApplyNext(int currentVersion);

		public bool HasBeenApplied(int currentVersion)
		{
			return currentVersion >= ToVersion;
		}

		/// <summary>
		/// Update user preferences from FromVersion to ToVersion.
		/// </summary>
		[ShowInInspector]
		public void UpdateNow([NotNull]InspectorPreferences preferences)
		{
			UndoHandler.RegisterUndoableAction(preferences, "Update Inspector Preferences");

			#if DEV_MODE
			Debug.Log("Updating preferences to version " + ToVersion + " now...");
			#endif
			
			ApplyUpdates(preferences);

			EditorUtility.SetDirty(preferences);
		}

		public int CompareTo(InspectorPreferencesUpdate other)
		{
			return ToVersion.CompareTo(other.ToVersion);
		}
		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Handle updating user preferences from FromVersion to ToVersion.
		/// </summary>
		protected abstract void ApplyUpdates(InspectorPreferences preferences);
	}
}
#endif