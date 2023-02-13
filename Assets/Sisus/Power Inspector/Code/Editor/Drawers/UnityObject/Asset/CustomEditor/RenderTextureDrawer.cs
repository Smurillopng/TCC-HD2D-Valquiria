using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable, DrawerByExtension(".renderTexture", true)]
	public class RenderTextureDrawer : CustomEditorAssetDrawer
	{
		/// <inheritdoc />
		protected override bool HasReferenceIcon
		{
			get
			{
				return false;
			}
		}

		#if UNITY_2018_1_OR_NEWER // Presets were added in Unity 2018.1
		/// <inheritdoc />
		protected override bool HasPresetIcon
		{
			get
			{
				return true;
			}
		}
		#endif

		/// <inheritdoc />
		public override bool RequiresConstantRepaint
		{
			get
			{
				return Platform.Time < InspectorUtility.LastInputTime + 1f;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static new RenderTextureDrawer Create(Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			RenderTextureDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new RenderTextureDrawer();
			}
			result.Setup(targets, null, null, parent, inspector);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		protected override void Setup(Object[] setTargets, Object[] setEditorTargets, Type setEditorType, IParentDrawer setParent, IInspector setInspector)
		{
			var selectionWas = setInspector.SelectedObjects;
			base.Setup(setTargets, setEditorTargets, setEditorType, setParent, setInspector);
			
			if(!selectionWas.ContentsMatch(setInspector.SelectedObjects))
			{
				#if DEV_MODE
				UnityEngine.Debug.LogWarning("Fixing issue where RenderTextureImporterInspector.Awake changes selected targets! Restoring selection " + StringUtils.ToString(selectionWas)+" which was changed to "+StringUtils.ToString(setInspector.SelectedObjects));
				#endif
				setInspector.Select(selectionWas);
			}
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 150f;
		}
	}
}