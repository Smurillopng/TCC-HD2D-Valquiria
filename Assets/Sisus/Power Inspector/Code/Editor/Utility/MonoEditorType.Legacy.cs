#if !UNITY_2023_1_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	internal readonly struct MonoEditorType
	{
		internal static readonly FieldInfo m_InspectedTypeField;
		internal static readonly FieldInfo m_InspectorTypeField;
		internal static readonly FieldInfo m_RenderPipelineTypeField;
		internal static readonly FieldInfo m_EditorForChildClassesField;
		internal static readonly FieldInfo m_IsFallbackField;

		private static Type internalMonoEditorType => CustomEditorUtility.GetInternalEditorType("UnityEditor.CustomEditorAttributes").GetNestedType("MonoEditorType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		internal readonly Type inspectedType;
		internal readonly Type inspectorType;
		internal readonly Type renderPipelineType;
		internal readonly bool editorForChildClasses;
		internal readonly bool isFallback;

		static MonoEditorType()
		{
			m_InspectedTypeField = internalMonoEditorType.GetField("m_InspectedType");
			m_InspectorTypeField = internalMonoEditorType.GetField("m_InspectorType");
			m_RenderPipelineTypeField = internalMonoEditorType.GetField("m_RenderPipelineType");
			m_EditorForChildClassesField = internalMonoEditorType.GetField("m_EditorForChildClasses");
			m_IsFallbackField = internalMonoEditorType.GetField("m_IsFallback");

			#if DEV_MODE
			Debug.Assert(m_InspectedTypeField != null, nameof(m_InspectedTypeField));
			Debug.Assert(m_InspectorTypeField != null, nameof(m_InspectorTypeField));
			Debug.Assert(m_RenderPipelineTypeField != null, nameof(m_RenderPipelineTypeField));
			Debug.Assert(m_EditorForChildClassesField != null, nameof(m_EditorForChildClassesField));
			Debug.Assert(m_IsFallbackField != null, nameof(m_IsFallbackField));
			#endif
		}

		public MonoEditorType(Type inspectedType, Type inspectorType, Type renderPipelineType, bool editorForChildClasses, bool isFallback)
		{
			this.inspectedType = inspectedType;
			this.inspectorType = inspectorType;
			this.renderPipelineType = renderPipelineType;
			this.editorForChildClasses = editorForChildClasses;
			this.isFallback = isFallback;

			#if DEV_MODE
			Debug.Assert(typeof(Object).IsAssignableFrom(inspectedType), inspectedType.Name);
			Debug.Assert(!typeof(Editor).IsAssignableFrom(inspectedType), inspectedType.Name);
			Debug.Assert(typeof(Editor).IsAssignableFrom(inspectorType), inspectorType.Name);
			#endif
		}

		public MonoEditorType(object obj)
		{
			inspectedType = m_InspectedTypeField.GetValue(obj) as Type;
			inspectorType = m_InspectorTypeField.GetValue(obj) as Type;
			renderPipelineType = m_RenderPipelineTypeField.GetValue(obj) as Type;
			editorForChildClasses = (bool)m_EditorForChildClassesField.GetValue(obj);
			isFallback = (bool)m_IsFallbackField.GetValue(obj);

			#if DEV_MODE
			//Debug.Assert(typeof(Object).IsAssignableFrom(inspectedType), inspectedType.Name); // Fails for some classes when Odin Inspector is installed
			//Debug.Assert(!typeof(Editor).IsAssignableFrom(inspectedType), inspectedType.Name); // Fails for internal AssetStoreAssetInspector
			Debug.Assert(typeof(Editor).IsAssignableFrom(inspectorType), inspectorType.Name);
			#endif
		}

		public object ToInternalType()
		{
			object instance;
			try
			{
				instance = Activator.CreateInstance(internalMonoEditorType);
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogError(e);
			#else
			catch
			{
			#endif
				instance = FormatterServices.GetUninitializedObject(internalMonoEditorType);
			}

			m_InspectedTypeField.SetValue(instance, inspectedType);
			m_InspectorTypeField.SetValue(instance, inspectorType);
			m_RenderPipelineTypeField.SetValue(instance, renderPipelineType);
			m_EditorForChildClassesField.SetValue(instance, editorForChildClasses);
			m_IsFallbackField.SetValue(instance, isFallback);

			return instance;
		}

		public IList ToInternalTypeList()
		{
			var listType = typeof(List<>).MakeGenericType(internalMonoEditorType);
			var list = Activator.CreateInstance(listType) as IList;
			list.Add(ToInternalType());
			return list;
		}
	}
}
#endif