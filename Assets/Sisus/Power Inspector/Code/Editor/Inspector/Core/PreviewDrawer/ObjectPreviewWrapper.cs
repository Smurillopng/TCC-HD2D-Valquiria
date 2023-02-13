//#define DEBUG_DISPOSE
#define DESTROY_DISPOSED_PREVIEWABLES

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable]
	public class ObjectPreviewWrapper : IPreviewableWrapper
	{
		[SerializeField]
		private GUIContent previewTitle;
		[SerializeField]
		private Type type;
		[SerializeField]
		private Object[] targets;
		[SerializeField]
		private PreviewableKey key;

		/// <summary> The wrapped ObjectPreview. </summary>
		private ObjectPreview objectPreview;
		
		public PreviewableKey Key
		{
			get
			{
				if(key.Equals(PreviewableKey.None))
				{
					key = new PreviewableKey(type, targets);
				}
				return key;
			}
		}
		
		private object Instance()
		{
			// Because objectPreview instance aren't be serialized by Unity
			// recreate them if they go null
			if(objectPreview == null)
			{
				objectPreview = (ObjectPreview)Activator.CreateInstance(type);
				objectPreview.Initialize(targets);
			}
			return objectPreview;
		}
		
		public ObjectPreviewWrapper(ObjectPreview setObjectPreview, Object[] setTargets, PreviewableKey setKey)
		{
			objectPreview = setObjectPreview;
			key = setKey;

			type = setObjectPreview.GetType();
			targets = setTargets;

			previewTitle = setObjectPreview.GetPreviewTitle();
			
			objectPreview.Initialize(setTargets);
		}
		
		/// <inheritdoc/>
		public Type Type
		{
			get
			{
				return type;
			}
		}

		public Object[] Targets
		{
			get
			{
				return targets;
			}
		}

		public bool StateIsValid
		{
			get
			{
				return Instance() != null && !Targets.ContainsNullObjects();
			}
		}

		public bool HasPreviewGUI()
		{
			return objectPreview.HasPreviewGUI();
		}

		public GUIContent GetPreviewTitle()
		{
			return previewTitle;
		}

		public string GetInfoString()
		{
			return objectPreview.GetInfoString();
		}

		public void OnPreviewSettings()
		{
			objectPreview.OnPreviewSettings();
		}

		public void DrawPreview(Rect previewArea)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(HasPreviewGUI());
			#endif

			objectPreview.DrawPreview(previewArea);
		}

		public void OnPreviewGUI(Rect position, GUIStyle background)
		{
			objectPreview.OnPreviewGUI(position, background);
		}

		public void OnInteractivePreviewGUI(Rect position, GUIStyle background)
		{
			objectPreview.OnInteractivePreviewGUI(position, background);
		}

		public void ResetTarget()
		{
			objectPreview.ResetTarget();
		}

		public void Dispose()
		{
			#if DEV_MODE && DEBUG_DISPOSE
			Debug.Log("Dispose called for Previewable "+StringUtils.TypeToString(objectPreview));
			#endif

			#if UNITY_2021_1_OR_NEWER
			objectPreview.Cleanup();
			#endif

			ResetTarget();
		}

		public override string ToString()
		{
			return string.Concat("ObjectPreviewWrapper(", StringUtils.ToString(type), ")");
		}

		public int GetInstanceId()
		{
			return Instance().GetHashCode();
		}

		public void OnBecameActive(Object[] targetObjects)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(targetObjects.ContentsMatch(targets));
			#endif

			ResetTarget(); 
			ReloadPreviewInstances();
		}

		public void ReloadPreviewInstances()
		{
			objectPreview.ReloadPreviewInstances();
		}

		public void OnForceReloadInspector()
		{
			ReloadPreviewInstances();
		}

		public void SetIsFirstInspectedEditor(bool value) { }

		public bool RequiresConstantRepaint
		{
			get
			{
				return false;
			}
		}
	}
}
#endif