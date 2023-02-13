using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable, DrawerByExtension(".shader", true)]
	public class ShaderDrawer : CustomEditorAssetDrawer
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
		public static new ShaderDrawer Create(Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			ShaderDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ShaderDrawer();
			}
			result.Setup(targets, targets, Types.GetInternalEditorType("UnityEditor.ShaderImporterInspector"), parent, inspector);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 125f;
		}

		/// <inheritdoc />
		protected override void Setup(Object[] setTargets, Object[] setEditorTargets, Type setEditorType, IParentDrawer setParent, IInspector setInspector)
		{
			if(setEditorTargets == null)
			{
				AssetImporters.TryGet(setTargets, ref setEditorTargets);
			}
			base.Setup(setTargets, setEditorTargets, setEditorType, setParent, setInspector);
		}

		/// <inheritdoc />
		protected override void GetHeaderSubtitle(ref GUIContent subtitle)
		{
			subtitle.text = "Shader Asset";
			subtitle.tooltip = "Shaders are small scripts that contain the mathematical calculations and algorithms for calculating the Color of each pixel rendered, based on the lighting input and the Material configuration.";
		}
	}
}