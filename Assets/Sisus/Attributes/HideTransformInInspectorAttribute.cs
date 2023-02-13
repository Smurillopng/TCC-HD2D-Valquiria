using System;
using UnityEngine;

namespace Sisus.Attributes
{
	/// <summary>
	/// When added before a component class causes the transform component to be hidden
	/// in the inspector view for any GameObject that contains the component with this attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class HideTransformInInspectorAttribute : Attribute, IComponentModifiedCallbackReceiver<Transform>
	{
		/// <inheritdoc/>
		public void OnComponentAdded(Component attributeHolder, Transform addedComponent)
		{
			addedComponent.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
			addedComponent.localPosition = Vector3.zero;
			addedComponent.localEulerAngles = Vector3.zero;
			addedComponent.localScale = Vector3.one;
		}

		/// <inheritdoc/>
		public void OnComponentModified(Component attributeHolder, Transform modifiedComponent)
		{
			if(modifiedComponent.localPosition != Vector3.zero || modifiedComponent.localEulerAngles != Vector3.zero || modifiedComponent.localScale != Vector3.one || modifiedComponent.hideFlags != (HideFlags.HideInInspector | HideFlags.NotEditable))
			{
				#if UNITY_EDITOR
				UnityEditor.EditorGUIUtility.editingTextField = false;
				#endif

				Debug.LogWarning(attributeHolder.GetType().Name + " requires that " + modifiedComponent.GetType().Name + " remains hidden and at default state.");

				modifiedComponent.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
				modifiedComponent.localPosition = Vector3.zero;
				modifiedComponent.localEulerAngles = Vector3.zero;
				modifiedComponent.localScale = Vector3.one;
			}
		}
	}
}