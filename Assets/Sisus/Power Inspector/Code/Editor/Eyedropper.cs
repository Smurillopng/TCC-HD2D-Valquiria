using JetBrains.Annotations;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[InitializeOnLoad]
	public class Eyedropper : IDisposable
	{
		public Object mouseovered;
		public static Eyedropper instance;
		public Type type;

		static Eyedropper()
		{
			ObjectReferenceDrawer.onStartedUsingEyedropper += EnableEyedropperPreview;
			ObjectReferenceDrawer.onStoppedUsingEyedropper += DisableEyedropperPreview;
		}

		private static void EnableEyedropperPreview(ObjectReferenceDrawer drawer)
		{
			if(instance == null)
			{
				instance = new Eyedropper();
				ObjectReferenceDrawer.eyedropperCurrentTarget += instance.GetMouseovered;
				instance.type = drawer.Type;
			}
		}

		private Object GetMouseovered()
		{
			return mouseovered;
		}

		private static void DisableEyedropperPreview(ObjectReferenceDrawer drawer)
		{
			if(instance != null)
			{
				ObjectReferenceDrawer.eyedropperCurrentTarget -= instance.GetMouseovered;
				instance.Dispose();
				instance = null;
			}
		}

		public Eyedropper()
		{
			#if UNITY_2019_1_OR_NEWER
			SceneView.duringSceneGui += OnSceneGUI;
			#else
			SceneView.onSceneGUIDelegate += OnSceneGUI;
			#endif
			EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
			EditorApplication.projectWindowItemOnGUI += OnProjectGUI;
			EditorApplication.update += OnUpdate;
		}

		private void OnUpdate()
		{
			if(PopupMenu.isOpen)
			{
				UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
				return;
			}

			if(InspectorUtility.MouseoveredInspector != null)
			{
				var mouseoveredDrawer = InspectorUtility.ActiveManager.MouseoveredSelectable;
				mouseovered = mouseoveredDrawer != null ? TryGetTargetOfSelectableType(mouseoveredDrawer.UnityObject, false) : null;
				SceneView.RepaintAll();
				return;
			}

			var mouseOverWindow = EditorWindow.mouseOverWindow;
			if(mouseOverWindow != null)
			{
				if(mouseOverWindow is SceneView)
				{
					return;
				}

				var type = mouseOverWindow.GetType();
				if(IsHierarchyWindow(type) || IsProjectWindow(type))
				{
					return;
				}
			}
			mouseovered = null;
		}

		private Object TryGetTargetOfSelectableType([CanBeNull]Object mouseovered, bool checkChildren)
		{
			if(mouseovered == null)
			{
				return null;
			}

			var mouseoveredType = mouseovered.GetType();
			if(type.IsAssignableFrom(mouseoveredType))
			{
				return mouseovered;
			}

			var mouseoveredGameObject = mouseovered.GameObject();
			if(mouseoveredGameObject == null)
			{
				return null;
			}

			if(type == Types.GameObject)
			{
				return mouseoveredGameObject;
			}

			if(Types.Component.IsAssignableFrom(type))
			{
				return checkChildren ? mouseoveredGameObject.GetComponentInChildren(type) : mouseoveredGameObject.GetComponent(type);
			}

			return checkChildren ? mouseoveredGameObject.FindAsset(type) : mouseoveredGameObject.FindAssetInChildren(type);
		}
		
		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			#if UNITY_2019_1_OR_NEWER
			UnityEditor.SceneView.duringSceneGui -= OnSceneGUI;
			#else
			UnityEditor.SceneView.onSceneGUIDelegate -= OnSceneGUI;
			#endif
			EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
			EditorApplication.projectWindowItemOnGUI -= OnProjectGUI;
			EditorApplication.update -= OnUpdate;

			UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
		}

		private double mouseoveredLastUpdated;

		private void OnHierarchyGUI(int instanceId, Rect selectionRect)
		{
			var mouseOverWindow = EditorWindow.mouseOverWindow;
			if(PopupMenu.isOpen || mouseOverWindow == null || !IsHierarchyWindow(mouseOverWindow.GetType()))
			{
				return;
			}

			EditorGUIUtility.AddCursorRect(new Rect(0f, 0f, Screen.width, Screen.height), UnityEditor.MouseCursor.ArrowPlus);

			if(selectionRect.Contains(Event.current.mousePosition))
			{
				mouseovered = TryGetTargetOfSelectableType(EditorUtility.InstanceIDToObject(instanceId), true);
				mouseoveredLastUpdated = EditorApplication.timeSinceStartup;
			}
			else if(selectionRect.y <= EditorGUIUtility.singleLineHeight)
			{
				if(EditorApplication.timeSinceStartup - mouseoveredLastUpdated > Time.deltaTime)
				{
					mouseovered = null;
					mouseoveredLastUpdated = EditorApplication.timeSinceStartup;
				}
			}
		}

		private void OnProjectGUI(string guid, Rect selectionRect)
		{
			var mouseOverWindow = EditorWindow.mouseOverWindow;
			if(PopupMenu.isOpen || mouseOverWindow == null || !IsProjectWindow(mouseOverWindow.GetType()))
			{
				return;
			}

			EditorGUIUtility.AddCursorRect(new Rect(0f, 0f, Screen.width, Screen.height), UnityEditor.MouseCursor.ArrowPlus);

			if(selectionRect.Contains(Event.current.mousePosition))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				mouseovered = TryGetTargetOfSelectableType(AssetDatabase.LoadMainAssetAtPath(path), true);
				mouseoveredLastUpdated = EditorApplication.timeSinceStartup;
			}
			else if(selectionRect.y <= EditorGUIUtility.singleLineHeight)
			{
				if(EditorApplication.timeSinceStartup - mouseoveredLastUpdated > Time.deltaTime)
				{
					mouseovered = null;
					mouseoveredLastUpdated = EditorApplication.timeSinceStartup;
				}
			}
		}

		private static bool IsHierarchyWindow([NotNull]Type type)
		{
			return string.Equals(type.Name, "SceneHierarchyWindow");
		}

		private static bool IsProjectWindow([NotNull]Type type)
		{
			return string.Equals(type.Name, "ProjectBrowser");
		}

		private void OnSceneGUI(SceneView sceneView)
		{
			if(Event.current.type == EventType.MouseMove && EditorWindow.mouseOverWindow == sceneView && !PopupMenu.isOpen)
			{
				mouseovered = null;

				if(type == Types.Material)
				{
					int materialIndex;
					var gameObject = HandleUtility.PickGameObject(Event.current.mousePosition, out materialIndex);
					if(gameObject != null)
					{
						if(materialIndex != -1)
						{
							var renderer = gameObject.GetComponentInChildren<Renderer>();
							if(renderer != null)
							{
								var materials = renderer.sharedMaterials;
								mouseovered = materials.Length > materialIndex ? materials[materialIndex] : renderer.sharedMaterial;
								return;
							}
							mouseovered = null;
							return;
						}
						mouseovered = TryGetTargetOfSelectableTypeFromSceneView(gameObject);
						return;
					}
				}
				else
				{
					var gameObject = HandleUtility.PickGameObject(Event.current.mousePosition, true);
					if(gameObject != null)
					{
						mouseovered = TryGetTargetOfSelectableTypeFromSceneView(gameObject);
						return;
					}
				}
				mouseovered = null;
				return;
			}

			DrawPreview(mouseovered, sceneView);
		}

		private Object TryGetTargetOfSelectableTypeFromSceneView([NotNull]GameObject gameObject)
		{
			if(gameObject.IsPrefabInstance())
			{
				#if UNITY_2018_3_OR_NEWER
				return TryGetTargetOfSelectableType(PrefabUtility.GetNearestPrefabInstanceRoot(gameObject), true);
				#else
				return TryGetTargetOfSelectableType(PrefabUtility.FindRootGameObjectWithSameParentPrefab(gameObject), true);
				#endif
			}
			return TryGetTargetOfSelectableType(gameObject, true);
		}

		public virtual void DrawPreview([CanBeNull]Object target, [NotNull]SceneView sceneView)
        {
			sceneView.Repaint();

			Handles.BeginGUI();

			EditorGUIUtility.AddCursorRect(new Rect(0f, 0f, Screen.width, Screen.height), UnityEditor.MouseCursor.ArrowPlus);

			GUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			GUILayout.Label(target == null ? "(none)" : target.name, InspectorPreferences.Styles.EyeDropper);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			Handles.EndGUI();
		}
	}
}