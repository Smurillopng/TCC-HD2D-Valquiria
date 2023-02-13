#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	//.fbx, .max, .jas, .c4d = 0.01, .mb, .ma, .lxo, .dxf, .blend, .dae = 1 .3ds = 0.1
	[Serializable, DrawerForAsset(typeof(ModelImporter), false, true),
	DrawerByExtension(".fbx", true), DrawerByExtension(".max", true), DrawerByExtension(".jas", true), DrawerByExtension(".c4d", true),
	DrawerByExtension(".mb", true), DrawerByExtension(".ma", true), DrawerByExtension(".lxo", true), DrawerByExtension(".dxf", true),
	DrawerByExtension(".blend", true), DrawerByExtension(".dae", true), DrawerByExtension(".3ds", true)]
	public class ModelDrawer : CustomEditorAssetDrawer
	{
		/// <inheritdoc />
		protected override bool HasDebugModeIcon
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc />
		protected override bool HasEnabledFlag
		{
			get
			{
				return false;
			}
		}
		
		/// <inheritdoc />
		protected override bool HasExecuteMethodIcon
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc />
		protected override Editor HeaderEditor
		{
			get
			{
				// without this override the header would look like a GameObject header
				return Editor;
			}
		}

		/// <inheritdoc />
		public override float MaxPrefixLabelWidth
		{
			get
			{
				return Mathf.Max(99f, 0.5f * DrawGUI.InspectorWidth - 130f);
			}
		}


		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static new ModelDrawer Create(Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			ModelDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ModelDrawer();
			}
			result.Setup(targets, null, Types.GetInternalEditorType("UnityEditor.ModelImporterEditor"), parent, inspector);
			result.LateSetup();
			return result;
		}
		
		/// <inheritdoc />
		protected override void Setup(Object[] setTargets, Object[] setEditorTargets, Type setEditorType, IParentDrawer setParent, IInspector setInspector)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(AssetDatabase.IsMainAsset(setTargets[0]) || setTargets[0].GetType() == typeof(UnityEngine.AnimationClip), StringUtils.ToColorizedString("ModelGUI.Setup called with setTargets=", setTargets, ", setEditorType=", setEditorType, ", IsMainAsset=", false));
			#endif

			if(setEditorTargets == null)
			{
				AssetImporters.TryGet(setTargets, ref setEditorTargets);
			}
			base.Setup(setTargets, setEditorTargets, setEditorType, setParent, setInspector);
		}

		/// <inheritdoc />
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 170f;
		}
	}
}
#endif