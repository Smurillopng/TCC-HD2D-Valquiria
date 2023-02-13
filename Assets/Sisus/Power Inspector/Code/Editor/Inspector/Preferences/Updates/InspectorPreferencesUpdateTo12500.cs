#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Sisus
{
	#if DEV_MODE
	[CreateAssetMenu(fileName = "Update To 1.2.5.asset", menuName = "Preferences Updates/To 1.2.5", order = 0)]
	#endif
	public sealed class InspectorPreferencesUpdateTo12500 : InspectorPreferencesUpdate
	{
		/// <inheritdoc/>
		public override int ToVersion
		{
			get
			{
				return 12500; // 1.2.5
			}
		}

		/// <inheritdoc/>
		public override bool ShouldApplyNext(int currentVersion)
		{
			return currentVersion < ToVersion;
		}

		/// <inheritdoc/>
		protected override void ApplyUpdates(InspectorPreferences preferences)
		{
			preferences.themes.PersonalClassic.ComponentHeaderBackground = new Color32(194, 194, 194, 255);
			preferences.themes.PersonalClassic.ComponentMouseoveredHeaderBackground = new Color32(194, 194, 194, 255);
			preferences.themes.PersonalClassic.AssetHeaderBackground = new Color32(218, 218, 218, 255);

			preferences.themes.ProClassic.ComponentHeaderBackground = new Color32(56, 56, 56, 255);
			preferences.themes.ProClassic.ComponentMouseoveredHeaderBackground = new Color32(56, 56, 56, 255);
			preferences.themes.ProClassic.AssetHeaderBackground = new Color32(62, 62, 62, 255);

			preferences.themes.PersonalModern.ComponentHeaderBackground = new Color32(203, 203, 203, 255);
			preferences.themes.PersonalModern.ComponentMouseoveredHeaderBackground = new Color32(214, 214, 214, 255);
			preferences.themes.PersonalModern.AssetHeaderBackground = new Color32(203, 203, 203, 255);

			preferences.themes.ProModern.ComponentHeaderBackground = new Color32(62, 62, 62, 255);
			preferences.themes.ProModern.ComponentMouseoveredHeaderBackground = new Color32(71, 71, 71, 255);
			preferences.themes.ProModern.AssetHeaderBackground = new Color32(60, 60, 60, 255);
			preferences.themes.ProModern.ComponentSeparatorLine = new Color32(26, 26, 26, 255);

			if(EditorPrefs.HasKey("PI.NewScript/SaveIn"))
			{
				#if DEV_MODE
				Debug.Log("Deleting PI.NewScript/SaveIn");
				#endif
				EditorPrefs.DeleteKey("PI.NewScript/SaveIn");
			}
			if(EditorPrefs.HasKey("PI.NewScript/Template"))
			{
				#if DEV_MODE
				Debug.Log("Deleting PI.NewScript/Template");
				#endif
				EditorPrefs.DeleteKey("PI.NewScript/Template");
			}
			if(EditorPrefs.HasKey("PI.NewScript/Namespace"))
			{
				#if DEV_MODE
				Debug.Log("Deleting PI.NewScript/Namespace");
				#endif
				EditorPrefs.DeleteKey("PI.NewScript/Namespace");
			}
			if(EditorPrefs.HasKey("PI.NewScript/Name"))
			{
				#if DEV_MODE
				Debug.Log("Deleting PI.NewScript/Name");
				#endif
				EditorPrefs.DeleteKey("PI.NewScript/Name");
			}
			if(EditorPrefs.HasKey("PI.NewScript/AttachTo"))
			{
				#if DEV_MODE
				Debug.Log("Deleting PI.NewScript/AttachTo");
				#endif
				EditorPrefs.DeleteKey("PI.NewScript/AttachTo");
			}
			if(EditorPrefs.HasKey("PI.NewScript/CreatedAtPath"))
			{
				#if DEV_MODE
				Debug.Log("Deleting PI.NewScript/CreatedAtPath");
				#endif
				EditorPrefs.DeleteKey("PI.NewScript/CreatedAtPath");
			}
		}
	}
}
#endif