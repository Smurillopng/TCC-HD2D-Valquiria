#if UNITY_EDITOR
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;

namespace Sisus
{
	public class PlayModeStateHolder : ScriptableObject
	{
		public PlayModeState state;
		
		[UsedImplicitly]
		private void OnEnable()
		{
			state = (PlayModeState)EditorPrefs.GetInt("PI.PlayModeState", (int)PlayModeState.EditMode);
		}

		[UsedImplicitly]
		private void OnDisable()
		{
			EditorPrefs.SetInt("PI.PlayModeState", (int)state);
		}
	}
}
#endif