using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable, DrawerByExtension(".shadergraph", true)]
	public class ShaderGraphDrawer : CustomEditorAssetDrawer
	{
		/// <inheritdoc />
		public override bool RequiresConstantRepaint
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc />
		protected override bool HasReferenceIcon
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc />
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 120f;
		}

		/// <inheritdoc />
		protected override Editor HeaderEditor
		{
			get
			{
				// this change is needed to display the preset control
				return Editor;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static new ShaderGraphDrawer Create(Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			ShaderGraphDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ShaderGraphDrawer();
			}
			result.Setup(targets, targets, null, parent, inspector);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		protected override void GetHeaderSubtitle(ref GUIContent subtitle)
		{
			subtitle.text = "Shader Graph Asset";
			subtitle.tooltip = "The Shader Graph Asset is the new Asset type introduced with the shader graph.\n\nYou can open the Shader Graph Window by double clicking a Shader Graph Asset or by clicking the Open Shader Editor button.";
		}
	}
}