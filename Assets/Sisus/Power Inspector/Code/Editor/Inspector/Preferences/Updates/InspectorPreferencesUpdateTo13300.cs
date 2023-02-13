#if UNITY_EDITOR
using UnityEngine;

namespace Sisus
{
	#if DEV_MODE
	[CreateAssetMenu(fileName = "Update To 1.3.3.asset", menuName = "Preferences Updates/To 1.3.3", order = 1)]
	#endif
	public sealed class InspectorPreferencesUpdateTo13300 : InspectorPreferencesUpdate
	{
		private const int FromVersion = 13000;

		/// <inheritdoc/>
		public override int ToVersion
		{
			get
			{
				return 13300; // 1.3.3
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
			preferences.themes.ProModern.PrefixIdleText = new Color32(210, 210, 210, 255);
			preferences.themes.ProModern.PrefixSelectedText = new Color32(124, 171, 240, 255);
		}
	}
}
#endif