using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	#if !UNITY_2017_2_OR_NEWER
	public enum PlayModeStateChange { EnteredEditMode, EnteredPlayMode, ExitingEditMode, ExitingPlayMode}
	#endif

	/// <summary>
	/// Class that tracks current PlayMode state and broadcasts OnPlayModeChangedEvent
	///	event whenever it changes. It can also be polled for the current state.
	///	
	///	In Unity versions older than 2017.2 Relies on DrawGUI.OnBeginOnGUI which only gets called
	///	if there's at least one	class that has an OnGUI method and calls DrawGUI.BeginOnGUI at the
	///	start of said method.
	/// </summary>
	[InitializeOnLoad]
	public static class PlayMode
	{
		public delegate void OnPlayModeChangedEvent(PlayModeStateChange playModeState);

		public static OnPlayModeChangedEvent OnStateChanged;

		private static PlayModeStateHolder stateHolder;

		private static PlayModeStateHolder StateHolder
		{
			get
			{
				if(stateHolder == null)
				{
					#if UNITY_2023_1_OR_NEWER
					stateHolder = Object.FindAnyObjectByType<PlayModeStateHolder>(FindObjectsInactive.Include);
					#else
					stateHolder = Object.FindObjectOfType<PlayModeStateHolder>();
					#endif

					if(stateHolder == null)
					{
						stateHolder = ScriptableObject.CreateInstance<PlayModeStateHolder>();
						stateHolder.hideFlags = HideFlags.DontSave;
					}
				}

				return stateHolder;
			}
		}

		public static PlayModeState CurrentState
		{
			get
			{
				return StateHolder.state;
			}

			private set
			{
				StateHolder.state = value;
			}
		}

		public static bool NowChangingState
		{
			get;
			private set;
		}

		private static bool initialized;

		#if UNITY_EDITOR
		/// <summary>
		/// This is called on editor load because of usage of the InitializeOnLoad attribute.
		/// </summary>
		[UsedImplicitly]
		static PlayMode()
		{
			EditorApplication.delayCall += Initialize;
		}
		#endif

		/// <summary>
		/// This is called when entering play mode or when the game is loaded
		/// </summary>
		[RuntimeInitializeOnLoadMethod, UsedImplicitly]
		private static void RuntimeInitializeOnLoad()
		{
			Initialize();
		}

		private static void Initialize()
		{
			if(initialized)
			{
				return;
			}

			#if UNITY_EDITOR
			if(!ApplicationUtility.IsReady())
			{
				EditorApplication.delayCall += Initialize;
				return;
			}
			#endif
			

			initialized = true;
			
			#if UNITY_2017_2_OR_NEWER
			EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
			#else
			DrawGUI.OnEveryBeginOnGUI(DetectPlaymodeStateChanges, false);
			#endif
		}

		#if !UNITY_2017_2_OR_NEWER
		private static void DetectPlaymodeStateChanges()
		{
			var state = StateHolder.state;
			if(EditorApplication.isPlaying)
			{
				if(state != PlayModeState.PlayMode)
				{
					OnPlaymodeStateChanged(PlayModeStateChange.ExitingEditMode);
					OnPlaymodeStateChanged(PlayModeStateChange.EnteredPlayMode);
				}
			}
			else if(state == PlayModeState.PlayMode)
			{
				OnPlaymodeStateChanged(PlayModeStateChange.ExitingPlayMode);
				OnPlaymodeStateChanged(PlayModeStateChange.EnteredEditMode);
			}
		}
		#endif
		
		private static void OnPlaymodeStateChanged(PlayModeStateChange state)
		{
			switch(state)
			{
				case PlayModeStateChange.EnteredEditMode:
					NowChangingState = false;
					CurrentState = PlayModeState.EditMode;
					break;
				case PlayModeStateChange.EnteredPlayMode:
					NowChangingState = false;
					CurrentState = PlayModeState.PlayMode;
					break;
				case PlayModeStateChange.ExitingEditMode:
					NowChangingState = true;
					CurrentState = PlayModeState.ExitingEditMode;
					break;
				case PlayModeStateChange.ExitingPlayMode:
					NowChangingState = true;
					CurrentState = PlayModeState.ExitingPlayMode;
					break;
				default:
					throw new ArgumentOutOfRangeException("state", state, null);
			}

			#if DEV_MODE && DEBUG_PLAY_MODE_CHANGED
			Debug.Log("OnEditorPlaymodeStateChanged("+ playModeStateChange + ")");
			#endif

			if(OnStateChanged != null)
			{
				OnStateChanged(state);
			}
		}
	}
}