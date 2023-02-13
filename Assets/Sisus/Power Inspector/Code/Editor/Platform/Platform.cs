using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// The Platform class helps with creating platform-based branching for methods,
	/// reducing the need to use preprocessor directives for platform dependent compilation
	/// and resulting in code that works across different platforms.
	/// 
	/// E.g. if you write the GUI drawing logic of your Custom Editors using the GUI
	/// property of this class, then it will incorporate all the benefits of using the
	/// editor-only EditorGUI class in the editor, while switching to use the runtime-supported
	/// GUI class in builds.
	/// 
	/// To use it, you need to set Platform.Active to refer to the correct plaform once at the
	/// beginning of each method call chain (like the beginning of your OnGUI method), and then
	/// everything else can just use Platform.Active to gain access to the right methods for the
	/// current platform.
	/// </summary>
	public class Platform
	{
		// not using a direct readonly assignment of EditorPlatform so that
		// it can be located inside an Editor folder
		//public static Platform Editor;
		//public static readonly RuntimePlatform Runtime = new RuntimePlatform();

		public static Action<GameObject> OnGameObjectCreated;

		/// <summary>
		/// Should be set to refer to Editor or Runtime at the beginning of
		/// each method call chain (like the beginning of your OnGUI method).
		/// </summary>
		public static readonly Platform Active = new Platform();

		public static readonly EditorGUIDrawer GUIDrawer = new EditorGUIDrawer();
		public static readonly RuntimeGUIDrawer RuntimeGUIDrawer = new RuntimeGUIDrawer();

		public static Platform Editor
		{
			get
			{
				return Active;
			}
		}

		public void SetPrefs(string key, int value, int defaultValue)
		{
			if(value.Equals(defaultValue))
			{
				DeletePrefs(key);
			}
			else
			{
				SetPrefs(key, value);
			}
		}

		public void SetPrefs(string key, float value, float defaultValue)
		{
			if(value.Equals(defaultValue))
			{
				DeletePrefs(key);
			}
			else
			{
				SetPrefs(key, value);
			}
		}

		public void SetPrefs(string key, bool value, bool defaultValue)
		{
			if(value == defaultValue)
			{
				DeletePrefs(key);
			}
			else
			{
				SetPrefs(key, value);
			}
		}

		
		
		public DrawGUI GUI
		{
			get
			{
				return GUIDrawer;
			}
		}

		public bool IsPlayingOrWillChangePlaymode
		{
			get
			{
				return EditorApplication.isPlayingOrWillChangePlaymode;
			}
		}

		public string GetPrefs(string key, string defaultValue)
		{
			return EditorPrefs.GetString(key, defaultValue);
		}

		public void SetPrefs(string key, string value)
		{
			EditorPrefs.SetString(key, value);
		}

		public void DeletePrefs(string key)
		{
			EditorPrefs.DeleteKey(key);
		}

		public float GetPrefs(string key, float defaultValue)
		{
			return EditorPrefs.GetFloat(key, defaultValue);
		}

		public void SetPrefs(string key, float value)
		{
			EditorPrefs.SetFloat(key, value);
		}

		public bool GetPrefs(string key, bool defaultValue)
		{
			return EditorPrefs.GetBool(key, defaultValue);
		}

		public void SetPrefs(string key, bool value)
		{
			EditorPrefs.SetBool(key, value);
		}

		public bool HasPrefs(string key)
		{
			return EditorPrefs.HasKey(key);
		}

		public Component AddComponent(GameObject gameObject, Type type)
		{
			#if DEV_MODE && DEBUG_ADD_COMPONENT
			Debug.Log("\"" + gameObject.name + "\".AddComponent("+type.Name+")");
			#endif
			
			#if UNITY_2018_1_OR_NEWER
			//ObjectFactory handles Undo registration and applies default values from the project
			return ObjectFactory.AddComponent(gameObject, type);
			#else
			return Undo.AddComponent(gameObject, type);
			#endif
		}

		public GameObject CreateGameObject(string name)
		{
			#if UNITY_2018_1_OR_NEWER
			//ObjectFactory handles Undo registration and applies default values from the project
			var created = ObjectFactory.CreateGameObject(name);
			#else
			var created = new GameObject(name);
			Undo.RegisterCreatedObjectUndo(created, "CreateGameObject(\""+name+"\")");
			#endif

			if(OnGameObjectCreated != null)
			{
				OnGameObjectCreated(created);
			}

			return created;
		}

		public Object CreateInstance(Type type)
		{
			#if UNITY_2018_1_OR_NEWER
			//ObjectFactory handles Undo registration and applies default values from the project
			return ObjectFactory.CreateInstance(type);
			#else
			var created = ScriptableObject.CreateInstance(type);
			Undo.RegisterCreatedObjectUndo(created, "CreateInstance("+type.Name+")");
			return created;
			#endif
		}

		public void SetDirty(Object asset)
		{
			EditorUtility.SetDirty(asset);
		}

		public void Destroy(Object target)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(target != null);
			Debug.Assert(!Types.EditorWindow.IsAssignableFrom(target.GetType()));
			#endif

			#if DEV_MODE && DEBUG_DESTROY
			Debug.Log("Destroy("+target.GetType().Name+")");
			#endif

			#if DEV_MODE
			if(target is EditorWindow)
			{
				Debug.LogWarning("Destroy was called for EditorWindow target. EditorWindow.Close should be used instead in most instances.");
			}
			#endif

			if(!Application.isPlaying)
			{
				Undo.DestroyObjectImmediate(target);
				return;
			}
			Object.Destroy(target);
		}

		public void Select(Object target)
		{
			#if DEV_MODE && DEBUG_SELECT
			Debug.Log("Select("+StringUtils.ToString(target)+")");
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			if(target is Transform) { Debug.LogWarning("Select called for transform, not GameObject. Intentional?"); }
			#endif

			var manager = InspectorUtility.ActiveManager;
			if(manager != null)
			{
				var inspector = manager.ActiveSelectedOrDefaultInspector();
				if(inspector != null)
				{
					inspector.Select(target);
					return;
				}
			}

			Selection.activeObject = target;
		}

		public void Select(Object[] targets)
		{
			#if DEV_MODE && DEBUG_SELECT
			Debug.Log("Select("+StringUtils.ToString(targets)+")");
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			if(targets.Length > 0 && targets[0] is Transform) { Debug.LogWarning("Select called for transforms, not GameObjects. Intentional?"); }
			#endif

			var manager = InspectorUtility.ActiveManager;
			if(manager != null)
			{
				var inspector = manager.ActiveSelectedOrDefaultInspector();
				if(inspector != null)
				{
					inspector.Select(targets);
					return;
				}
			}
			Selection.objects = targets;
		}

		public static bool EditorMode
		{
			get
			{
				#if UNITY_EDITOR
				return true;
				#else
				return false;
				#endif
			}
		}

		public static float Time
		{
			get
			{
				#if UNITY_EDITOR
				if(!Application.isPlaying)
				{
					return (float)UnityEditor.EditorApplication.timeSinceStartup;
				}
				#endif
				return UnityEngine.Time.realtimeSinceStartup;
			}
		}

		public static bool IsCompiling
		{
			get
			{
				#if UNITY_EDITOR
				return UnityEditor.EditorApplication.isCompiling;
				#else
				return false;
				#endif
			}
		}
	}
}