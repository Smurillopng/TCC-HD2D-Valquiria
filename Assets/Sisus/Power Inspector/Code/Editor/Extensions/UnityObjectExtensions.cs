using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	public static class UnityObjectExtensions
	{
		#if UNITY_EDITOR
		public static bool RunsInEditMode(this Object target)
		{
			var monoBehaviour = target as MonoBehaviour;
			if(monoBehaviour != null)
			{
				return monoBehaviour.runInEditMode;
			}
			return target.GetType().GetCustomAttributes(typeof(ExecuteInEditMode), true).Length > 0;
		}
		#endif

		public static void GetComponentsOfExactType(this GameObject gameObject, Type type, List<Component> results)
		{
			int countWas = results.Count;
			gameObject.GetComponents(type, results);
			for(int n = results.Count - 1; n >= countWas; n++)
			{
				if(!type.Equals(results[n].GetType()))
				{
					results.RemoveAt(n);
				}
			}
		}

		[CanBeNull]
		public static Component GetComponentOfExactTypeInChildren(this GameObject gameObject, Type type)
		{
			Component result;
			#if UNITY_2019_2_OR_NEWER // Prefer TryGetComponent because it does not generate garbage in the Editor even if component is not found (unlike GetComponent).
			if(gameObject.TryGetComponent(type, out result) && result.GetType().Equals(type))
			{
				return result;
			}
			#else
			result = gameObject.GetComponent(type);
			if(result != null && result.GetType().Equals(type))
			{
				return result;
			}
			#endif
			
			var transform = gameObject.transform;
			for(int n = 0, count = transform.childCount; n < count; n++)
			{
				result = transform.GetChild(n).gameObject.GetComponentOfExactTypeInChildren(type);
				if(result != null)
				{
					return result;
				}
			}
			return null;
		}

		public static void GetComponentsOfExactTypeInChildren(this GameObject gameObject, Type type, List<Component> results)
		{
			int countWas = results.Count;
			gameObject.GetComponents(type, results);
			for(int n = results.Count - 1; n >= countWas; n--)
			{
				if(!type.Equals(results[n].GetType()))
				{
					results.RemoveAt(n);
				}
			}
			
			for(int n = 0, count = gameObject.transform.childCount; n < count; n++)
			{
				gameObject.transform.GetChild(n).gameObject.GetComponentsOfExactTypeInChildren(type, results);
			}
		}

		public static string HierarchyOrAssetPath([NotNull]this Object target)
		{
			#if UNITY_EDITOR
			string path = AssetDatabase.GetAssetPath(target);
			if(!string.IsNullOrEmpty(path))
			{
				if(string.Equals(path, "Library/unity editor resources"))
				{
					return path + "/" + target.name;
				}
				return path;
			}
			#endif

			var transform = target.Transform();
			if(transform != null)
			{
				return transform.HierarchyPath();
			}
			return target.name;
		}

		public static Transform Transform([NotNull]this Object target)
		{
			var comp = target as Component;
			if(comp != null)
			{
				try
				{
					return comp.transform;
				}
				catch(MissingReferenceException)
				{
					return null;
				}
			}

			var go = target as GameObject;
			if(go != null)
			{
				return go.transform;
			}

			return null;
		}

		public static GameObject GameObject([NotNull]this Object target)
		{
			var comp = target as Component;
			if(comp != null)
			{
				try
				{
					return comp.gameObject;
				}
				catch(MissingReferenceException)
				{
					return null;
				}
			}

			var go = target as GameObject;
			if(go != null)
			{
				return go;
			}

			return null;
		}

		public static bool IsPrefab([NotNull]this Object target)
		{
			#if UNITY_EDITOR

			#if UNITY_2018_3_OR_NEWER
			return PrefabUtility.IsPartOfPrefabAsset(target);
			#else
			var type = PrefabUtility.GetPrefabType(target);
			return type == PrefabType.Prefab || type == PrefabType.ModelPrefab;
			#endif

			#else
			var go = target as GameObject;
			if(go != null)
			{
				if(!go.scene.IsValid())
				{
					return true;
				}
			}
			return false;
			#endif
		}

		public static bool IsPrefabInstance([NotNull]this Object target)
		{
			#if UNITY_EDITOR

			#if UNITY_2018_3_OR_NEWER
			return PrefabUtility.IsPartOfPrefabInstance(target);
			#else
			switch(PrefabUtility.GetPrefabType(target))
			{
				case PrefabType.PrefabInstance:
				case PrefabType.ModelPrefabInstance:
				case PrefabType.DisconnectedPrefabInstance:
				case PrefabType.MissingPrefabInstance:
					return true;
				default:
					return false;
			}
			#endif

			#else
			var go = target as GameObject;
			if(go != null)
			{
				if(!go.scene.IsValid())
				{
					return true;
				}
			}
			return false;
			#endif
		}

		public static bool IsModel([NotNull]this Object target)
		{
			#if UNITY_EDITOR

			#if UNITY_2018_3_OR_NEWER
			return PrefabUtility.IsPartOfModelPrefab(target);
			#else
			var type = PrefabUtility.GetPrefabType(target);
			return type == PrefabType.ModelPrefab || type == PrefabType.ModelPrefabInstance || type == PrefabType.DisconnectedModelPrefabInstance;
			#endif

			#else
			return false;
			#endif
		}

		public static bool IsSceneObject(this Object target)
		{
			var go = target.GameObject();
			return go != null && go.scene.IsValid();
		}

		/// <summary> If object is a GameObject gets is defining Component, else returns the object itself. </summary>
		/// <param name="obj"> The object to act on. </param>
		/// <returns> Defining component or asset itself. </returns>
		public static Object GetAssetOrMainComponent(this Object obj)
		{
			var gameObject = obj.GameObject();
			return gameObject != null ? gameObject.GetMainComponent() : obj;
		}

		/// <summary>
		/// Given a UnityEngine.Object whose == operator returns true when compared against null, this
		/// attempts to make it not be null, but trying to find instance again using the target's Instance ID.
		/// </summary>
		/// <param name="obj"> [in,out] The null object. </param>
		/// <returns> True if it succeeds, false if it fails. </returns>
		public static bool TryToFixNull(ref Object obj)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(obj == null);
			#endif

			if(ReferenceEquals(obj, null))
			{
				return false;
			}

			// if reference not actually null try to recover UnityEngine.Object reference using target InstanceID
			var id = obj.GetInstanceID();
			obj = EditorUtility.InstanceIDToObject(id);
			return obj;
		}

		/// <summary> Attempts to extract UnityEngine.Object references to a a List&lt;Object&gt; from the given object. </summary>
		/// <param name="something"> An object of unknown type. </param>
		/// <param name="result"> [in,out] The result. This cannot be null. </param>
		/// <returns> True if it succeeds, false if it fails. </returns>
		public static bool TryExtractObjectReferences(object something, [NotNull]ref List<Object> result)
		{
			if(something == null)
			{
				return false;
			}

			var obj = something as Object;
			if(obj != null)
			{
				result.Add(obj);
				return true;
			}

			var ienumerable = obj as IEnumerable;
			if(ienumerable != null)
			{
				result = new List<Object>();
				foreach(var innerSomething in ienumerable)
				{
					TryExtractObjectReferences(innerSomething, ref result);
				}
				return result.Count > 0;
			}
			return false;
		}
		
		public static bool IsUnityObjectOrUnityObjectCollectionType(Type type)
		{
			if(Types.UnityObject.IsAssignableFrom(type))
			{
				return true;
			}

			if(type.IsArray)
			{
				return IsUnityObjectOrUnityObjectCollectionType(type.GetElementType());
			}

			if(Types.IList.IsAssignableFrom(type) && type.IsGenericType)
			{
				return IsUnityObjectOrUnityObjectCollectionType(type.GetGenericArguments()[0]);
			}
			
			#if DEV_MODE
			Debug.Log("IsUnityObjectOrUnityObjectCollectionType "+StringUtils.ToStringSansNamespace(type)+": "+StringUtils.False);
			#endif

			return false;
		}

		public static GameObject[] GameObjects([NotNull]this Object[] targets)
		{
			int count = targets.Length;
			var result = ArrayPool<GameObject>.Create(count);
			for(int n = count - 1; n >= 0; n--)
			{
				var target = targets[n];
				result[n] = target == null ? null : target.GameObject();
			}
			return result;
		}
	}
}