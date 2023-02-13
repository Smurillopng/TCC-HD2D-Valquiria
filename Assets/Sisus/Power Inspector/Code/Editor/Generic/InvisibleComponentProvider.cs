using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// A class where only a single instance is allowed to be created.
	/// 
	/// The Instance() method can be used to return an instance of the class.
	/// If the instance doesn't yet exist, one will be created.
	/// </summary>
	/// <typeparam name="T"> Type of the class inheriting from this Singleton class. </typeparam>
	public static class InvisibleComponentProvider
	{
		/// <summary>
		/// Tries to find a GameObject with name matching containerGameObjectName and component of type T inside it.
		/// If not found, creates new instance of Component inside a container GameObject with containerGameObjectName as its name.
		/// If application is quitting when method is called, returns null.
		/// </summary>
		/// <typeparam name="T"> Type of Component to return. </typeparam>
		/// <returns> And existing or new instance of Component type, or null if application is quitting. </returns>
		[CanBeNull]
		public static T GetOrCreate<T>(string containerGameObjectName) where T : Component
		{
			T result;
			if(!TryFindExisting(containerGameObjectName, out result))
			{
				result = Create<T>(containerGameObjectName);
			}
			return result;
		}

		/// <summary>
		/// Creates new instance of Component, or if one doesn't yet exist, creates a new one and caches it.
		/// If application is quitting when method is called, returns null.
		/// </summary>
		/// <typeparam name="T"> Type of Component to create. </typeparam>
		/// <returns> And instance of Component type, or null if application is quitting. </returns>
		[CanBeNull]
		public static T Create<T>() where T : Component
		{
			return Create<T>(typeof(T).Name);
		}

		/// <summary>
		/// Creates new instance of Component, or if one doesn't yet exist, creates a new one and caches it.
		/// If application is quitting when method is called, returns null.
		/// </summary>
		/// <typeparam name="T"> Type of Component to create. </typeparam>
		/// <param name="containerGameObjectName"> Name for the created container GameObject. </param>
		/// <returns> And instance of Component type, or null if application is quitting. </returns>
		[CanBeNull]
		public static T Create<T>(string containerGameObjectName) where T : Component
		{
			return Create<T>(CreateContainerGameObject(containerGameObjectName));
		}

		/// <summary>
		/// Creates new instance of Component, or if one doesn't yet exist, creates a new one and caches it.
		/// If application is quitting when method is called, returns null.
		/// </summary>
		/// <typeparam name="T"> Type of instance to return. </typeparam>
		/// <returns> And instance of class type, or null if application is quitting. </returns>
		[CanBeNull]
		public static T Create<T>(GameObject containerGameObject) where T : Component
		{
			if(ApplicationUtility.IsQuitting)
			{
				#if DEV_MODE
				Debug.LogWarning("InvisibleComponentProvider.Get<"+typeof(T).Name+">(\""+containerGameObject.name+"\") returning null because Application IsQuitting");
				#endif
				return null;
			}
			
			var result = containerGameObject.GetOrAddComponent<T>();
			result.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
			return result;
		}

		public static GameObject CreateContainerGameObject(string name)
		{
			if(ApplicationUtility.IsQuitting)
			{
				#if DEV_MODE
				Debug.LogWarning("InvisibleComponentProvider.CreateContainerGameObject(\""+name+"\") returning null because Application IsQuitting");
				#endif
				return null;
			}

			var go = new GameObject(name);
			go.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
			return go;
		}

		public static bool TryFindExisting<T>(string containerGameObjectName, out T result) where T : Component
		{
			// GameObject.Find can't be used to find GameObjects with HideFlags.HideInHierarchy
			// so we have to use the slower Resources.FindObjectsOfTypeAll
			var all = Resources.FindObjectsOfTypeAll<T>();
			foreach(var instance in all)
			{
				if(string.Equals(instance.gameObject.name, containerGameObjectName))
				{
					result = instance;
					return true;
				}
			}

			result = null;
			return false;
		}
	}
}