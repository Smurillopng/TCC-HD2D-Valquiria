using System;

#if DEV_MODE && PI_ASSERTATIONS
using UnityEngine;
#endif

namespace Sisus
{
	/// <summary>
	/// Classes can inherit from this to get callbacks for MonoBehaviour-specific Unity Event Functions.
	/// </summary>
	public abstract class UnityEventFunctionsListener : IDisposable
	{
		private UnityEventFunctions subscribedEvents;
		private static MonoBehaviourEventCatcher eventCatcher;

		protected UnityEventFunctionsListener(UnityEventFunctions subscribeToEvents)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(subscribeToEvents != UnityEventFunctions.None, GetType().Name);
			#endif

			if(eventCatcher == null)
			{
				eventCatcher = InvisibleComponentProvider.GetOrCreate<MonoBehaviourEventCatcher>("EventFunctionsListener/MonoBehaviourEventCatcher");
				
				// can be null if application is quitting
				if(eventCatcher == null)
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(ApplicationUtility.IsQuitting);
					#endif
					return;
				}
			}

			subscribedEvents = subscribeToEvents;

			if(subscribedEvents.HasFlag(UnityEventFunctions.Update))
			{
				eventCatcher.OnUpdate += Update;
			}
			if(subscribedEvents.HasFlag(UnityEventFunctions.OnApplicationQuit))
			{
				eventCatcher.OnApplicationQuitting += OnApplicationQuit;
			}
			if(subscribedEvents.HasFlag(UnityEventFunctions.OnApplicationGainedFocus))
			{
				eventCatcher.OnApplicationGainedFocus += OnApplicationGainedFocus;
			}
			if(subscribedEvents.HasFlag(UnityEventFunctions.OnApplicationLostFocus))
			{
				eventCatcher.OnApplicationLostFocus += OnApplicationLostFocus;
			}
		}
		
		/// <summary>
		/// In play mode Update is called every frame.
		/// In edit mode Update is only called when something in the Scene changed.
		/// </summary>
		protected virtual void Update() { }
		protected virtual void OnApplicationGainedFocus() { }
		protected virtual void OnApplicationLostFocus() { }
		protected virtual void OnApplicationQuit() { }
		
		public void Dispose()
		{
			// check if already disposed
			if(subscribedEvents == UnityEventFunctions.None)
			{
				return;
			}

			if(subscribedEvents.HasFlag(UnityEventFunctions.Update))
			{
				eventCatcher.OnUpdate -= Update;
			}
			if(subscribedEvents.HasFlag(UnityEventFunctions.OnApplicationQuit))
			{
				eventCatcher.OnApplicationQuitting -= OnApplicationQuit;
			}
			if(subscribedEvents.HasFlag(UnityEventFunctions.OnApplicationGainedFocus))
			{
				eventCatcher.OnApplicationGainedFocus -= OnApplicationGainedFocus;
			}
			if(subscribedEvents.HasFlag(UnityEventFunctions.OnApplicationLostFocus))
			{
				eventCatcher.OnApplicationLostFocus -= OnApplicationLostFocus;
			}
			subscribedEvents = UnityEventFunctions.None;

			OnDisposing();

			GC.SuppressFinalize(this);
		}

		protected virtual void OnDisposing() { }
	}
}