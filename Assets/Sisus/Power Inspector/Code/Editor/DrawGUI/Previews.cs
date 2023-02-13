#define DESTROY_DISPOSED_EDITORS
#define DESTROY_DISPOSED_EDITOR_SERIALIZED_OBJECTS
//#define DEBUG_DISPOSE

using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Class for creating and caching IPreviewables for targets, to be drawn in the Preview area of the inspector.
	/// Instance and cached previews will persists through assembly reloads.
	/// </summary>
	public class Previews
	{
		private static Previews instance;

		private Dictionary<Type, List<Type>> previewablesByTarget;
		private Dictionary<PreviewableKey, IPreviewableWrapper> cachedPreviews;

		private List<KeyValuePair<PreviewableKey, IPreviewableWrapper>> cachedPreviewsToDispose = new List<KeyValuePair<PreviewableKey, IPreviewableWrapper>>();
		private Dictionary<PreviewableKey, IPreviewableWrapper> cachedPreviewsCleaned = new Dictionary<PreviewableKey, IPreviewableWrapper>();
		
		private void BuildPreviewablesByTypeDictionary()
		{
			previewablesByTarget = new Dictionary<Type, List<Type>>(128);

			var ipreviewableInterface = Types.Editor.GetInterface("UnityEditor.IPreviewable");
			var customPreviewType = typeof(CustomPreviewAttribute);
			var declaredFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic;
			var customPreviewTypeField = customPreviewType.GetField("m_Type", declaredFlags);

			foreach(var previewableType in ipreviewableInterface.GetImplementingTypes(true, false))
			{
				if(Types.Editor.IsAssignableFrom(previewableType))
				{
					continue;
				}
				var atts = previewableType.GetCustomAttributes(customPreviewType, false);
				for(int a = atts.Length - 1; a >= 0; a--)
				{
					var customPreviewAttribute = atts[a] as CustomPreviewAttribute;
					var targetType = (Type)customPreviewTypeField.GetValue(customPreviewAttribute);
						
					List<Type> previewablesList;
					if(previewablesByTarget.TryGetValue(targetType, out previewablesList))
					{
						previewablesList.Add(previewableType);
					}
					else
					{
						previewablesByTarget.Add(targetType, new List<Type>{previewableType});
					}
				}
			}
		}

		public static Previews Instance()
		{
			if(instance == null)
			{
				instance = new Previews();
				instance.Setup();
			}
			return instance;
		}

		private void Setup()
		{
			if(previewablesByTarget == null)
			{
				BuildPreviewablesByTypeDictionary();
			}

			if(cachedPreviews == null)
			{
				cachedPreviews = new Dictionary<PreviewableKey, IPreviewableWrapper>();
			}

			if(cachedPreviewsCleaned == null)
			{
				cachedPreviewsCleaned = new Dictionary<PreviewableKey, IPreviewableWrapper>();
			}

			if(cachedPreviewsToDispose == null)
			{
				cachedPreviewsToDispose = new List<KeyValuePair<PreviewableKey, IPreviewableWrapper>>();
			}
		}
		
		public static bool IsCached(IPreviewableWrapper previewableWrapper)
		{
			return IsCached(previewableWrapper.Key);
		}
		
		public static bool IsCached(PreviewableKey editor)
		{
			return Instance().cachedPreviews.ContainsKey(editor);
		}

		public static bool RemoveFromCache(IPreviewableWrapper previewableWrapper)
		{
			return RemoveFromCache(previewableWrapper.Key);
		}

		public static bool RemoveFromCache(PreviewableKey key)
		{
			return Instance().cachedPreviews.Remove(key);
		}

		/// <summary>
		/// Get IPreviewables of Editor of GameObject or asset
		/// </summary>
		public static void GetPreviews(Editor gameObjectOrAssetEditor, Object[] targets, ref List<IPreviewableWrapper> results)
		{
			for(int i = targets.Length - 1; i >= 0; i--)
			{
				if(targets[i] == null)
				{
					#if DEV_MODE
					UnityEngine.Debug.LogWarning($"Previews.GetPreviews called with targets[{i}] null.");
					#endif
					return;
				}
			}

			Instance().GetPreviewsInternal(gameObjectOrAssetEditor, targets, ref results);
		}

		/// <inheritdoc cref="GetPreviews(Editor, Object[], ref List&lt;IPreviewableWrapper&gt;)"/>
		private void GetPreviewsInternal(Editor gameObjectOrAssetEditor, Object[] targets, ref List<IPreviewableWrapper> results)
		{
			results.Add(new EditorWrapper(gameObjectOrAssetEditor, targets));

			var targetType = targets[0].GetType();

			List<Type> previewableTypes;
			if(!previewablesByTarget.TryGetValue(targetType, out previewableTypes))
			{
				return;
			}
			
			for(int n = previewableTypes.Count - 1; n >= 0; n--)
			{
				var previewableType = previewableTypes[n];
				var key = new PreviewableKey(previewableType, targets);
				IPreviewableWrapper cachedWrapper;
				if(cachedPreviews.TryGetValue(key, out cachedWrapper))
				{
					cachedWrapper.OnBecameActive(targets);
					results.Add(cachedWrapper);
				}
				else
				{
					results.Add(CreatePreviewableWrapper(previewableType, targets, key));
				}
			}
			
			#if DEV_MODE && DEBUG_GET_PREVIEWS
			UnityEngine.Debug.Log(StringUtils.ToColorizedString("GetPreviews(editor=", gameObjectOrAssetEditor.GetType(), ", target=", editorTargetType, ", results=", results, ")"));
			#endif
		}

		private static ObjectPreviewWrapper CreatePreviewableWrapper([NotNull]Type previewableType, Object[] targets, PreviewableKey key)
		{
			var previewable = Activator.CreateInstance(previewableType) as ObjectPreview;
			return new ObjectPreviewWrapper(previewable, targets, key);
		}
		
		/// <summary>
		/// Handles disposing the previewable. If previewable is not found in cache, or has invalid state, destroys it, otherwise just sets it null.
		/// </summary>
		/// <param name="previewable"> [in,out] The IPreviewableWrapper to Dispose. This should not be null when the method is called. </param>
		public static void Dispose(ref IPreviewableWrapper previewable)
		{
			Instance().DisposeInternal(ref previewable);
		}

		/// <inheritdoc cref="Dispose(ref IPreviewableWrapper)"/>
		private void DisposeInternal(ref IPreviewableWrapper previewable)
		{
			#if DEV_MODE && DEBUG_DISPOSE
			UnityEngine.Debug.Log("Dispose called for IPreviewableWrapper "+StringUtils.TypeToString(previewable)+" of "+StringUtils.TypeToString(previewable.target)+" ("+ StringUtils.ToString(editor.target)+")");
			#endif
		
			#if DEV_MODE
			UnityEngine.Debug.Assert(previewable != null, "Dispose called for null editor where ReferenceEquals(editor, null)="+ReferenceEquals(previewable, null));
			#endif

			var key = previewable.Key;
			if(IsCached(key))
			{
				if(!Platform.IsCompiling && Validate(previewable))
				{
					#if DEV_MODE && DEBUG_DISPOSE
					UnityEngine.Debug.Log("Dispose - Keeping cached IPreviewableWrapper "+StringUtils.TypeToString(editor)+" of "+StringUtils.TypeToString(editor.target)+" ("+ StringUtils.ToString(editor.target)+")");
					#endif
					previewable = null;
					return;
				}

				#if DEV_MODE && DEBUG_DISPOSE
				UnityEngine.Debug.Log("Dispose - Removing cached IPreviewableWrapper "+StringUtils.TypeToString(editor)+" of "+StringUtils.TypeToString(editor.target)+" ("+ StringUtils.ToString(editor.target)+")");
				#endif

				RemoveFromCache(key);
			}

			#if DEV_MODE && DEBUG_DISPOSE
			UnityEngine.Debug.Log("Dispose - Disposing IPreviewableWrapper "+previewable);
			#endif

			previewable.Dispose();
			previewable = null;
		}

		public static void CleanUp()
		{
			Instance().CleanUpInternal();
		}

		private void CleanUpInternal()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(cachedPreviewsCleaned != null);
			UnityEngine.Debug.Assert(cachedPreviews != null);
			UnityEngine.Debug.Assert(cachedPreviewsCleaned != cachedPreviews);
			#endif

			foreach(var cached in cachedPreviews)
			{
				var editor = cached.Value;
				if(editor == null)
				{
					#if DEV_MODE && DEBUG_CLEAN_UP
					UnityEngine.Debug.LogWarning("Previews - removing null IPreviewableWrapper");
					#endif
					continue;
				}

				if(!editor.StateIsValid)
				{
					#if DEV_MODE && DEBUG_CLEAN_UP
					UnityEngine.Debug.LogWarning("Previews - removing IPreviewableWrapper "+editor.GetType().Name+" with null targets");
					#endif
					cachedPreviewsToDispose.Add(cached);
					continue;
				}

				#if DEV_MODE && DEBUG_CLEAN_UP
				UnityEngine.Debug.Log("Previews - keeping IPreviewableWrapper "+editor.GetType().Name+".");
				#endif

				cachedPreviewsCleaned.Add(cached.Key, editor);
			}
			
			var swap = cachedPreviews;
			cachedPreviews = cachedPreviewsCleaned;
			cachedPreviewsCleaned = swap;
			cachedPreviewsCleaned.Clear();

			for(int n = cachedPreviewsToDispose.Count - 1; n >= 0; n--)
			{
				var dispose = cachedPreviewsToDispose[n];
				var editor = dispose.Value;
				DisposeInternal(ref editor);
			}
			cachedPreviewsToDispose.Clear();

			#if DEV_MODE
			UnityEngine.Debug.Assert(cachedPreviewsCleaned != cachedPreviews);
			#endif
		}

		/// <summary> Checks that the given editor has no null target objects. </summary>
		/// <param name="editor"> The editor to check. This cannot be null. </param>
		/// <returns> True if editor is valid, false if has null targets. </returns>
		public static bool Validate([NotNull]IPreviewableWrapper previewable)
		{
			return previewable.StateIsValid;
		}

		/// <summary> Disposes editor if it contains null targets. </summary>
		/// <param name="editor"> [in,out] The editor to check. </param>
		/// <returns> True if editor was disposed, false if not. </returns>
		public static bool DisposeIfInvalid([NotNull]ref IPreviewableWrapper editor)
		{
			if(!editor.StateIsValid)
			{
				Dispose(ref editor);
				return true;
			}
			return false;
		}
	}
}