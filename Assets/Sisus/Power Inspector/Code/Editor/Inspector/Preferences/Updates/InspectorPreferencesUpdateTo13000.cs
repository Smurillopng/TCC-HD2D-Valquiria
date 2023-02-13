#if UNITY_EDITOR
using UnityEngine;

namespace Sisus
{
	#if DEV_MODE
	[CreateAssetMenu(fileName = "Update To 1.3.0.asset", menuName = "Preferences Updates/To 1.3.0", order = 1)]
	#endif
	public sealed class InspectorPreferencesUpdateTo13000 : InspectorPreferencesUpdate
	{
		private const int FromVersion = 12500;

		[SerializeField]
		private Texture contextMenuIcon = null;

		/// <inheritdoc/>
		public override int ToVersion
		{
			get
			{
				return 13000; // 1.3.0
			}
		}

		/// <inheritdoc/>
		public override bool ShouldApplyNext(int currentVersion)
		{
			return currentVersion < ToVersion && currentVersion >= FromVersion;
		}

		/// <inheritdoc/>
		protected override void ApplyUpdates(InspectorPreferences preferences)
		{
			preferences.themes.ProClassic.PrefixIdleText = new Color32(209, 209, 209, 255);

			var color = new Color32(103, 103, 103, 128);
			preferences.themes.PersonalClassic.SelectedLineIndicatorUnfocused = color;
			preferences.themes.PersonalModern.SelectedLineIndicatorUnfocused = color;

			color = new Color32(86, 86, 86, 128);
			preferences.themes.ProClassic.SelectedLineIndicatorUnfocused = color;
			preferences.themes.ProModern.SelectedLineIndicatorUnfocused = color;

			color = new Color32(72, 72, 72, 255);
			preferences.themes.ProClassic.ToolbarItemSelectedUnfocused = color;
			preferences.themes.ProModern.ToolbarItemSelectedUnfocused = color;

			color = new Color32(143, 143, 143, 255);
			preferences.themes.ProClassic.PrefixSelectedUnfocusedText = color;
			preferences.themes.ProModern.PrefixSelectedUnfocusedText = color;

			color = new Color32(72, 72, 72, 255);
			preferences.themes.PersonalClassic.PrefixSelectedUnfocusedText = color;
			preferences.themes.PersonalModern.PrefixSelectedUnfocusedText = color;

			color = new Color32(73, 142, 228, 128);
			preferences.themes.ProClassic.SelectedLineIndicator = color;
			preferences.themes.ProModern.SelectedLineIndicator = color;

			color = new Color32(100, 100, 100, 255);
			preferences.themes.ProClassic.ControlSelectedUnfocusedRect = color;
			preferences.themes.ProModern.ControlSelectedUnfocusedRect = color;

			color = new Color32(115, 115, 115, 255);
			preferences.themes.PersonalClassic.ControlSelectedUnfocusedRect = color;
			preferences.themes.PersonalModern.ControlSelectedUnfocusedRect = color;

			preferences.labels.contextMenu.image = contextMenuIcon;
		}
	}
}
#endif