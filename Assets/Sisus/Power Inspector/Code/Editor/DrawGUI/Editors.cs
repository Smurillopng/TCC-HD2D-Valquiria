//#define ENABLE_PI_IN_DEFAULT_INSPECTOR
#define SUPPORT_EDITORS_FOR_INTERFACES // the default inspector doesn't support this but we can

#define DESTROY_DISPOSED_EDITORS
//#define SKIP_DESTROY_GAME_OBJECT_INSPECTOR
#define DESTROY_DISPOSED_EDITOR_SERIALIZED_OBJECTS
//#define NEVER_CACHE_ANY_EDITORS

//#define DEBUG_GET_EDITOR
//#define DEBUG_DESTROYED_CACHED_EDITORS
//#define DEBUG_DISPOSE
//#define DEBUG_CLEAR
//#define DEBUG_CLEAN_UP

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

#if UNITY_2020_2_OR_NEWER
using AssetImporterEditor = UnityEditor.AssetImporters.AssetImporterEditor;
#elif UNITY_2017_2_OR_NEWER
using AssetImporterEditor = UnityEditor.Experimental.AssetImporters.AssetImporterEditor;
#endif


#if CSHARP_7_3_OR_NEWER
using Sisus.Vexe.FastReflection;
#endif

namespace Sisus
{
	/// <summary>
	/// Class for creating and caching Editors for targets.
	/// Instance and cached will persists through assembly reloads.
	/// </summary>
	[Serializable]
	public class Editors : ISerializationCallbackReceiver
	{
		#if SKIP_DESTROY_GAME_OBJECT_INSPECTOR
		private static readonly Type PrefabImporterEditorType;
		#endif
		public static readonly Type GameObjectInspectorType;

		#if CSHARP_7_3_OR_NEWER
		[NonSerialized]
		private static readonly MemberGetter<Editor, Object[]> getTargets;
		[NonSerialized]
		private static readonly MemberGetter<Editor, Object> getContext;
		#else
		private static readonly FieldInfo getTargets;
		private static readonly FieldInfo getContext;
		#endif

		[NonSerialized]
		public Dictionary<EditorKey, Editor> cachedEditors = new Dictionary<EditorKey, Editor>(); //temp public for testing

		[NonSerialized]
		private Dictionary<EditorKey, Editor> cachedEditorsCleaned = new Dictionary<EditorKey, Editor>();

		[NonSerialized]
		private List<KeyValuePair<EditorKey, Editor>> cachedEditorsToDispose = new List<KeyValuePair<EditorKey, Editor>>();

		[SerializeField]
		private Editor[] editorsSerialized = null;

		static Editors()
		{
			#if CSHARP_7_3_OR_NEWER
			getTargets = Types.Editor.GetField("m_Targets", BindingFlags.Instance | BindingFlags.NonPublic).DelegateForGet<Editor, Object[]>();
			getContext = Types.Editor.GetField("m_Context", BindingFlags.Instance | BindingFlags.NonPublic).DelegateForGet<Editor, Object>();
			#else
			getTargets = Types.Editor.GetField("m_Targets", BindingFlags.Instance | BindingFlags.NonPublic);
			getContext = Types.Editor.GetField("m_Context", BindingFlags.Instance | BindingFlags.NonPublic);
			#endif

			#if SKIP_DESTROY_GAME_OBJECT_INSPECTOR
			PrefabImporterEditorType = Types.GetInternalEditorType("UnityEditor.PrefabImporterEditor");
			#endif
			GameObjectInspectorType = Types.GetInternalEditorType("UnityEditor.GameObjectInspector");
		}

		[CanBeNull]
		private static Editors Instance()
		{
			return InspectorUtility.ActiveInspectorDrawer == null ? null : InspectorUtility.ActiveInspectorDrawer.Editors;
		}

		private static EditorKey GetKey(Editor editor)
		{
			#if UNITY_2017_2_OR_NEWER
			return new EditorKey(editor.targets, editor is AssetImporterEditor);
			#else
			return new EditorKey(editor.targets, false);
			#endif
		}

		private bool RemoveFromCache(EditorKey key)
		{
			return cachedEditors.Remove(key);
		}

		/// <summary> Gets an Editor for targets. </summary>
		/// <param name="editor"> [in,out] This will be updated to contain the Editor. </param>
		/// <param name="targets"> The targets for the editor. </param>
		/// <param name="editorType"> (Optional) Type of the editor. </param>
		/// <param name="context"> (Optional) SerializedObject will be created using this if not null. </param>
		/// <param name="cache">
		/// (Optional) True if should cache Editor for later reuse. If false existing editor instance will be Disposed if a new one is created.
		/// </param>
		public static void GetEditor(ref Editor editor, Object[] targets, Type editorType = null, Object context = null, bool cache = true)
		{
			GetEditor(ref editor, targets, editorType, targets.AllSameType(), context, cache);
		}

		/// <summary> Gets an Editor for targets. </summary>
		/// <param name="editor"> [in,out] This will be updated to contain the Editor. </param>
		/// <param name="targets"> The targets for the editor. </param>
		/// <param name="editorType"> (Optional) Type of the editor. </param>
		/// <param name="allTargetsHaveSameType">
		/// True if all targets are of the same type. If false, Editor will be created using only the first target.
		/// </param>
		/// <param name="context"> (Optional) SerializedObject will be created using this if not null. </param>
		/// <param name="cache">
		/// (Optional) True if should cache Editor for later reuse. If false existing editor instance will be Disposed if a new one is created.
		/// </param>
		public static void GetEditor(ref Editor editor, [NotNull]Object[] targets, [CanBeNull]Type editorType, bool allTargetsHaveSameType, Object context = null, bool cache = true)
		{
			var instance = Instance();
			if(instance == null)
			{
				#if DEV_MODE
				Debug.LogWarning("Editors.GetEditor("+StringUtils.ToString(targets)+") called with instance null.");
				#endif

				if(!allTargetsHaveSameType)
				{
					Editor.CreateCachedEditor(targets[0], editorType, ref editor);
					return;
				}
				Editor.CreateCachedEditor(targets, editorType, ref editor);
				return;
			}

			instance.GetEditorInternal(ref editor, targets, editorType, allTargetsHaveSameType, context, cache);
		}

		/// <inheritdoc cref="GetEditor(ref Editor, Object[], Type, bool, Object, bool)"/>
		public void GetEditorInternal(ref Editor editor, [NotNull]Object[] targets, [CanBeNull]Type editorType, bool allTargetsHaveSameType, Object context = null, bool cache = true)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(targets != null);
			Debug.Assert(!targets.ContainsNullObjects(), "Editors.GetEditorInternal called with targets containing null Objects");
			Debug.Assert(allTargetsHaveSameType == targets.AllSameType());
			Debug.Assert(targets.Length > 0, "GetEditor called with empty targets array! editorType was "+(editorType == null ? "null" : editorType.Name));
			#endif

			if(editor != null)
			{
				#if CSHARP_7_3_OR_NEWER
				var previousEditorTargets = getTargets(editor);
				var previousEditorContext = getContext(editor);
				#else
				var previousEditorTargets = getTargets.GetValue(editor) as Object[];
				var previousEditorContext = getContext.GetValue(editor) as Object;
				#endif
				if(targets.ContentsMatch(previousEditorTargets) && context == previousEditorContext)
				{
					return;
				}

				DisposeInternal(ref editor);
			}

			if(!allTargetsHaveSameType)
			{
				GetEditor(ref editor, targets[0], editorType, context, cache);
				return;
			}

			#if UNITY_2017_2_OR_NEWER
			bool isAssetImporterEditor = editorType != null ? Types.AssetImporterEditor.IsAssignableFrom(editorType) : typeof(AssetImporter).IsAssignableFrom(targets[0].GetType());
			var editorKey = new EditorKey(targets, isAssetImporterEditor);
			#else
			var editorKey = new EditorKey(targets, false);
			#endif

			if(cachedEditors.TryGetValue(editorKey, out editor))
			{
				if(editor != null)
				{
					if(!DisposeIfInvalid(ref editor))
					{
						OnBecameActive(editor);

						#if DEV_MODE && DEBUG_GET_EDITOR
						Debug.Log("Editors.GetEditor: for targets " + StringUtils.TypesToString(targets) + " and editorType "+StringUtils.ToString(editorType)+" returning cached: "+editor.GetType().Name+" with key="+editorKey.GetHashCode());
						#endif
						return;
					}
					#if DEV_MODE && DEBUG_DESTROYED_CACHED_EDITORS
					Debug.LogWarning("cachedEditors for targets "+StringUtils.TypeToString(targets)+ " and editorType " + StringUtils.ToString(editorType)+ " with EditorKey hashCode " + editorKey.GetHashCode()+ " contained editor with null targets!\nCachedEditors:\n" + StringUtils.ToString(cachedEditors, "\n"));
					#endif
				}
				#if DEV_MODE && DEBUG_DESTROYED_CACHED_EDITORS
				else { Debug.LogWarning("cachedEditors for targets "+StringUtils.TypesToString(targets) +" and editorType "+StringUtils.ToString(editorType)+ " with EditorKey hashCode " + editorKey.GetHashCode() + " contained a null value!\nCachedEditors:\n" + StringUtils.ToString(cachedEditors, "\n")); }
				#endif
			}

			#if DEV_MODE && DEBUG_GET_EDITOR
			Debug.Log(StringUtils.ToColorizedString("Editors.GetEditor called for ", StringUtils.ToString(targets), " with editorType=", editorType, ", context=", context, ", key=", editorKey.GetHashCode(), ", cache=", cache));
			#endif

			
			if(editorType == null)
			{
				var target = targets[0];

				#if SUPPORT_EDITORS_FOR_INTERFACES
				var interfaces = target.GetType().GetInterfaces();
				for(int n = interfaces.Length - 1; n >= 0; n--)
				{
					Type editorForInterface;
					if(CustomEditorUtility.TryGetCustomEditorType(interfaces[n], out editorForInterface))
					{
						editorType = editorForInterface;
						#if DEV_MODE
						Debug.Log("Editors.GetEditor : Replaced null editorType with interface based type " + StringUtils.ToString(editorType));
						#endif
					}
				}
				#endif

				#if DEV_MODE && ENABLE_PI_IN_DEFAULT_INSPECTOR
				if(editorType == null)
				{
					Type ignoredEditor;
					if(target is GameObject)
					{
						if(!CustomEditorUtility.TryGetCustomEditorType(target.GetType(), out ignoredEditor) || ignoredEditor == typeof(PIGameObjectEditor))
						{
							#if DEV_MODE
							Debug.Log("Replacing PIGameObjectEditor with "+ GameObjectInspectorType.Name+" for GameObject " + target.name);
							#endif
							editorType = GameObjectInspectorType;
						}
					}
					else if(target is Component && (!CustomEditorUtility.TryGetCustomEditorType(target.GetType(), out ignoredEditor) || ignoredEditor == typeof(PIComponentEditor)))
					{
						#if DEV_MODE
						Debug.Log("Replacing PIComponentEditor with GenericInspector for component " + target.GetType().Name);
						#endif
						editorType = Types.GetInternalEditorType("UnityEditor.GenericInspector");
					}
				}
				#endif
			}

			try
			{
				editor = Editor.CreateEditorWithContext(targets, context, editorType);
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogError("Editor.CreateEditor for targets " + StringUtils.TypesToString(targets) + " and editorType "+StringUtils.ToString(editorType)+": "+e);
			#else
			catch
			{
			#endif
				return;
			}

			if(editor == null)
			{
				#if DEV_MODE
				Debug.LogWarning("Editor.CreateEditor for targets " + StringUtils.TypesToString(targets) + " and editorType "+StringUtils.ToString(editorType)+" returned null!");
				#endif
				return;
			}
			
			#if DEV_MODE && DEBUG_GET_EDITOR
			Debug.Log("Editors.GetEditor: Created new: "+editor.GetType().Name+" for "+StringUtils.ToString(targets)+" with key="+editorKey.GetHashCode()+", cache="+StringUtils.ToColorizedString(cache));
			#endif

			if(cache)
			{
				cachedEditors[editorKey] = editor;
			}
		}

		public static void OnBecameActive([NotNull]Editor editor)
		{
			editor.ReloadPreviewInstances();
		}

		/// <summary> Gets an Editor for target. </summary>
		/// <param name="editor"> [in,out] This will be updated to contain the Editor. </param>
		/// <param name="target"> The target for the editor. </param>
		/// <param name="editorType"> (Optional) Type of the editor. </param>
		/// <param name="context"> (Optional) SerializedObject will be created using this if not null. </param>
		/// <param name="cache">
		/// (Optional) True if should cache Editor for later reuse. If false existing editor instance will be Disposed if a new one is created.
		/// </param>
		public static void GetEditor(ref Editor editor, [NotNull]Object target, Type editorType = null, Object context = null, bool cache = true)
		{
			var instance = Instance();
			if(instance == null)
			{
				#if DEV_MODE
				Debug.LogWarning("Editors.GetEditor("+StringUtils.ToString(target)+") called with instance null.");
				#endif

				Editor.CreateCachedEditor(target, editorType, ref editor);
				return;
			}

			instance.GetEditorInternal(ref editor, target, editorType, context, cache);
		}

		/// <inheritdoc cref="GetEditor(ref Editor, Object, Type, Object, bool)"/>
		public void GetEditorInternal(ref Editor editor, [NotNull]Object target, Type editorType = null, Object context = null, bool cache = true)
		{
			if(editor != null)
			{
				#if CSHARP_7_3_OR_NEWER
				var editorTargets = getTargets(editor);
				if(editorTargets.Length == 1 && editorTargets[0] == target && context == getContext(editor))
				#else
				var editorTargets = getTargets.GetValue(editor) as Object[];
				if(editorTargets.Length == 1 && editorTargets[0] == target && context == getContext.GetValue(editor) as Object)
				#endif
				{
					return;
				}

				DisposeInternal(ref editor);
			}
			
			#if UNITY_2017_2_OR_NEWER
			bool isAssetImporterEditor = editorType != null && Types.AssetImporterEditor.IsAssignableFrom(editorType);
			var editorKey = new EditorKey(target, isAssetImporterEditor);
			#else
			var editorKey = new EditorKey(target, false);
			#endif
			
			if(cachedEditors.TryGetValue(editorKey, out editor))
			{
				if(editor != null)
				{
					if(!DisposeIfInvalid(ref editor))
					{
						OnBecameActive(editor);

						#if DEV_MODE && DEBUG_GET_EDITOR
						Debug.Log("Editors.GetEditor: returning cached: "+editor.GetType().Name+" for "+StringUtils.ToString(target)+" with key="+editorKey.GetHashCode());
						#endif
						return;
					}
					#if DEV_MODE && DEBUG_DESTROYED_CACHED_EDITORS
					Debug.LogWarning("cachedEditors for target "+StringUtils.TypeToString(target)+ " and editorType " + StringUtils.ToString(editorType)+ " with EditorKey hashCode " + editorKey.GetHashCode()+ " contained editor with null targets!\nCachedEditors:\n" + StringUtils.ToString(cachedEditors, "\n"));
					#endif
				}
				#if DEV_MODE && DEBUG_DESTROYED_CACHED_EDITORS
				else { Debug.LogWarning("cachedEditors for target "+StringUtils.TypeToString(target)+ " and editorType " + StringUtils.ToString(editorType)+ " with EditorKey hashCode " + editorKey.GetHashCode()+ " contained a null value!\nCachedEditors:\n" + StringUtils.ToString(cachedEditors, "\n")); }
				#endif
			}

			if(editorType == null)
			{
				#if SUPPORT_EDITORS_FOR_INTERFACES
				var interfaces = target.GetType().GetInterfaces();
				for(int n = interfaces.Length - 1; n >= 0; n--)
				{
					Type editorForInterface;
					if(CustomEditorUtility.TryGetCustomEditorType(interfaces[n], out editorForInterface))
					{
						editorType = editorForInterface;
						#if DEV_MODE
						Debug.Log("Editors.GetEditor : Replaced null editorType with interface based type " + StringUtils.ToString(editorType));
						#endif
					}
				}
				#endif

				#if DEV_MODE && ENABLE_PI_IN_DEFAULT_INSPECTOR
				if(editorType == null)
				{
					Type ignoredEditor;
					if(target is GameObject)
					{
						if(!CustomEditorUtility.TryGetCustomEditorType(target.GetType(), out ignoredEditor) || ignoredEditor == typeof(PIGameObjectEditor))
						{
							editorType = GameObjectInspectorType;
						}
					}
					else if(target is Component && (!CustomEditorUtility.TryGetCustomEditorType(target.GetType(), out ignoredEditor) || ignoredEditor == typeof(PIComponentEditor)))
					{
						#if DEV_MODE
						Debug.Log("Using generic inspector for component " + target.GetType().Name);
						#endif
						editorType = Types.GetInternalEditorType("UnityEditor.GenericInspector");
					}
				}
				#endif
			}

			editor = Editor.CreateEditorWithContext(ArrayPool<Object>.CreateWithContent(target), context, editorType);

			if(editor == null)
			{
				#if DEV_MODE
				Debug.LogWarning("Editor.CreateEditor for target " + StringUtils.TypeToString(target) + " and editorType "+StringUtils.ToString(editorType)+" returned null!");
				#endif
				return;
			}

			#if DEV_MODE && DEBUG_GET_EDITOR
			Debug.Log("Editors.GetEditor: Created new: "+editor.GetType().Name+" for "+StringUtils.ToString(target)+" with key="+editorKey.GetHashCode());
			#endif
			
			if(cache)
			{
				cachedEditors[editorKey] = editor;
			}
		}

		/// <summary>
		/// Disposes the SerializedObject of the Editor and destroys the Editor.
		/// </summary>
		/// <param name="editor"> [in,out] The editor to Dispose. This should not be null when the method is called. </param>
		/// <param name="forceRemoveFromCache"> If true the editor will be removed from cache even if it seems to have a valid state. </param>
		public static void Dispose(ref Editor editor, bool forceRemoveFromCache = false)
		{
			var instance = Instance();

			if(instance == null)
			{
				DisposeStatic(ref editor);
				return;
			}

			var editorKey = GetKey(editor);

			if(forceRemoveFromCache)
			{
				instance.RemoveFromCache(editorKey);

				// Handle nested Editors.
				foreach(var editorTarget in editor.targets)
				{
					var nestedEditor = editorTarget as Editor;
					if(!ReferenceEquals(nestedEditor, null))
					{
						#if DEV_MODE && DEBUG_DISPOSE
						Debug.Log("Editors.Dispose - <color=red>Removing from cache</color> nestedEditor "+StringUtils.TypeToString(editor));
						#endif
						instance.RemoveFromCache(GetKey(nestedEditor));
					}
				}

				DisposeStatic(ref editor);

				return;
			}

			instance.Dispose(ref editor, editorKey);
		}

		/// <summary>
		/// Disposes the SerializedObject of the Editor and destroys the Editor.
		/// </summary>
		/// <param name="editor"> [in,out] The editor to Dispose. This should not be null when the method is called. </param>
		private void DisposeInternal(ref Editor editor)
		{
			Dispose(ref editor, GetKey(editor));
		}

		/// <summary>
		/// Disposes the SerializedObject of the Editor and destroys the Editor.
		/// </summary>
		/// <param name="editor"> [in,out] The editor to Dispose. This should not be null when the method is called. </param>
		/// <param name="key"> Dictionary key for the editor </param>
		private void Dispose(ref Editor editor, EditorKey key)
		{
			#if DEV_MODE
			Debug.Assert(!ReferenceEquals(editor, null), "Dispose called for null editor where ReferenceEquals(editor, "+StringUtils.Null+")="+StringUtils.True);
			#endif

			if(IsCached(key))
			{
				#if !NEVER_CACHE_ANY_EDITORS
				if(!EditorApplication.isCompiling && Validate(editor))
				{
					#if DEV_MODE && DEBUG_DISPOSE
					Debug.Log("Editors.Dispose - <color=green>Keeping</color> cached Editor "+StringUtils.TypeToString(editor)+" of "+StringUtils.TypeToString(editor.target)+" ("+ StringUtils.ToString(editor.target)+") with key="+key.GetHashCode());
					#endif
					editor = null;
					return;
				}
				#endif

				#if DEV_MODE && DEBUG_DISPOSE
				Debug.Log("Editors.Dispose - <color=red>Removing</color> cached Editor "+StringUtils.TypeToString(editor)+" of "+StringUtils.TypeToString(editor.target)+" ("+ StringUtils.ToString(editor.target)+") with key="+key.GetHashCode());
				#endif

				RemoveFromCache(key);
			}
			#if DEV_MODE && DEBUG_DISPOSE
			else { Debug.Log("Editors.Dispose - IsCached("+StringUtils.False+"): Editor "+StringUtils.TypeToString(editor)+" of "+StringUtils.TypeToString(editor.target)+" ("+ StringUtils.ToString(editor.target)+") with key="+key.GetHashCode()+"\ncachedEditors:\n"+StringUtils.ToString(cachedEditors, "\n")); }
			#endif
			
			DisposeStatic(ref editor);
		}

		private bool IsCached(EditorKey key)
		{
			return cachedEditors.ContainsKey(key);
		}

		/// <summary>
		/// Disposes the SerializedObject of the Editor and destroys the Editor.
		/// Won't create a new instance of Editors even if it's missing, and won't
		/// remove editor from cache of existing Editors instance.
		/// </summary>
		/// <param name="editor"> [in,out] The editor to Dispose. This should not be null when the method is called. </param>
		private static void DisposeStatic(ref Editor editor)
		{
			#if DEV_MODE
			Debug.Assert(!ReferenceEquals(editor, null), "Dispose called for null editor where ReferenceEquals(editor, "+StringUtils.Null+")="+StringUtils.True);
			#endif
			
			#if SKIP_DESTROY_GAME_OBJECT_INSPECTOR
			// Check that field m_PreviewCache of GameObjectInspector is not null. If it is, and Destroy is called for an Editor,
			// a NullReferenceException will get thrown. I'm guessing that the field goes null after assembly reloading, because
			// Unity can't serialize Dictionary fields.
			var editorType = editor.GetType();
			if(editorType == GameObjectInspectorType || editorType == PrefabImporterEditorType)
			{
				editor = null;
				return;
			}
			#endif

			#if DEV_MODE && DEBUG_DISPOSE
			Debug.Log("Editors.Dispose - <color=red>Destroying</color> Editor "+StringUtils.TypeToString(editor)+" of "+StringUtils.TypeToString(editor.target)+" ("+ StringUtils.ToString(editor.target)+")");
			#endif
			
			var target = editor.target;

			#if DESTROY_DISPOSED_EDITOR_SERIALIZED_OBJECTS
			SerializedObject serializedObject;
			try
			{
				serializedObject = target == null ? null : editor.serializedObject;
			}
			// ArgumentException: Object at index 0 is null can happen when we try to fetch
			// the SerializedObject of an editor target which no longer exists
			#if DEV_MODE
			catch(ArgumentException e)
			{
				Debug.LogError(e);
				serializedObject = null;
			}
			#else
			catch(ArgumentException)
			{
				serializedObject = null;
			}
			#endif

			#endif

			// ad-hoc fix for internal NullReferenceException from GameObjectInspector
			if(editor.GetType() == GameObjectInspectorType)
			{
				editor.OnPreviewSettings();
			}

			#if DESTROY_DISPOSED_EDITORS
			try
			{
				Object.DestroyImmediate(editor);
			}
			#if DEV_MODE
			catch(NullReferenceException e) // this has happened in rare cases somehow
			{
				Debug.LogError(e);
			#else
			catch(NullReferenceException) // this has happened in rare cases somehow
			{
			#endif
				editor = null;
				return;
			}
			#endif
			
			// Handle nested Editors.
			foreach(var editorTarget in editor.targets)
			{
				var nestedEditor = editorTarget as Editor;
				if(nestedEditor != null)
				{
					#if DEV_MODE && DEBUG_DISPOSE
					Debug.Log("Editors.Dispose - <color=red>Destroying</color> nestedEditor "+StringUtils.TypeToString(editor));
					#endif
					Dispose(ref nestedEditor, false);
				}
			}

			editor = null;

			#if DESTROY_DISPOSED_EDITOR_SERIALIZED_OBJECTS
			// Destroy the SerializedObject after the Editor because it's
			// possible that OnDisable / OnDestroy methods have references
			// to the serializedObject.
			if(serializedObject != null)
			{
				serializedObject.Dispose();
			}
			#endif
		}

		public void OnBeforeAssemblyReload()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(cachedEditors != null);
			#endif

			// dispose all gameobject drawers because they often throw internal exceptions after assembly reloads
			foreach(var editor in cachedEditors.Values)
			{
				if(editor == null)
				{
					#if DEV_MODE && DEBUG_CLEAR
					Debug.LogWarning("Editors.Clear - skipping null Editor");
					#endif
					continue;
				}

				if(editor.GetType() == GameObjectInspectorType)
				{
					//#if DEV_MODE && DEBUG_CLEAR
					//Debug.LogWarning("Editors.OnBeforeAssemblyReload - calling GameObjectInspector.OnPreviewSettings.");
					//#endif
					//editor.OnPreviewSettings(); // this helps make sure that no internal NullReferenceException is thrown?
					
					#if DEV_MODE && DEBUG_CLEAR
					Debug.LogWarning("Editors.OnBeforeAssemblyReload - disposing GameObjectEditor...");
					#endif
					var dispose = editor;
					DisposeStatic(ref dispose);
				}
			}
		}

		public void Clear()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(cachedEditors != null);
			#endif

			foreach(var editor in cachedEditors.Values)
			{
				if(editor == null)
				{
					#if DEV_MODE && DEBUG_CLEAR
					Debug.LogWarning("Editors.Clear - skipping null Editor");
					#endif
					continue;
				}
				var dispose = editor;
				#if DEV_MODE && DEBUG_CLEAR
				Debug.LogWarning("Editors.Clear - Disposing Editor "+editor.GetType().Name);
				#endif
				DisposeStatic(ref dispose);
			}

			cachedEditors.Clear();
		}

		public void CleanUp()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(cachedEditorsCleaned != null);
			Debug.Assert(cachedEditors != null);
			Debug.Assert(cachedEditorsCleaned != cachedEditors);
			#endif

			foreach(var cached in cachedEditors)
			{
				var editor = cached.Value;
				if(editor == null)
				{
					#if DEV_MODE && DEBUG_CLEAN_UP
					Debug.LogWarning("Editors - removing null Editor");
					#endif
					continue;
				}

				if(!Validate(editor))
				{
					cachedEditorsToDispose.Add(cached);
					continue;
				}

				#if DEV_MODE && DEBUG_CLEAN_UP
				Debug.Log("Editors - keeping Editor "+editor.GetType().Name+".");
				#endif

				cachedEditorsCleaned.Add(cached.Key, editor);
			}
			
			var swap = cachedEditors;
			cachedEditors = cachedEditorsCleaned;
			cachedEditorsCleaned = swap;
			cachedEditorsCleaned.Clear();

			for(int n = cachedEditorsToDispose.Count - 1; n >= 0; n--)
			{
				var dispose = cachedEditorsToDispose[n];
				var editor = dispose.Value;
				Dispose(ref editor, dispose.Key);
			}
			cachedEditorsToDispose.Clear();

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(cachedEditorsCleaned != cachedEditors);
			#endif
		}

		/// <summary> Checks that the given editor has no null target objects. </summary>
		/// <param name="editor"> The editor to check. This cannot be null. </param>
		/// <returns> True if editor is valid, false if has null targets. </returns>
		private static bool Validate([NotNull]Editor editor)
		{
			for(int n = editor.targets.Length - 1; n >= 0; n--)
			{
				var target = editor.targets[n];
				if(target == null)
				{
					return false;
				}

				// Handle nested Editors.
				var nestedEditor = target as Editor;
				if(nestedEditor != null)
				{
					if(nestedEditor.targets.ContainsNullObjects())
					{
						return false;
					}
				}
			}

			return true;
		}

		/// <summary> Disposes editor if it contains null targets. </summary>
		/// <param name="editor"> [in,out] The editor to check. </param>
		/// <returns> True if editor was disposed, false if not. </returns>
		public static bool DisposeIfInvalid([NotNull]ref Editor editor)
		{
			if(Validate(editor))
			{
				return false;
			}
			Dispose(ref editor, true);
			return true;
		}

		public void OnBeforeSerialize()
		{
			int count = cachedEditors.Count;
			editorsSerialized = new Editor[count];
			cachedEditors.Values.CopyTo(editorsSerialized, 0);
		}

		public void OnAfterDeserialize()
		{
			int count = editorsSerialized == null ? 0 : editorsSerialized.Length;
			cachedEditors = new Dictionary<EditorKey, Editor>(count);
			cachedEditorsCleaned = new Dictionary<EditorKey, Editor>(count);
			cachedEditorsToDispose = new List<KeyValuePair<EditorKey, Editor>>();

			if(count > 0)
			{
				EditorApplication.delayCall +=() => // editor.target can cause exception if not delayed?
				{
					for(int n = editorsSerialized == null ? -1 : editorsSerialized.Length - 1; n >= 0; n--)
					{
						var editor = editorsSerialized[n];
						if(editor != null && !DisposeIfInvalid(ref editor))
						{
							#if UNITY_2017_2_OR_NEWER
							var editorKey = new EditorKey(editor.targets, editor is AssetImporterEditor); 
							#else
							var editorKey = new EditorKey(targets, false);
							#endif

							cachedEditors[editorKey] = editor;
						}
					}
				};
			}
		}
	}
}
#endif