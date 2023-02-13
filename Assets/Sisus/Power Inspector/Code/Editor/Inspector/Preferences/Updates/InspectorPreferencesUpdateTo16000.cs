#if UNITY_EDITOR
using UnityEngine;

namespace Sisus
{
	#if DEV_MODE
	[CreateAssetMenu(fileName = "Update To 1.6.0.asset", menuName = "Preferences Updates/To 1.6.0", order = 1)]
	#endif
	public sealed class InspectorPreferencesUpdateTo16000 : InspectorPreferencesUpdate
	{
		private const int FromVersion = 14400;

		/// <inheritdoc/>
		public override int ToVersion
		{
			get
			{
				return 16000; // 1.6.0
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
			preferences.themes.ProModern.PrefixMouseoveredRectHighlight = new Color32(79, 79, 79, 255);
			preferences.themes.ProModern.PrefixMouseoveredRectShadow = new Color32(79, 79, 79, 255);
		}
	}
}
#endif