#if UNITY_2023_1_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

namespace Sisus
{
	internal readonly struct MonoEditorTypeStorage
	{
		public readonly MonoEditorType[] customEditors;

		public readonly MonoEditorType[] customEditorsMultiEdition;

		public MonoEditorTypeStorage(MonoEditorType[] customEditors, MonoEditorType[] customEditorsMultiEdition)
		{
			this.customEditors = customEditors;
			this.customEditorsMultiEdition = customEditorsMultiEdition;
		}
	}

	internal readonly struct MonoEditorType
	{
		internal static readonly FieldInfo inspectorTypeField;
		internal static readonly FieldInfo supportedRenderPipelineTypesField;
		internal static readonly FieldInfo editorForChildClassesField;
		internal static readonly FieldInfo isFallbackField;

		private static Type internalMonoEditorType => CustomEditorUtility.GetInternalEditorType("UnityEditor.CustomEditorAttributes").GetNestedType("MonoEditorType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		internal readonly Type inspectorType;
		internal readonly Type[] supportedRenderPipelineTypes;
		internal readonly bool editorForChildClasses;
		internal readonly bool isFallback;

		static MonoEditorType()
		{
			inspectorTypeField = internalMonoEditorType.GetField("inspectorType");
			supportedRenderPipelineTypesField = internalMonoEditorType.GetField("supportedRenderPipelineTypes");
			editorForChildClassesField = internalMonoEditorType.GetField("editorForChildClasses");
			isFallbackField = internalMonoEditorType.GetField("isFallback");

			#if DEV_MODE
			Debug.Assert(inspectorTypeField != null, nameof(inspectorTypeField));
			Debug.Assert(supportedRenderPipelineTypesField != null, nameof(supportedRenderPipelineTypesField));
			Debug.Assert(editorForChildClassesField != null, nameof(editorForChildClassesField));
			Debug.Assert(isFallbackField != null, nameof(isFallbackField));
			#endif
		}

		public MonoEditorType(Type inspectorType, Type[] supportedRenderPipelineTypes, bool editorForChildClasses, bool isFallback)
		{
			this.inspectorType = inspectorType;
			this.supportedRenderPipelineTypes = supportedRenderPipelineTypes;
			this.editorForChildClasses = editorForChildClasses;
			this.isFallback = isFallback;

			#if DEV_MODE
			Debug.Assert(typeof(Editor).IsAssignableFrom(inspectorType), inspectorType.Name);
			#endif
		}

		public MonoEditorType(object obj)
		{
			inspectorType = inspectorTypeField.GetValue(obj) as Type;
			supportedRenderPipelineTypes = (Type[])supportedRenderPipelineTypesField.GetValue(obj) ?? Type.EmptyTypes;
			editorForChildClasses = (bool)editorForChildClassesField.GetValue(obj);
			isFallback = (bool)isFallbackField.GetValue(obj);

			#if DEV_MODE
			//Debug.Assert(typeof(Object).IsAssignableFrom(inspectedType), inspectedType.Name); // Fails for some classes when Odin Inspector is installed
			//Debug.Assert(!typeof(Editor).IsAssignableFrom(inspectedType), inspectedType.Name); // Fails for internal AssetStoreAssetInspector
			Debug.Assert(typeof(Editor).IsAssignableFrom(inspectorType), inspectorType.Name);
			#endif
		}

		public static MonoEditorType[] Create(IList internalList) // List<MonoEditorType>
		{ 
			int count = internalList.Count;
			var result = new MonoEditorType[count];
			for(int i = 0; i < count; i++)
			{
				result[i] = new MonoEditorType(internalList[i]);
			}

			return result;
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

			inspectorTypeField.SetValue(instance, inspectorType);
			supportedRenderPipelineTypesField.SetValue(instance, supportedRenderPipelineTypes);
			editorForChildClassesField.SetValue(instance, editorForChildClasses);
			isFallbackField.SetValue(instance, isFallback);

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