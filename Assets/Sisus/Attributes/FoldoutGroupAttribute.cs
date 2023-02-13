using System;
using UnityEngine;

namespace Sisus.Attributes
{
	/// <summary>
	/// Causes target member to be drawn inside a custom foldout group.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public class FoldoutGroupAttribute : Attribute, ITargetableAttribute, IGroupAttribute
	{
		/// <summary> Prefix label for group foldout. </summary>
		public readonly GUIContent label;

		/// <inheritdoc/>
		public GUIContent Label
		{
			get
			{
				return label;
			}
		}

		/// <inheritdoc/>
		public Type GetDrawerType(IDrawerByNameProvider drawerByNameProvider)
		{
			return drawerByNameProvider.GetFieldDrawerTypeByName("CustomDataSetDrawer");
		}

		/// <inheritdoc/>
		public Target Target
		{
			get
			{
				return Target.This;
			}
		}

		/// <summary> Causes target member to be drawn inside a custom foldout group. </summary>
		/// <param name="groupLabel"> Prefix label for group foldout. </param>
		public FoldoutGroupAttribute(string groupLabel)
		{
			label = new GUIContent(groupLabel);
		}

		/// <summary> Causes target member to be drawn inside a custom foldout group. </summary>
		/// <param name="groupLabel"> Prefix label for group foldout. </param>
		/// <param name="tooltip"> Tooltip for prefix label. </param>
		public FoldoutGroupAttribute(string groupLabel, string tooltip)
		{
			label = new GUIContent(groupLabel, tooltip);
		}
	}
}