//#define DEBUG_CONSTRUCTOR

#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable]
	public class EditorWrapper : IPreviewableWrapper
	{
		private static readonly MethodInfo canBeExpandedViaAFoldout;
		private static readonly MethodInfo onForceReloadInspector;
		private static MethodInfo gameObjectInspectorOnEnable;
		private static MethodInfo gameObjectInspectorOnDisable;

		static EditorWrapper()
        {
			canBeExpandedViaAFoldout = Types.Editor.GetMethod("CanBeExpandedViaAFoldout", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			onForceReloadInspector = Types.Editor.GetMethod("OnForceReloadInspector", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		}

		[SerializeField]
		private Editor editor;
		private PreviewableKey key;
		
		public PreviewableKey Key
		{
			get
			{
				if(key.Equals(PreviewableKey.None))
				{
					key = new PreviewableKey(editor.GetType(), editor.targets);
				}
				return key;
			}
		}

		public Type Type
		{
			get
			{
				return Target.GetType();
			}
		}

		public bool RequiresConstantRepaint
		{
			get
			{
				return editor.RequiresConstantRepaint();
			}
		}
		
		public Object Target
		{
			get
			{
				return editor.target;
			}
		}

		public Object[] Targets
		{
			get
			{
				return editor.targets;
			}
		}

		public bool StateIsValid
		{
			get
			{
				return editor != null && !Targets.ContainsNullObjects();
			}
		}

		public EditorWrapper(Editor previewEditor)
		{
			editor = previewEditor;
			key = new PreviewableKey(editor.GetType(), editor.targets);
		}

		public EditorWrapper(Editor previewEditor, Object[] targets)
		{
			editor = previewEditor;
			key = new PreviewableKey(previewEditor.GetType(), targets);

			#if DEV_MODE && DEBUG_CONSTRUCTOR
			Debug.Log("EditorWrapper("+previewEditor+", "+StringUtils.ToString(targets)+") with key="+key.GetHashCode());
			#endif
		}
		
		public bool CanBeExpandedViaAFoldout()
		{
			try
			{
				return (bool)canBeExpandedViaAFoldout.Invoke(editor, null);
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogError(e);
			#else
			catch
			{
			#endif
				return false;
			}
		}

		public void OnInspectorGUI()
		{
			editor.OnInspectorGUI();
		}

		public bool HasPreviewGUI()
		{
			if(editor == null)
			{
				return false;
			}

			try
			{
				return editor.HasPreviewGUI();
			}
			// AnimationClipEditor threw a null reference exception
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogError(e);
			#else
			catch
			{
			#endif
				return false;
			}
		}

		public GUIContent GetPreviewTitle()
		{
			return editor.GetPreviewTitle();
		}

		public string GetInfoString()
		{
			return editor.GetInfoString();
		}

		public void OnPreviewSettings()
		{
			editor.OnPreviewSettings();
		}

		public void DrawPreview(Rect previewArea)
		{
			editor.DrawPreview(previewArea);
		}

		public void OnPreviewGUI(Rect position, GUIStyle background)
		{
			editor.OnPreviewGUI(position, background);
		}

		public void OnInteractivePreviewGUI(Rect position, GUIStyle background)
		{
			editor.OnInteractivePreviewGUI(position, background);
		}

		public void Dispose()
		{
			if(editor == null)
			{
				#if DEV_MODE
				Debug.LogWarning("EditorWrapper.Dispose - editor was null. key="+Key.GetHashCode());
				#endif
				return;
			}

			if(!ReferenceEquals(editor, null))
			{
				Editors.Dispose(ref editor);
			}
		}

		public override string ToString()
		{
			return string.Concat("EditorWrapper(", StringUtils.TypeToString(editor), ":"+ Type.FullName+")");
		}

		public void ReloadPreviewInstances()
		{
			#if DEV_MODE
			Debug.Log(editor.GetType().Name + ".ReloadPreviewInstances()");
			#endif

			editor.ReloadPreviewInstances();
			editor.Repaint();
		}

		public void OnBecameActive(Object[] targetObjects)
		{
			// trying to fix issue where Prefab preview would not update even after
			// Transform scale was changed
			if(string.Equals(editor.GetType().Name, "GameObjectInspector"))
			{
				if(gameObjectInspectorOnEnable == null)
                {
					gameObjectInspectorOnEnable = editor.GetType().GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, CallingConventions.Any, ArrayPool<Type>.ZeroSizeArray, null);
					if(gameObjectInspectorOnEnable != null)
					{
						gameObjectInspectorOnEnable.Invoke();
					}
				}
				else
                {
					gameObjectInspectorOnEnable.Invoke();
				}

				if(gameObjectInspectorOnDisable == null)
                {
					gameObjectInspectorOnDisable = editor.GetType().GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, CallingConventions.Any, ArrayPool<Type>.ZeroSizeArray, null);
					if(gameObjectInspectorOnDisable != null)
					{
						gameObjectInspectorOnDisable.Invoke();
					}
				}
				else
                {
					gameObjectInspectorOnDisable.Invoke();
				}
			}

			ReloadPreviewInstances();
			editor.ResetTarget();
			OnForceReloadInspector();
			editor.Repaint();
		}

		public void SetIsFirstInspectedEditor(bool value)
        {
			if(editor == null)
			{
				#if DEV_MODE
				Debug.LogWarning("EditorWrapper.SetIsFirstInspectedEditor - editor was null. key=" + Key.GetHashCode());
				#endif
				return;
			}

			editor.SetIsFirstInspectedEditor(value);
        }
		
		public void OnForceReloadInspector()
		{
			onForceReloadInspector.Invoke(editor);
		}
	}
}
#endif