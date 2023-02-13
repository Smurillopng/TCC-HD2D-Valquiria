//#define DEBUG_ENABLED

using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
#if DEV_MODE && DEBUG_ENABLED
using System.Linq;
#endif
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Implementation of the PersistentSingleton pattern. Class that handles the creation, caching and fetching of instances,
	/// in such a way that only a single instance of any class will be instantiated.
	/// 
	/// The PersistentSingleton class itself is a Singleton; only one instance of it can exist.
	/// 
	/// The PersistentSingleton can be used during Edit Mode and at Runtime. It, and all the instances created using it,
	/// will persist through scene changes, play mode state changes and assembly reloading.
	/// It will not be saved to any scene, and will get destroyed when the application is closed.
	/// </summary>
	public class PersistentSingleton : ScriptableObject, ISerializationCallbackReceiver
	{
		private static PersistentSingleton persistentSingletonInstance;
		private static bool ready = true;
		private static readonly List<object> InstancesToSerialize = new List<object>();
		
		[NonSerialized]
		private bool setupInProgressOrDone;

		[NonSerialized]
		private Dictionary<Type, object> instances;

		[SerializeField]
		private PersistentSingletonSerialized[] serializedState;
		
		/// <summary>
		/// Gets a value indicating whether the PersistentSingleton class is currently ready to be used
		/// by external classes.
		/// 
		/// This is set to false for the duration of the Setup phase and when the application is quitting.
		/// </summary>
		/// <value> True if ready, false if not. </value>
		public static bool Ready
		{
			get
			{
				return ready;
			}
		}

		[UsedImplicitly]
		private void OnEnable()
		{
			// Don't allow more than one PersistentSingleton instance.
			// This makes accessing the PersistentSingleton via a cached instance simpler, and
			// helps with making sure there are no memory leaks (since we are using
			// the DontSave hideFlag, to keep the ScriptableObject from being unloaded
			// during assembly reloading etc.).
			if(persistentSingletonInstance == null)
			{
				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log("PersistentSingleton - OnEnable");
				#endif

				persistentSingletonInstance = this;
				Setup();
			}
			else if(persistentSingletonInstance != this)
			{
				#if DEV_MODE
				Debug.LogError("Destroying PersistentSingleton instance because one already existed; there can only be one.");
				#endif
				if(Application.isPlaying)
				{
					Destroy(this);
				}
				else
				{
					DestroyImmediate(this);
				}
			}
		}

		[UsedImplicitly]
		private void OnDisable()
		{
			if(persistentSingletonInstance == this)
			{
				persistentSingletonInstance = null;
			}
		}
		
		/// <summary>
		/// Gets existing instance of class, or if one doesn't yet exist, creates a new one and caches it.
		/// If application is quitting when method is called, returns null.
		/// </summary>
		/// <typeparam name="T"> Type of instance to return. </typeparam>
		/// <returns> And instance of class type, or null if application is quitting. </returns>
		[CanBeNull]
		public static T Get<T>() where T : class, new()
		{
			#if DEV_MODE
			//Debug.Log("PersistentSingleton.Get<"+typeof(T).Name+">() with persistentSingletonInstance="+(persistentSingletonInstance == null ? "null" : "Exists"));
			#endif

			if(ApplicationUtility.IsQuitting)
			{
				#if DEV_MODE
				Debug.LogWarning("PersistentSingleton.Get<"+typeof(T).Name+">() returning null because applicationIsQuitting");
				#endif
				return null;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			if(!ready)
			{
				if(Exists<T>())
				{
					Debug.LogWarning("PersistentSingleton.Get<"+typeof(T).Name+"> called with Setup still in progress, but all is fine because instance in question already existed.");
				}
				else
				{
					Debug.LogError("PersistentSingleton.Get<"+typeof(T).Name+"> called with Setup still in progress, and instance in question did not exist yet! This could potentially lead to instance in question not getting deserialized.");
				}
			}
			#endif

			return Instance().GetInternal<T>();
		}

		/// <summary>
		/// Gets existing instance of class, or if one doesn't yet exist, creates a new one and caches it.
		/// </summary>
		/// <typeparam name="T"> Type of instance to return. </typeparam>
		/// <returns> And instance of class type. </returns>
		[CanBeNull]
		private T GetInternal<T>() where T : class, new()
		{
			T result;
			if(TryGetInternal(out result))
			{
				return result;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!typeof(T).IsUnityObject());
			#endif

			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("PersistentSingleton - Creating new "+StringUtils.ToStringSansNamespace(typeof(T))+" instance.");
			#endif
			return Cache(new T());
		}
		
		private T Cache<T>(T instance)
		{
			instances[typeof(T)] = instance;
			return instance;
		}

		public static T GetScriptableObject<T>() where T : ScriptableObject
		{
			if(ApplicationUtility.IsQuitting)
			{
				#if DEV_MODE
				Debug.LogWarning("PersistentSingleton.GetScriptableObject<"+typeof(T).Name+">() returning null because applicationIsQuitting");
				#endif
				return null;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			if(!ready)
			{
				if(Exists<T>())
				{
					Debug.LogWarning("PersistentSingleton.Get<"+typeof(T).Name+"> called with Setup still in progress, but all is fine because instance in question already existed.");
				}
				else
				{
					Debug.LogError("PersistentSingleton.Get<"+typeof(T).Name+"> called with Setup still in progress, and instance in question did not exist yet! This could potentially lead to instance in question not getting deserialized.");
				}
			}
			#endif

			return Instance().GetScriptableObjectInternal<T>();
		}

		private T GetScriptableObjectInternal<T>() where T : ScriptableObject
		{
			T result;
			if(TryGetInternal(out result))
			{
				return result;
			}
			return Cache(FindOrCreateScriptableObject<T>());
		}

		public static bool TryGet<T>(out T result) where T : class
		{
			if(ApplicationUtility.IsQuitting)
			{
				#if DEV_MODE && DEBUG_ENABLED
				Debug.LogWarning("PersistentSingleton.TryGet<"+typeof(T).Name+">() returning "+StringUtils.False+" because applicationIsQuitting");
				#endif
				result = null;
				return false;
			}

			return Instance().TryGetInternal(out result);
		}

		private bool TryGetInternal<T>(out T result) where T : class
		{
			object cachedInstance;
			if(instances.TryGetValue(typeof(T), out cachedInstance))
			{
				result = cachedInstance as T;
				return true;
			}
			result = null;
			return false;
		}

		public static bool Exists<T>() where T : class
		{
			if(ApplicationUtility.IsQuitting)
			{
				return false;
			}
			return Instance().instances.ContainsKey(typeof(T));
		}

		private static PersistentSingleton Instance()
		{
			if(persistentSingletonInstance == null)
			{
				if(ApplicationUtility.IsQuitting)
				{
					#if DEV_MODE && DEBUG_ENABLED
					Debug.LogWarning("PersistentSingleton.Instance returning null because applicationIsQuitting");
					#endif
					return null;
				}
				persistentSingletonInstance = FindOrCreateScriptableObject<PersistentSingleton>();
				if(!persistentSingletonInstance.setupInProgressOrDone)
				{
					persistentSingletonInstance.Setup();
				}
			}
			return persistentSingletonInstance;
		}

		private void Setup()
		{
			setupInProgressOrDone = true;
			ready = false;
			DeserializeDictionary();
			ready = true;
		}

		private static T FindOrCreateScriptableObject<T>() where T : ScriptableObject
		{
			// FindObjectOfType can't find instances that have HideFlags.DontSave or are disabled,
			// which is why we use this slower method of searching for existing instances.
			var foundInstances = Resources.FindObjectsOfTypeAll<T>();
			#if UNITY_EDITOR
			for(int n = foundInstances.Length - 1; n >= 0; n--)
			{
				var instance = foundInstances[n];
				if(UnityEditor.AssetDatabase.Contains(instance))
				{
					#if DEV_MODE && DEBUG_ENABLED
					Debug.Log("PersistentSingleton - Found existing "+StringUtils.ToStringSansNamespace(typeof(T))+" instance using Resources.FindObjectsOfTypeAll but ignoring because it was an asset: "+UnityEditor.AssetDatabase.GetAssetPath(instance));
					#endif
					continue;
				}
				return instance;
			}
			#else
			if(foundInstances.Length > 0)
			{
				return foundInstances[0];
			}
			#endif

			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("PersistentSingleton - Creating new "+StringUtils.ToStringSansNamespace(typeof(T))+" instance.");
			#endif

			var result = CreateInstance<T>();
			// make singleton instances persist through scene changes, play mode state changes and assembly reloading.
			result.hideFlags = HideFlags.DontSave;
			return result;
		}
		
		private void SerializeDictionary()
		{
			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("PersistentSingleton - Serializing "+instances.Count+" instances:\n"+StringUtils.ToString(instances.Keys.ToArray(), "\n"));
			#endif
			
			int count = instances.Count;
			serializedState = new PersistentSingletonSerialized[count];
			if(count == 0)
			{
				return;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(InstancesToSerialize.Count == 0, "InstancesToSerialize List not empty: "+StringUtils.ToString(InstancesToSerialize));
			#endif

			var formatter = new BinaryFormatter();

			// copy values of instances temporarily to a List before iterating through it
			// to avoid exceptions should its contents get altered during the iteration
			InstancesToSerialize.AddRange(instances.Values);
			for(int n = count - 1; n >= 0; n--)
			{
				serializedState[n] = new PersistentSingletonSerialized(InstancesToSerialize[n], formatter);
			}
			InstancesToSerialize.Clear();

			if(serializedState.Length > 1)
			{
				Array.Sort(serializedState);
			}
		}
		
		private void DeserializeDictionary()
		{
			if(serializedState == null)
			{
				instances = new Dictionary<Type, object>(0);
				return;
			}

			int count = serializedState.Length;
			
			instances = new Dictionary<Type, object>(count);
			if(count == 0)
			{
				return;
			}

			var formatter = new BinaryFormatter();
			for(int n = 0; n < count; n++)
			{
				var serialized = serializedState[n];
				if(serialized == null) //TEMP
				{
					#if DEV_MODE
					Debug.LogError("PersistentSingleton.serializedState["+n+"] was null! serializedState.Length="+serializedState.Length);
					#endif
					continue;
				}

				serialized.Deserialize(formatter, ref instances);
			}
			
			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("PersistentSingleton - Deserialized "+instances.Count + " instances:\n"+StringUtils.ToString(instances.Keys.ToArray(), "\n"));
			#endif
		}

		public void OnBeforeSerialize()
		{
			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("PersistentSingleton.OnBeforeSerialize called with "+instances.Count+" instances:\n"+StringUtils.ToString(instances.Keys.ToArray(), "\n"));
			#endif

			SerializeDictionary();
		}

		public void OnAfterDeserialize()
		{
			#if DEV_MODE && DEBUG_ENABLED
			if(instances == null)
			{
				Debug.Log("PersistentSingleton.OnAfterDeserialize called with null instances, "+serializedState.Length+" serializedState items:\n"+StringUtils.ToString(serializedState, "\n"));
			}
			else
			{
				Debug.Log("PersistentSingleton.OnAfterDeserialize called with "+instances.Count+" instances, "+serializedState.Length+" serializedState items:\n"+StringUtils.ToString(serializedState, "\n"));
			}
			#endif
		}
	}
}