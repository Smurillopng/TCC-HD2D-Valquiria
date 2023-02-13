using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(Renderer), true, true)]
	public class RendererDrawer : CustomEditorComponentDrawer
	{
		/// <inheritdoc/>
		protected override bool Enabled
		{
			get
			{
				return (Target as Renderer).enabled;
			}
		}

		/// <inheritdoc/>
		public override bool DrawBody(Rect position)
		{
			bool guiChangedWas = GUI.changed;
			GUI.changed = false;
			var renderer = Target as Renderer;
			var materials = renderer.sharedMaterials;
			int instanceIdWas = InstanceId;

			bool dirty = base.DrawBody(position);

			if(GUI.changed && !inactive && instanceIdWas == InstanceId && !materials.ContentsMatch(renderer.sharedMaterials)/* && !ObjectPicker.IsOpen*/)
			{
				// If the materials on the renderer changed then rebuild members of parent
				// in order to update the MaterialDrawers embedded on the GameObjectDrawer

				//parent.RebuildMemberBuildListAndMembers();
				var gameObjectDrawer = parent as IGameObjectDrawer;
				if(gameObjectDrawer != null)
				{
					gameObjectDrawer.RebuildMaterialDrawers();
					Inspector.RebuildPreviews();
					ExitGUIUtility.ExitGUI();
				}
			}
			else
			{
				GUI.changed = guiChangedWas;
			}

			return dirty;
		}
	}
}