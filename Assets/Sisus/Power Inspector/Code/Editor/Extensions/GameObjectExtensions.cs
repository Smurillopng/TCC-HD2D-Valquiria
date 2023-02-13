#define SAFE_MODE

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;
using JetBrains.Annotations;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	public static class GameObjectExtensions
	{
		private static List<GameObject> getGameObjects = new List<GameObject>(0);

		public static T AddComponentUndoable<T>(this GameObject gameObject) where T : Component
		{
			#if UNITY_EDITOR
			if(!Application.isPlaying)
			{
				return Undo.AddComponent<T>(gameObject);
			}
			#endif
			return gameObject.AddComponent<T>();
		}

		public static Component AddComponentUndoable(this GameObject gameObject, Type type)
		{
			#if UNITY_EDITOR
			if(!Application.isPlaying)
			{
				return Undo.AddComponent(gameObject, type);
			}
			#endif
			return gameObject.AddComponent(type);
		}


		public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
		{
			T result = gameObject.GetComponent<T>();
			if(result != null)
			{
				return result;
			}
			return gameObject.AddComponentUndoable<T>();
		}
		
		public static GameObject[] GetAllGameObjects()
		{
			int loadedSceneCount = SceneManager.sceneCount;
			
			for(int sceneIndex = 0; sceneIndex < loadedSceneCount; sceneIndex++)
			{
				var scene = SceneManager.GetSceneAt(sceneIndex);
				if(scene.IsValid())
				{
					var rootObjects = scene.GetRootGameObjects();
					int count = rootObjects.Length;
					for(int n = count - 1; n >= 0; n--)
					{
						rootObjects[n].AddGameObjectAndChildGameObjectsToList(ref getGameObjects);
					}
				}
			}
			var results = getGameObjects.ToArray();
			getGameObjects.Clear();
			return results;
		}

		public static GameObject[] GetRootGameObjects()
		{
			int loadedSceneCount = SceneManager.sceneCount;
			for(int sceneIndex = 0; sceneIndex < loadedSceneCount; sceneIndex++)
			{
				var scene = SceneManager.GetSceneAt(sceneIndex);
				if(scene.IsValid())
				{
					getGameObjects.AddRange(scene.GetRootGameObjects());
				}
			}
			var results = getGameObjects.ToArray();
			getGameObjects.Clear();
			return results;
		}

		/// <summary> 
		/// Gets all GameObjects in scene, sorted by order in hierarchy.
		/// </summary>
		/// <param name="scene"> The target scene. </param>
		/// <returns> An array containing all GameObjects in scene. </returns>
		public static GameObject[] GetAllGameObjects(this Scene scene)
		{
			GetAllGameObjects(scene, getGameObjects);
			var results = getGameObjects.ToArray();
			getGameObjects.Clear();
			return results;
		}

		/// <summary> 
		/// Gets all GameObjects in scene, sorted by order in hierarchy.
		/// </summary>
		/// <param name="scene"> The target scene. </param>
		/// <param name="list"> List into which all GameObjects in scene are added. </param>
		public static void GetAllGameObjects(this Scene scene, List<GameObject> list)
		{
			if(scene.IsValid())
			{
				var rootObjects = scene.GetRootGameObjects();

				int count = rootObjects.Length;

				for(int n = count - 1; n >= 0; n--)
				{
					rootObjects[n].AddGameObjectAndChildGameObjectsToList(ref list);
				}
			}
		}
		
		/// <summary> 
		/// Gets array containing target and all GameObjects in its children, where target
		/// will be the first element in the array, and its members (if any) will be
		/// sorted by their hierarchy order.
		/// </summary>
		/// <param name="target"> The target GameObject. </param>
		/// <returns> An array containing target and its children. </returns>
		public static GameObject[] GetAllGameObjectsInChildren([NotNull]this GameObject target)
		{
			AddGameObjectAndChildGameObjectsToList(target, ref getGameObjects);
			var result = getGameObjects.ToArray();
			getGameObjects.Clear();
			return result;
		}

		/// <summary> 
		/// Gets target and all GameObjects in its children, where target will be added to the List first,
		/// and its members (if any) will be added after that in order matching their hierarchy order.
		/// </summary>
		/// <param name="target"> The target GameObject. </param>
		/// <param name="list"> List into which target and its children are added. </param>
		public static void AddGameObjectAndChildGameObjectsToList([NotNull]this GameObject target, ref List<GameObject> list)
		{
			list.Add(target);
			var t = target.transform;
			for(int n = t.childCount - 1; n >= 0; n--)
			{
				AddGameObjectAndChildGameObjectsToList(t.GetChild(n).gameObject, ref list);
			}
		}

		public static bool ActiveInPrefabHierarchy([NotNull]this GameObject target)
		{
			return target.transform.ActiveInPrefabHierarchy();
		}

		/// <summary> Gets the defining Component of the GameObject. </summary>
		/// <param name="gameObject"> The gameObject whose main Component to get. </param>
		/// <returns> If GameObject has only Transform component, returns that, else returns the first Component after that. </returns>
		public static Component GetMainComponent([NotNull]this GameObject gameObject)
		{
			var allComponents = gameObject.GetComponents<Component>();
			var fallback = allComponents[0];
			for(int n = 1; n < allComponents.Length; n++)
			{
				var component = allComponents[n];
				if(component != null)
				{
					//skip disabled Components, if there are others available
					if(component.IsEnabled())
					{
						return component;
					}
					fallback = component;
				}
			}
			return fallback;
		}

		public static GameObject[] GetChildren([NotNull]this GameObject parent)
		{
			var parentTransform = parent.transform;
			int count = parentTransform.childCount;
			var results = new GameObject[count];
			for(int n = 0; n < count; n++)
			{
				results[n] = parentTransform.GetChild(n).gameObject;
			}
			return results;
		}

		public static Object FindAssetInChildren([NotNull]this GameObject gameObject, Type assetType)
		{
			if(assetType == Types.Texture)
			{
				return gameObject.FindTextureInChildren<Texture>();
			}
			if(assetType == Types.Texture2D)
			{
				return gameObject.FindTextureInChildren<Texture2D>();
			}
			if(assetType == typeof(Sprite))
			{
				return gameObject.FindSpriteInChildren();
			}
			if(assetType == Types.Material)
			{
				return gameObject.FindMaterialInChildren();
			}
			if(assetType == typeof(Mesh))
			{
				return gameObject.FindMeshInChildren();
			}
			return null;
		}

		public static Object FindAsset([NotNull]this GameObject gameObject, Type assetType)
		{
			if(assetType == Types.Texture)
			{
				return gameObject.FindTexture<Texture>();
			}
			if(assetType == Types.Texture2D)
			{
				return gameObject.FindTexture<Texture2D>();
			}
			if(assetType == typeof(Sprite))
			{
				return gameObject.FindSprite();
			}
			if(assetType == Types.Material)
			{
				return gameObject.FindMaterial();
			}
			if(assetType == typeof(Mesh))
			{
				return gameObject.FindMesh();
			}
			return null;
		}

		public static Material FindMaterialInChildren([NotNull]this GameObject gameObject)
		{
			var result = gameObject.FindMaterial();
			if(result != null)
			{
				return result;
			}

			var transform = gameObject.transform;
			for(int n = 0, count = transform.childCount; n < count; n++)
			{
				result = transform.GetChild(n).gameObject.FindMaterialInChildren();
				if(result != null)
				{
					return result;
				}
			}

			return null;
		}

		public static Material FindMaterial([NotNull]this GameObject gameObject)
		{
			#if UNITY_2019_2_OR_NEWER // Prefer TryGetComponent because it does not generate garbage in the Editor even if component is not found (unlike GetComponent).
			Renderer renderer;
			if(gameObject.TryGetComponent(out renderer))
			{
				return renderer.sharedMaterial;
			}
			Image image;
			if(gameObject.TryGetComponent(out image))
			{
				return image.materialForRendering;
			}
			#else
			var renderer = gameObject.GetComponent<Renderer>();
			if(renderer != null)
			{
				return renderer.sharedMaterial;
			}
			var image = gameObject.GetComponent<Image>();
			if(image != null)
			{
				return image.materialForRendering;
			}
			#endif

			return null;
		}

		public static Mesh FindMeshInChildren([NotNull]this GameObject gameObject)
		{
			var result = gameObject.FindMesh();
			if(result != null)
			{
				return result;
			}

			var transform = gameObject.transform;
			for(int n = 0, count = transform.childCount; n < count; n++)
			{
				result = transform.GetChild(n).gameObject.FindMeshInChildren();
				if(result != null)
				{
					return result;
				}
			}

			return null;
		}

		public static Mesh FindMesh([NotNull]this GameObject gameObject)
		{
			#if UNITY_2019_2_OR_NEWER // Prefer TryGetComponent because it does not generate garbage in the Editor even if component is not found (unlike GetComponent).
			MeshFilter meshFilter;
			if(!gameObject.TryGetComponent(out meshFilter))
			{
				return null;
			}
			#else
			var meshFilter = gameObject.GetComponent<MeshFilter>();
			if(meshFilter == null)
			{
				return null;
			}
			#endif

			return meshFilter.sharedMesh;
		}


		public static TTexture FindTextureInChildren<TTexture>([NotNull]this GameObject gameObject) where TTexture : Texture
		{
			var result = gameObject.FindTexture<TTexture>();
			if(result != null)
			{
				return result;
			}

			var transform = gameObject.transform;
			for(int n = 0, count = transform.childCount; n < count; n++)
			{
				result = transform.GetChild(n).gameObject.FindTextureInChildren<TTexture>();
				if(result != null)
				{
					return result;
				}
			}

			return null;
		}

		public static TTexture FindTexture<TTexture>([NotNull]this GameObject gameObject) where TTexture : Texture
		{
			#if UNITY_2019_2_OR_NEWER // Prefer TryGetComponent because it does not generate garbage in the Editor even if component is not found (unlike GetComponent).
			Renderer renderer;
			if(gameObject.TryGetComponent(out renderer))
			{
				var material = renderer.sharedMaterial;
				return material == null ? null : material.mainTexture as TTexture;
			}
			Image image;
			if(gameObject.TryGetComponent(out image))
			{
				return image.mainTexture as TTexture;
			}
			#else
			var renderer = gameObject.GetComponent<Renderer>();
			if(renderer != null)
			{
				var material = renderer.sharedMaterial;
				return material == null ? null : material.mainTexture as TTexture;
			}
			var image = gameObject.GetComponent<Image>();
			if(image != null)
			{
				return image.mainTexture as TTexture;
			}
			#endif

			return null;
		}

		public static Sprite FindSpriteInChildren([NotNull]this GameObject gameObject)
		{
			var result = gameObject.FindSprite();
			if(result != null)
			{
				return result;
			}

			var transform = gameObject.transform;
			for(int n = 0, count = transform.childCount; n < count; n++)
			{
				result = transform.GetChild(n).gameObject.FindSpriteInChildren();
				if(result != null)
				{
					return result;
				}
			}

			return null;
		}

		public static Sprite FindSprite([NotNull]this GameObject gameObject)
		{
			#if UNITY_2019_2_OR_NEWER // Prefer TryGetComponent because it does not generate garbage in the Editor even if component is not found (unlike GetComponent).
			SpriteRenderer renderer;
			if(gameObject.TryGetComponent(out renderer))
			{
				return renderer.sprite;
			}
			Image image;
			if(gameObject.TryGetComponent(out image))
			{
				return image.sprite;
			}
			#else
			var renderer = gameObject.GetComponent<SpriteRenderer>();
			if(renderer != null)
			{
				return renderer.sprite;
			}
			var image = gameObject.GetComponent<Image>();
			if(image != null)
			{
				return image.sprite;
			}
			#endif
			return null;
		}
	}
}