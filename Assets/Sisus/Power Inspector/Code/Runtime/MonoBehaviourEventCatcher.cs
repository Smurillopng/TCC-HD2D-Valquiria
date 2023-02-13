#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Enables non-MonoBehaviour classes to get callbacks for MonoBehaviour-specific Unity Event Functions.
	/// Note that the class has the ExecuteAlways flag, meaning the event functions will get called in Edit mode too.
	/// </summary>
	#if UNITY_2018_3_OR_NEWER
	[ExecuteAlways]
	#else
	[ExecuteInEditMode]
	#endif
	[AddComponentMenu("")]
	public class MonoBehaviourEventCatcher : MonoBehaviour
	{
		public Action OnUpdate;
		public Action OnApplicationGainedFocus;
		public Action OnApplicationLostFocus;
		public Action OnApplicationQuitting;

		private static MonoBehaviourEventCatcher instance;

		public bool ApplicationHasFocus
		{
			get;
			private set;
		}

		public bool ApplicationQuitting
		{
			get;
			private set;
		}
		
		[UsedImplicitly]
		private void Update()
		{
			if(OnUpdate != null)
			{
				OnUpdate();
			}
		}

		[UsedImplicitly]
		private void OnApplicationFocus(bool hasFocus)
		{
			ApplicationHasFocus = hasFocus;

			if(hasFocus)
			{
				if(OnApplicationGainedFocus != null)
				{
					OnApplicationGainedFocus();
				}
			}
			else if(OnApplicationLostFocus != null)
			{
				OnApplicationLostFocus();
			}
		}
		
		[UsedImplicitly]
		private void OnApplicationQuit()
		{
			ApplicationQuitting = true;

			if(OnApplicationQuitting != null)
			{
				OnApplicationQuitting();
			}
		}
	}
}
#endif