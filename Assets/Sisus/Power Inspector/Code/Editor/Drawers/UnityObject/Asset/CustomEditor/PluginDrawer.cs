#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable, DrawerByExtension(".dll", true)]
	public class PluginDrawer : CustomEditorAssetDrawer
	{
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
		public static new PluginDrawer Create(Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			PluginDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new PluginDrawer();
			}
			result.Setup(targets, targets, Types.GetInternalEditorType("UnityEditor.PluginImporterInspector"), parent, inspector);
			result.LateSetup();
			return result;
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
			subtitle.text = "DLL Asset";
			subtitle.tooltip = "Dynamic-link library containing scripts compiled using an external compiler.";
		}
	}
}
#endif