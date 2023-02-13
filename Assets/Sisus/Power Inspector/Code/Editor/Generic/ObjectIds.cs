#define SAFE_MODE

#define DEBUG_CACHE

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	/// <summary>
	/// Class that handles caching and fetching Objects by their instance ids.
	/// This is similar to using EditorUtility.InstanceIDToObject, except it has runtime support
	/// </summary>
	public class ObjectIds : IBinarySerializable
	{
		private static ObjectIds instance;
		private static readonly Dictionary<int, Scene> SceneHandlesCache = new Dictionary<int, Scene>();
		
		private Dictionary<Scene, BiDictionary<int, Object>> instanceIdsByScene;
		
		int? IBinarySerializable.DeserializationOrder
		{
			get
			{
				return PersistentSingletonSerialized.DefaultDeserializationOrder - 100;
			}
		}

		private static ObjectIds Instance()
		{
			if(instance == null)
			{
				instance = PersistentSingleton.Get<ObjectIds>();
				if(instance != null)
				{
					instance.Setup(0);
				}
			}
			return instance;
		}
		
		private void Setup(int dictionaryCapacity)
		{
			SceneManager.sceneUnloaded += OnSceneUnloaded;
			instanceIdsByScene = new Dictionary<Scene, BiDictionary<int, Object>>(dictionaryCapacity);
		}

		public static int[] Get(IList<Object> objs)
		{
			return Instance().GetInternal(objs);
		}
		
		public static void Get(IList<Object> objs, [CanBeNull]ref List<int> ids)
		{
			Instance().GetInternal(objs, ref ids);
		}
		
		public static void GetTargets(IList<int> ids, [CanBeNull]ref List<Object> objs)
		{
			Instance().GetTargetsInternal(ids, ref objs);
		}
		
		/// <summary> Gets int for the given target. </summary>
		/// <param name="target"> The target for which int should be created. This cannot be null. </param>
		/// <returns> int. </returns>
		public static int Get([NotNull]Object target)
		{
			return Instance().GetIdInternal(target);
		}

		/// <summary> Gets target that the int represents. </summary>
		/// <returns> The target. </returns>
		public static Object GetTarget(int data)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(PersistentSingleton.Ready || PersistentSingleton.Exists<ObjectIds>(), "GetTarget called with with PersistentSingleton not ready and ObjectIds instance not yet existing");
			#endif

			return Instance().GetTargetInternal(data);
		}

		public static int GetHandle(Scene scene)
		{
			#if UNITY_2018_3_OR_NEWER
			return scene.handle;
			#else
			return (int)typeof(Scene).GetField("m_Handle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(scene);
			#endif
		}

		public byte[] Serialize(BinaryFormatter formatter)
		{
			int invalidSceneHandle = GetHandle(default(Scene));
			int sceneCount = instanceIdsByScene == null ? 0 : instanceIdsByScene.Count;

			if(sceneCount == 0)
			{
				using(var stream = new MemoryStream(54))
				{
					formatter.Serialize(stream, 0);
					return stream.ToArray();
				}
			}
			
			using(var stream = new MemoryStream(sceneCount * 57))
			{
				// 1: scene count
				formatter.Serialize(stream, sceneCount);

				foreach(var sceneIds in instanceIdsByScene)
				{
					var scene = sceneIds.Key;
					// 2: scene handle
					if(!scene.IsValid())
					{
						formatter.Serialize(stream, invalidSceneHandle);
					}
					else
					{
						formatter.Serialize(stream, GetHandle(scene));
					}
					
					var objs = sceneIds.Value;
					
					// 3: InstanceID count for scene
					formatter.Serialize(stream, objs.Count);

					// 4: Instance IDs for scene
					foreach(var obj in objs)
					{
						formatter.Serialize(stream, obj.Key);
					}
				}

				#if DEV_MODE
				UnityEngine.Debug.Log("sceneCount="+sceneCount+", stream.Length="+stream.Length);
				#endif

				return stream.ToArray();
			}
		}
		
		public void DeserializeOverride(BinaryFormatter formatter, MemoryStream stream)
		{
			// 1: scene count
			int sceneCount = (int)formatter.Deserialize(stream);
			
			Setup(sceneCount);

			if(sceneCount == 0)
			{
				return;
			}

			var defaultScene = default(Scene);
			SceneHandlesCache.Add(GetHandle(defaultScene), defaultScene);
			for(int n = SceneManager.sceneCount; n >= 0; n--)
			{
				var scene = SceneManager.GetSceneAt(n);
				SceneHandlesCache.Add(GetHandle(scene), scene);
			}

			for(int n = 0; n < sceneCount; n++)
			{
				// 2: scene handle
				int sceneHandle = (int)formatter.Deserialize(stream);
				Scene scene;
				bool sceneIsLoaded = SceneHandlesCache.TryGetValue(sceneHandle, out scene);

				// 3: InstanceID count for scene
				int idCount = (int)formatter.Deserialize(stream);

				if(sceneIsLoaded)
				{
					var objs = new BiDictionary<int, Object>(idCount);

					// 4: Instance IDs for scene
					for(int i = idCount - 1; i >= 0; i--)
					{
						var id = (int)formatter.Deserialize(stream);
						objs.Add(id, GetTargetInternal(id));
					}
					instanceIdsByScene.Add(scene, objs);
				}
				// if scene is not loaded, discard the data
				else
				{
					for(int i = idCount - 1; i >= 0; i--)
					{
						formatter.Deserialize(stream);
					}
				}
			}

			#if DEV_MODE
			UnityEngine.Debug.Log(StringUtils.ToStringSansNamespace(GetType())+".DeserializeOverride InstanceIdsByScene now has "+instanceIdsByScene.Count+" items:\n"+StringUtils.ToString(instanceIdsByScene, "\n"));
			#endif
		}
		
		// InstanceIDs for Objects in a scene are not the same if a Scene is unloaded and loaded again,
		// so we should discard all InstanceID data for an unloaded scene.
		private void OnSceneUnloaded(Scene unloadedScene)
		{
			BiDictionary<int, Object> ids;
			if(instanceIdsByScene.TryGetValue(unloadedScene, out ids))
			{
				// Handle instance where objects have been moved between scenes using SceneManager.MoveGameObjectToScene
				var objs = ids.SecondToFirstDictionary().Keys;
				foreach(var obj in objs)
				{
					if(obj == null)
					{
						continue;
					}
					var go = obj.GameObject();
					var scene = go.scene;
					if(go.scene == unloadedScene || !scene.IsValid())
					{
						continue;
					}

					instanceIdsByScene[scene].Add(go.GetInstanceID(), go);
				}
				instanceIdsByScene.Remove(unloadedScene);
			}
			
		}

		private int[] GetInternal(IList<Object> objs)
		{
			int count = objs.Count;
			var ids = new int[count];
			for(int n = 0; n < count; n++)
			{
				var obj = objs[n];
				ids[n] = obj == null ? 0 : obj.GetInstanceID();
			}
			return ids;
		}

		private void GetInternal(IList<Object> objs, [CanBeNull]ref List<int> ids)
		{
			int count = objs.Count;
			if(ids == null)
			{
				ids = new List<int>(count);
			}
			for(int n = 0; n < count; n++)
			{
				var obj = objs[n];
				ids.Add(obj == null ? 0 : obj.GetInstanceID());
			}
		}

		private void GetTargetsInternal(IList<int> ids, [CanBeNull]ref List<Object> objs)
		{
			int count = ids.Count;
			if(objs == null)
			{
				objs = new List<Object>(count);
			}
			for(int n = 0; n < count; n++)
			{
				var id = ids[n];
				var obj = GetTargetInternal(id);
				if(obj != null)
				{
					objs.Add(obj);
				}
			}
		}

		private int GetIdInternal(Object target)
		{
			if(target == null)
			{
				return 0;
			}

			BiDictionary<int, Object> ids;

			Scene scene;
			var go = target.GameObject();
			if(go != null)
			{
				scene = go.scene;
				if(!scene.IsValid())
				{
					scene = default(Scene);
				}
			}
			else
			{
				scene = default(Scene);
			}
			
			if(!instanceIdsByScene.TryGetValue(scene, out ids))
			{
				ids = new BiDictionary<int, Object>();
			}

			int id = target.GetInstanceID();

			ids[id] = target;

			return id;
		}

		/// <summary>
		/// Gets target that the int represents.
		/// </summary>
		/// <returns> The target. </returns>
		[CanBeNull]
		private Object GetTargetInternal(int id)
		{
			if(id == 0)
			{
				return null;
			}

			#if UNITY_EDITOR
			return EditorUtility.InstanceIDToObject(id);
			#else
			foreach(var sceneData in instanceIdsByScene)
			{
				var ids = sceneData.Value;
				Object result;
				if(ids.TryGet(id, out result))
				{
					return result;
				}
			}
			return null;
			#endif
		}
	}
}